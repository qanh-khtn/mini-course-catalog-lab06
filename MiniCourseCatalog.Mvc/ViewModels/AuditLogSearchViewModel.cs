using System.ComponentModel.DataAnnotations;
using MiniCourseCatalog.Mvc.Models;

namespace MiniCourseCatalog.Mvc.ViewModels;

public class AuditLogSearchViewModel
{
    [Display(Name = "Từ khóa (User/Action)")]
    public string? Keyword { get; set; }

    [Display(Name = "Kết quả")]
    public string? Result { get; set; }

    [Display(Name = "Từ ngày")]
    [DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }

    [Display(Name = "Đến ngày")]
    [DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }

    public List<AuditLog> Logs { get; set; } = new();
}
