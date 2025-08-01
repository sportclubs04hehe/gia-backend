using Dapper;
using server.Repository.IDanhMuc.IDm_HangHoaThiTruong;
using System.Data;

namespace server.Repository.Implement.DanhMucImpl.Dm_HangHoaThiTruongImpl
{
    public class Dm_HangHoaThiTruongHierarchyRepository : IDm_HangHoaThiTruongHierarchyRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogger<Dm_HangHoaThiTruongHierarchyRepository> _logger;

        public Dm_HangHoaThiTruongHierarchyRepository(IDbConnection dbConnection,
            ILogger<Dm_HangHoaThiTruongHierarchyRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task UpdateTreeClosureAsync(Guid entityId, Guid? parentId, IDbTransaction transaction)
        {
            // 1. Luôn tạo quan hệ tự tham chiếu (node đến chính nó với độ sâu 0)
            var selfReferenceSql = @"
                INSERT INTO ""TreeClosure"" (""AncestorId"", ""DescendantId"", ""Depth"")
                VALUES (@EntityId, @EntityId, 0);";

            _logger.LogInformation($"Execute SQL: {selfReferenceSql} with EntityId: {entityId}");
            await _dbConnection.ExecuteAsync(selfReferenceSql, new { EntityId = entityId }, transaction);

            // 2. Nếu có parent, cập nhật quan hệ với cha và tổ tiên
            if (parentId.HasValue)
            {
                var ancestorsSql = @"
                    INSERT INTO ""TreeClosure"" (""AncestorId"", ""DescendantId"", ""Depth"")
                    SELECT ""AncestorId"", @EntityId, ""Depth"" + 1
                    FROM ""TreeClosure""
                    WHERE ""DescendantId"" = @ParentId;";

                _logger.LogInformation($"Execute SQL: {ancestorsSql} with EntityId: {entityId}, ParentId: {parentId}");
                await _dbConnection.ExecuteAsync(
                    ancestorsSql,
                    new { EntityId = entityId, ParentId = parentId },
                    transaction);
            }
        }
    }
}
