namespace MiniCourseCatalog.Mvc.ViewModels;

public class CategoryStatsViewModel
{
    public string Category { get; set; } = "";
    public int CourseCount { get; set; }
    public int StudentCount { get; set; }
    public int Capacity { get; set; }
    public decimal Revenue { get; set; }
    public double FillRate { get; set; }
    public string RevenueText => $"{Revenue:N0} VND";
}
