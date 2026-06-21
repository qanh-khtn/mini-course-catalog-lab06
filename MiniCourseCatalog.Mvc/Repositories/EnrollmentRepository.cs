using Microsoft.EntityFrameworkCore;
using MiniCourseCatalog.Mvc.Data;
using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Repositories.Interfaces;

namespace MiniCourseCatalog.Mvc.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly AppDbContext _context;

    public EnrollmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Enrollment>> GetAllWithDetailsReadOnlyAsync() =>
        await _context.Enrollments
            .Include(e => e.Course).ThenInclude(c => c.CourseCategory)
            .Include(e => e.Student)
            .AsNoTracking()
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();

    public async Task<bool> IsAlreadyEnrolledAsync(int courseId, int studentId) =>
        await _context.Enrollments.AsNoTracking()
            .AnyAsync(e => e.CourseId == courseId && e.StudentId == studentId);

    public async Task AddAsync(Enrollment enrollment) =>
        await _context.Enrollments.AddAsync(enrollment);
}
