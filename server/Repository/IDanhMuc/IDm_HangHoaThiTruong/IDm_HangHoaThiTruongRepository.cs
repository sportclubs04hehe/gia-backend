using server.Dtos.Common;
using server.Models.DanhMuc;
using server.Models.Extends;
using System.Data;

namespace server.Repository.IDanhMuc.IDm_HangHoaThiTruong
{
    public interface IDm_HangHoaThiTruongRepository
    {
        // Lấy danh sách các mặt hàng cấp cao nhất (không có cha)
        Task<IEnumerable<Dm_HangHoaThiTruongJoined>> GetTopLevelItemsAsync();
        // Lấy danh sách các mặt hàng con trực tiếp của một mặt hàng cha với phân trang và tìm kiếm
        Task<PagedResult<Dm_HangHoaThiTruongJoined>> GetChildrenAsync(
            Guid parentId,
            PagedRequest request,
            string? searchTerm = null);

        // Lấy mặt hàng theo ID
        Task<Dm_HangHoaThiTruong?> GetByIdAsync(Guid id);
        // Thêm mới một mặt hàng với cha (nếu có) và cập nhật cấu trúc cây    
        Task<Dm_HangHoaThiTruong> AddAsync(Dm_HangHoaThiTruong entity, Guid? parentId = null, IDbTransaction? transaction = null);
        
        // Cập nhật một mặt hàng với cha mới (nếu có) và cập nhật cấu trúc cây
        Task<Dm_HangHoaThiTruong> UpdateAsync(Dm_HangHoaThiTruong entity, Guid? newParentId = null, IDbTransaction? transaction = null);

        // Xóa một mặt hàng
        Task<int> DeleteAsync(Guid id, IDbTransaction? transaction = null);
        // Xóa nhiều mặt hàng
        Task<int> DeleteManyAsync(IEnumerable<Guid> ids, IDbTransaction? transaction = null);
        // Kiểm tra xem mặt hàng có đang được tham chiếu ở nơi khác không
        Task<bool> IsReferencedAsync(Guid id);
        // Đếm số lượng mặt hàng con trực tiếp của một mặt hàng
        Task<int> CountDescendantsAsync(Guid id);
        /// <returns>Danh sách các mặt hàng cha và con</returns>
        Task<IEnumerable<Dm_HangHoaThiTruongJoined>> GetAllParentItemsWithChildrenAsync();
    }
}
