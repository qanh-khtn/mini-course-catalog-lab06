using MiniCourseCatalog.Mvc.Models;

namespace MiniCourseCatalog.Mvc.Repositories.Interfaces;

public interface ICourseReviewRepository
{
    Task AddReviewAsync(CourseReview review);
    Task<List<CourseReview>> GetReviewsByCourseIdAsync(int courseId);
    Task<bool> HasUserReviewedCourseAsync(int courseId, string userId);
    Task SaveChangesAsync();
}
