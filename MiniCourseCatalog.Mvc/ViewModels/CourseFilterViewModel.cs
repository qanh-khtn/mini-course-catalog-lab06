using MiniCourseCatalog.Mvc.Models;

namespace MiniCourseCatalog.Mvc.ViewModels;

public class CourseFilterViewModel
{
    public int? CategoryId { get; set; }
    public decimal? MinFee { get; set; }
    public decimal? MaxFee { get; set; }
    public string Theme { get; set; } = "light";

    public List<CourseCategory> Categories { get; set; } = new();
    public List<CourseListItemViewModel> Results { get; set; } = new();
    public bool HasSearched { get; set; }
}
