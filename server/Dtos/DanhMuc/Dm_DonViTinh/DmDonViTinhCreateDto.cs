using System.ComponentModel.DataAnnotations;

namespace server.Dtos.DanhMuc.Dm_DonViTinh
{
    public class DmDonViTinhCreateDto
    {
        [Required(ErrorMessage = "Mã đơn vị tính không được để trống")]
        public string Ma { get; set; } = null!;
        [Required(ErrorMessage = "Tên đơn vị tính không được để trống")]
        public string Ten { get; set; } = null!;
        public string? GhiChu { get; set; }
        public DateTime NgayHieuLuc { get; set; }
        public DateTime NgayHetHieuLuc { get; set; }
    }
}
