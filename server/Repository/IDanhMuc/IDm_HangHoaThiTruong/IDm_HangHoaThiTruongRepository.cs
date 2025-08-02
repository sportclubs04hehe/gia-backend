using server.Models.DanhMuc;
using System.Data;

namespace server.Repository.IDanhMuc.IDm_HangHoaThiTruong
{
    public interface IDm_HangHoaThiTruongRepository
    {
        Task<Dm_HangHoaThiTruong> AddAsync(Dm_HangHoaThiTruong entity, Guid? parentId = null, IDbTransaction transaction = null);
        
        Task<Dm_HangHoaThiTruong?> GetByIdAsync(Guid id);
    }
}
