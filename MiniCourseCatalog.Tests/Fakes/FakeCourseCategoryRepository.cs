using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Repositories.Interfaces;

namespace MiniCourseCatalog.Tests.Fakes;

/// <summary>
/// Fake Repository cho CourseCategory — dữ liệu in-memory phục vụ unit test.
/// </summary>
public class FakeCourseCategoryRepository : ICourseCategoryRepository
{
    private readonly List<CourseCategory> _categories;

    public FakeCourseCategoryRepository(IEnumerable<CourseCategory>? seed = null)
    {
        _categories = seed?.ToList() ?? new List<CourseCategory>();
    }

    public Task<List<CourseCategory>> GetAllReadOnlyAsync() =>
        Task.FromResult(_categories.ToList());

    public Task<CourseCategory?> GetByIdAsync(int id) =>
        Task.FromResult(_categories.FirstOrDefault(c => c.Id == id));
}
