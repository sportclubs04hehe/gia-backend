using Dapper;
using server.Models.DanhMuc;
using server.Dtos.Common;
using System.Data;
using System.Text;
using server.Repository.IDanhMuc.IDm_DonViTinh;

namespace server.Repository.Implement.DanhMucImpl.Dm_DonViTinhImpl
{
    public class Dm_DonViTinhRepository : IDm_DonViTinhRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogger<Dm_DonViTinhRepository> _logger;

        public Dm_DonViTinhRepository(IDbConnection dbConnection, ILogger<Dm_DonViTinhRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        // Lấy tất cả phân trang
        public async Task<PagedResult<Dm_DonViTinh>> GetPagedAsync(PagedRequest request)
        {
            var whereClause = new StringBuilder("WHERE \"IsDelete\" = false");
            var parameters = new DynamicParameters();

            // Thêm điều kiện tìm kiếm
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                whereClause.Append(" AND (\"Ma\" ILIKE @SearchTerm OR \"Ten\" ILIKE @SearchTerm)");
                parameters.Add("SearchTerm", $"%{request.SearchTerm}%");
            }

            // Xác định cột sắp xếp an toàn
            var allowedSortColumns = new[] { "Ma", "Ten", "CreatedDate", "NgayHieuLuc" };
            var sortColumn = allowedSortColumns.Contains(request.SortBy) ? request.SortBy : "CreatedDate";
            var sortDirection = request.SortDescending ? "DESC" : "ASC";

            // Tính toán offset
            var offset = (request.PageNumber - 1) * request.PageSize;
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", request.PageSize);

            // Query để đếm tổng số bản ghi và lấy dữ liệu trong một lần truy vấn
            var sql = $@"
                -- Đếm tổng số bản ghi
                SELECT COUNT(*) 
                FROM ""Dm_DonViTinh"" 
                {whereClause};

                -- Lấy dữ liệu phân trang
                SELECT ""Id"", ""Ma"", ""Ten"", ""GhiChu"", ""NgayHieuLuc"", ""NgayHetHieuLuc"", 
                       ""CreatedBy"", ""CreatedDate"", ""ModifiedBy"", ""ModifiedDate"", ""IsDelete""
                FROM ""Dm_DonViTinh"" 
                {whereClause}
                ORDER BY ""{sortColumn}"" {sortDirection}
                LIMIT @PageSize OFFSET @Offset";

            _logger.LogInformation("Executing paged query: {Sql}", sql);

            using var multi = await _dbConnection.QueryMultipleAsync(sql, parameters);
            
            var totalCount = await multi.ReadSingleAsync<int>();
            var items = await multi.ReadAsync<Dm_DonViTinh>();

            return new PagedResult<Dm_DonViTinh>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        // Lấy tất cả (không phân trang)
        public async Task<IEnumerable<Dm_DonViTinh>> GetAllAsync()
        {
            var sql = @"SELECT ""Id"", ""Ma"", ""Ten"", ""GhiChu"", ""NgayHieuLuc"", ""NgayHetHieuLuc"", 
                               ""CreatedBy"", ""CreatedDate"", ""ModifiedBy"", ""ModifiedDate"", ""IsDelete""
                        FROM ""Dm_DonViTinh"" 
                        WHERE ""IsDelete"" = false";
            
            _logger.LogInformation(sql);
            return await _dbConnection.QueryAsync<Dm_DonViTinh>(sql);
        }

        // Tìm bản ghi theo id
        public async Task<Dm_DonViTinh?> GetByIdAsync(Guid id)
        {
            var sql = @"SELECT ""Id"", ""Ma"", ""Ten"", ""GhiChu"", ""NgayHieuLuc"", ""NgayHetHieuLuc"", 
                               ""CreatedBy"", ""CreatedDate"", ""ModifiedBy"", ""ModifiedDate"", ""IsDelete""
                        FROM ""Dm_DonViTinh"" 
                        WHERE ""Id"" = @Id AND ""IsDelete"" = false";
            
            _logger.LogInformation(sql);
            return await _dbConnection.QueryFirstOrDefaultAsync<Dm_DonViTinh>(sql, new { Id = id });
        }

        // Kiểm tra trùng mã
        public async Task<bool> IsCodeExistsAsync(string ma, Guid? excludeId = null)
        {
            var sql = @"SELECT COUNT(*) 
                        FROM ""Dm_DonViTinh"" 
                        WHERE ""Ma"" = @Ma AND ""IsDelete"" = false";

            var parameters = new DynamicParameters();
            parameters.Add("Ma", ma);

            if (excludeId.HasValue)
            {
                sql += " AND \"Id\" != @ExcludeId";
                parameters.Add("ExcludeId", excludeId.Value);
            }

            _logger.LogInformation("Checking code exists: {Sql}", sql);
            var count = await _dbConnection.QuerySingleAsync<int>(sql, parameters);
            return count > 0;
        }

        //Thêm mới 1
        public async Task<Dm_DonViTinh> CreateAsync(Dm_DonViTinh entity)
        {
            entity.Id = Guid.NewGuid();
            entity.CreatedDate = DateTime.Now;
            entity.IsDelete = false;

            var sql = @"INSERT INTO ""Dm_DonViTinh"" (""Id"", ""Ma"", ""Ten"", ""GhiChu"", ""NgayHieuLuc"", ""NgayHetHieuLuc"", 
                                                      ""CreatedBy"", ""CreatedDate"", ""IsDelete"")
                        VALUES (@Id, @Ma, @Ten, @GhiChu, @NgayHieuLuc, @NgayHetHieuLuc, 
                                @CreatedBy, @CreatedDate, @IsDelete)";
            
            _logger.LogInformation(sql);
            await _dbConnection.ExecuteAsync(sql, entity);
            return entity;
        }

        // Cập nhật 
        public async Task<bool> UpdateAsync(Dm_DonViTinh entity)
        {
            entity.ModifiedDate = DateTime.Now;

            var sql = @"UPDATE ""Dm_DonViTinh"" 
                        SET ""Ma"" = @Ma, ""Ten"" = @Ten, ""GhiChu"" = @GhiChu, 
                            ""NgayHieuLuc"" = @NgayHieuLuc, ""NgayHetHieuLuc"" = @NgayHetHieuLuc,
                            ""ModifiedBy"" = @ModifiedBy, ""ModifiedDate"" = @ModifiedDate
                        WHERE ""Id"" = @Id AND ""IsDelete"" = false";
            
            _logger.LogInformation(sql);
            var result = await _dbConnection.ExecuteAsync(sql, entity);
            return result > 0;
        }

        // Xóa bản  ghi
        public async Task<bool> DeleteAsync(Guid id)
        {
            var sql = @"UPDATE ""Dm_DonViTinh"" 
                        SET ""IsDelete"" = true, ""ModifiedDate"" = @ModifiedDate 
                        WHERE ""Id"" = @Id";
            
            _logger.LogInformation(sql);
            var result = await _dbConnection.ExecuteAsync(sql, new { Id = id, ModifiedDate = DateTime.Now });
            return result > 0;
        }

    }
}
