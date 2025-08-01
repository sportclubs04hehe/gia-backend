using AutoMapper;
using Microsoft.Extensions.Logging;
using server.Dtos.DanhMuc.Dm_HangHoaThiTruong;
using server.Models.DanhMuc;
using server.Repository.IDanhMuc.IDm_HangHoaThiTruong;

namespace server.Service.Impl
{
    public class Dm_HangHoaThiTruongService : IDm_HangHoaThiTruongService
    {
        private readonly IDm_HangHoaThiTruongRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<Dm_HangHoaThiTruongService> _logger;

        public Dm_HangHoaThiTruongService(
            IDm_HangHoaThiTruongRepository repository,
            IMapper mapper,
            ILogger<Dm_HangHoaThiTruongService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Dm_HangHoaThiTruongDto> CreateAsync(DmHangHoaThiTruongCreateDto createDto)
        {
            try
            {
                // Kiểm tra dữ liệu
                if (string.IsNullOrWhiteSpace(createDto.Ma) || string.IsNullOrWhiteSpace(createDto.Ten))
                {
                    throw new ArgumentException("Mã và tên không được để trống");
                }

                // Map từ DTO sang entity
                var entity = _mapper.Map<Dm_HangHoaThiTruong>(createDto);
                
                // Thêm mới và lấy kết quả
                var result = await _repository.AddAsync(entity, createDto.ParentId);
                
                // Map kết quả sang DTO để trả về
                return _mapper.Map<Dm_HangHoaThiTruongDto>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm mới hàng hóa thị trường");
                throw;
            }
        }

        public async Task<Dm_HangHoaThiTruongDto?> GetByIdAsync(Guid id)
        {
            try
            {
                // Gọi repository để lấy entity theo ID
                var entity = await _repository.GetByIdAsync(id);

                // Nếu không tìm thấy, trả về null
                if (entity == null)
                    return null;

                // Map entity sang DTO và trả về
                return _mapper.Map<Dm_HangHoaThiTruongDto>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy thông tin hàng hóa thị trường với ID: {id}");
                throw;
            }
        }
    }
}
