using server.Dtos.Common;
using server.Models.DanhMuc;
using System.Data;

namespace server.Repository.IDanhMuc.IDm_DonViTinh
{
    public interface IDm_DonViTinhRepository
    {
        Task<PagedResult<Dm_DonViTinh>> GetPagedAsync(PagedRequest request);
        Task<IEnumerable<Dm_DonViTinh>> GetAllAsync();
        Task<Dm_DonViTinh?> GetByIdAsync(Guid id);
        Task<bool> IsCodeExistsAsync(string ma, Guid? excludeId = null);
        
        // Bổ sung tham số transaction
        Task<Dm_DonViTinh> CreateAsync(Dm_DonViTinh entity, IDbTransaction transaction = null);
        Task<bool> UpdateAsync(Dm_DonViTinh entity, IDbTransaction transaction = null);
        Task<bool> DeleteAsync(Guid id, IDbTransaction transaction = null);
        Task<IEnumerable<Dm_DonViTinh>> SearchAsync(string searchTerm);
    }
}
