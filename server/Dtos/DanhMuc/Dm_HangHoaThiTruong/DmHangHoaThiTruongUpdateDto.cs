namespace server.Dtos.DanhMuc.Dm_HangHoaThiTruong
{
    public class DmHangHoaThiTruongUpdateDto
    {
        public Guid Id { get; set; }
        public string Ma { get; set; }
        public string Ten { get; set; }
        public string? GhiChu { get; set; }
        public string? DacTinh { get; set; }
        public Guid? DonViTinhId { get; set; }
        public DateTime NgayHieuLuc { get; set; }
        public DateTime NgayHetHieuLuc { get; set; }
        public Guid? ParentId { get; set; } // Cho phép thay đổi node cha
    }
}
