using server.Dtos.Common;
using server.Dtos.DanhMuc.Dm_HangHoaThiTruongDto;

namespace server.Service
{
    public interface IDm_HangHoaThiTruongService
    {
        Task<Dm_HangHoaThiTruongDto> CreateAsync(DmHangHoaThiTruongCreateDto createDto);
        
        Task<Dm_HangHoaThiTruongDto?> GetByIdAsync(Guid id);
        Task<PagedResult<Dm_HangHoaThiTruongDto>> GetChildrenAsync(
            Guid parentId, 
            PagedRequest request, 
            string searchTerm = null);
    }
}
