using AutoMapper;
using Microsoft.Extensions.Logging;
using server.Dtos.Common;
using server.Dtos.DanhMuc.Dm_HangHoaThiTruongDto;
using server.Models.DanhMuc;
using server.Repository.UnitOfWork;

namespace server.Service.Impl
{
    public class Dm_HangHoaThiTruongService : IDm_HangHoaThiTruongService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<Dm_HangHoaThiTruongService> _logger;

        public Dm_HangHoaThiTruongService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<Dm_HangHoaThiTruongService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<Dm_HangHoaThiTruongDto>> GetTopLevelItemsAsync()
        {
                // Get data from repository
                var items = await _unitOfWork.HangHoaThiTruong.GetTopLevelItemsAsync();

                // Map to DTOs
                var result = _mapper.Map<IEnumerable<Dm_HangHoaThiTruongDto>>(items);

                return result;
        }

        public async Task<PagedResult<Dm_HangHoaThiTruongDto>> GetChildrenAsync(
            Guid parentId,
            PagedRequest request,
            string searchTerm = null)
        {
                // Get data from repository
                var result = await _unitOfWork.HangHoaThiTruong.GetChildrenAsync(
                    parentId,
                    request,
                    searchTerm);

                // Map to DTOs
                var items = _mapper.Map<IEnumerable<Dm_HangHoaThiTruongDto>>(result.Items);

                // Return paged result with mapped items
                return new PagedResult<Dm_HangHoaThiTruongDto>
                {
                    Items = items,
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
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

                // Bắt đầu transaction
                await _unitOfWork.BeginTransactionAsync();
                var transaction = _unitOfWork.CurrentTransaction;

                // Kiểm tra mã trùng lặp cùng cấp sử dụng UnitOfWork
                bool isCodeExists = await _unitOfWork.HangHoaThiTruongValidation
                    .IsCodeExistsAtSameLevelAsync(createDto.Ma, createDto.ParentId);
                
                if (isCodeExists)
                {
                    await _unitOfWork.RollbackAsync();
                    throw new ArgumentException($"Mã '{createDto.Ma}' đã tồn tại ở cùng cấp độ");
                }

                // Map từ DTO sang entity
                var entity = _mapper.Map<Dm_HangHoaThiTruong>(createDto);
                entity.IsDelete = false;
                
                // Truyền transaction hiện tại vào phương thức AddAsync
                var result = await _unitOfWork.HangHoaThiTruong.AddAsync(entity, createDto.ParentId, transaction);
                
                // Commit transaction nếu mọi thứ thành công
                await _unitOfWork.CommitAsync();
                
                // Map kết quả sang DTO để trả về
                return _mapper.Map<Dm_HangHoaThiTruongDto>(result);
            }
            catch (Exception ex)
            {
                // Đảm bảo rollback transaction nếu có lỗi
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi thêm mới hàng hóa thị trường");
                throw;
            }
        }

        public async Task<Dm_HangHoaThiTruongDto?> GetByIdAsync(Guid id)
        {
                // Gọi repository thông qua UnitOfWork để lấy entity theo ID
                var entity = await _unitOfWork.HangHoaThiTruong.GetByIdAsync(id);

                // Nếu không tìm thấy, trả về null
                if (entity == null)
                    return null;

                // Map entity sang DTO và trả về
                return _mapper.Map<Dm_HangHoaThiTruongDto>(entity);
        }

        public async Task<Dm_HangHoaThiTruongDto> UpdateAsync(DmHangHoaThiTruongUpdateDto updateDto)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(updateDto.Ma) || string.IsNullOrWhiteSpace(updateDto.Ten))
                {
                    throw new ArgumentException("Mã và tên không được để trống");
                }

                // Kiểm tra sự tồn tại của bản ghi
                var existingEntity = await _unitOfWork.HangHoaThiTruong.GetByIdAsync(updateDto.Id);
                if (existingEntity == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy hàng hóa thị trường với ID: {updateDto.Id}");
                }

                // Bắt đầu transaction
                await _unitOfWork.BeginTransactionAsync();
                var transaction = _unitOfWork.CurrentTransaction;

                // Kiểm tra mã trùng lặp cùng cấp, sử dụng phương thức IsCodeExistsAtSameLevelAsync thay vì IsCodeExistsForUpdateAsync
                // Truyền ID hiện tại vào tham số excludeId để loại trừ chính bản ghi đang cập nhật
                bool isCodeExists = await _unitOfWork.HangHoaThiTruongValidation
                    .IsCodeExistsAtSameLevelAsync(updateDto.Ma, updateDto.ParentId, updateDto.Id);
                
                if (isCodeExists)
                {
                    await _unitOfWork.RollbackAsync();
                    throw new ArgumentException($"Mã '{updateDto.Ma}' đã tồn tại ở cùng cấp độ");
                }

                // Chuyển đổi từ DTO sang entity, giữ nguyên một số trường
                var entity = _mapper.Map<Dm_HangHoaThiTruong>(updateDto);
                entity.IsDelete = false;
                entity.CreatedBy = existingEntity.CreatedBy;
                entity.CreatedDate = existingEntity.CreatedDate;
                
                // Cập nhật entity và xử lý thay đổi parent nếu có
                var result = await _unitOfWork.HangHoaThiTruong.UpdateAsync(entity, updateDto.ParentId, transaction);
                
                // Commit transaction nếu mọi thao tác thành công
                await _unitOfWork.CommitAsync();
                
                // Chuyển đổi kết quả về DTO
                return _mapper.Map<Dm_HangHoaThiTruongDto>(result);
            }
            catch (Exception ex)
            {
                // Đảm bảo rollback transaction khi xảy ra lỗi
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi cập nhật hàng hóa thị trường: {Message}", ex.Message);
                throw;
            }
        }
    }
}
