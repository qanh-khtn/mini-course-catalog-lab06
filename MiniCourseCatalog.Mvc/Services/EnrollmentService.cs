using Microsoft.EntityFrameworkCore;
using MiniCourseCatalog.Mvc.Data;
using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Repositories.Interfaces;
using MiniCourseCatalog.Mvc.Services.Interfaces;

namespace MiniCourseCatalog.Mvc.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly AppDbContext _context;

    public EnrollmentService(IEnrollmentRepository enrollmentRepository, AppDbContext context)
    {
        _enrollmentRepository = enrollmentRepository;
        _context = context;
    }

    public async Task<(bool Success, string Message)> EnrollStudentAsync(int courseId, int studentId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Tracked query — cần cập nhật CurrentEnrollment
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null)
            {
                await transaction.RollbackAsync();
                return (false, "Không tìm thấy khóa học.");
            }

            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
            {
                await transaction.RollbackAsync();
                return (false, "Không tìm thấy học viên.");
            }

            if (course.CurrentEnrollment >= course.MaxCapacity)
            {
                await transaction.RollbackAsync();
                return (false, $"Khóa học '{course.Name}' đã đầy chỗ ({course.MaxCapacity}/{course.MaxCapacity}).");
            }

            var alreadyEnrolled = await _enrollmentRepository.IsAlreadyEnrolledAsync(courseId, studentId);
            if (alreadyEnrolled)
            {
                await transaction.RollbackAsync();
                return (false, $"Học viên '{student.FullName}' đã đăng ký khóa học này rồi.");
            }

            var enrollment = new Enrollment
            {
                CourseId = courseId,
                StudentId = studentId,
                EnrolledAt = DateTime.UtcNow
            };

            await _enrollmentRepository.AddAsync(enrollment);
            course.CurrentEnrollment++;  // Tracked — EF Core sẽ tự UPDATE
            course.Version++;            // Concurrency token: UPDATE kèm WHERE Version = giá trị cũ

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, $"Đăng ký thành công! Học viên '{student.FullName}' vào khóa '{course.Name}'.");
        }
        catch (DbUpdateConcurrencyException)
        {
            // 2 request cùng đọc AvailableSeats rồi cùng trừ: request commit sau bị từ chối
            // => rollback toàn bộ, không bị "oversell" chỗ ngồi
            await transaction.RollbackAsync();
            return (false, "Lớp vừa có người khác đăng ký cùng lúc. Vui lòng tải lại trang và thử lại.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Lỗi hệ thống khi đăng ký: {ex.Message}");
        }
    }

    public async Task<List<Enrollment>> GetAllEnrollmentsAsync() =>
        await _enrollmentRepository.GetAllWithDetailsReadOnlyAsync();
}
