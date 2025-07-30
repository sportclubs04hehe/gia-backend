using server.Dtos.Common;
using server.Dtos.DanhMuc.Dm_DonViTinh;

namespace server.Service
{
    public interface IDm_DonViTinhService
    {
        Task<PagedResult<Dm_DonViTinhDto>> GetPagedAsync(PagedRequest request);
        Task<IEnumerable<Dm_DonViTinhDto>> GetAllAsync();
        Task<Dm_DonViTinhDto?> GetByIdAsync(Guid id);
        Task<bool> IsCodeExistsAsync(string ma, Guid? excludeId = null);
        Task<Dm_DonViTinhDto> CreateAsync(DmDonViTinhCreateDto createDto);
        Task<bool> UpdateAsync(DmDonViTinhUpdateDto updateDto);
        Task<bool> DeleteAsync(Guid id);
        Task<ImportResultDto> ImportFromExcelAsync(IFormFile file);
    }
}
