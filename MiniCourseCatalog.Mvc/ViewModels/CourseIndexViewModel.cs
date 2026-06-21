namespace MiniCourseCatalog.Mvc.ViewModels;

public class CourseIndexViewModel
{
    public List<CourseListItemViewModel> Courses { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string Keyword { get; set; } = "";
    public string Category { get; set; } = "";
    public string Theme { get; set; } = "light";
    public int TotalCoursesBeforeFilter { get; set; }
}
