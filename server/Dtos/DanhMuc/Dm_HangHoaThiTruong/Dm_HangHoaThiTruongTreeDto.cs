namespace server.Dtos.DanhMuc.Dm_HangHoaThiTruongDto
{
    /// <summary>
    /// Dùng cho tree view (lazy loading hoặc full tree), có Children để lồng cấu trúc.
    /// </summary>
    public class Dm_HangHoaThiTruongTreeDto
    {
        public Guid Id { get; set; }
        public string Ma { get; set; } = string.Empty;
        public string Ten { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public DateTime NgayHieuLuc { get; set; }
        public DateTime NgayHetHieuLuc { get; set; }
        public bool HasChildren { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public List<Dm_HangHoaThiTruongTreeDto> Children { get; set; } = new List<Dm_HangHoaThiTruongTreeDto>();
    }
}
