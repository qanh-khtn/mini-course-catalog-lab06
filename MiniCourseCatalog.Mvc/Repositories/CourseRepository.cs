using Microsoft.EntityFrameworkCore;
using MiniCourseCatalog.Mvc.Data;
using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Repositories.Interfaces;

namespace MiniCourseCatalog.Mvc.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly AppDbContext _context;

    public CourseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Course>> GetAllReadOnlyAsync() =>
        await _context.Courses
            .Include(c => c.CourseCategory)
            .AsNoTracking()
            .ToListAsync();

    public async Task<List<Course>> GetAllAsync() =>
        await _context.Courses
            .Include(c => c.CourseCategory)
            .ToListAsync();

    public async Task<Course?> GetByIdReadOnlyAsync(int id) =>
        await _context.Courses
            .Include(c => c.CourseCategory)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Course?> GetByIdAsync(int id) =>
        await _context.Courses
            .Include(c => c.CourseCategory)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(Course course) =>
        await _context.Courses.AddAsync(course);

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();

    public async Task<bool> ExistsSameClassAsync(string code, string instructor, DateTime startDate) =>
        await _context.Courses.AsNoTracking()
            .AnyAsync(c =>
                c.Code.ToLower() == code.Trim().ToLower() &&
                c.Instructor.ToLower() == instructor.Trim().ToLower() &&
                c.StartDate.Date == startDate.Date);

    public async Task<List<Course>> FilterAsync(int? categoryId, decimal? minFee, decimal? maxFee)
    {
        var query = _context.Courses
            .Include(c => c.CourseCategory)
            .AsNoTracking()
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(c => c.CourseCategoryId == categoryId.Value);

        if (minFee.HasValue)
            query = query.Where(c => c.TuitionFee >= minFee.Value);

        if (maxFee.HasValue)
            query = query.Where(c => c.TuitionFee <= maxFee.Value);

        return await query.OrderBy(c => c.TuitionFee).ToListAsync();
    }

    // Bỏ qua global query filter để lấy cả khóa đã xóa mềm (tracked) — dùng cho Restore
    public async Task<Course?> GetByIdIncludingDeletedAsync(int id) =>
        await _context.Courses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id);

    // Trang Trash: chỉ lấy khóa đã xóa mềm, chỉ đọc
    public async Task<List<Course>> GetTrashReadOnlyAsync() =>
        await _context.Courses
            .IgnoreQueryFilters()
            .Where(c => c.IsDeleted)
            .Include(c => c.CourseCategory)
            .AsNoTracking()
            .OrderByDescending(c => c.DeletedAt)
            .ToListAsync();

    // Kiểm tra trùng CourseCode trên TOÀN BỘ dữ liệu (kể cả đã xóa mềm) để giữ mã định danh duy nhất
    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var normalized = code.Trim().ToLower();
        return await _context.Courses
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Code.ToLower() == normalized && (!excludeId.HasValue || c.Id != excludeId.Value));
    }

    // Gán OriginalValue của RowVersion = phiên bản user thấy lúc mở form,
    // để EF sinh WHERE RowVersion = @original và phát hiện Last-Save-Wins
    public void SetOriginalRowVersion(Course course, byte[] rowVersion) =>
        _context.Entry(course).Property(nameof(Course.RowVersion)).OriginalValue = rowVersion;

    public async Task<List<Course>> GetAllIncludingDeletedReadOnlyAsync() =>
        await _context.Courses
            .IgnoreQueryFilters()
            .Include(c => c.CourseCategory)
            .AsNoTracking()
            .OrderByDescending(c => c.IsDeleted)   // khóa đã xóa lên đầu
            .ThenByDescending(c => c.DeletedAt)    // xóa gần nhất trước
            .ThenBy(c => c.Code)
            .ToListAsync();

    // Xóa vĩnh viễn: dùng ExecuteDeleteAsync để bypass ApplyAuditAndSoftDelete interceptor.
    // Phải xóa Enrollment trước vì ExecuteDeleteAsync không kích hoạt cascade của EF Core.
    public async Task HardDeleteAsync(int id)
    {
        await _context.Enrollments
            .Where(e => e.CourseId == id)
            .ExecuteDeleteAsync();

        await _context.Courses
            .IgnoreQueryFilters()
            .Where(c => c.Id == id && c.IsDeleted)
            .ExecuteDeleteAsync();
    }
}
