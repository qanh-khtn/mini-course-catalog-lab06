using System.ComponentModel.DataAnnotations;

namespace MiniCourseCatalog.Mvc.Models;

public class AuditLog
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Action { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string EntityName { get; set; } = "";

    public string? EntityId { get; set; }

    [StringLength(100)]
    public string? UserName { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [Required]
    [StringLength(50)]
    public string Result { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? Note { get; set; }
}
