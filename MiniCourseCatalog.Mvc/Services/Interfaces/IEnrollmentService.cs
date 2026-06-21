using MiniCourseCatalog.Mvc.Models;

namespace MiniCourseCatalog.Mvc.Services.Interfaces;

public interface IEnrollmentService
{
    Task<(bool Success, string Message)> EnrollStudentAsync(int courseId, int studentId);
    Task<List<Enrollment>> GetAllEnrollmentsAsync();
}
