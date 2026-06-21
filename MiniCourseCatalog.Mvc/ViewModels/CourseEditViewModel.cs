using System.ComponentModel.DataAnnotations;

namespace MiniCourseCatalog.Mvc.ViewModels;

/// <summary>
/// Form Edit dùng ViewModel riêng (không bind Entity) để chống overposting:
/// user không thể gửi kèm CreatedAt, IsDeleted, DeletedAt hay Version.
/// Kế thừa toàn bộ DataAnnotations + IValidatableObject của CourseCreateViewModel.
/// </summary>
public class CourseEditViewModel : CourseCreateViewModel
{
    public int Id { get; set; }

    // RowVersion (base64) lúc user mở form — đưa vào hidden field, server so với DB
    // để phát hiện Last-Save-Wins. Required: thiếu là không kiểm tra được concurrency.
    [Required]
    public string RowVersion { get; set; } = "";
}
