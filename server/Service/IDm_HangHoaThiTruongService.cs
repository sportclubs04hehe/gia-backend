using server.Dtos.Common;
using server.Dtos.DanhMuc.Dm_HangHoaThiTruongDto;

namespace server.Service
{
    public interface IDm_HangHoaThiTruongService
    {
        Task<IEnumerable<Dm_HangHoaThiTruongDto>> GetTopLevelItemsAsync();
        Task<Dm_HangHoaThiTruongDto> CreateAsync(DmHangHoaThiTruongCreateDto createDto);
        
        Task<Dm_HangHoaThiTruongDto?> GetByIdAsync(Guid id);
        Task<PagedResult<Dm_HangHoaThiTruongDto>> GetChildrenAsync(
            Guid parentId, 
            PagedRequest request, 
            string searchTerm = null);
        Task<Dm_HangHoaThiTruongDto> UpdateAsync(DmHangHoaThiTruongUpdateDto updateDto);
        Task<DeleteResult> DeleteAsync(Guid id);
        Task<DeleteResult> DeleteManyAsync(IEnumerable<Guid> ids);
        Task<ImportResultDto> ImportFromExcelAsync(IFormFile file, string userName);
        Task<List<ImportErrorDto>> ValidateExcelAsync(IFormFile file);
    }
}
