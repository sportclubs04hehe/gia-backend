using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using server.Dtos.Common;
using server.Dtos.DanhMuc.Dm_HangHoaThiTruongDto;
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

        [HttpGet("top-level")]
        public async Task<IActionResult> GetTopLevelItems()
        {
            try
            {
                // Call service method
                var result = await _service.GetTopLevelItemsAsync();

                // Return result
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách mặt hàng cấp cao nhất");
                return StatusCode(500, "Đã xảy ra lỗi khi xử lý yêu cầu");
            }
        }

        /// <summary>
        /// Lấy danh sách các hàng hóa con trực tiếp của một hàng hóa cha
        /// </summary>
        /// <param name="parentId">ID của hàng hóa cha</param>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="sortBy">Sắp xếp theo trường (mặc định: CreatedDate)</param>
        /// <param name="sortDescending">Sắp xếp giảm dần (mặc định: false)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm (tùy chọn)</param>
        /// <returns>Danh sách các hàng hóa con với phân trang</returns>
        [HttpGet("children/{parentId}")]
        public async Task<IActionResult> GetChildren(
            Guid parentId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "CreatedDate",
            [FromQuery] bool sortDescending = false,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                // Create paging request
                var request = new PagedRequest
                {
                    PageNumber = pageNumber > 0 ? pageNumber : 1,
                    PageSize = pageSize > 0 ? pageSize : 10,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                // Call service method
                var result = await _service.GetChildrenAsync(parentId, request, searchTerm);

                // Return paged result
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh sách con của hàng hóa thị trường với ID: {parentId}");
                return StatusCode(500, "Đã xảy ra lỗi khi xử lý yêu cầu");
            }
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
