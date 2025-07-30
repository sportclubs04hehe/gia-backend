using System.ComponentModel.DataAnnotations;

namespace server.Models
{
    public class BaseModel
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime NgayHieuLuc { get; set; }

        public DateTime NgayHetHieuLuc { get; set; }

        public bool IsDelete { get; set; } = false;

        public string? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}
