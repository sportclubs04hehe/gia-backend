using System.ComponentModel.DataAnnotations;

namespace server.Dtos.DanhMuc.Dm_HangHoaThiTruong
{
    public class DmHangHoaThiTruongCreateDto
    {
        [Required(ErrorMessage = "Mã hàng hóa thị trường không được để trống.")]
        public string Ma { get; set; }
        [Required(ErrorMessage = "Tên hàng hóa thị trường không được để trống.")]
        public string Ten { get; set; }
        public string? GhiChu { get; set; }
        public string? DacTinh { get; set; }
        [Required(ErrorMessage = "Đơn vị tính không được để trống.")]
        public Guid DonViTinhId { get; set; }
        public DateTime NgayHieuLuc { get; set; }
        public DateTime NgayHetHieuLuc { get; set; }
        public Guid? ParentId { get; set; }  // ID của node cha trong cây
    }
}
