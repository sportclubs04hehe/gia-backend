using server.Dtos.DanhMuc.Dm_HangHoaThiTruong;

namespace server.Service
{
    public interface IDm_HangHoaThiTruongService
    {
        Task<Dm_HangHoaThiTruongDto> CreateAsync(DmHangHoaThiTruongCreateDto createDto);
        
        Task<Dm_HangHoaThiTruongDto?> GetByIdAsync(Guid id);
    }
}
