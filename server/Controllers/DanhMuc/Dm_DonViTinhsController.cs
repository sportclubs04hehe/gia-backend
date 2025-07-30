using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using server.Dtos.Common;
using server.Dtos.DanhMuc.Dm_DonViTinh;
using server.Service;

namespace server.Controllers.DanhMuc
{
    [Route("api/[controller]")]
    [ApiController]
    public class Dm_DonViTinhsController : ControllerBase
    {
        private readonly IDm_DonViTinhService _service;
        private readonly ILogger<Dm_DonViTinhsController> _logger;

        public Dm_DonViTinhsController(IDm_DonViTinhService service, ILogger<Dm_DonViTinhsController> logger)
        {
            _service = service;
            _logger = logger;
        }
        
        // Lấy danh sách đơn vị tính phân trang
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<Dm_DonViTinhDto>>> GetPaged([FromQuery] PagedRequest request)
        {
            try
            {
                var result = await _service.GetPagedAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // Lấy tất cả đơn vị tính
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Dm_DonViTinhDto>>> GetAll()
        {
            try
            {
                var result = await _service.GetAllAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // Lấy đơn vị tính theo Id
        [HttpGet("{id}")]
        public async Task<ActionResult<Dm_DonViTinhDto>> GetById(Guid id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                if (result == null)
                    return NotFound("Không tìm thấy đơn vị tính");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // Kiểm tra mã đơn vị tính có tồn tại không
        [HttpGet("check-code/{ma}")]
        public async Task<ActionResult<bool>> CheckCodeExists(string ma, [FromQuery] Guid? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ma))
                    return BadRequest("Mã không được để trống");

                var exists = await _service.IsCodeExistsAsync(ma, excludeId);
                return Ok(new { exists = exists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // Tạo mới đơn vị tính
        [HttpPost]
        public async Task<ActionResult<Dm_DonViTinhDto>> Create(DmDonViTinhCreateDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // Cập nhật đơn vị tính
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, DmDonViTinhUpdateDto updateDto)
        {
            try
            {
                if (id != updateDto.Id)
                    return BadRequest("Id không khớp");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.UpdateAsync(updateDto);
                if (!result)
                    return NotFound("Không tìm thấy đơn vị tính để cập nhật");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // Xóa đơn vị tính
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);
                if (!result)
                    return NotFound("Không tìm thấy đơn vị tính để xóa");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        /// <summary>
        /// Import danh sách đơn vị tính từ file Excel (.xlsx, .xls)
        /// </summary>
        /// <param name="file">Excel file (.xlsx, .xls) with required headers: Mã, Tên, Ghi chú</param>
        /// <returns>Import result with success count and errors</returns>
        [HttpPost("import")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ImportResultDto>> ImportFromExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");
                
                var result = await _service.ImportFromExcelAsync(file);
                
                // Return 400 if there were any errors, but still include the result
                if (result.ErrorCount > 0)
                {
                    _logger.LogWarning("Import completed with {ErrorCount} errors", result.ErrorCount);
                    return BadRequest(result);
                }
                    
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Excel import");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}
