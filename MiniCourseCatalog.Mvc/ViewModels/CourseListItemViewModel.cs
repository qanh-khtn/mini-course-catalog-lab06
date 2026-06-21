namespace MiniCourseCatalog.Mvc.ViewModels;

public class CourseListItemViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Instructor { get; set; } = "";
    public decimal TuitionFee { get; set; }
    public int CurrentEnrollment { get; set; }
    public int MaxCapacity { get; set; }

    public string TuitionFeeText => $"{TuitionFee:N0} VND";
    public string EnrollmentStatus => $"{CurrentEnrollment}/{MaxCapacity}";

    public string CourseStatus
    {
        get
        {
            if (CurrentEnrollment >= MaxCapacity) return "Đã đầy lớp";
            if (MaxCapacity - CurrentEnrollment <= 3) return "Sắp kín chỗ";
            if (CurrentEnrollment < 10) return "Lớp vắng học viên";
            return "Còn chỗ đăng ký";
        }
    }

    public string StatusClass
    {
        get
        {
            if (CurrentEnrollment >= MaxCapacity) return "badge bg-danger";
            if (MaxCapacity - CurrentEnrollment <= 3) return "badge bg-warning text-dark";
            if (CurrentEnrollment < 10) return "badge bg-secondary";
            return "badge bg-success";
        }
    }
}
