using System.ComponentModel.DataAnnotations;

namespace MiniCourseCatalog.Mvc.Models;

public class Course : IAuditable, ISoftDeletable
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string Code { get; set; } = "";

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [Required]
    [StringLength(100)]
    public string Instructor { get; set; } = "";

    public decimal TuitionFee { get; set; }
    public int CurrentEnrollment { get; set; }
    public int MaxCapacity { get; set; }
    public int AvailableSeats => MaxCapacity - CurrentEnrollment;
    public DateTime StartDate { get; set; }
    
    [StringLength(500)]
    public string? ThumbnailPath { get; set; }

    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // --- Soft delete fields (Lab05) ---
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public int CourseCategoryId { get; set; }
    public CourseCategory CourseCategory { get; set; } = null!;

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<CourseReview> CourseReviews { get; set; } = new List<CourseReview>();

    public double AverageRating => CourseReviews.Any() ? CourseReviews.Average(r => r.Rating) : 0;
}
