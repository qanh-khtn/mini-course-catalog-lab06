namespace MiniCourseCatalog.Mvc.Models;

public class Enrollment
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int StudentId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    public Course Course { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
