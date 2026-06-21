namespace MiniCourseCatalog.Mvc.ViewModels;

public class CourseStatsViewModel
{
    public int TotalCourses { get; set; }
    public int TotalStudents { get; set; }
    public decimal TotalExpectedRevenue { get; set; }
    public int FullCoursesCount { get; set; }
    public int PendingCoursesCount { get; set; }

    public double OverallFillRate { get; set; }
    public string TopInstructor { get; set; } = "";
    public List<CategoryStatsViewModel> CategoryStats { get; set; } = new();

    public string TotalExpectedRevenueText => $"{TotalExpectedRevenue:N0} VND";
}
