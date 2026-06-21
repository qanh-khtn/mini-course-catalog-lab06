using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Repositories.Interfaces;
using MiniCourseCatalog.Mvc.Services.Interfaces;

namespace MiniCourseCatalog.Mvc.Services;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;

    public StudentService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<List<Student>> GetAllAsync() =>
        await _studentRepository.GetAllReadOnlyAsync();

    public async Task<Student?> GetByIdAsync(int id) =>
        await _studentRepository.GetByIdAsync(id);
}
