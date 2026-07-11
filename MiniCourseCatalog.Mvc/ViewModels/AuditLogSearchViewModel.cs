using System.ComponentModel.DataAnnotations;
using MiniCourseCatalog.Mvc.Models;

namespace MiniCourseCatalog.Mvc.ViewModels;

public class AuditLogSearchViewModel
{
    [Display(Name = "Người dùng")]
    public string? User { get; set; }

    // Đặt tên "ActionName" (không phải "Action") để tránh đụng route value "action" của MVC —
    // model binder mặc định sẽ lấy nhầm tên action hiện tại (vd "Index") nếu trùng tên.
    [Display(Name = "Hành động")]
    public string? ActionName { get; set; }

    [Display(Name = "Kết quả")]
    public string? Result { get; set; }

    [Display(Name = "Từ ngày")]
    [DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }

    [Display(Name = "Đến ngày")]
    [DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }

    public PaginationViewModel<AuditLog> Logs { get; set; } = new();
}
