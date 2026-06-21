namespace MiniCourseCatalog.Mvc.ViewModels;

public class CourseCategoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<CourseListItemViewModel> Courses { get; set; } = new();

    public int CourseCount => Courses.Count;
    public int TotalEnrollment => Courses.Sum(c => c.CurrentEnrollment);
    public int TotalCapacity => Courses.Sum(c => c.MaxCapacity);
}
