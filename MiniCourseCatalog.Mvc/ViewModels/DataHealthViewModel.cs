namespace MiniCourseCatalog.Mvc.ViewModels;

public class DataHealthViewModel
{
    public bool CanConnect { get; set; }
    public string DbError { get; set; } = "";
    public int CourseCategoryCount { get; set; }
    public int CourseCount { get; set; }
    public int StudentCount { get; set; }
    public int EnrollmentCount { get; set; }
    public string DatabasePath { get; set; } = "";
    public string LastMigration { get; set; } = "";
    public bool MigrationApplied { get; set; }
}
