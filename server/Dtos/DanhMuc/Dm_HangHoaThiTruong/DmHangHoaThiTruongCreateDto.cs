using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace server.Dtos.DanhMuc.Dm_HangHoaThiTruongDto
{
    public class DmHangHoaThiTruongCreateDto
    {
        [Required(ErrorMessage = "Mã là bắt buộc")]
        public string Ma { get; set; }
        
        [Required(ErrorMessage = "Tên là bắt buộc")]
        public string Ten { get; set; }
        
        public string? GhiChu { get; set; }
        public string? DacTinh { get; set; }
        public Guid? DonViTinhId { get; set; }
        public DateTime NgayHieuLuc { get; set; }
        public DateTime NgayHetHieuLuc { get; set; }
        public bool IsParent { get; set; } = false; 
        public Guid? ParentId { get; set; } 
    }
}
