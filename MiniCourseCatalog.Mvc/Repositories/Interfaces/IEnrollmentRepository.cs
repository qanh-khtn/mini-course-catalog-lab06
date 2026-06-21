using MiniCourseCatalog.Mvc.Models;

namespace MiniCourseCatalog.Mvc.Repositories.Interfaces;

public interface IEnrollmentRepository
{
    Task<List<Enrollment>> GetAllWithDetailsReadOnlyAsync();
    Task<bool> IsAlreadyEnrolledAsync(int courseId, int studentId);
    Task AddAsync(Enrollment enrollment);
}
