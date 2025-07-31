using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models.DanhMuc
{
    /// Mô hình đại diện cho mối quan hệ giữa các mặt hàng trong thị trường, kê khai,
    /// , sử dụng để xây dựng cấu trúc cây.
    /// Mỗi bản ghi trong bảng này đại diện cho một mối quan hệ giữa một tổ tiên (ancestor) và một hậu duệ (descendant),
    /// với độ sâu (depth) xác định khoảng cách từ tổ tiên đến hậu duệ trong cấu trúc cây.
    public class TreeClosure
    {
        public Guid AncestorId { get; set; }
        public Guid DescendantId { get; set; }
        public int Depth { get; set; }
    }
}
