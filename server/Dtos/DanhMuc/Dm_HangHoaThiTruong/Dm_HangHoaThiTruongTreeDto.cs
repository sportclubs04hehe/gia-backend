namespace server.Dtos.DanhMuc.Dm_HangHoaThiTruongDto
{
    /// <summary>
    /// Dùng cho tree view (lazy loading hoặc full tree), có Children để lồng cấu trúc.
    /// </summary>
    public class Dm_HangHoaThiTruongTreeDto
    {
        public Guid Id { get; set; }
        public string Ma { get; set; } = null!;
        public string Ten { get; set; } = null!;
        public string? GhiChu { get; set; }
        public string? DacTinh { get; set; }
        public Guid? DonViTinhId { get; set; }
        public string? DonViTinhTen { get; set; }
        public int Depth { get; set; }
        public DateTime NgayHieuLuc { get; set; }
        public DateTime NgayHetHieuLuc { get; set; }
        public List<Dm_HangHoaThiTruongTreeDto> Children { get; set; } = new List<Dm_HangHoaThiTruongTreeDto>();
    }
}
