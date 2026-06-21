namespace MiniCourseCatalog.Mvc.ViewModels;

/// <summary>
/// Một dòng trong trang Trash (khóa học đã ngưng tuyển sinh / xóa mềm).
/// </summary>
public class CourseTrashItemViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime? DeletedAt { get; set; }

    public string DeletedAtText => DeletedAt?.ToString("dd/MM/yyyy HH:mm") ?? "—";
}
