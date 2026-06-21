using System.ComponentModel.DataAnnotations;

namespace MiniCourseCatalog.Mvc.ViewModels;

/// <summary>
/// Form điều chỉnh nhanh sĩ số (CurrentEnrollment) — chỉ nhận giá trị mới + RowVersion,
/// không cho sửa các field khác. Dùng RowVersion để chống ghi đè (Last-Save-Wins).
/// </summary>
public class CourseAdjustSeatsViewModel
{
    public int Id { get; set; }

    // Thông tin chỉ hiển thị (không bind khi submit)
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int MaxCapacity { get; set; }
    public int CurrentEnrollment { get; set; }

    [Display(Name = "Sĩ số mới")]
    [Range(0, 1000, ErrorMessage = "Sĩ số phải từ 0 trở lên")]
    public int NewEnrollment { get; set; }

    // RowVersion (base64) lúc user mở form — hidden field, server so với DB
    [Required]
    public string RowVersion { get; set; } = "";

    public string EnrollmentStatus => $"{CurrentEnrollment}/{MaxCapacity}";
}
