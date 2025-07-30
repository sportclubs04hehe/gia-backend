namespace server.Dtos.Common
{
    public class ImportErrorDto
    {
        public int Row { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string> ColumnErrors { get; set; } = new Dictionary<string, string>();
    }
}
