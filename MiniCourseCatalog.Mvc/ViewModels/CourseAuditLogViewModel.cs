namespace MiniCourseCatalog.Mvc.ViewModels;

public class CourseAuditLogItemViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Instructor { get; set; } = "";
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public string StatusBadge => IsDeleted ? "Đã xóa" : "Đang hoạt động";
    public string CreatedAtText => CreatedAt.ToString("dd/MM/yyyy HH:mm:ss");
    public string UpdatedAtText => UpdatedAt?.ToString("dd/MM/yyyy HH:mm:ss") ?? "—";
    public string DeletedAtText => DeletedAt?.ToString("dd/MM/yyyy HH:mm:ss") ?? "—";
}
