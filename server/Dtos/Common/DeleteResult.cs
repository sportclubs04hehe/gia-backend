namespace server.Dtos.Common
{
    /// <summary>
    /// DTO chứa thông tin kết quả của các thao tác xóa
    /// </summary>
    public class DeleteResult
    {
        /// <summary>
        /// Kết quả thao tác thành công hay không
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Thông báo kết quả
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Số lượng bản ghi bị ảnh hưởng
        /// </summary>
        public int AffectedRecords { get; set; }
    }
}