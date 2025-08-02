using server.Dtos.Common;
using server.Dtos.DanhMuc.Dm_HangHoaThiTruongDto;
using server.Models.DanhMuc;
using server.Models.Extends;
using System.Data;

namespace server.Repository.IDanhMuc.IDm_HangHoaThiTruong
{
    public interface IDm_HangHoaThiTruongRepository
    {
        Task<PagedResult<Dm_HangHoaThiTruongJoined>> GetChildrenAsync(
            Guid parentId,
            PagedRequest request,
            string searchTerm = null);
            
        Task<Dm_HangHoaThiTruong> AddAsync(Dm_HangHoaThiTruong entity, Guid? parentId = null, IDbTransaction transaction = null);
        
        Task<Dm_HangHoaThiTruong?> GetByIdAsync(Guid id);
    }
}
