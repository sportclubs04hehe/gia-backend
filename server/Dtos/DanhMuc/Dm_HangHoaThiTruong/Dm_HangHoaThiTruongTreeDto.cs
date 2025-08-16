namespace server.Dtos.DanhMuc.Dm_HangHoaThiTruongDto
{
    /// <summary>
    /// Dùng cho tree view (lazy loading hoặc full tree), có Children để lồng cấu trúc.
    /// </summary>
    public class Dm_HangHoaThiTruongTreeDto : BaseDto
    {
        public string Ma { get; set; } = string.Empty;
        public string Ten { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public string? DacTinh { get; set; }
        public Guid? DonViTinhId { get; set; }
        public DateTime NgayHieuLuc { get; set; }
        public DateTime NgayHetHieuLuc { get; set; }
        public bool IsParent { get; set; } = false;
        public bool HasChildren { get; set; }
        public Guid? ParentId { get; set; }
        public int Level { get; set; }
        public string? DonViTinhTen { get; set; }
        public List<Dm_HangHoaThiTruongTreeDto> Children { get; set; } = new();
    }
}
