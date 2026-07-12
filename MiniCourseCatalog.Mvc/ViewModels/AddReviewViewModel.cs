using System.ComponentModel.DataAnnotations;

namespace MiniCourseCatalog.Mvc.ViewModels;

public class AddReviewViewModel
{
    [Required]
    public int CourseId { get; set; }

    [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5 sao.")]
    public int Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Nhận xét tối đa 1000 ký tự.")]
    public string? Comment { get; set; }
}
