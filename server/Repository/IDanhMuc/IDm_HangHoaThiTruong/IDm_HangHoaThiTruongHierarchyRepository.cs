using System.Data;

namespace server.Repository.IDanhMuc.IDm_HangHoaThiTruong
{
    public interface IDm_HangHoaThiTruongHierarchyRepository
    {
        Task UpdateTreeClosureAsync(Guid entityId, Guid? parentId, IDbTransaction transaction);
        Task UpdateParentAsync(Guid nodeId, Guid newParentId, IDbTransaction transaction = null);
    }
}
