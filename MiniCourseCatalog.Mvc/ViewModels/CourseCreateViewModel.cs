using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MiniCourseCatalog.Mvc.ViewModels;

public class CourseCreateViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Mã khóa học không được để trống")]
    [StringLength(20, ErrorMessage = "Mã khóa học tối đa 20 ký tự")]
    [RegularExpression(@"^[A-Z]{2,4}-\d{3}$", ErrorMessage = "Mã khóa học phải có định dạng CHUỖI-SỐ, ví dụ: CS-101, MATH-005")]
    [Display(Name = "Mã khóa học")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Tên khóa học không được để trống")]
    [StringLength(100, ErrorMessage = "Tên khóa học tối đa 100 ký tự")]
    [Display(Name = "Tên khóa học")]
    public string Name { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn chuyên ngành")]
    [Display(Name = "Chuyên ngành")]
    public int CourseCategoryId { get; set; }

    public List<SelectListItem> CategoryOptions { get; set; } = new();

    [Required(ErrorMessage = "Giảng viên không được để trống")]
    [Display(Name = "Giảng viên")]
    public string Instructor { get; set; } = "";

    [Range(0, 100000000, ErrorMessage = "Học phí phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Học phí")]
    public decimal TuitionFee { get; set; }

    [Range(0, 1000, ErrorMessage = "Số học viên hiện tại không hợp lệ")]
    [Display(Name = "Số học viên hiện tại")]
    public int CurrentEnrollment { get; set; }

    [Range(1, 1000, ErrorMessage = "Sức chứa tối đa phải lớn hơn 0")]
    [Display(Name = "Sức chứa tối đa")]
    public int MaxCapacity { get; set; } = 20;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày khai giảng")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Display(Name = "Ảnh đại diện")]
    public IFormFile? Thumbnail { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.Date < DateTime.Today)
        {
            yield return new ValidationResult(
                "Ngày khai giảng không được là ngày trong quá khứ.",
                new[] { nameof(StartDate) }
            );
        }

        // Logic nghiệp vụ: sức chứa không được nhỏ hơn sĩ số hiện tại (không thể đăng ký vượt số chỗ)
        if (MaxCapacity < CurrentEnrollment)
        {
            yield return new ValidationResult(
                "Sức chứa tối đa không được nhỏ hơn số học viên hiện tại.",
                new[] { nameof(MaxCapacity) }
            );
        }
    }
}
