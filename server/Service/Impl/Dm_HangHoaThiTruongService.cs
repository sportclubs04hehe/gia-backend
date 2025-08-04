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

        // Xóa một hàng hóa và tất cả các con của nó
        public async Task<DeleteResult> DeleteAsync(Guid id)
        {
            try
            {
                // Kiểm tra sự tồn tại của bản ghi
                var entity = await _unitOfWork.HangHoaThiTruong.GetByIdAsync(id);
                if (entity == null)
                {
                    return new DeleteResult
                    {
                        Success = false,
                        Message = $"Không tìm thấy hàng hóa thị trường với ID: {id}"
                    };
                }

                // Kiểm tra xem hàng hóa có đang được tham chiếu không
                bool isReferenced = await _unitOfWork.HangHoaThiTruong.IsReferencedAsync(id);
                if (isReferenced)
                {
                    return new DeleteResult
                    {
                        Success = false,
                        Message = $"Hàng hóa '{entity.Ten}' đang được sử dụng và không thể xóa"
                    };
                }

                // Đếm số lượng node con sẽ bị xóa
                int descendantCount = await _unitOfWork.HangHoaThiTruong.CountDescendantsAsync(id);

                // Bắt đầu transaction
                await _unitOfWork.BeginTransactionAsync();
                var transaction = _unitOfWork.CurrentTransaction;

                // Thực hiện xóa mềm
                var affectedRows = await _unitOfWork.HangHoaThiTruong.DeleteAsync(id, transaction);

                // Commit transaction nếu mọi thao tác thành công
                await _unitOfWork.CommitAsync();

                // Tạo thông báo kết quả
                string message;
                if (descendantCount > 0)
                {
                    message = $"Đã xóa hàng hóa '{entity.Ten}' và {descendantCount} hàng hóa con";
                }
                else
                {
                    message = $"Đã xóa hàng hóa '{entity.Ten}'";
                }

                return new DeleteResult
                {
                    Success = true,
                    Message = message,
                    AffectedRecords = affectedRows
                };
            }
            catch (Exception ex)
            {
                // Đảm bảo rollback transaction khi xảy ra lỗi
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xóa hàng hóa thị trường: {Message}", ex.Message);
                
                return new DeleteResult
                {
                    Success = false,
                    Message = $"Lỗi khi xóa hàng hóa thị trường: {ex.Message}"
                };
            }
        }

        // Xóa nhiều hàng hóa và tất cả các con của chúng
        public async Task<DeleteResult> DeleteManyAsync(IEnumerable<Guid> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return new DeleteResult
                    {
                        Success = false,
                        Message = "Danh sách ID cần xóa không được để trống"
                    };
                }

                // Tạo danh sách các ID hợp lệ
                var validIds = new List<Guid>();
                var referencedItems = new List<string>();
                var notFoundIds = new List<Guid>();

                // Kiểm tra từng ID một
                foreach (var id in ids)
                {
                    var entity = await _unitOfWork.HangHoaThiTruong.GetByIdAsync(id);
                    if (entity == null)
                    {
                        notFoundIds.Add(id);
                        continue;
                    }

                    // Kiểm tra xem hàng hóa có đang được tham chiếu không
                    bool isReferenced = await _unitOfWork.HangHoaThiTruong.IsReferencedAsync(id);
                    if (isReferenced)
                    {
                        referencedItems.Add($"{entity.Ma} - {entity.Ten}");
                        continue;
                    }

                    validIds.Add(id);
                }

                // Nếu không có ID hợp lệ nào
                if (!validIds.Any())
                {
                    string message = "Không thể xóa các hàng hóa đã chọn.";
                    if (notFoundIds.Any())
                    {
                        message += $" {notFoundIds.Count} hàng hóa không tìm thấy.";
                    }
                    if (referencedItems.Any())
                    {
                        message += $" {referencedItems.Count} hàng hóa đang được sử dụng: {string.Join(", ", referencedItems)}.";
                    }

                    return new DeleteResult
                    {
                        Success = false,
                        Message = message
                    };
                }

                // Bắt đầu transaction
                await _unitOfWork.BeginTransactionAsync();
                var transaction = _unitOfWork.CurrentTransaction;

                // Thực hiện xóa mềm
                var affectedRows = await _unitOfWork.HangHoaThiTruong.DeleteManyAsync(validIds, transaction);

                // Commit transaction nếu mọi thao tác thành công
                await _unitOfWork.CommitAsync();

                // Tạo thông báo kết quả
                string resultMessage = $"Đã xóa {validIds.Count} hàng hóa và các hàng hóa con của chúng.";
                
                if (notFoundIds.Any())
                {
                    resultMessage += $" {notFoundIds.Count} hàng hóa không tìm thấy.";
                }
                
                if (referencedItems.Any())
                {
                    resultMessage += $" {referencedItems.Count} hàng hóa không thể xóa do đang được sử dụng.";
                }

                return new DeleteResult
                {
                    Success = true,
                    Message = resultMessage,
                    AffectedRecords = affectedRows
                };
            }
            catch (Exception ex)
            {
                // Đảm bảo rollback transaction khi xảy ra lỗi
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xóa nhiều hàng hóa thị trường: {Message}", ex.Message);
                
                return new DeleteResult
                {
                    Success = false,
                    Message = $"Lỗi khi xóa nhiều hàng hóa thị trường: {ex.Message}"
                };
            }
        }
    }
}
