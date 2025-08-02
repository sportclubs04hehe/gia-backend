using Microsoft.Extensions.Logging;
using server.Repository.DanhMucImpl.Dm_DonViTinhImpl;
using server.Repository.DanhMucImpl.Dm_HangHoaThiTruongImpl;
using server.Repository.IDanhMuc.IDm_DonViTinh;
using server.Repository.IDanhMuc.IDm_HangHoaThiTruong;
using server.Repository.UnitOfWork;
using System.Data;

namespace server.Repository.Implement
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbConnection _connection;
        private IDbTransaction? _transaction;
        private readonly ILogger<UnitOfWork> _logger;
        private readonly ILoggerFactory _loggerFactory; 
        private bool _disposed;

        // Repository instances - using lazy initialization to avoid circular dependencies
        private Lazy<IDm_HangHoaThiTruongRepository> _hangHoaThiTruongRepository;
        private Lazy<IDm_HangHoaThiTruongHierarchyRepository> _hangHoaThiTruongHierarchyRepository;
        private Lazy<IDm_HangHoaThiTruongValidationRepository> _hangHoaThiTruongValidationRepository;
        private Lazy<IDm_DonViTinhRepository> _donViTinhRepository;
        private Lazy<IDm_DonViTinhImportExcel> _donViTinhImportRepository;

        public UnitOfWork(
            IDbConnection connection,
            ILoggerFactory loggerFactory)
        {
            _connection = connection;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<UnitOfWork>();

            // Initialize lazy repositories
            _hangHoaThiTruongHierarchyRepository = new Lazy<IDm_HangHoaThiTruongHierarchyRepository>(() => 
                new Dm_HangHoaThiTruongHierarchyRepository(
                    _connection,
                    _loggerFactory.CreateLogger<Dm_HangHoaThiTruongHierarchyRepository>()));
                    
            _hangHoaThiTruongValidationRepository = new Lazy<IDm_HangHoaThiTruongValidationRepository>(() => 
                new Dm_HangHoaThiTruongValidationRepository(
                    _connection,
                    _loggerFactory.CreateLogger<Dm_HangHoaThiTruongValidationRepository>()));
                    
            _hangHoaThiTruongRepository = new Lazy<IDm_HangHoaThiTruongRepository>(() => 
                new Dm_HangHoaThiTruongRepository(
                    _connection,
                    _loggerFactory.CreateLogger<Dm_HangHoaThiTruongRepository>(),
                    HangHoaThiTruongHierarchy));

            _donViTinhRepository = new Lazy<IDm_DonViTinhRepository>(() =>
                new Dm_DonViTinhRepository(
                    _connection,
                    _loggerFactory.CreateLogger<Dm_DonViTinhRepository>()));
                    
            _donViTinhImportRepository = new Lazy<IDm_DonViTinhImportExcel>(() =>
                new Dm_DonViTinhImportExcelImpl(
                    _connection,
                    _loggerFactory.CreateLogger<Dm_DonViTinhImportExcelImpl>()));
        }

        // Repository properties with lazy initialization
        public IDm_HangHoaThiTruongRepository HangHoaThiTruong => _hangHoaThiTruongRepository.Value;
        public IDm_HangHoaThiTruongHierarchyRepository HangHoaThiTruongHierarchy => _hangHoaThiTruongHierarchyRepository.Value;
        public IDm_HangHoaThiTruongValidationRepository HangHoaThiTruongValidation => _hangHoaThiTruongValidationRepository.Value;
        public IDm_DonViTinhRepository DonViTinh => _donViTinhRepository.Value;
        public IDm_DonViTinhImportExcel DonViTinhImport => _donViTinhImportRepository.Value;

        // Transaction management
        public async Task<IDbTransaction> BeginTransactionAsync()
        {
            if (_connection.State != ConnectionState.Open)
            {
                if (_connection is System.Data.Common.DbConnection dbConnection)
                {
                    await dbConnection.OpenAsync();
                }
                else
                {
                    _connection.Open();
                }
            }
            
            _transaction = _connection.BeginTransaction();
            _logger.LogInformation("Bắt đầu giao dịch mới");
            return _transaction;
        }

        public async Task CommitAsync()
        {
            // Kiểm tra transaction có tồn tại không
            if (_transaction == null)
            {
                _logger.LogWarning("Không có giao dịch nào đang hoạt động để commit");
                return;
            }

            try
            {
                _transaction.Commit();
                _logger.LogInformation("Giao dịch đã được commit thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi commit giao dịch");
                await RollbackAsync();
                throw;
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public async Task RollbackAsync()
        {
            // Kiểm tra transaction có tồn tại không
            if (_transaction == null)
            {
                _logger.LogWarning("Không có giao dịch nào đang hoạt động để rollback");
                return;
            }

            // Đơn giản hóa - không cần try-catch vì rollback hiếm khi phát sinh lỗi
            _transaction.Rollback();
            _logger.LogInformation("Đã rollback giao dịch");
            
            _transaction.Dispose();
            _transaction = null;
            
            await Task.CompletedTask;
        }

        // IDisposable implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _transaction?.Dispose();
                // Don't dispose the connection here as it might be managed by the DI container
            }

            _disposed = true;
        }
        
        // IAsyncDisposable implementation
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            
            Dispose(false);
            GC.SuppressFinalize(this);
        }
        
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_disposed)
                return;
                
            if (_transaction != null)
            {
                if (_transaction is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else
                {
                    _transaction.Dispose();
                }
                
                _transaction = null;
            }
            
            // Connection is managed by DI container, don't dispose
            
            await Task.CompletedTask;
        }

        public IDbTransaction? CurrentTransaction => _transaction;
    }
}