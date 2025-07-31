using server.Dtos.Common;
using server.Models.DanhMuc;

namespace server.Repository.IDanhMuc.IDm_DonViTinh
{
    public interface IDm_DonViTinhRepository
    {
        Task<PagedResult<Dm_DonViTinh>> GetPagedAsync(PagedRequest request);
        Task<IEnumerable<Dm_DonViTinh>> GetAllAsync();
        Task<Dm_DonViTinh?> GetByIdAsync(Guid id);
        Task<bool> IsCodeExistsAsync(string ma, Guid? excludeId = null);
        Task<Dm_DonViTinh> CreateAsync(Dm_DonViTinh entity);
        Task<bool> UpdateAsync(Dm_DonViTinh entity);
        Task<bool> DeleteAsync(Guid id);
        Task<IEnumerable<Dm_DonViTinh>> SearchAsync(string searchTerm);
    }
}
