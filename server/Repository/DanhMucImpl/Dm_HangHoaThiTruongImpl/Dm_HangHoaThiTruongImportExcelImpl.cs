using Dapper;
using ExcelDataReader;
using server.Dtos.Common;
using server.Dtos.DanhMuc.Dm_HangHoaThiTruong;
using server.Models.DanhMuc;
using server.Repository.IDanhMuc.IDm_HangHoaThiTruong;
using System.Data;
using System.Globalization;
using System.Text;

namespace server.Repository.DanhMucImpl.Dm_HangHoaThiTruongImpl
{
    public class Dm_HangHoaThiTruongImportExcelImpl : IDm_HangHoaThiTruongImportExcel
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogger<Dm_HangHoaThiTruongImportExcelImpl> _logger;

        public Dm_HangHoaThiTruongImportExcelImpl(
            IDbConnection dbConnection,
            ILogger<Dm_HangHoaThiTruongImportExcelImpl> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
            // Register encoding provider for Excel
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public async Task<ImportResultDto> ImportFromExcelAsync(IFormFile file, string createdBy, IDbTransaction? transaction = null)
        {
            var result = new ImportResultDto();

            // Check file size (limit to 10MB)
            const int maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSize)
            {
                result.ErrorCount = 1;
                result.Errors.Add(new ImportErrorDto
                {
                    Row = 0,
                    Message = $"Kích thước file vượt quá giới hạn cho phép (10MB). Kích thước hiện tại: {file.Length / (1024.0 * 1024):F2}MB"
                });
                return result;
            }

            var items = await ReadExcelFileAsync(file, result);

            if (items == null || items.Count == 0 || result.ErrorCount > 0)
                return result;

            var ownTransaction = false;
            if (transaction == null)
            {
                if (_dbConnection.State != ConnectionState.Open)
                    _dbConnection.Open();

                transaction = _dbConnection.BeginTransaction();
                ownTransaction = true;
            }

            try
            {
                // Validate all items before importing
                var validationErrors = await ValidateItemsAsync(items, transaction);
                if (validationErrors.Any())
                {
                    result.ErrorCount = validationErrors.Count;
                    result.Errors.AddRange(validationErrors);

                    if (ownTransaction)
                        transaction.Rollback();

                    return result;
                }

                // Get all existing DonViTinh for mapping
                var donViTinhMap = await GetDonViTinhMapAsync(transaction);

                // Process and import items in batches
                await ProcessImportInBatchesAsync(items, donViTinhMap, createdBy, transaction);

                if (ownTransaction)
                    transaction.Commit();

                result.SuccessCount = items.Count;
                return result;
            }
            catch (Exception ex)
            {
                if (ownTransaction)
                    transaction.Rollback();

                _logger.LogError(ex, "Error importing data from Excel");

                result.ErrorCount++;
                result.Errors.Add(new ImportErrorDto
                {
                    Row = 0,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                });

                return result;
            }
            finally
            {
                if (ownTransaction && transaction != null)
                    transaction.Dispose();
            }
        }

        public async Task<List<ImportErrorDto>> ValidateExcelAsync(IFormFile file)
        {
            var result = new ImportResultDto();

            // Check file size (limit to 10MB)
            const int maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSize)
            {
                result.Errors.Add(new ImportErrorDto
                {
                    Row = 0,
                    Message = $"Kích thước file vượt quá giới hạn cho phép (10MB). Kích thước hiện tại: {file.Length / (1024.0 * 1024):F2}MB"
                });
                return result.Errors;
            }

            var items = await ReadExcelFileAsync(file, result);

            if (items == null || items.Count == 0)
                return result.Errors;

            // Use using statement to ensure connection is properly managed
            if (_dbConnection.State != ConnectionState.Open)
                _dbConnection.Open();

            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                var errors = await ValidateItemsAsync(items, transaction);
                transaction.Rollback(); // Just validation, no changes
                return errors;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error validating Excel data");
                return new List<ImportErrorDto>
        {
            new ImportErrorDto
            {
                Row = 0,
                Message = $"Lỗi kiểm tra dữ liệu: {ex.Message}"
            }
        };
            }
        }

        private async Task ProcessImportInBatchesAsync(
    List<HangHoaThiTruongImportDto> items,
    Dictionary<string, Guid> donViTinhMap,
    string createdBy,
    IDbTransaction transaction)
        {
            // First build the dependency order
            var processOrder = BuildProcessingOrder(items);

            // Track processed items for parent resolution
            var processedIds = new Dictionary<string, Guid>();

            // Get existing parents in one query
            var parentCodes = items.Where(i => !string.IsNullOrEmpty(i.ParentCode))
                                  .Select(i => i.ParentCode)
                                  .Distinct()
                                  .ToList();
            if (parentCodes.Any())
            {
                var existingParentSql = @"SELECT ""Id"", ""Ma"" FROM ""Dm_HangHoaThiTruong"" 
                          WHERE ""Ma"" = ANY(@Codes) AND ""IsDelete"" = false";

                _logger.LogInformation("Executing SQL: {Sql} with params: {@Params}",
            existingParentSql, new { Codes = parentCodes.ToArray() });

                var existingParents = await _dbConnection.QueryAsync<(Guid Id, string Ma)>(
                    existingParentSql,
                    new { Codes = parentCodes.ToArray() }, // Sửa thành ToArray()
                    transaction);

                foreach (var parent in existingParents)
                {
                    processedIds[parent.Ma] = parent.Id;
                }
            }

            // Define batch size
            const int batchSize = 1000;

            // Process in batches following dependency order
            for (int i = 0; i < processOrder.Count; i += batchSize)
            {
                // Get current batch
                var batch = processOrder.Skip(i).Take(batchSize).ToList();
                await ProcessBatchAsync(batch, donViTinhMap, createdBy, processedIds, transaction);
            }
        }

        private async Task<List<HangHoaThiTruongImportDto>> ReadExcelFileAsync(IFormFile file, ImportResultDto result)
        {
            var items = new List<HangHoaThiTruongImportDto>();

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var reader = ExcelReaderFactory.CreateReader(stream);
                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = true
                    }
                });

                if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
                {
                    result.Errors.Add(new ImportErrorDto
                    {
                        Row = 0,
                        Message = "File Excel không có dữ liệu hoặc không đúng định dạng"
                    });
                    result.ErrorCount = 1;
                    return items;
                }

                var dataTable = dataSet.Tables[0];
                result.TotalRecords = dataTable.Rows.Count;

                // Log tất cả các cột được tìm thấy trong Excel để debug
                var columnNames = new List<string>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    columnNames.Add($"'{column.ColumnName}' (Length: {column.ColumnName.Length})");
                }
                _logger.LogInformation("Các cột tìm thấy trong file Excel: {0}", string.Join(", ", columnNames));

                // Tạo dictionary ánh xạ các tên cột với phiên bản normalized 
                var columnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (DataColumn column in dataTable.Columns)
                {
                    // Normalize tên cột - loại bỏ BOM, khoảng trắng và ký tự đặc biệt
                    var normalizedName = NormalizeColumnName(column.ColumnName);
                    columnMap[normalizedName] = column.ColumnName;

                    _logger.LogInformation("Mapped column: '{0}' -> '{1}'", normalizedName, column.ColumnName);
                }

                // Check required columns using the normalized map
                var requiredColumns = new[] { "Mã", "Tên" };
                var missingColumns = new List<string>();

                foreach (var column in requiredColumns)
                {
                    if (!columnMap.ContainsKey(column))
                    {
                        missingColumns.Add(column);
                    }
                }

                if (missingColumns.Any())
                {
                    result.Errors.Add(new ImportErrorDto
                    {
                        Row = 0,
                        Message = $"Thiếu các cột bắt buộc: {string.Join(", ", missingColumns)}. Vui lòng kiểm tra mẫu file import."
                    });
                    result.ErrorCount = 1;
                    return items;
                }

                // Parse each row
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    var row = dataTable.Rows[i];

                    // Skip empty rows
                    var maColumnName = columnMap["Mã"];
                    var tenColumnName = columnMap["Tên"];

                    if (string.IsNullOrWhiteSpace(row[maColumnName]?.ToString()) &&
                        string.IsNullOrWhiteSpace(row[tenColumnName]?.ToString()))
                        continue;

                    var item = new HangHoaThiTruongImportDto
                    {
                        Ma = row[maColumnName]?.ToString()?.Trim() ?? string.Empty,
                        Ten = row[tenColumnName]?.ToString()?.Trim() ?? string.Empty,
                        ParentCode = GetSafeColumnValue(row, columnMap.GetValueOrDefault("Mã cha", "Mã cha")),
                        DonViTinh = GetSafeColumnValue(row, columnMap.GetValueOrDefault("Tên Đơn Vị Tính", "Tên Đơn Vị Tính")),
                        GhiChu = GetSafeColumnValue(row, columnMap.GetValueOrDefault("Ghi Chú", "Ghi Chú")),
                        DacTinh = GetSafeColumnValue(row, columnMap.GetValueOrDefault("Đặc Tính", "Đặc Tính")),
                        NgayHieuLuc = TryParseExcelDate(row, columnMap.GetValueOrDefault("Ngày Hiệu Lực", "Ngày Hiệu Lực")),
                        NgayHetHieuLuc = TryParseExcelDate(row, columnMap.GetValueOrDefault("Ngày Hết Hiệu Lực", "Ngày Hết Hiệu Lực")),
                        RowIndex = i + 2
                    };

                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                result.ErrorCount = 1;
                result.Errors.Add(new ImportErrorDto
                {
                    Row = 0,
                    Message = $"Lỗi đọc file Excel: {ex.Message}"
                });
            }

            return items;
        }

        // Thêm phương thức helper để normalize column name
        private string NormalizeColumnName(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                return string.Empty;

            // Loại bỏ BOM và các ký tự đặc biệt
            var normalized = columnName
                .Replace("\uFEFF", "") // BOM UTF-8
                .Replace("\u200B", "") // Zero-width space
                .Replace("\u00A0", " ") // Non-breaking space
                .Trim();

            return normalized;
        }

        private string? GetSafeColumnValue(DataRow row, string columnName)
        {
            return row.Table.Columns.Contains(columnName) ?
                   row[columnName]?.ToString()?.Trim() : null;
        }

        private DateTime? TryParseExcelDate(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName) ||
                row[columnName] == null ||
                row[columnName] == DBNull.Value)
                return null;

            var value = row[columnName];

            // Case 1: Value is already a DateTime
            if (value is DateTime dateValue)
                return dateValue;

            // Case 2: Value is a number (Excel serial date)
            if (value is double doubleValue)
            {
                try
                {
                    return DateTime.FromOADate(doubleValue);
                }
                catch
                {
                    // Invalid Excel date format
                    return null;
                }
            }

            // Case 3: Value is a string that needs to be parsed
            string dateString = value.ToString().Trim();

            // Try parsing with invariant culture (yyyy-MM-dd)
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture,
                                  DateTimeStyles.None, out DateTime result1))
                return result1;

            // Try with Vietnamese culture if applicable
            if (DateTime.TryParse(dateString, new CultureInfo("vi-VN"),
                                  DateTimeStyles.None, out DateTime result2))
                return result2;

            // Try common formats explicitly
            string[] formats = {
        "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy/MM/dd",
        "yyyy-MM-dd", "MM/dd/yyyy", "dd.MM.yyyy", "yyyy.MM.dd"
    };

            if (DateTime.TryParseExact(dateString, formats,
                                      CultureInfo.InvariantCulture,
                                      DateTimeStyles.None, out DateTime result3))
                return result3;

            // Couldn't parse the date
            return null;
        }

        private async Task ProcessBatchAsync(
            List<HangHoaThiTruongImportDto> batch,
            Dictionary<string, Guid> donViTinhMap,
            string createdBy,
            Dictionary<string, Guid> processedIds,
            IDbTransaction transaction)
        {
            // First, collect any new DonViTinh that need to be created
            var newUnitNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in batch)
            {
                if (!string.IsNullOrEmpty(item.DonViTinh) &&
                    !donViTinhMap.ContainsKey(item.DonViTinh.ToLower()))
                {
                    newUnitNames.Add(item.DonViTinh.Trim());
                }
            }

            // Create and insert new DonViTinh records if needed
            if (newUnitNames.Any())
            {
                var newUnits = new List<Dm_DonViTinh>();
                foreach (var unitName in newUnitNames)
                {
                    var unitId = Guid.NewGuid();
                    newUnits.Add(new Dm_DonViTinh
                    {
                        Id = unitId,
                        Ma = GenerateUnitCode(unitName),  // Implement a method to generate codes
                        Ten = unitName,
                        NgayHieuLuc = DateTime.Now,
                        NgayHetHieuLuc = DateTime.Now.AddYears(10),
                        IsDelete = false,
                        CreatedBy = createdBy,
                        CreatedDate = DateTime.Now,
                        ModifiedBy = createdBy,
                        ModifiedDate = DateTime.Now
                    });

                    // Add to the map for use with this batch
                    donViTinhMap[unitName.ToLower()] = unitId;
                }

                // Insert the new units into the database
                var insertUnitSql = @"
INSERT INTO ""Dm_DonViTinh"" (""Id"", ""Ma"", ""Ten"", ""GhiChu"", 
                           ""NgayHieuLuc"", ""NgayHetHieuLuc"",
                           ""IsDelete"", ""CreatedBy"", ""CreatedDate"", 
                           ""ModifiedBy"", ""ModifiedDate"")
VALUES (@Id, @Ma, @Ten, @GhiChu, @NgayHieuLuc, 
        @NgayHetHieuLuc, @IsDelete,
        @CreatedBy, @CreatedDate, @ModifiedBy, @ModifiedDate)";

                _logger.LogInformation("Creating {Count} new DonViTinh records", newUnits.Count);
                await _dbConnection.ExecuteAsync(insertUnitSql, newUnits, transaction);
            }

            // First, create all entities and insert them
            var entitiesToInsert = new List<(Dm_HangHoaThiTruong Entity, Guid? ParentId)>();
            var idMappings = new Dictionary<string, Guid>();

            foreach (var item in batch)
            {
                var id = Guid.NewGuid();
                Guid? parentId = null;

                // Resolve parent if specified
                if (!string.IsNullOrEmpty(item.ParentCode) &&
                    processedIds.TryGetValue(item.ParentCode, out var resolvedParentId))
                {
                    parentId = resolvedParentId;
                }

                // Create entity
                var entity = new Dm_HangHoaThiTruong
                {
                    Id = id,
                    Ma = item.Ma,
                    Ten = item.Ten,
                    GhiChu = item.GhiChu,
                    DacTinh = item.DacTinh,
                    NgayHieuLuc = item.NgayHieuLuc ?? DateTime.Now,
                    NgayHetHieuLuc = item.NgayHetHieuLuc ?? DateTime.Now.AddYears(10),
                    IsDelete = false,
                    CreatedBy = createdBy,
                    CreatedDate = DateTime.Now,
                    ModifiedBy = createdBy,
                    ModifiedDate = DateTime.Now
                };

                // Set DonViTinhId if specified
                if (!string.IsNullOrEmpty(item.DonViTinh) &&
                    donViTinhMap.TryGetValue(item.DonViTinh.ToLower(), out var donViTinhId))
                {
                    entity.DonViTinhId = donViTinhId;
                }

                entitiesToInsert.Add((entity, parentId));
                idMappings[item.Ma] = id;
                processedIds[item.Ma] = id;
            }

            // Bulk insert entities
            if (entitiesToInsert.Any())
            {
                var insertSql = @"
INSERT INTO ""Dm_HangHoaThiTruong"" (""Id"", ""Ma"", ""Ten"", ""GhiChu"", ""DacTinh"", 
                                 ""DonViTinhId"", ""NgayHieuLuc"", ""NgayHetHieuLuc"",
                                 ""IsDelete"", ""CreatedBy"", ""CreatedDate"", 
                                 ""ModifiedBy"", ""ModifiedDate"")
VALUES (@Id, @Ma, @Ten, @GhiChu, @DacTinh, @DonViTinhId, 
        @NgayHieuLuc, @NgayHetHieuLuc, @IsDelete,
        @CreatedBy, @CreatedDate, @ModifiedBy, @ModifiedDate)";

                _logger.LogInformation("Executing Bulk Insert SQL: {Sql} for {Count} records",
            insertSql, entitiesToInsert.Count);

                await _dbConnection.ExecuteAsync(
                    insertSql,
                    entitiesToInsert.Select(e => e.Entity),
                    transaction);
            }

            // Bulk insert self-references
            if (idMappings.Any())
            {
                var selfRefSql = @"INSERT INTO ""TreeClosure"" (""AncestorId"", ""DescendantId"", ""Depth"") 
                         VALUES (@AncestorId, @DescendantId, 0)";

                _logger.LogInformation("Executing Self-Reference SQL: {Sql} for {Count} records",
           selfRefSql, idMappings.Count);

                await _dbConnection.ExecuteAsync(
                    selfRefSql,
                    idMappings.Select(m => new { AncestorId = m.Value, DescendantId = m.Value }),
                    transaction);
            }

            // Bulk insert direct parent relationships
            var directParentRels = entitiesToInsert
                .Where(e => e.ParentId.HasValue)
                .Select(e => new
                {
                    AncestorId = e.ParentId.Value,
                    DescendantId = e.Entity.Id
                })
                .ToList();

            if (directParentRels.Any())
            {
                var directParentSql = @"INSERT INTO ""TreeClosure"" (""AncestorId"", ""DescendantId"", ""Depth"") 
                              VALUES (@AncestorId, @DescendantId, 1)";

                _logger.LogInformation("Executing Direct Parent SQL: {Sql} for {Count} records",
            directParentSql, directParentRels.Count);

                await _dbConnection.ExecuteAsync(directParentSql, directParentRels, transaction);
            }

            // Bulk insert ancestor relationships
            foreach (var rel in directParentRels)
            {
                // For each entity with a parent, inherit all ancestors
                var inheritAncestorsSql = @"
INSERT INTO ""TreeClosure"" (""AncestorId"", ""DescendantId"", ""Depth"")
SELECT tc.""AncestorId"", @DescendantId, tc.""Depth"" + 1
FROM ""TreeClosure"" tc
WHERE tc.""DescendantId"" = @ParentId AND tc.""AncestorId"" != @ParentId";

                _logger.LogInformation("Executing Ancestor SQL: {Sql} with params: {@Params}",
            inheritAncestorsSql, new { DescendantId = rel.DescendantId, ParentId = rel.AncestorId });

                await _dbConnection.ExecuteAsync(
                    inheritAncestorsSql,
                    new { DescendantId = rel.DescendantId, ParentId = rel.AncestorId },
                    transaction);
            }
        }

        // Tạo đơn vị tính tự động
        // Tạo đơn vị tính tự động
        private string GenerateUnitCode(string unitName)
        {
            if (string.IsNullOrWhiteSpace(unitName))
                return "DVT01";

            // Remove diacritics (Vietnamese accents)
            string normalized = RemoveDiacritics(unitName);

            // Remove special characters and spaces, keep only letters and numbers
            string cleaned = new string(normalized
                .Where(c => char.IsLetterOrDigit(c))
                .ToArray())
                .ToUpper();

            // If no valid characters, use default
            if (string.IsNullOrEmpty(cleaned))
                return "DVT01";

            // Take maximum 5 characters
            string code = cleaned.Length > 5 ? cleaned.Substring(0, 5) : cleaned;

            return code;
        }

        // Helper method to remove Vietnamese diacritics
        private string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        private async Task<List<ImportErrorDto>> ValidateItemsAsync(
    List<HangHoaThiTruongImportDto> items,
    IDbTransaction transaction)
        {
            var errors = new List<ImportErrorDto>();

            // Create a map of parent codes to their IDs for quick lookup
            var parentCodesMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            var uniqueParentCodes = items.Where(i => !string.IsNullOrEmpty(i.ParentCode))
                              .Select(i => i.ParentCode)
                              .Distinct(StringComparer.OrdinalIgnoreCase)
                              .ToList();

            _logger.LogInformation("Found unique parent codes in import: {Codes}", string.Join(", ", uniqueParentCodes));

            if (uniqueParentCodes.Any())
            {
                var existingParentsSql = @"SELECT ""Id"", ""Ma"" FROM ""Dm_HangHoaThiTruong"" 
                        WHERE ""Ma"" = ANY(@Codes) AND ""IsDelete"" = false";

                _logger.LogInformation("Executing SQL: {Sql} with params: {@Params}",
                    existingParentsSql, new { Codes = uniqueParentCodes.ToArray() });

                var existingParents = await _dbConnection.QueryAsync<(Guid Id, string Ma)>(
                    existingParentsSql,
                    new { Codes = uniqueParentCodes.ToArray() },
                    transaction);

                foreach (var parent in existingParents)
                {
                    _logger.LogInformation("Found existing parent: {Code} with ID {Id}", parent.Ma, parent.Id);
                    parentCodesMap[parent.Ma] = parent.Id;
                }
            }

            // Log which parent codes were found in the database
            foreach (var parentCode in uniqueParentCodes)
            {
                if (parentCodesMap.ContainsKey(parentCode))
                    _logger.LogInformation("Parent code {Code} exists in database with ID {Id}", parentCode, parentCodesMap[parentCode]);
                else
                    _logger.LogInformation("Parent code {Code} does not exist in database yet", parentCode);
            }

            // Check for existing codes at same level in database
            var codesWithParentToCheck = items.Select(i => new
            {
                Code = i.Ma,
                ParentCode = i.ParentCode ?? string.Empty,
                RowIndex = i.RowIndex
            }).ToList();

            var existingDuplicates = new Dictionary<string, List<string>>();

            foreach (var groupByParent in codesWithParentToCheck.GroupBy(i => i.ParentCode))
            {
                var parentCode = groupByParent.Key;
                var codes = groupByParent.Select(i => i.Code).Distinct().ToList();

                if (codes.Count == 0) continue;

                // Only check for duplicates in the database if the parent exists
                if (!string.IsNullOrEmpty(parentCode) && !parentCodesMap.ContainsKey(parentCode))
                {
                    _logger.LogInformation("Skipping database duplicate check for parent {ParentCode} as it doesn't exist yet", parentCode);
                    continue; // Skip this check if parent doesn't exist
                }

                // Get parent ID if exists
                Guid? parentId = null;
                if (!string.IsNullOrEmpty(parentCode) && parentCodesMap.TryGetValue(parentCode, out var id))
                {
                    parentId = id;
                }

                // SQL to check duplicates at same level
                var sql = @"
SELECT h.""Ma""
FROM ""Dm_HangHoaThiTruong"" h
WHERE h.""IsDelete"" = false
AND h.""Ma"" = ANY(@Codes)
AND (
    -- If parentId is provided, check items with same parent
    (@ParentId IS NOT NULL AND EXISTS (
        SELECT 1 FROM ""TreeClosure"" tc_child 
        WHERE tc_child.""DescendantId"" = h.""Id"" AND tc_child.""Depth"" = 1
        AND EXISTS (
            SELECT 1 FROM ""TreeClosure"" tc_parent 
            WHERE tc_parent.""DescendantId"" = @ParentId 
            AND tc_parent.""AncestorId"" = tc_child.""AncestorId""
        )
    ))
    OR
    -- If no parentId, check root level items
    (@ParentId IS NULL AND NOT EXISTS (
        SELECT 1 FROM ""TreeClosure"" tc 
        WHERE tc.""DescendantId"" = h.""Id"" AND tc.""Depth"" > 0
    ))
)";

                _logger.LogInformation("Checking duplicates SQL: {Sql} with params: {@Params}",
                    sql, new { Codes = codes.ToArray(), ParentId = parentId });

                var duplicateCodes = await _dbConnection.QueryAsync<string>(
                    sql,
                    new { Codes = codes.ToArray(), ParentId = parentId },
                    transaction);

                if (duplicateCodes.Any())
                {
                    _logger.LogInformation("Found duplicate codes: {Codes} for parent: {Parent}",
                        string.Join(", ", duplicateCodes), parentCode);
                    string parentKey = string.IsNullOrEmpty(parentCode) ? "(root)" : parentCode;
                    existingDuplicates[parentKey] = duplicateCodes.ToList();
                }
            }

            // Check for duplicate codes at same level within the import
            var codesByParent = items.GroupBy(i => i.ParentCode ?? string.Empty)
                              .ToDictionary(g => g.Key, g => g.ToList());

            // Validation logic
            foreach (var item in items)
            {
                var error = new ImportErrorDto { Row = item.RowIndex };
                var hasError = false;

                // Required fields validation
                if (string.IsNullOrWhiteSpace(item.Ma))
                {
                    hasError = true;
                    error.ColumnErrors["Mã"] = "Mã không được để trống";
                }

                if (string.IsNullOrWhiteSpace(item.Ten))
                {
                    hasError = true;
                    error.ColumnErrors["Tên"] = "Tên không được để trống";
                }

                // Date validation (keep existing code)
                if (item.NgayHieuLuc.HasValue && item.NgayHetHieuLuc.HasValue)
                {
                    if (item.NgayHieuLuc > item.NgayHetHieuLuc)
                    {
                        hasError = true;
                        error.ColumnErrors["Ngày Hiệu Lực"] =
                            $"Ngày hiệu lực ({item.NgayHieuLuc:dd/MM/yyyy}) không thể sau ngày hết hiệu lực ({item.NgayHetHieuLuc:dd/MM/yyyy})";
                    }
                }
                else if (!item.NgayHieuLuc.HasValue && !string.IsNullOrEmpty(GetColumnNameForRow(item.RowIndex, "Ngày Hiệu Lực")))
                {
                    hasError = true;
                    error.ColumnErrors["Ngày Hiệu Lực"] = "Không thể đọc định dạng ngày tháng, vui lòng sử dụng định dạng dd/MM/yyyy";
                }
                else if (!item.NgayHetHieuLuc.HasValue && !string.IsNullOrEmpty(GetColumnNameForRow(item.RowIndex, "Ngày Hết Hiệu Lực")))
                {
                    hasError = true;
                    error.ColumnErrors["Ngày Hết Hiệu Lực"] = "Không thể đọc định dạng ngày tháng, vui lòng sử dụng định dạng dd/MM/yyyy";
                }

                // Check for duplicate code within existing data AT SAME LEVEL
                if (!string.IsNullOrEmpty(item.Ma))
                {
                    string parentKey = string.IsNullOrEmpty(item.ParentCode) ? "(root)" : item.ParentCode;

                    if (existingDuplicates.TryGetValue(parentKey, out var duplicates) &&
                        duplicates.Contains(item.Ma, StringComparer.OrdinalIgnoreCase))
                    {
                        hasError = true;
                        error.ColumnErrors["Mã"] = $"Mã '{item.Ma}' đã tồn tại trong hệ thống ở cùng cấp với mã cha '{(string.IsNullOrEmpty(item.ParentCode) ? "không có" : item.ParentCode)}'";
                    }
                }

                // Check for duplicate code at same level within import data (keep existing code)
                var parentCode = item.ParentCode ?? string.Empty;
                if (!string.IsNullOrEmpty(item.Ma) &&
                    codesByParent.TryGetValue(parentCode, out var siblings) &&
                    siblings.Count(s => s.Ma.Equals(item.Ma, StringComparison.OrdinalIgnoreCase)) > 1)
                {
                    hasError = true;
                    var duplicates = siblings.Where(s => s.Ma.Equals(item.Ma, StringComparison.OrdinalIgnoreCase) && s.RowIndex != item.RowIndex)
                                       .Select(s => s.RowIndex);
                    error.ColumnErrors["Mã"] = $"Mã '{item.Ma}' bị trùng lặp ở cùng cấp (dòng {string.Join(", ", duplicates)})";
                }

                // Add error if any found
                if (hasError)
                {
                    error.Message = $"Lỗi dữ liệu ở dòng {item.RowIndex}";
                    errors.Add(error);
                }
            }

            return errors;
        }
        private string GetColumnNameForRow(int rowIndex, string columnName)
        {
            if (columnName == "ParentCode")
                return "Mã cha";

            // Điều này sẽ được triển khai để lấy tham chiếu ô thực tế (ví dụ: A5, B12)
            // để có thông báo lỗi tốt hơn. Hiện tại sẽ chỉ trả về tên cột.
            return columnName;
        }

        // Improved circular reference detection
        private bool HasCircularReference(
            string code,
            string parentCode,
            Dictionary<string, HangHoaThiTruongImportDto> importItems,
            HashSet<string>? visited = null)
        {
            // Initialize the visited set if null
            visited ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Direct self-reference check
            if (code.Equals(parentCode, StringComparison.OrdinalIgnoreCase))
                return true;

            // If we've already visited this parent in this chain, it's a circular reference
            if (visited.Contains(parentCode))
                return true;

            // Add the current parent to the visited set for this chain
            visited.Add(parentCode);

            // Check if the parent's parent creates a circular reference
            if (importItems.TryGetValue(parentCode, out var parentItem) &&
                !string.IsNullOrEmpty(parentItem.ParentCode))
            {
                return HasCircularReference(code, parentItem.ParentCode, importItems, visited);
            }

            // No circular reference found
            return false;
        }

        private bool HasCircularReference(string code, Dictionary<string, string?> dependencyGraph, HashSet<string>? visited = null)
        {
            visited ??= new HashSet<string>();

            if (!dependencyGraph.TryGetValue(code, out var parentCode) ||
                string.IsNullOrEmpty(parentCode))
                return false;

            if (visited.Contains(parentCode))
                return true;

            visited.Add(code);
            return HasCircularReference(parentCode, dependencyGraph, visited);
        }

        private async Task<Dictionary<string, Guid>> GetDonViTinhMapAsync(IDbTransaction transaction)
        {
            var sql = @"SELECT ""Id"", ""Ten"" FROM ""Dm_DonViTinh"" WHERE ""IsDelete"" = false";
            _logger.LogInformation("Executing SQL: {Sql}", sql);
            var units = await _dbConnection.QueryAsync<(Guid Id, string Ten)>(sql, transaction: transaction);
            return units.ToDictionary(u => u.Ten.ToLower(), u => u.Id, StringComparer.OrdinalIgnoreCase);
        }

        private List<HangHoaThiTruongImportDto> BuildProcessingOrder(List<HangHoaThiTruongImportDto> items)
        {
            // Build dependency graph
            var dependencyGraph = new Dictionary<string, List<string>>();

            // Sử dụng GroupBy để xử lý các mã trùng lặp, chỉ lấy item đầu tiên của mỗi mã
            var itemsMap = items
                .GroupBy(i => i.Ma, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation("Building processing order for {TotalItems} items, {UniqueItems} unique codes",
                items.Count, itemsMap.Count);

            foreach (var item in itemsMap.Values)
            {
                if (!dependencyGraph.ContainsKey(item.Ma))
                {
                    dependencyGraph[item.Ma] = new List<string>();
                }

                if (!string.IsNullOrEmpty(item.ParentCode))
                {
                    if (!dependencyGraph.ContainsKey(item.ParentCode))
                    {
                        dependencyGraph[item.ParentCode] = new List<string>();
                    }

                    dependencyGraph[item.ParentCode].Add(item.Ma);
                }
            }

            // Topological sort (process parents before children)
            var result = new List<HangHoaThiTruongImportDto>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var temp = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in itemsMap.Values)
            {
                if (!visited.Contains(item.Ma))
                {
                    TopologicalSort(item.Ma, dependencyGraph, visited, temp, result, itemsMap);
                }
            }

            // Trả về tất cả items gốc nhưng theo thứ tự đã được sắp xếp
            // Đối với các mã trùng lặp, chúng sẽ được xử lý theo thứ tự xuất hiện
            var orderedItems = new List<HangHoaThiTruongImportDto>();
            var processedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var orderedItem in result)
            {
                var sameCodeItems = items.Where(i => i.Ma.Equals(orderedItem.Ma, StringComparison.OrdinalIgnoreCase));
                foreach (var item in sameCodeItems)
                {
                    orderedItems.Add(item);
                }
                processedCodes.Add(orderedItem.Ma);
            }

            // Thêm bất kỳ items nào không được xử lý (nếu có)
            foreach (var item in items)
            {
                if (!processedCodes.Contains(item.Ma))
                {
                    orderedItems.Add(item);
                }
            }

            return orderedItems;
        }
        private void TopologicalSort(
            string code,
            Dictionary<string, List<string>> graph,
            HashSet<string> visited,
            HashSet<string> temp,
            List<HangHoaThiTruongImportDto> result,
            Dictionary<string, HangHoaThiTruongImportDto> itemsMap)
        {
            if (!graph.ContainsKey(code) || !itemsMap.ContainsKey(code))
                return;

            if (temp.Contains(code))
                return; // Circular reference already handled by validation

            if (visited.Contains(code))
                return;

            temp.Add(code);

            // Visit all parents first (reverse topological order)
            var item = itemsMap[code];
            if (!string.IsNullOrEmpty(item.ParentCode) && itemsMap.ContainsKey(item.ParentCode))
            {
                TopologicalSort(item.ParentCode, graph, visited, temp, result, itemsMap);
            }

            temp.Remove(code);
            visited.Add(code);
            result.Add(item);
        }
    }
}