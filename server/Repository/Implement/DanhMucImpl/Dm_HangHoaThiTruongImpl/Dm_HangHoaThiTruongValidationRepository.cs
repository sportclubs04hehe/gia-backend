using Dapper;
using server.Repository.IDanhMuc.IDm_HangHoaThiTruong;
using System.Data;
using System.Data.Common;

namespace server.Repository.Implement.DanhMucImpl.Dm_HangHoaThiTruongImpl
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

        public async Task<bool> IsCodeExistsAtSameLevelAsync(string code, Guid? parentId, Guid? excludeId = null)
        {
            var sql = @"
    WITH SameParentItems AS (
        -- Nếu có parentId, lấy các items có cùng cha
        SELECT h.""Id"", h.""Ma""
        FROM ""Dm_HangHoaThiTruong"" h
        JOIN ""TreeClosure"" tc_item ON h.""Id"" = tc_item.""DescendantId""
        JOIN ""TreeClosure"" tc_parent ON tc_parent.""DescendantId"" = @ParentId::uuid
        WHERE tc_item.""AncestorId"" = tc_parent.""AncestorId""
          AND tc_item.""Depth"" = 1
          AND h.""IsDelete"" = false
        
        UNION ALL
        
        -- Nếu không có parentId, lấy các root items (không có cha)
        SELECT h.""Id"", h.""Ma""
        FROM ""Dm_HangHoaThiTruong"" h
        WHERE NOT EXISTS (
            SELECT 1 FROM ""TreeClosure"" tc
            WHERE tc.""DescendantId"" = h.""Id"" AND tc.""Depth"" > 0
        )
        AND h.""IsDelete"" = false
        AND @ParentId IS NULL
    )
    
    SELECT COUNT(*)
    FROM SameParentItems
    WHERE ""Ma"" = @Code
    AND (@ExcludeId::uuid IS NULL OR ""Id"" != @ExcludeId::uuid)";

            _logger.LogInformation("SQL: {Sql}, Params: Code={Code}, ParentId={ParentId}, ExcludeId={ExcludeId}", 
                sql, code, parentId, excludeId);

            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            var parameters = new DynamicParameters();
            parameters.Add("Code", code);
            parameters.Add("ParentId", parentId.HasValue ? parentId.Value : (object)DBNull.Value, DbType.Guid);
            parameters.Add("ExcludeId", excludeId.HasValue ? excludeId.Value : (object)DBNull.Value, DbType.Guid);

            var count = await _dbConnection.ExecuteScalarAsync<int>(sql, parameters);
            return count > 0;
        }
    }
}
