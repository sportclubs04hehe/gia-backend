using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models.DanhMuc
{
    public class Dm_HangHoaThiTruong : BaseModel
    {
        public required string Ma { get; set; }
        public required string Ten { get; set; }
        public string? GhiChu { get; set; }
        public string? DacTinh { get; set; }
        public Guid? DonViTinhId { get; set; }
    }
}
