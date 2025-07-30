namespace server.Dtos.Common
{
    public class ImportResultDto
    {
        public int TotalRecords { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<ImportErrorDto> Errors { get; set; } = new List<ImportErrorDto>();
    }

}
