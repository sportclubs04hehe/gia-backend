using server.Dtos.Common;
using System.Data;

namespace server.Repository.IDanhMuc.IDm_HangHoaThiTruong
{
    public interface IDm_HangHoaThiTruongImportExcel
    {
        Task<ImportResultDto> ImportFromExcelAsync(IFormFile file,
            string createdBy, IDbTransaction? transaction = null);
        Task<List<ImportErrorDto>> ValidateExcelAsync(IFormFile file);
    }
}
