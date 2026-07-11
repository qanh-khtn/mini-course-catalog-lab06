using Microsoft.EntityFrameworkCore;
using MiniCourseCatalog.Mvc.Data;
using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Repositories.Interfaces;

namespace MiniCourseCatalog.Mvc.Repositories;

public class CourseReviewRepository : ICourseReviewRepository
{
    private readonly AppDbContext _context;

    public CourseReviewRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddReviewAsync(CourseReview review)
    {
        await _context.CourseReviews.AddAsync(review);
    }

    public async Task<List<CourseReview>> GetReviewsByCourseIdAsync(int courseId)
    {
        return await _context.CourseReviews
            .Include(r => r.User)
            .Where(r => r.CourseId == courseId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> HasUserReviewedCourseAsync(int courseId, string userId)
    {
        return await _context.CourseReviews.AnyAsync(r => r.CourseId == courseId && r.UserId == userId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
