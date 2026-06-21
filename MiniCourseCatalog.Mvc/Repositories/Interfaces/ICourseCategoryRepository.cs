using MiniCourseCatalog.Mvc.Models;

namespace MiniCourseCatalog.Mvc.Repositories.Interfaces;

public interface ICourseCategoryRepository
{
    Task<List<CourseCategory>> GetAllReadOnlyAsync();
    Task<CourseCategory?> GetByIdAsync(int id);
}
