namespace server.Dtos.DanhMuc.Dm_HangHoaThiTruongDto
{
    // Dùng cho thao tác cơ bản (lấy)
    public class Dm_HangHoaThiTruongDto : BaseDto
    {
        public string Ma { get; set; } = null!;
        public string Ten { get; set; } = null!;
        public string? GhiChu { get; set; }
        public string? DacTinh { get; set; }
        public Guid? DonViTinhId { get; set; }
        public string? DonViTinhTen { get; set; } 
        public DateTime NgayHieuLuc { get; set; }
        public DateTime NgayHetHieuLuc { get; set; }
        public bool HasChildren { get; set; }
    }
}
