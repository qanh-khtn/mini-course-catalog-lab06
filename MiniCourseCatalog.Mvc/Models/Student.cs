using System.ComponentModel.DataAnnotations;

namespace MiniCourseCatalog.Mvc.Models;

public class Student
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = "";

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
