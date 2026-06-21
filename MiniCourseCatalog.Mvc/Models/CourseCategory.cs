using System.ComponentModel.DataAnnotations;

namespace MiniCourseCatalog.Mvc.Models;

public class CourseCategory
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
