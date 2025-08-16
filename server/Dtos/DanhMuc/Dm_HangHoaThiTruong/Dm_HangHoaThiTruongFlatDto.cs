namespace server.Dtos.DanhMuc.Dm_HangHoaThiTruongDto
{
    // Dùng cho báo cáo dạng phẳng, không cần Children
    public class Dm_HangHoaThiTruongFlatDto : BaseDto
    {
        public string Ma { get; set; }
        public string Ten { get; set; }
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
    }
}
