using Dapper;
using server.Models.DanhMuc;
using server.Dtos.Common;
using System.Data;
using System.Text;
using server.Repository.IDanhMuc.IDm_DonViTinh;
using server.Repository.Base;

namespace server.Repository.DanhMucImpl.Dm_DonViTinhImpl
{
    public class Dm_DonViTinhRepository : Dm_BaseNoChildrentRepository<Dm_DonViTinh>, IDm_DonViTinhRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogger<Dm_DonViTinhRepository> _logger;

        public Dm_DonViTinhRepository(IDbConnection dbConnection, ILogger<Dm_DonViTinhRepository> logger) 
            : base(dbConnection, logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        // Lấy tất cả phân trang
        public async Task<PagedResult<Dm_DonViTinh>> GetPagedAsync(PagedRequest request)
        {
            // Define columns to include in the result
            var columns = @"""Id"", ""Ma"", ""Ten"", ""GhiChu"", ""NgayHieuLuc"", ""NgayHetHieuLuc"", 
                          ""CreatedBy"", ""CreatedDate"", ""ModifiedBy"", ""ModifiedDate"", ""IsDelete""";
            
            // Define columns to search
            var searchColumns = new[] { "Ma", "Ten" };

            // Use the base class implementation for pagination
            return await base.GetPagedAsync(
                request: request,
                tableName: "Dm_DonViTinh",
                columns: columns,
                searchColumns: searchColumns,
                additionalWhereClause: null,
                customSortMappings: null,
                defaultSortColumn: "CreatedDate"  
            );
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
        public async Task<Dm_DonViTinh> CreateAsync(Dm_DonViTinh entity, IDbTransaction transaction = null)
        {
            entity.Id = Guid.NewGuid();
            entity.CreatedDate = DateTime.Now;
            entity.IsDelete = false;

            var sql = @"INSERT INTO ""Dm_DonViTinh"" (""Id"", ""Ma"", ""Ten"", ""GhiChu"", ""NgayHieuLuc"", ""NgayHetHieuLuc"", 
                                            ""CreatedBy"", ""CreatedDate"", ""IsDelete"")
                        VALUES (@Id, @Ma, @Ten, @GhiChu, @NgayHieuLuc, @NgayHetHieuLuc, 
                                @CreatedBy, @CreatedDate, @IsDelete)";
            
            _logger.LogInformation(sql);
            
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }
            
            await _dbConnection.ExecuteAsync(sql, entity, transaction);
            return entity;
        }

        // Cập nhật 
        public async Task<bool> UpdateAsync(Dm_DonViTinh entity, IDbTransaction transaction = null)
        {
            entity.ModifiedDate = DateTime.Now;

            var sql = @"UPDATE ""Dm_DonViTinh"" 
                        SET ""Ma"" = @Ma, ""Ten"" = @Ten, ""GhiChu"" = @GhiChu, 
                            ""NgayHieuLuc"" = @NgayHieuLuc, ""NgayHetHieuLuc"" = @NgayHetHieuLuc,
                            ""ModifiedBy"" = @ModifiedBy, ""ModifiedDate"" = @ModifiedDate
                        WHERE ""Id"" = @Id AND ""IsDelete"" = false";
            
            _logger.LogInformation(sql);
            
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }
            
            var result = await _dbConnection.ExecuteAsync(sql, entity, transaction);
            return result > 0;
        }

        // Xóa bản  ghi
        public async Task<bool> DeleteAsync(Guid id, IDbTransaction transaction = null)
        {
            var sql = @"UPDATE ""Dm_DonViTinh"" 
                        SET ""IsDelete"" = true, ""ModifiedDate"" = @ModifiedDate 
                        WHERE ""Id"" = @Id";
            
            _logger.LogInformation(sql);
            
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }
            
            var result = await _dbConnection.ExecuteAsync(sql, new { Id = id, ModifiedDate = DateTime.Now }, transaction);
            return result > 0;
        }

        // Tìm kiếm theo từ khóa có phân trang
        public async Task<PagedResult<Dm_DonViTinh>> SearchAsync(string searchTerm, int pageNumber = 1, int pageSize = 50)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new PagedResult<Dm_DonViTinh> 
                { 
                    PageNumber = pageNumber, 
                    PageSize = pageSize 
                };

            // Create a PagedRequest to reuse the base GetPagedAsync method
            var request = new PagedRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                SortBy = "CreatedDate",
                SortDescending = true
            };

            // Use the base implementation with the search term
            return await GetPagedAsync(request);
        }
    }
}
