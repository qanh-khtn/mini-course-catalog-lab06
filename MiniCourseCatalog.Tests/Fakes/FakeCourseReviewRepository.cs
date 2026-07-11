using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Repositories.Interfaces;

namespace MiniCourseCatalog.Tests.Fakes;

public class FakeCourseReviewRepository : ICourseReviewRepository
{
    private readonly List<CourseReview> _reviews = new();

    public async Task AddReviewAsync(CourseReview review)
    {
        _reviews.Add(review);
        await Task.CompletedTask;
    }

    public async Task<List<CourseReview>> GetReviewsByCourseIdAsync(int courseId)
    {
        return await Task.FromResult(_reviews.Where(r => r.CourseId == courseId).ToList());
    }

    public async Task<bool> HasUserReviewedCourseAsync(int courseId, string userId)
    {
        return await Task.FromResult(_reviews.Any(r => r.CourseId == courseId && r.UserId == userId));
    }

    public async Task SaveChangesAsync()
    {
        await Task.CompletedTask;
    }
}
