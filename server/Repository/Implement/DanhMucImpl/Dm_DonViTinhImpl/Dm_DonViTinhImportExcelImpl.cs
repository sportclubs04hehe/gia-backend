using Dapper;
using ExcelDataReader;
using server.Dtos.Common;
using server.Dtos.DanhMuc.Dm_DonViTinh;
using server.Models.DanhMuc;
using server.Repository.IDanhMuc.IDm_DonViTinh;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Text;

namespace server.Repository.Implement.DanhMucImpl.Dm_DonViTinhImpl
{
    public class Dm_DonViTinhImportExcelImpl : IDm_DonViTinhImportExcel
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogger<Dm_DonViTinhImportExcelImpl> _logger;
        private const int BATCH_SIZE = 500;
        private const int MAX_RECORDS = 10000;
        private readonly string[] REQUIRED_HEADERS = { "Mã", "Tên", "Ghi chú" };

        public Dm_DonViTinhImportExcelImpl(IDbConnection dbConnection, ILogger<Dm_DonViTinhImportExcelImpl> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
            // Register encoding provider for ExcelDataReader
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public async Task<ImportResultDto> ImportFromExcelAsync(IFormFile file)
        {
            _logger.LogInformation("Starting Excel import for file {FileName}, size: {FileSize} bytes", file.FileName, file.Length);
            
            var result = new ImportResultDto();
            
            // Validate file
            var fileValidationError = ValidateFile(file);
            if (fileValidationError != null)
            {
                result.Errors.Add(fileValidationError);
                return result;
            }

            try
            {
                using (var stream = file.OpenReadStream())
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration
                        {
                            UseHeaderRow = true
                        }
                    });

                    if (dataSet.Tables.Count == 0)
                    {
                        result.Errors.Add(new ImportErrorDto { Row = 0, Message = "Không tìm thấy dữ liệu trong file Excel." });
                        return result;
                    }

                    var dataTable = dataSet.Tables[0];
                    
                    // Validate headers
                    var headerError = ValidateHeaders(dataTable);
                    if (headerError != null)
                    {
                        result.Errors.Add(headerError);
                        return result;
                    }

                    // Check record count
                    if (dataTable.Rows.Count > MAX_RECORDS)
                    {
                        result.Errors.Add(new ImportErrorDto
                        {
                            Row = 0,
                            Message = $"File vượt quá {MAX_RECORDS:N0} bản ghi, vui lòng chia nhỏ."
                        });
                        return result;
                    }

                    result.TotalRecords = dataTable.Rows.Count;
                    _logger.LogInformation("Found {RecordCount} records in Excel file", result.TotalRecords);
                    
                    // Process in batches
                    for (int i = 0; i < dataTable.Rows.Count; i += BATCH_SIZE)
                    {
                        var batchSize = Math.Min(BATCH_SIZE, dataTable.Rows.Count - i);
                        var importBatch = new List<DmDonViTinhImportDto>(batchSize);
                        
                        // Extract data from Excel to DTOs
                        for (int j = 0; j < batchSize; j++)
                        {
                            var row = dataTable.Rows[i + j];
                            var importDto = new DmDonViTinhImportDto
                            {
                                Ma = row["Mã"]?.ToString()?.Trim() ?? string.Empty,
                                Ten = row["Tên"]?.ToString()?.Trim() ?? string.Empty,
                                GhiChu = row["Ghi chú"]?.ToString()?.Trim()
                            };
                            
                            importBatch.Add(importDto);
                        }
                        
                        _logger.LogInformation("Processing batch {BatchNumber}, size: {BatchSize}", i / BATCH_SIZE + 1, batchSize);

                        // Validate batch
                        var batchErrors = await ValidateBatchAsync(importBatch, i + 2); 
                        result.Errors.AddRange(batchErrors);
                        
                        // Convert to entities and save valid records
                        if (batchErrors.Count < batchSize)
                        {
                            var validEntities = new List<Dm_DonViTinh>();
                            for (int j = 0; j < importBatch.Count; j++)
                            {
                                // Skip records with errors
                                if (batchErrors.Any(e => e.Row == i + j + 2))
                                    continue;
                                
                                validEntities.Add(new Dm_DonViTinh
                                {
                                    Id = Guid.NewGuid(),
                                    Ma = importBatch[j].Ma,
                                    Ten = importBatch[j].Ten,
                                    GhiChu = importBatch[j].GhiChu,
                                    NgayHieuLuc = DateTime.Now.Date,
                                    NgayHetHieuLuc = DateTime.Now.Date.AddYears(10),
                                    IsDelete = false,
                                    CreatedDate = DateTime.Now,
                                    ModifiedDate = DateTime.Now
                                });
                            }
                            
                            if (validEntities.Count > 0)
                            {
                                var savedCount = await SaveBatchAsync(validEntities);
                                result.SuccessCount += savedCount;
                                _logger.LogInformation("Saved {SavedCount} records from batch", savedCount);
                            }
                        }
                    }
                    
                    result.ErrorCount = result.Errors.Count;
                    _logger.LogInformation("Import completed. Success: {SuccessCount}, Errors: {ErrorCount}", 
                        result.SuccessCount, result.ErrorCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Excel import");
                result.Errors.Add(new ImportErrorDto { Row = 0, Message = $"Lỗi xử lý file: {ex.Message}" });
            }
            
            return result;
        }

        public async Task<List<ImportErrorDto>> ValidateBatchAsync(List<DmDonViTinhImportDto> importData, int startRow)
        {
            var errors = new List<ImportErrorDto>();
            var maList = importData.Select(x => x.Ma).Where(x => !string.IsNullOrEmpty(x)).ToList();
            
            // Check for duplicates within the batch
            var duplicatesInBatch = maList
                .GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
                
            // Check for duplicates in database
            var existingRecords = new List<string>();
            if (maList.Any())
            {
                var sql = @"SELECT ""Ma"" FROM ""Dm_DonViTinh"" WHERE ""Ma"" = ANY(@Codes) AND ""IsDelete"" = false";
                existingRecords = (await _dbConnection.QueryAsync<string>(sql, new { Codes = maList.ToArray() })).ToList();
            }
            
            // Validate each record
            for (int i = 0; i < importData.Count; i++)
            {
                var item = importData[i];
                var rowNumber = startRow + i;
                var rowErrors = new Dictionary<string, string>();
                
                // Validate using Data Annotations
                var validationContext = new ValidationContext(item);
                var validationResults = new List<ValidationResult>();
                var isValid = Validator.TryValidateObject(item, validationContext, validationResults, true);
                
                foreach (var validationResult in validationResults)
                {
                    if (validationResult.MemberNames.Any())
                    {
                        foreach (var memberName in validationResult.MemberNames)
                        {
                            rowErrors[memberName] = validationResult.ErrorMessage ?? $"Lỗi trong trường {memberName}";
                        }
                    }
                }
                
                // Check duplicates within batch
                if (!string.IsNullOrEmpty(item.Ma) && duplicatesInBatch.Contains(item.Ma))
                {
                    rowErrors["Ma"] = "Mã bị trùng lặp trong file";
                }
                
                // Check duplicates in database
                if (!string.IsNullOrEmpty(item.Ma) && existingRecords.Contains(item.Ma))
                {
                    rowErrors["Ma"] = "Mã đã tồn tại trong hệ thống";
                }
                
                if (rowErrors.Count > 0)
                {
                    errors.Add(new ImportErrorDto
                    {
                        Row = rowNumber,
                        Message = $"Lỗi tại dòng {rowNumber}",
                        ColumnErrors = rowErrors
                    });
                }
            }
            
            return errors;
        }

        public async Task<int> SaveBatchAsync(List<Dm_DonViTinh> validData)
        {
            if (!validData.Any())
                return 0;
                
            try
            {
                if (_dbConnection.State != ConnectionState.Open)
                    (_dbConnection as DbConnection)?.Open();
                    
                using (var transaction = (_dbConnection as DbConnection)?.BeginTransaction())
                {
                    try
                    {
                        const string sql = @"
                            INSERT INTO ""Dm_DonViTinh"" (""Id"", ""Ma"", ""Ten"", ""GhiChu"", ""NgayHieuLuc"", 
                                ""NgayHetHieuLuc"", ""IsDelete"", ""CreatedDate"", ""ModifiedDate"")
                            VALUES (@Id, @Ma, @Ten, @GhiChu, @NgayHieuLuc, 
                                @NgayHetHieuLuc, @IsDelete, @CreatedDate, @ModifiedDate)";
                        
                        // Log SQL statement
                        _logger.LogInformation("Executing SQL: {Sql}", sql);
                        
                        // Log parameters for first record (to avoid logging too much data)
                        if (validData.Any())
                        {
                            var firstRecord = validData.First();
                            _logger.LogInformation("Sample parameters - Id: {Id}, Ma: {Ma}, Ten: {Ten}, GhiChu: {GhiChu}", 
                                firstRecord.Id, firstRecord.Ma, firstRecord.Ten, firstRecord.GhiChu);
                        }
                        
                        _logger.LogInformation("Executing batch insert for {RecordCount} records", validData.Count);
                        
                        var affectedRows = await _dbConnection.ExecuteAsync(sql, validData, transaction);
                        
                        _logger.LogInformation("Successfully inserted {AffectedRows} records", affectedRows);
                        
                        transaction?.Commit();
                        return affectedRows;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving batch. SQL: {Sql}");
                        transaction?.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error saving batch");
                throw;
            }
        }

        private ImportErrorDto? ValidateFile(IFormFile file)
        {
            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
            {
                _logger.LogWarning("Invalid file extension: {Extension}", extension);
                return new ImportErrorDto
                {
                    Row = 0,
                    Message = "Định dạng file không hợp lệ. Chỉ chấp nhận file Excel (.xlsx, .xls)."
                };
            }
            
            // Check file size (10MB = 10 * 1024 * 1024 bytes)
            if (file.Length > 10 * 1024 * 1024)
            {
                _logger.LogWarning("File size exceeds limit: {Size} bytes", file.Length);
                return new ImportErrorDto
                {
                    Row = 0,
                    Message = "Kích thước file vượt quá 10MB."
                };
            }
            
            return null;
        }

        private ImportErrorDto? ValidateHeaders(DataTable dataTable)
        {
            var missingHeaders = new List<string>();
            
            foreach (var requiredHeader in REQUIRED_HEADERS)
            {
                if (!dataTable.Columns.Contains(requiredHeader))
                {
                    missingHeaders.Add(requiredHeader);
                }
            }
            
            if (missingHeaders.Any())
            {
                var missingHeadersStr = string.Join(", ", missingHeaders);
                _logger.LogWarning("Missing required headers: {Headers}", missingHeadersStr);
                return new ImportErrorDto
                {
                    Row = 1,
                    Message = $"Thiếu các cột bắt buộc: {missingHeadersStr}. Vui lòng kiểm tra mẫu file import."
                };
            }
            
            return null;
        }
    }
}
