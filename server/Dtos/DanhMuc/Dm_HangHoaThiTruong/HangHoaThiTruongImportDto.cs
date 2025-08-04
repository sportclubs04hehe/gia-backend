using System;

namespace server.Dtos.DanhMuc.Dm_HangHoaThiTruong
{
    public class HangHoaThiTruongImportDto
    {
        public string Ma { get; set; } = string.Empty;
        public string Ten { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
        public string? DonViTinh { get; set; }
        public string? GhiChu { get; set; }
        public string? DacTinh { get; set; }
        public DateTime? NgayHieuLuc { get; set; }
        public DateTime? NgayHetHieuLuc { get; set; }
        public int RowIndex { get; set; } // Dùng để lưu chỉ số dòng trong file import
    }
}
