using server.Models.DanhMuc;

namespace server.Repository.IDanhMuc.IDm_HangHoaThiTruong
{
    public interface IDm_HangHoaThiTruongRepository
    {
        Task<Dm_HangHoaThiTruong> AddAsync(Dm_HangHoaThiTruong entity, Guid? parentId = null);
        
        Task<Dm_HangHoaThiTruong?> GetByIdAsync(Guid id);
    }
}
