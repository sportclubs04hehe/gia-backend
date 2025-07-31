using AutoMapper;
using server.Dtos.Common;
using server.Dtos.DanhMuc.Dm_DonViTinh;
using server.Models.DanhMuc;
using server.Repository.IDanhMuc.IDm_DonViTinh;

namespace server.Service
{
    public class Dm_DonViTinhService : IDm_DonViTinhService
    {
        private readonly IDm_DonViTinhRepository _repository;
        private readonly IDm_DonViTinhImportExcel _importRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<Dm_DonViTinhService> _logger;

        public Dm_DonViTinhService(
            IDm_DonViTinhRepository repository, 
            IDm_DonViTinhImportExcel importRepository,
            IMapper mapper,
            ILogger<Dm_DonViTinhService> logger)
        {
            _repository = repository;
            _importRepository = importRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<Dm_DonViTinhDto>> GetPagedAsync(PagedRequest request)
        {
            var result = await _repository.GetPagedAsync(request);
            
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
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<Dm_DonViTinhDto>>(entities);
        }

        public async Task<Dm_DonViTinhDto?> GetByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<Dm_DonViTinhDto>(entity);
        }

        public async Task<bool> IsCodeExistsAsync(string ma, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(ma))
                return false;

            return await _repository.IsCodeExistsAsync(ma.Trim(), excludeId);
        }
        public async Task<Dm_DonViTinhDto> CreateAsync(DmDonViTinhCreateDto createDto)
        {
            var entity = _mapper.Map<Dm_DonViTinh>(createDto);
            var createdEntity = await _repository.CreateAsync(entity);
            return _mapper.Map<Dm_DonViTinhDto>(createdEntity);
        }

        public async Task<bool> UpdateAsync(DmDonViTinhUpdateDto updateDto)
        {
            var entity = _mapper.Map<Dm_DonViTinh>(updateDto);
            return await _repository.UpdateAsync(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<ImportResultDto> ImportFromExcelAsync(IFormFile file)
        {
            _logger.LogInformation("Starting Excel import process for file {FileName}", file.FileName);
            var startTime = DateTime.Now;
            
            var result = await _importRepository.ImportFromExcelAsync(file);
            
            var duration = DateTime.Now - startTime;
            _logger.LogInformation(
                "Excel import completed. Total: {Total}, Success: {Success}, Errors: {Errors}, Duration: {Duration} seconds",
                result.TotalRecords, result.SuccessCount, result.ErrorCount, duration.TotalSeconds);
                
            return result;
        }

        public async Task<IEnumerable<Dm_DonViTinhDto>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<Dm_DonViTinhDto>();

            var entities = await _repository.SearchAsync(searchTerm);
            return _mapper.Map<IEnumerable<Dm_DonViTinhDto>>(entities);
        }

    }
}
