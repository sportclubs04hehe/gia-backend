namespace server.Dtos.DanhMuc.Dm_HangHoaThiTruong
{
    // Dùng cho báo cáo dạng phẳng, không cần Children
    public class Dm_HangHoaThiTruongFlatDto
    {
        public Guid Id { get; set; }
        public string Ma { get; set; }
        public string Ten { get; set; }
        public string? GhiChu { get; set; }
        public string? DacTinh { get; set; }
        public Guid? DonViTinhId { get; set; }
        public int Depth { get; set; }
        public DateTime NgayHieuLuc { get; set; }
        public DateTime NgayHetHieuLuc { get; set; }
    }
}
