using System.ComponentModel.DataAnnotations;

namespace server.Models.DanhMuc
{
    public class Dm_DonViTinh : BaseModel
    {
        public required string Ma { get; set; } 
        public required string Ten { get; set; } 

        public string? GhiChu { get; set; }

        public DateTime NgayHieuLuc { get; set; }

        public DateTime NgayHetHieuLuc { get; set; }

    }
}
