namespace server.Repository.IDanhMuc.IDm_HangHoaThiTruong
{
    public interface IDm_HangHoaThiTruongValidationRepository
    {
        Task<bool> IsCodeExistsAtSameLevelAsync(string code, Guid? parentId, Guid? excludeId = null);
    }
}
