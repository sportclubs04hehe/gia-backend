using Dapper;
using server.Models.DanhMuc;
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
