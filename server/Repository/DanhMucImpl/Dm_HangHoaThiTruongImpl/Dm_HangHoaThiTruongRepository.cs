using Dapper;
using server.Dtos.Common;
using server.Dtos.DanhMuc.Dm_HangHoaThiTruongDto;
using server.Models.DanhMuc;
using server.Models.Extends;
using server.Repository.IDanhMuc.IDm_HangHoaThiTruong;
using System.Data;

namespace server.Repository.DanhMucImpl.Dm_HangHoaThiTruongImpl
{
    public class Dm_HangHoaThiTruongRepository : IDm_HangHoaThiTruongRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogger<Dm_HangHoaThiTruongRepository> _logger;
        private readonly IDm_HangHoaThiTruongHierarchyRepository _hierarchyRepository;

        public Dm_HangHoaThiTruongRepository(IDbConnection dbConnection,
            ILogger<Dm_HangHoaThiTruongRepository> logger,
            IDm_HangHoaThiTruongHierarchyRepository hierarchyRepository)
        {
            _dbConnection = dbConnection;
            _logger = logger;
            _hierarchyRepository = hierarchyRepository;
        }

        public async Task<IEnumerable<Dm_HangHoaThiTruongJoined>> GetTopLevelItemsAsync()
        {
            var sql = @"
    SELECT h.""Id"", h.""Ma"", h.""Ten"", h.""GhiChu"", h.""DacTinh"", 
           h.""NgayHieuLuc"", h.""NgayHetHieuLuc"",
           h.""IsDelete"", h.""CreatedDate"", h.""ModifiedDate"",
           h.""CreatedBy"", h.""ModifiedBy"",
           NULL AS ""DonViTinhId"", 
           NULL AS ""DonViTinhTen""
    FROM ""Dm_HangHoaThiTruong"" h
    WHERE h.""IsDelete"" = false
    AND NOT EXISTS (
        SELECT 1 FROM ""TreeClosure"" tc 
        WHERE tc.""DescendantId"" = h.""Id"" 
        AND tc.""AncestorId"" != h.""Id""
        AND tc.""Depth"" > 0
    )
    ORDER BY 
    CASE 
        WHEN h.""Ma"" ~ '^[0-9]+$' THEN 0
        WHEN h.""Ma"" ~ '^[0-9]+\.[0-9.]+$' THEN 1
        ELSE 2
    END,
    CASE 
        WHEN h.""Ma"" ~ '^[0-9]+$' THEN (h.""Ma"")::numeric
        ELSE 0
    END,
    h.""Ma""";

            _logger.LogInformation($"Execute SQL: {sql}");

            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            var result = await _dbConnection.QueryAsync<Dm_HangHoaThiTruongJoined>(sql);
            return result;
        }

        public async Task<PagedResult<Dm_HangHoaThiTruongJoined>> GetChildrenAsync(
            Guid parentId,
            PagedRequest request,
            string searchTerm = null)
        {
            // Input validation
            if (request.PageNumber < 1)
                request.PageNumber = 1;

            if (request.PageSize <= 0)
                request.PageSize = 10;

            // Create parameters
            var parameters = new DynamicParameters();
            parameters.Add("ParentId", parentId);
            parameters.Add("Offset", (request.PageNumber - 1) * request.PageSize);
            parameters.Add("PageSize", request.PageSize);

            // Build search condition
            var searchCondition = string.Empty;
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // Use unaccent to improve search accuracy for accented characters
                searchCondition = @" AND (
            unaccent(h.""Ma"") ILIKE unaccent(@SearchTerm) OR 
            unaccent(h.""Ten"") ILIKE unaccent(@SearchTerm) OR 
            unaccent(COALESCE(h.""GhiChu"", '')) ILIKE unaccent(@SearchTerm) OR 
            unaccent(COALESCE(h.""DacTinh"", '')) ILIKE unaccent(@SearchTerm)
        )";
                parameters.Add("SearchTerm", $"%{searchTerm}%");
            }

            // Determine sorting
            var sortColumn = !string.IsNullOrWhiteSpace(request.SortBy) ? request.SortBy : "CreatedDate";
            var sortDirection = request.SortDescending ? "DESC" : "ASC";

            // Build ORDER BY clause with proper column handling
            string orderByClause;

            if (sortColumn.ToLower() == "ma")
            {
                // Natural sorting for "Ma" field
                orderByClause = $@"
        CASE 
            WHEN h.""Ma"" ~ '^[0-9]+$' THEN 0
            WHEN h.""Ma"" ~ '^[0-9]+\.[0-9.]+$' THEN 1
            ELSE 2
        END {sortDirection},
        CASE 
            WHEN h.""Ma"" ~ '^[0-9]+$' THEN (h.""Ma"")::numeric
            ELSE 0
        END {sortDirection},
        CASE 
            WHEN h.""Ma"" ~ '^[0-9]+\.[0-9.]+$' THEN 
                array_to_string(array(
                    SELECT lpad(split_part(h.""Ma"", '.', generate_series(1, regexp_count(h.""Ma"", '\\.')+1))::text, 10, '0')
                    FROM generate_series(1, regexp_count(h.""Ma"", '\\.')+1)
                ), '.')
            ELSE h.""Ma""
        END {sortDirection},
        h.""Ma"" {sortDirection}";
            }
            else if (sortColumn.ToLower() == "ten")
            {
                // Sort "Ten" column without diacritics
                orderByClause = $@"unaccent(LOWER(h.""Ten"")) {sortDirection}";
            }
            else
            {
                // Default column sorting with proper case
                orderByClause = $"h.\"{sortColumn}\" {sortDirection}";
            }

            // SQL to get direct children (Depth = 1) with count and pagination
            var sql = $@"
    -- Ensure unaccent extension is available
    CREATE EXTENSION IF NOT EXISTS unaccent;

    -- Count total records
    SELECT COUNT(*) 
    FROM ""Dm_HangHoaThiTruong"" h
    JOIN ""TreeClosure"" tc ON tc.""DescendantId"" = h.""Id""
    WHERE tc.""AncestorId"" = @ParentId 
    AND tc.""Depth"" = 1
    AND h.""IsDelete"" = false
    {searchCondition};

    -- Get paged data with unit name
    SELECT h.""Id"", h.""Ma"", h.""Ten"", h.""GhiChu"", h.""DacTinh"", 
           h.""DonViTinhId"", h.""NgayHieuLuc"", h.""NgayHetHieuLuc"",
           h.""IsDelete"", h.""CreatedDate"", h.""ModifiedDate"",
           h.""CreatedBy"", h.""ModifiedBy"",
           dvt.""Ten"" AS ""DonViTinhTen""
    FROM ""Dm_HangHoaThiTruong"" h
    JOIN ""TreeClosure"" tc ON tc.""DescendantId"" = h.""Id""
    LEFT JOIN ""Dm_DonViTinh"" dvt ON dvt.""Id"" = h.""DonViTinhId"" AND dvt.""IsDelete"" = false
    WHERE tc.""AncestorId"" = @ParentId 
    AND tc.""Depth"" = 1
    AND h.""IsDelete"" = false
    {searchCondition}
    ORDER BY {orderByClause}
    LIMIT @PageSize OFFSET @Offset";

            _logger.LogInformation("sql= {sql}", sql);
            _logger.LogInformation("Executing GetChildrenAsync query with parentId: {ParentId}", parentId);

            // Execute the query
            using var multi = await _dbConnection.QueryMultipleAsync(sql, parameters);

            var totalCount = await multi.ReadSingleAsync<int>();
            var items = await multi.ReadAsync<Dm_HangHoaThiTruongJoined>();

            return new PagedResult<Dm_HangHoaThiTruongJoined>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<Dm_HangHoaThiTruong> AddAsync(Dm_HangHoaThiTruong entity, Guid? parentId = null, IDbTransaction transaction = null)
        {
            // Đảm bảo entity có Id nếu chưa được set
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();

            // SQL để thêm mới hàng hóa - Sử dụng dấu ngoặc kép cho tên bảng và cột
            var insertSql = @"
                INSERT INTO ""Dm_HangHoaThiTruong"" (""Id"", ""Ma"", ""Ten"", ""GhiChu"", ""DacTinh"", ""DonViTinhId"", 
                                                   ""NgayHieuLuc"", ""NgayHetHieuLuc"", ""CreatedDate"", ""ModifiedDate"", ""IsDelete"")
                VALUES (@Id, @Ma, @Ten, @GhiChu, @DacTinh, @DonViTinhId, @NgayHieuLuc, @NgayHetHieuLuc, 
                        NOW(), NOW(), @IsDelete);
                SELECT * FROM ""Dm_HangHoaThiTruong"" WHERE ""Id"" = @Id;";

            _logger.LogInformation($"Execute SQL: {insertSql} with parameters: {System.Text.Json.JsonSerializer.Serialize(entity)}");

            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            // Sử dụng transaction được truyền vào thay vì tạo mới
            var result = await _dbConnection.QueryFirstOrDefaultAsync<Dm_HangHoaThiTruong>(
                insertSql, entity, transaction);

            // Truyền cùng transaction vào hàm UpdateTreeClosureAsync
            await _hierarchyRepository.UpdateTreeClosureAsync(entity.Id, parentId, transaction);

            return result;
        }

        public async Task<Dm_HangHoaThiTruong?> GetByIdAsync(Guid id)
        {
            var sql = @"
                SELECT * FROM ""Dm_HangHoaThiTruong"" 
                WHERE ""Id"" = @Id AND ""IsDelete"" = false";

            _logger.LogInformation($"Execute SQL: {sql} with Id: {id}");

            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            var result = await _dbConnection.QueryFirstOrDefaultAsync<Dm_HangHoaThiTruong>(sql, new { Id = id });

            return result;
        }

    }
}
