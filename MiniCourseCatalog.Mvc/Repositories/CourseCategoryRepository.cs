using Microsoft.EntityFrameworkCore;
using MiniCourseCatalog.Mvc.Data;
using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Repositories.Interfaces;

namespace MiniCourseCatalog.Mvc.Repositories;

public class CourseCategoryRepository : ICourseCategoryRepository
{
    private readonly AppDbContext _context;

    public CourseCategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CourseCategory>> GetAllReadOnlyAsync() =>
        await _context.CourseCategories.AsNoTracking().ToListAsync();

    public async Task<CourseCategory?> GetByIdAsync(int id) =>
        await _context.CourseCategories.FindAsync(id);
}
