using MiniCourseCatalog.Mvc.Models;

namespace MiniCourseCatalog.Mvc.Services.Interfaces;

public interface IStudentService
{
    Task<List<Student>> GetAllAsync();
    Task<Student?> GetByIdAsync(int id);
}
