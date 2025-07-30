using server.Models.DanhMuc;

namespace server.Repository.IDanhMuc
{
    public interface IDm_HangHoaThiTruongRepository
    {
        Task<IEnumerable<Dm_HangHoaThiTruong>> GetAllUsersAsync();
        Task<Dm_HangHoaThiTruong> GetUserByIdAsync(int id);
        Task AddUserAsync(Dm_HangHoaThiTruong dm_HangHoaThiTruong);
    }   
}
