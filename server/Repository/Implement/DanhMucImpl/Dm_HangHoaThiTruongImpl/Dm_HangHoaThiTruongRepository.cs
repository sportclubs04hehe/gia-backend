using Dapper;
using server.Models.DanhMuc;
using server.Repository.IDanhMuc.IDm_HangHoaThiTruong;
using System.Data;

namespace server.Repository.Implement.DanhMucImpl.Dm_HangHoaThiTruongImpl
{
    public class Dm_HangHoaThiTruongRepository : IDm_HangHoaThiTruongRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogger<Dm_HangHoaThiTruongRepository> _logger;

        public Dm_HangHoaThiTruongRepository(IDbConnection dbConnection, 
            ILogger<Dm_HangHoaThiTruongRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task<Dm_HangHoaThiTruong> AddAsync(Dm_HangHoaThiTruong entity, Guid? parentId = null)
        {
            // Đảm bảo entity có Id nếu chưa được set
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();

            // SQL để thêm mới hàng hóa
            var insertSql = @"
                INSERT INTO Dm_HangHoaThiTruong (Id, Ma, Ten, GhiChu, DacTinh, DonViTinhId, NgayHieuLuc, NgayHetHieuLuc, CreatedDate, UpdatedDate)
                VALUES (@Id, @Ma, @Ten, @GhiChu, @DacTinh, @DonViTinhId, @NgayHieuLuc, @NgayHetHieuLuc, GETDATE(), GETDATE());
                SELECT * FROM Dm_HangHoaThiTruong WHERE Id = @Id;";

            _logger.LogInformation($"Execute SQL: {insertSql} with parameters: {System.Text.Json.JsonSerializer.Serialize(entity)}");

            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Thêm bản ghi vào bảng Dm_HangHoaThiTruong
                    var result = await _dbConnection.QueryFirstOrDefaultAsync<Dm_HangHoaThiTruong>(
                        insertSql, entity, transaction);

                    // Thêm mối quan hệ trong TreeClosure
                    await UpdateTreeClosureAsync(entity.Id, parentId, transaction);

                    transaction.Commit();
                    return result;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Error adding Dm_HangHoaThiTruong");
                    throw;
                }
            }
        }

        private async Task UpdateTreeClosureAsync(Guid entityId, Guid? parentId, IDbTransaction transaction)
        {
            // 1. Luôn tạo quan hệ tự tham chiếu (node đến chính nó với độ sâu 0)
            var selfReferenceSql = @"
                INSERT INTO TreeClosure (AncestorId, DescendantId, Depth)
                VALUES (@EntityId, @EntityId, 0);";

            _logger.LogInformation($"Execute SQL: {selfReferenceSql} with EntityId: {entityId}");
            await _dbConnection.ExecuteAsync(selfReferenceSql, new { EntityId = entityId }, transaction);

            // 2. Nếu có parent, cập nhật quan hệ với cha và tổ tiên
            if (parentId.HasValue)
            {
                var ancestorsSql = @"
                    INSERT INTO TreeClosure (AncestorId, DescendantId, Depth)
                    SELECT AncestorId, @EntityId, Depth + 1
                    FROM TreeClosure
                    WHERE DescendantId = @ParentId;";

                _logger.LogInformation($"Execute SQL: {ancestorsSql} with EntityId: {entityId}, ParentId: {parentId}");
                await _dbConnection.ExecuteAsync(
                    ancestorsSql,
                    new { EntityId = entityId, ParentId = parentId },
                    transaction);
            }
        }

        // Thêm phương thức này vào lớp Dm_HangHoaThiTruongRepository
        public async Task<Dm_HangHoaThiTruong?> GetByIdAsync(Guid id)
        {
            var sql = @"
                SELECT * FROM Dm_HangHoaThiTruong 
                WHERE Id = @Id AND IsDeleted = 0";

            _logger.LogInformation($"Execute SQL: {sql} with Id: {id}");
            
            var result = await _dbConnection.QueryFirstOrDefaultAsync<Dm_HangHoaThiTruong>(sql, new { Id = id });
            
            return result;
        }
    }
}
