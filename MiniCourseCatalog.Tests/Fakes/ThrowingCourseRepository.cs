using Microsoft.EntityFrameworkCore;
using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Repositories.Interfaces;

namespace MiniCourseCatalog.Tests.Fakes;

/// <summary>
/// Fake repository mô phỏng tình huống concurrency: SaveChangesAsync ném
/// DbUpdateConcurrencyException để test nhánh ConcurrencyConflict trong CourseService.
/// Dùng explicit interface re-implementation để đảm bảo phương thức này được gọi
/// khi CourseService truy cập qua ICourseRepository.
/// </summary>
public class ThrowingCourseRepository : FakeCourseRepository, ICourseRepository
{
    public ThrowingCourseRepository(IEnumerable<Course>? seed = null) : base(seed) { }

    Task ICourseRepository.SaveChangesAsync() =>
        throw new DbUpdateConcurrencyException("Mô phỏng xung đột concurrency (RowVersion không khớp).");
}
