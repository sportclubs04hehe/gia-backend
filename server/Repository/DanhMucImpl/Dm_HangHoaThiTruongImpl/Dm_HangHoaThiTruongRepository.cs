using Dapper;
using server.Dtos.Common;
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

        // Lấy danh sách các hàng hóa cấp cao nhất (không có cha)
        public async Task<IEnumerable<Dm_HangHoaThiTruongJoined>> GetTopLevelItemsAsync()
        {
            var sql = @"
SELECT h.""Id"", h.""Ma"", h.""Ten"", h.""GhiChu"", h.""DacTinh"", 
       h.""NgayHieuLuc"", h.""NgayHetHieuLuc"", h.""IsParent"",
       h.""IsDelete"", h.""CreatedDate"", h.""ModifiedDate"",
       h.""CreatedBy"", h.""ModifiedBy"",
       h.""DonViTinhId"",
       CASE WHEN EXISTS (
           SELECT 1 
           FROM ""TreeClosure"" tc 
           WHERE tc.""AncestorId"" = h.""Id"" 
             AND tc.""Depth"" = 1
             AND EXISTS (
                 SELECT 1 
                 FROM ""Dm_HangHoaThiTruong"" child
                 WHERE child.""Id"" = tc.""DescendantId""
                   AND child.""IsDelete"" = false
             )
       ) THEN true ELSE false END AS ""HasChildren""
FROM ""Dm_HangHoaThiTruong"" h
WHERE h.""IsDelete"" = false
  AND NOT EXISTS (
      SELECT 1 
      FROM ""TreeClosure"" tc 
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
    h.""Ma"";
";

            _logger.LogInformation($"Execute SQL: {sql}");

            if (_dbConnection.State != ConnectionState.Open)
                _dbConnection.Open();

            return await _dbConnection.QueryAsync<Dm_HangHoaThiTruongJoined>(sql);
        }


        public async Task<IEnumerable<Dm_HangHoaThiTruongJoined>> GetAllParentItemsWithChildrenAsync()
        {
            var sql = @"
WITH ParentNodes AS (
    SELECT DISTINCT tc.""AncestorId""
    FROM ""TreeClosure"" tc
    JOIN ""Dm_HangHoaThiTruong"" child 
        ON child.""Id"" = tc.""DescendantId"" 
       AND child.""IsDelete"" = false
    WHERE tc.""Depth"" = 1
)
SELECT h.""Id"", h.""Ma"", h.""Ten"", h.""GhiChu"", h.""DacTinh"",
       h.""NgayHieuLuc"", h.""NgayHetHieuLuc"", h.""IsParent"",
       h.""IsDelete"", h.""CreatedDate"", h.""ModifiedDate"",
       h.""CreatedBy"", h.""ModifiedBy"",
       CASE WHEN p.""AncestorId"" IS NOT NULL THEN true ELSE false END AS ""HasChildren"",
       tc_parent.""AncestorId"" AS ""ParentId"",
       tc_parent.""Depth""
FROM ""Dm_HangHoaThiTruong"" h
LEFT JOIN ""TreeClosure"" tc_parent 
       ON tc_parent.""DescendantId"" = h.""Id"" AND tc_parent.""Depth"" = 1
LEFT JOIN ParentNodes p ON p.""AncestorId"" = h.""Id""
WHERE h.""IsDelete"" = false
  AND p.""AncestorId"" IS NOT NULL
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
    h.""Ma"";
";

            _logger.LogInformation($"Execute SQL: {sql}");

            if (_dbConnection.State != ConnectionState.Open)
                _dbConnection.Open();

            var result = await _dbConnection.QueryAsync<Dm_HangHoaThiTruongJoinedWithParent>(sql);
            return result.Cast<Dm_HangHoaThiTruongJoined>();
        }

        // Lấy danh sách các hàng hóa con trực tiếp của một hàng hóa cha
        public async Task<PagedResult<Dm_HangHoaThiTruongJoined>> GetChildrenAsync(
            Guid parentId,
            PagedRequest request,
            string? searchTerm = null)
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

    -- Get paged data with unit name and hasChildren info
    SELECT h.""Id"", h.""Ma"", h.""Ten"", h.""GhiChu"", h.""DacTinh"", 
           h.""DonViTinhId"", h.""NgayHieuLuc"", h.""NgayHetHieuLuc"", h.""IsParent"",
           h.""IsDelete"", h.""CreatedDate"", h.""ModifiedDate"",
           h.""CreatedBy"", h.""ModifiedBy"",
           dvt.""Ten"" AS ""DonViTinhTen"",
           CASE WHEN EXISTS (
               SELECT 1 FROM ""TreeClosure"" tc_child 
               WHERE tc_child.""AncestorId"" = h.""Id"" 
               AND tc_child.""Depth"" = 1
               AND EXISTS (
                   SELECT 1 FROM ""Dm_HangHoaThiTruong"" child
                   WHERE child.""Id"" = tc_child.""DescendantId""
                   AND child.""IsDelete"" = false
               )
           ) THEN true ELSE false END AS ""HasChildren""
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

        // Thêm mới hàng hóa với khả năng thiết lập cha mới
        public async Task<Dm_HangHoaThiTruong> AddAsync(Dm_HangHoaThiTruong entity, Guid? parentId = null, IDbTransaction? transaction = null)
        {
            // Đảm bảo entity có Id nếu chưa được set
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();

            // SQL để thêm mới hàng hóa - Sử dụng dấu ngoặc kép cho tên bảng và cột
            var insertSql = @"
                INSERT INTO ""Dm_HangHoaThiTruong"" (""Id"", ""Ma"", ""Ten"", ""GhiChu"", ""DacTinh"", ""DonViTinhId"", 
                                                   ""NgayHieuLuc"", ""NgayHetHieuLuc"", ""IsParent"", ""CreatedDate"", ""ModifiedDate"", ""IsDelete"")
                VALUES (@Id, @Ma, @Ten, @GhiChu, @DacTinh, @DonViTinhId, @NgayHieuLuc, @NgayHetHieuLuc, 
                        @IsParent, NOW(), NOW(), @IsDelete);
                SELECT * FROM ""Dm_HangHoaThiTruong"" WHERE ""Id"" = @Id;";

            _logger.LogInformation($"Execute SQL: {insertSql} with parameters: {System.Text.Json.JsonSerializer.Serialize(entity)}");

            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            // Sử dụng transaction được truyền vào thay vì tạo mới
            var result = await _dbConnection.QueryFirstOrDefaultAsync<Dm_HangHoaThiTruong>(
                insertSql, entity, transaction);
            
            if (result == null)
                throw new InvalidOperationException($"Không thể thêm mới hàng hóa thị trường với ID: {entity.Id}");

            // Truyền cùng transaction vào hàm UpdateTreeClosureAsync
            await _hierarchyRepository.UpdateTreeClosureAsync(entity.Id, parentId, transaction!);

            // Nếu có parentId, cập nhật IsParent của parent thành true
            if (parentId.HasValue)
            {
                await UpdateParentIsParentStatus(parentId.Value, transaction);
            }

            return result;
        }

        // Cập nhật hàng hóa với khả năng thay đổi cha mới
        public async Task<Dm_HangHoaThiTruong> UpdateAsync(Dm_HangHoaThiTruong entity, Guid? newParentId = null, IDbTransaction? transaction = null)
        {
            // SQL to update the entity with proper double quotes for PostgreSQL
            var updateSql = @"
        UPDATE ""Dm_HangHoaThiTruong""
        SET ""Ma"" = @Ma, 
            ""Ten"" = @Ten, 
            ""GhiChu"" = @GhiChu, 
            ""DacTinh"" = @DacTinh,
            ""DonViTinhId"" = @DonViTinhId,
            ""NgayHieuLuc"" = @NgayHieuLuc, 
            ""NgayHetHieuLuc"" = @NgayHetHieuLuc,
            ""IsParent"" = @IsParent,
            ""ModifiedDate"" = NOW(),
            ""ModifiedBy"" = @ModifiedBy
        WHERE ""Id"" = @Id AND ""IsDelete"" = false
        RETURNING *;";

            _logger.LogInformation($"Execute SQL: {updateSql} with parameters: {System.Text.Json.JsonSerializer.Serialize(entity)}");

            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            var result = await _dbConnection.QueryFirstOrDefaultAsync<Dm_HangHoaThiTruong>(
                updateSql, entity, transaction);

            if (result == null)
                throw new InvalidOperationException($"Không thể cập nhật hàng hóa thị trường với ID: {entity.Id}");

            if (newParentId.HasValue)
            {
                // Lấy parent cũ trước khi thay đổi
                var oldParentId = await GetCurrentParentId(entity.Id);
                
                // Update the hierarchical structure if parent has changed
                await _hierarchyRepository.UpdateParentAsync(entity.Id, newParentId.Value, transaction!);
                
                // Cập nhật IsParent cho parent mới
                await UpdateParentIsParentStatus(newParentId.Value, transaction);
                
                // Cập nhật IsParent cho parent cũ (nếu có)
                if (oldParentId.HasValue)
                {
                    await UpdateParentIsParentStatus(oldParentId.Value, transaction);
                }
            }

            return result;
        }

        // Lấy hàng hóa theo Id
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

        // Xóa mềm một hàng hóa và tất cả các hàng hóa con của nó
        public async Task<int> DeleteAsync(Guid id, IDbTransaction? transaction = null)
        {
            // Lấy parent của node trước khi xóa
            var parentId = await GetCurrentParentId(id);
            
            var sql = @"
        WITH RECURSIVE TreeItems AS (
            -- Lấy tất cả các node con (bao gồm cả node hiện tại)
            SELECT tc.""DescendantId"" 
            FROM ""TreeClosure"" tc
            WHERE tc.""AncestorId"" = @Id
        )
        UPDATE ""Dm_HangHoaThiTruong""
        SET ""IsDelete"" = true, 
            ""ModifiedDate"" = NOW()
        WHERE ""Id"" IN (SELECT ""DescendantId"" FROM TreeItems)
        AND ""IsDelete"" = false;";

            _logger.LogInformation($"Execute SQL: {sql} with Id: {id}");

            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            var affectedRows = await _dbConnection.ExecuteAsync(
                sql,
                new { Id = id },
                transaction);

            // Cập nhật IsParent của parent nếu có
            if (parentId.HasValue)
            {
                await UpdateParentIsParentStatus(parentId.Value, transaction);
            }

            return affectedRows;
        }

        // Xóa mềm nhiều hàng hóa và tất cả các hàng hóa con của chúng
        public async Task<int> DeleteManyAsync(IEnumerable<Guid> ids, IDbTransaction? transaction = null)
        {
            if (ids == null || !ids.Any())
                return 0;

            // Lấy danh sách parents của các nodes sẽ bị xóa
            var parentIds = new List<Guid>();
            foreach (var id in ids)
            {
                var parentId = await GetCurrentParentId(id);
                if (parentId.HasValue && !parentIds.Contains(parentId.Value))
                {
                    parentIds.Add(parentId.Value);
                }
            }

            var sql = @"
        WITH RECURSIVE AllItems AS (
            -- Lấy tất cả các node con của các node được chọn
            SELECT DISTINCT tc.""DescendantId"" 
            FROM ""TreeClosure"" tc
            WHERE tc.""AncestorId"" IN @Ids
        )
        UPDATE ""Dm_HangHoaThiTruong""
        SET ""IsDelete"" = true, 
            ""ModifiedDate"" = NOW()
        WHERE ""Id"" IN (SELECT ""DescendantId"" FROM AllItems)
        AND ""IsDelete"" = false;";

            _logger.LogInformation($"Execute SQL: {sql} with Ids: {string.Join(", ", ids)}");

            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            var affectedRows = await _dbConnection.ExecuteAsync(
                sql,
                new { Ids = ids },
                transaction);

            // Cập nhật IsParent cho tất cả các parents
            foreach (var parentId in parentIds)
            {
                await UpdateParentIsParentStatus(parentId, transaction);
            }

            return affectedRows;
        }

        // Kiểm tra xem một hàng hóa có đang được sử dụng bởi các bảng khác không
        public async Task<bool> IsReferencedAsync(Guid id)
        {
            // Kiểm tra tham chiếu trong các bảng khác
            var sql = @"
        -- Kiểm tra xem hàng hóa có được tham chiếu trong bảng khác không
        -- Ví dụ: Kiểm tra trong bảng đơn hàng, chi tiết đơn hàng, v.v.
        -- Thay thế bằng các bảng thực tế trong hệ thống của bạn
        SELECT EXISTS (
            SELECT 1 
            FROM ""TreeClosure"" tc 
            WHERE tc.""AncestorId"" = @Id OR tc.""DescendantId"" = @Id
            LIMIT 1
        );";

            _logger.LogInformation($"Execute SQL: {sql} with Id: {id}");

            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            // Trả về false cho đến khi có logic kiểm tra thực tế
            return await _dbConnection.QueryFirstOrDefaultAsync<bool>(sql, new { Id = id });
        }

        // Đếm số lượng node con trực tiếp và gián tiếp của một node
        public async Task<int> CountDescendantsAsync(Guid id)
        {
            var sql = @"
        SELECT COUNT(*) - 1 -- Trừ đi chính node hiện tại
        FROM ""TreeClosure"" tc
        WHERE tc.""AncestorId"" = @Id";

            _logger.LogInformation($"Execute SQL: {sql} with Id: {id}");

            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            return await _dbConnection.ExecuteScalarAsync<int>(sql, new { Id = id });
        }

        // Cập nhật trạng thái IsParent của một node dựa trên việc có con hay không
        private async Task UpdateParentIsParentStatus(Guid parentId, IDbTransaction? transaction = null)
        {
            var sql = @"
                UPDATE ""Dm_HangHoaThiTruong""
                SET ""IsParent"" = CASE 
                    WHEN EXISTS (
                        SELECT 1 
                        FROM ""TreeClosure"" tc 
                        WHERE tc.""AncestorId"" = @ParentId 
                          AND tc.""Depth"" = 1
                          AND EXISTS (
                              SELECT 1 
                              FROM ""Dm_HangHoaThiTruong"" child
                              WHERE child.""Id"" = tc.""DescendantId""
                                AND child.""IsDelete"" = false
                          )
                    ) THEN true 
                    ELSE false 
                END,
                ""ModifiedDate"" = NOW()
                WHERE ""Id"" = @ParentId AND ""IsDelete"" = false";

            _logger.LogInformation($"Execute SQL: {sql} with ParentId: {parentId}");

            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            await _dbConnection.ExecuteAsync(sql, new { ParentId = parentId }, transaction);
        }

        // Lấy parent hiện tại của một node
        private async Task<Guid?> GetCurrentParentId(Guid nodeId)
        {
            var sql = @"
                SELECT tc.""AncestorId""
                FROM ""TreeClosure"" tc
                WHERE tc.""DescendantId"" = @NodeId AND tc.""Depth"" = 1";

            _logger.LogInformation($"Execute SQL: {sql} with NodeId: {nodeId}");

            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            return await _dbConnection.QueryFirstOrDefaultAsync<Guid?>(sql, new { NodeId = nodeId });
        }
    }
}
