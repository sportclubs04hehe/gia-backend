using Dapper;
using server.Repository.IDanhMuc.IDm_HangHoaThiTruong;
using System.Data;
using System.Data.Common;

namespace server.Repository.DanhMucImpl.Dm_HangHoaThiTruongImpl
{
    public class Dm_HangHoaThiTruongValidationRepository : IDm_HangHoaThiTruongValidationRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogger<Dm_HangHoaThiTruongValidationRepository> _logger;

        public Dm_HangHoaThiTruongValidationRepository(IDbConnection dbConnection,
            ILogger<Dm_HangHoaThiTruongValidationRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        // Kiểm tra mã tồn tại trên cùng một cấp.
        public async Task<bool> IsCodeExistsAtSameLevelAsync(string code, Guid? parentId, Guid? excludeId = null)
        {
            // SQL query remains the same
            var sql = @"
SELECT COUNT(*)
FROM ""Dm_HangHoaThiTruong"" h
LEFT JOIN ""TreeClosure"" tc_child ON h.""Id"" = tc_child.""DescendantId"" AND tc_child.""Depth"" = 1
LEFT JOIN ""TreeClosure"" tc_parent ON tc_parent.""DescendantId"" = @ParentId AND tc_parent.""AncestorId"" = tc_child.""AncestorId""
WHERE h.""IsDelete"" = false
AND h.""Ma"" = @Code
AND (@ExcludeId IS NULL OR h.""Id"" != @ExcludeId)
AND (
    -- Nếu có parentId, chỉ lấy các hàng hóa có cùng cha
    (@ParentId IS NOT NULL AND tc_parent.""DescendantId"" IS NOT NULL)
    OR
    -- Nếu không có parentId, chỉ lấy các hàng hóa cấp root
    (@ParentId IS NULL AND NOT EXISTS (
        SELECT 1 FROM ""TreeClosure"" tc 
        WHERE tc.""DescendantId"" = h.""Id"" AND tc.""Depth"" > 0
    ))
)";

            _logger.LogInformation("SQL: {Sql}, Params: Code={Code}, ParentId={ParentId}, ExcludeId={ExcludeId}", 
                sql, code, parentId, excludeId);

            // LOẠI BỎ using block để không dispose connection
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            var parameters = new DynamicParameters();
            parameters.Add("Code", code);
            parameters.Add("ParentId", parentId.HasValue ? parentId.Value : DBNull.Value, DbType.Guid);
            parameters.Add("ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value, DbType.Guid);

            var count = await _dbConnection.ExecuteScalarAsync<int>(sql, parameters);
            return count > 0;
        }
    }
}
