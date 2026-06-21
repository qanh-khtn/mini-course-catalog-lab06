using MiniCourseCatalog.Mvc.Models;

namespace MiniCourseCatalog.Mvc.Repositories.Interfaces;

public interface IStudentRepository
{
    Task<List<Student>> GetAllReadOnlyAsync();
    Task<Student?> GetByIdAsync(int id);
}
