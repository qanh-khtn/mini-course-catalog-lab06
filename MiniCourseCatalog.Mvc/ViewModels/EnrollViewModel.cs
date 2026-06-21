using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MiniCourseCatalog.Mvc.ViewModels;

public class EnrollViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khóa học")]
    [Display(Name = "Khóa học")]
    public int CourseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn học viên")]
    [Display(Name = "Học viên")]
    public int StudentId { get; set; }

    public List<SelectListItem> CourseOptions { get; set; } = new();
    public List<SelectListItem> StudentOptions { get; set; } = new();

    public string ResultMessage { get; set; } = "";
    public bool? IsSuccess { get; set; }
}
