namespace MiniCourseCatalog.Mvc.ViewModels;

public class CourseSearchViewModel
{
    public string Keyword { get; set; } = "";
    public string Category { get; set; } = "";
    public List<string> Categories { get; set; } = new();
    public List<CourseListItemViewModel> Results { get; set; } = new();
}