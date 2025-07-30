using Dapper;
using Npgsql;
using server.Models.DanhMuc;
using server.Repository.IDanhMuc;

namespace server.Repository.Implement.DanhMucImpl
{
    public class Dm_HangHoaThiTruongRepository : IDm_HangHoaThiTruongRepository
    {
        private readonly NpgsqlConnection _connection;
        private readonly ILogger<Dm_HangHoaThiTruongRepository> _logger;

        public Dm_HangHoaThiTruongRepository(NpgsqlConnection connection, ILogger<Dm_HangHoaThiTruongRepository> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AddUserAsync(Dm_HangHoaThiTruong dm_HangHoaThiTruong)
        {
            const string query = "INSERT INTO Dm_HangHoaThiTruong (MaHangHoa, TenHangHoa) VALUES (@MaHangHoa, @TenHangHoa)";
            
            _logger.LogDebug("Executing SQL: {Query} with parameters: {@Parameters}", 
                query, dm_HangHoaThiTruong);
            
            await _connection.ExecuteAsync(query, dm_HangHoaThiTruong);
        }

        public async Task<IEnumerable<Dm_HangHoaThiTruong>> GetAllUsersAsync()
        {
            const string query = "SELECT * FROM Dm_HangHoaThiTruong";
            
            _logger.LogDebug("Executing SQL: {Query}", query);
            
            var result = await _connection.QueryAsync<Dm_HangHoaThiTruong>(query);
            
            return result;
        }

        public async Task<Dm_HangHoaThiTruong> GetUserByIdAsync(int id)
        {
            const string query = "SELECT * FROM Dm_HangHoaThiTruong WHERE Id = @Id";
            
            _logger.LogDebug("Executing SQL: {Query} with parameter Id: {Id}", query, id);
            
            var result = await _connection.QueryFirstOrDefaultAsync<Dm_HangHoaThiTruong>(query, new { Id = id });
            return result;
        }
    }
}
