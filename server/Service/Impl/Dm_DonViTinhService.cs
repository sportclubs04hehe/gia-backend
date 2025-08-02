using AutoMapper;
using server.Dtos.Common;
using server.Dtos.DanhMuc.Dm_DonViTinh;
using server.Models.DanhMuc;
using server.Repository.UnitOfWork;

namespace server.Service
{
    public class Dm_DonViTinhService : IDm_DonViTinhService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<Dm_DonViTinhService> _logger;

        public Dm_DonViTinhService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<Dm_DonViTinhService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<Dm_DonViTinhDto>> GetPagedAsync(PagedRequest request)
        {
            var result = await _unitOfWork.DonViTinh.GetPagedAsync(request);
            
            return new PagedResult<Dm_DonViTinhDto>
            {
                Items = _mapper.Map<IEnumerable<Dm_DonViTinhDto>>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }

        public async Task<IEnumerable<Dm_DonViTinhDto>> GetAllAsync()
        {
            var entities = await _unitOfWork.DonViTinh.GetAllAsync();
            return _mapper.Map<IEnumerable<Dm_DonViTinhDto>>(entities);
        }

        public async Task<Dm_DonViTinhDto?> GetByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.DonViTinh.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<Dm_DonViTinhDto>(entity);
        }

        public async Task<bool> IsCodeExistsAsync(string ma, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(ma))
                return false;

            return await _unitOfWork.DonViTinh.IsCodeExistsAsync(ma.Trim(), excludeId);
        }
        
        public async Task<Dm_DonViTinhDto> CreateAsync(DmDonViTinhCreateDto createDto)
        {
            try
            {
                // Bắt đầu transaction
                await _unitOfWork.BeginTransactionAsync();
                var transaction = _unitOfWork.CurrentTransaction;
                
                // Chuyển đổi DTO sang entity
                var entity = _mapper.Map<Dm_DonViTinh>(createDto);
                
                // Thêm mới entity, truyền transaction vào
                var createdEntity = await _unitOfWork.DonViTinh.CreateAsync(entity, transaction);
                
                // Commit transaction nếu mọi thứ thành công
                await _unitOfWork.CommitAsync();
                
                return _mapper.Map<Dm_DonViTinhDto>(createdEntity);
            }
            catch (Exception ex)
            {
                // Đảm bảo rollback transaction nếu có lỗi
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi thêm mới đơn vị tính");
                throw;
            }
        }

        public async Task<bool> UpdateAsync(DmDonViTinhUpdateDto updateDto)
        {
            try
            {
                // Bắt đầu transaction
                await _unitOfWork.BeginTransactionAsync();
                var transaction = _unitOfWork.CurrentTransaction;
                
                // Chuyển đổi DTO sang entity
                var entity = _mapper.Map<Dm_DonViTinh>(updateDto);
                
                // Cập nhật entity, truyền transaction vào
                var result = await _unitOfWork.DonViTinh.UpdateAsync(entity, transaction);
                
                if (!result)
                {
                    await _unitOfWork.RollbackAsync();
                    return false;
                }
                
                // Commit transaction nếu mọi thứ thành công
                await _unitOfWork.CommitAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                // Đảm bảo rollback transaction nếu có lỗi
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi cập nhật đơn vị tính");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                // Bắt đầu transaction
                await _unitOfWork.BeginTransactionAsync();
                var transaction = _unitOfWork.CurrentTransaction;
                
                // Xóa entity, truyền transaction vào
                var result = await _unitOfWork.DonViTinh.DeleteAsync(id, transaction);
                
                if (!result)
                {
                    await _unitOfWork.RollbackAsync();
                    return false;
                }
                
                // Commit transaction nếu mọi thứ thành công
                await _unitOfWork.CommitAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                // Đảm bảo rollback transaction nếu có lỗi
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xóa đơn vị tính");
                throw;
            }
        }

        public async Task<ImportResultDto> ImportFromExcelAsync(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Starting Excel import process for file {FileName}", file.FileName);
                var startTime = DateTime.Now;
                
                // Bắt đầu transaction
                await _unitOfWork.BeginTransactionAsync();
                var transaction = _unitOfWork.CurrentTransaction;
                
                // Import dữ liệu từ Excel, truyền transaction vào
                var result = await _unitOfWork.DonViTinhImport.ImportFromExcelAsync(file, transaction);
                
                if (result.ErrorCount > 0)
                {
                    await _unitOfWork.RollbackAsync();
                }
                else
                {
                    await _unitOfWork.CommitAsync();
                }
                
                var duration = DateTime.Now - startTime;
                _logger.LogInformation(
                    "Excel import completed. Total: {Total}, Success: {Success}, Errors: {Errors}, Duration: {Duration} seconds",
                    result.TotalRecords, result.SuccessCount, result.ErrorCount, duration.TotalSeconds);
                    
                return result;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi import Excel");
                throw;
            }
        }

        public async Task<PagedResult<Dm_DonViTinhDto>> SearchAsync(string searchTerm, int pageNumber = 1, int pageSize = 50)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new PagedResult<Dm_DonViTinhDto>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

            var result = await _unitOfWork.DonViTinh.SearchAsync(searchTerm, pageNumber, pageSize);
            
            return new PagedResult<Dm_DonViTinhDto>
            {
                Items = _mapper.Map<IEnumerable<Dm_DonViTinhDto>>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }
    }
}
