using server.Repository.IDanhMuc.IDm_DonViTinh;
using server.Repository.IDanhMuc.IDm_HangHoaThiTruong;
using System.Data;

namespace server.Repository.UnitOfWork
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        // Repository properties
        IDm_HangHoaThiTruongRepository HangHoaThiTruong { get; }
        IDm_HangHoaThiTruongHierarchyRepository HangHoaThiTruongHierarchy { get; }
        IDm_HangHoaThiTruongValidationRepository HangHoaThiTruongValidation { get; }
        
        IDm_DonViTinhRepository DonViTinh { get; }
        IDm_DonViTinhImportExcel DonViTinhImport { get; }

        // Transaction management
        Task<IDbTransaction> BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        IDbTransaction? CurrentTransaction { get; }
    }
}
