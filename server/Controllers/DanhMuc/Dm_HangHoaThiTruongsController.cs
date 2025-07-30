using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using server.Models.DanhMuc;
using server.Dtos.DanhMuc;
using server.Repository.IDanhMuc;

namespace server.Controllers.DanhMuc
{
    [Route("api/[controller]")]
    [ApiController]
    public class Dm_HangHoaThiTruongsController : ControllerBase
    {
        private readonly IDm_HangHoaThiTruongRepository _repository;

        public Dm_HangHoaThiTruongsController(IDm_HangHoaThiTruongRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Dm_HangHoaThiTruongDto>>> GetAll()
        {
            try
            {
                var hangHoas = await _repository.GetAllUsersAsync();
                var hangHoaDtos = hangHoas.Select(h => new Dm_HangHoaThiTruongDto
                {
                    Id = h.Id,
                    MaHangHoa = h.MaHangHoa,
                    TenHangHoa = h.TenHangHoa
                });

                return Ok(hangHoaDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Dm_HangHoaThiTruongDto>> GetById(int id)
        {
            try
            {
                var hangHoa = await _repository.GetUserByIdAsync(id);
                if (hangHoa == null)
                {
                    return NotFound($"Hàng hóa với ID {id} không tồn tại");
                }

                var hangHoaDto = new Dm_HangHoaThiTruongDto
                {
                    Id = hangHoa.Id,
                    MaHangHoa = hangHoa.MaHangHoa,
                    TenHangHoa = hangHoa.TenHangHoa
                };

                return Ok(hangHoaDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Dm_HangHoaThiTruongDto>> Create([FromBody] Dm_HangHoaThiTruongDto hangHoaDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var hangHoa = new Dm_HangHoaThiTruong
                {
                    MaHangHoa = hangHoaDto.MaHangHoa,
                    TenHangHoa = hangHoaDto.TenHangHoa
                };

                await _repository.AddUserAsync(hangHoa);

                return CreatedAtAction(nameof(GetById), new { id = hangHoa.Id }, hangHoaDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
