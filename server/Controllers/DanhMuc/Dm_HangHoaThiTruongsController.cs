using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using server.Dtos.DanhMuc.Dm_HangHoaThiTruong;
using server.Service;

namespace server.Controllers.DanhMuc
{
    [Route("api/[controller]")]
    [ApiController]
    public class Dm_HangHoaThiTruongsController : ControllerBase
    {
        private readonly IDm_HangHoaThiTruongService _service;
        private readonly ILogger<Dm_HangHoaThiTruongsController> _logger;

        public Dm_HangHoaThiTruongsController(
            IDm_HangHoaThiTruongService service,
            ILogger<Dm_HangHoaThiTruongsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Thêm mới một hàng hóa thị trường
        /// </summary>
        /// <param name="createDto">Thông tin hàng hóa thị trường cần thêm mới</param>
        /// <returns>Thông tin hàng hóa thị trường sau khi thêm mới</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DmHangHoaThiTruongCreateDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Lỗi dữ liệu đầu vào");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm mới hàng hóa thị trường");
                return StatusCode(500, "Đã xảy ra lỗi khi xử lý yêu cầu");
            }
        }

        // Lấy thông tin hàng hóa thị trường theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                // Gọi service để lấy thông tin theo ID
                var result = await _service.GetByIdAsync(id);
                
                // Nếu không tìm thấy, trả về 404 Not Found
                if (result == null)
                    return NotFound($"Không tìm thấy hàng hóa thị trường với ID: {id}");
                    
                // Nếu tìm thấy, trả về 200 OK với thông tin chi tiết
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy thông tin hàng hóa thị trường với ID: {id}");
                return StatusCode(500, "Đã xảy ra lỗi khi xử lý yêu cầu");
            }
        }
    }
}
