using Dapper;
using server.Repository.IDanhMuc.IDm_HangHoaThiTruong;
using System.Data;

namespace server.Repository.DanhMucImpl.Dm_HangHoaThiTruongImpl
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

        public async Task UpdateParentAsync(Guid nodeId, Guid newParentId, IDbTransaction transaction = null)
        {
            // Bước 1: Kiểm tra xem nút cha mới có phải là con cháu của nút đó không (sẽ tạo ra một chu trình)
            var checkCycleSql = @"
                SELECT COUNT(*) FROM ""TreeClosure"" 
                WHERE ""AncestorId"" = @NodeId AND ""DescendantId"" = @NewParentId";
    
            var cycleCount = await _dbConnection.ExecuteScalarAsync<int>(
                checkCycleSql, 
                new { NodeId = nodeId, NewParentId = newParentId },
                transaction);
    
            if (cycleCount > 0)
            {
                throw new InvalidOperationException("Cannot move a node to its own descendant - would create a cycle");
            }

            // Bước 2: Lấy nút cha hiện tại của nút
            var getCurrentParentSql = @"
                SELECT tc.""AncestorId"" FROM ""TreeClosure"" tc
                WHERE tc.""DescendantId"" = @NodeId AND tc.""Depth"" = 1";
    
            var currentParent = await _dbConnection.QueryFirstOrDefaultAsync<Guid?>(
                getCurrentParentSql,
                new { NodeId = nodeId },
                transaction);

            // Nếu cha mẹ không thay đổi, không còn gì để làm nữa
            if (currentParent == newParentId)
            {
                return;
            }

            // Bước 3: Xóa các mối quan hệ đóng cây hiện có
            // Xóa tất cả các mục nhập mà nút hoặc các nút con của nó được kết nối với nút tổ tiên
            var deleteRelationshipsSql = @"
                DELETE FROM ""TreeClosure""
                WHERE (""DescendantId"" IN (
                        SELECT d.""DescendantId"" FROM ""TreeClosure"" d
                        WHERE d.""AncestorId"" = @NodeId
                    ))
                AND (""AncestorId"" IN (
                        SELECT a.""AncestorId"" FROM ""TreeClosure"" a
                        WHERE a.""DescendantId"" = @NodeId AND a.""AncestorId"" != a.""DescendantId""
                    ));";

            await _dbConnection.ExecuteAsync(
                deleteRelationshipsSql,
                new { NodeId = nodeId },
                transaction);

            // Bước 4: Tạo các mối quan hệ đóng cây mới
            // Đối với mỗi tổ tiên của nút cha mới, hãy kết nối nó với nút và tất cả các nút con của nó
            var insertNewRelationshipsSql = @"
                INSERT INTO ""TreeClosure"" (""AncestorId"", ""DescendantId"", ""Depth"")
                SELECT a.""AncestorId"", d.""DescendantId"", a.""Depth"" + d.""Depth"" + 1
                FROM ""TreeClosure"" a
                CROSS JOIN ""TreeClosure"" d
                WHERE a.""DescendantId"" = @NewParentId
                AND d.""AncestorId"" = @NodeId;";

            await _dbConnection.ExecuteAsync(
                insertNewRelationshipsSql,
                new { NodeId = nodeId, NewParentId = newParentId },
                transaction);
        }
    }
}
