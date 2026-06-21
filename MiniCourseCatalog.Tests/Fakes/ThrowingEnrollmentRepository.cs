using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Repositories.Interfaces;

namespace MiniCourseCatalog.Tests.Fakes;

public class ThrowingEnrollmentRepository : IEnrollmentRepository
{
    public Task<List<Enrollment>> GetAllWithDetailsReadOnlyAsync() =>
        Task.FromResult(new List<Enrollment>());

    public Task<bool> IsAlreadyEnrolledAsync(int courseId, int studentId) =>
        Task.FromResult(false);

    public Task AddAsync(Enrollment enrollment) =>
        throw new InvalidOperationException("Mô phỏng lỗi ghi dữ liệu giữa transaction.");
}
