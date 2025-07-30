using System.ComponentModel.DataAnnotations;
using server.Dtos;

namespace server.Dtos.DanhMuc.Dm_DonViTinh
{
    public class Dm_DonViTinhDto : BaseDto
    {
        public string Ma { get; set; } = null!;
        public string Ten { get; set; } = null!;
        public string? GhiChu { get; set; }
        public DateTime NgayHieuLuc { get; set; }
        public DateTime NgayHetHieuLuc { get; set; }
    }
}
