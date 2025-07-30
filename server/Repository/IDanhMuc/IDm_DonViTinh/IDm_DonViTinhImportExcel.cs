using Microsoft.AspNetCore.Http;
using server.Dtos.Common;
using server.Dtos.DanhMuc.Dm_DonViTinh;
using server.Models.DanhMuc;

namespace server.Repository.IDanhMuc.IDm_DonViTinh
{
    public interface IDm_DonViTinhImportExcel
    {
        /// <summary>
        /// Import data from Excel file
        /// </summary>
        /// <param name="file">Excel file</param>
        /// <returns>Import result with success count and errors</returns>
        Task<ImportResultDto> ImportFromExcelAsync(IFormFile file);
        
        /// <summary>
        /// Validate imported data against database
        /// </summary>
        /// <param name="importData">List of data to validate</param>
        /// <returns>List of validation errors by row</returns>
        Task<List<ImportErrorDto>> ValidateBatchAsync(List<DmDonViTinhImportDto> importData, int startRow);
        
        /// <summary>
        /// Save valid imported data to database
        /// </summary>
        /// <param name="validData">List of validated data to save</param>
        /// <returns>Number of records saved</returns>
        Task<int> SaveBatchAsync(List<Dm_DonViTinh> validData);
    }
}
