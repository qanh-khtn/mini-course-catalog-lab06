namespace MiniCourseCatalog.Mvc.ViewModels;

/// <summary>
/// Trang xác nhận xóa mềm — chỉ hiển thị thông tin, không cho sửa.
/// </summary>
public class CourseDeleteViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Instructor { get; set; } = "";
    public decimal TuitionFee { get; set; }
    public int CurrentEnrollment { get; set; }
    public int MaxCapacity { get; set; }
    public DateTime StartDate { get; set; }

    public string TuitionFeeText => $"{TuitionFee:N0} VND";
    public string EnrollmentStatus => $"{CurrentEnrollment}/{MaxCapacity}";
}
