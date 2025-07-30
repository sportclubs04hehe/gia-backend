using System.ComponentModel.DataAnnotations;

namespace server.Dtos.DanhMuc.Dm_DonViTinh
{
    public class DmDonViTinhImportDto
    {
        [Required(ErrorMessage = "Mã không được để trống")]
        [StringLength(50, ErrorMessage = "Mã không được vượt quá 50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Mã chỉ được chứa chữ cái, số, gạch dưới và gạch ngang")]
        public string Ma { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên không được để trống")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        public string Ten { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? GhiChu { get; set; }
    }

}
