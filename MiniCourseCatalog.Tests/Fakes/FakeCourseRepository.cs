using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Repositories.Interfaces;

namespace MiniCourseCatalog.Tests.Fakes;

/// <summary>
/// Fake Repository thay thế CourseRepository thật khi unit test.
/// Dữ liệu nằm hoàn toàn trong bộ nhớ — không cần database,
/// chứng minh Service đã được tách dependency đúng cách (minimal test mindset).
/// </summary>
public class FakeCourseRepository : ICourseRepository
{
    private readonly List<Course> _courses;

    public bool SaveChangesCalled { get; private set; }

    public FakeCourseRepository(IEnumerable<Course>? seed = null)
    {
        _courses = seed?.ToList() ?? new List<Course>();
    }

    public Task<List<Course>> GetAllReadOnlyAsync() =>
        Task.FromResult(_courses.ToList());

    public Task<List<Course>> GetAllAsync() =>
        Task.FromResult(_courses.ToList());

    public Task<Course?> GetByIdReadOnlyAsync(int id) =>
        Task.FromResult(_courses.FirstOrDefault(c => c.Id == id));

    public Task<Course?> GetByIdAsync(int id) =>
        Task.FromResult(_courses.FirstOrDefault(c => c.Id == id));

    public Task AddAsync(Course course)
    {
        course.Id = _courses.Count == 0 ? 1 : _courses.Max(c => c.Id) + 1;
        _courses.Add(course);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        SaveChangesCalled = true;
        return Task.CompletedTask;
    }

    public Task<bool> ExistsSameClassAsync(string code, string instructor, DateTime startDate) =>
        Task.FromResult(_courses.Any(c =>
            c.Code == code && c.Instructor == instructor && c.StartDate.Date == startDate.Date));

    public Task<List<Course>> FilterAsync(int? categoryId, decimal? minFee, decimal? maxFee)
    {
        var query = _courses.AsEnumerable();

        if (categoryId.HasValue)
            query = query.Where(c => c.CourseCategoryId == categoryId.Value);

        if (minFee.HasValue)
            query = query.Where(c => c.TuitionFee >= minFee.Value);

        if (maxFee.HasValue)
            query = query.Where(c => c.TuitionFee <= maxFee.Value);

        return Task.FromResult(query.OrderBy(c => c.TuitionFee).ToList());
    }

    public Task<Course?> GetByIdIncludingDeletedAsync(int id) =>
        Task.FromResult(_courses.FirstOrDefault(c => c.Id == id));

    public Task<List<Course>> GetTrashReadOnlyAsync() =>
        Task.FromResult(_courses.Where(c => c.IsDeleted).ToList());

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null) =>
        Task.FromResult(_courses.Any(c =>
            c.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase) &&
            (!excludeId.HasValue || c.Id != excludeId.Value)));

    public void SetOriginalRowVersion(Course course, byte[] rowVersion)
    {
        // No-op: fake repository không có concurrency token thật
    }

    public Task HardDeleteAsync(int id)
    {
        var course = _courses.FirstOrDefault(c => c.Id == id && c.IsDeleted);
        if (course != null)
            _courses.Remove(course);
        SaveChangesCalled = true;
        return Task.CompletedTask;
    }

    public Task<List<Course>> GetAllIncludingDeletedReadOnlyAsync() =>
        Task.FromResult(_courses.OrderBy(c => c.Code).ToList());
}
