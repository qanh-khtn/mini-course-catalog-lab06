using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniCourseCatalog.Mvc.Data;
using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Repositories;
using MiniCourseCatalog.Mvc.Services;
using MiniCourseCatalog.Tests.Fakes;

namespace MiniCourseCatalog.Tests;


public class EnrollmentServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;

    public EnrollmentServiceTests()
    {
        // SQLite in-memory: connection phải giữ mở trong suốt vòng đời test
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        // Dữ liệu test riêng (Id 100+ để không đụng seed data)
        _context.CourseCategories.Add(new CourseCategory { Id = 100, Name = "Test Category" });
        _context.Courses.Add(new Course
        {
            Id = 100, Code = "TEST-01", Name = "Khóa Test", Instructor = "GV Test",
            TuitionFee = 1_000_000, CurrentEnrollment = 1, MaxCapacity = 2,
            StartDate = new DateTime(2026, 9, 1), CourseCategoryId = 100
        });
        _context.Students.Add(new Student
        {
            Id = 100, FullName = "Học Viên Test", Email = "test@example.com", PhoneNumber = "0900000000"
        });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task EnrollStudentAsync_ConCho_DangKyThanhCong_TruGhe()
    {
        // Arrange
        var service = new EnrollmentService(new EnrollmentRepository(_context), _context);

        // Act
        var (success, message) = await service.EnrollStudentAsync(courseId: 100, studentId: 100);

        // Assert: enrollment được lưu và sĩ số tăng 1 (commit thành công)
        Assert.True(success, message);
        var course = await _context.Courses.AsNoTracking().FirstAsync(c => c.Id == 100);
        Assert.Equal(2, course.CurrentEnrollment);
        Assert.Equal(1, await _context.Enrollments.CountAsync(e => e.CourseId == 100));
    }

    [Fact]
    public async Task EnrollStudentAsync_LopDaDay_TuChoi_KhongLuuGi()
    {
        // Arrange: đẩy lớp lên trạng thái đầy (2/2)
        var course = await _context.Courses.FirstAsync(c => c.Id == 100);
        course.CurrentEnrollment = course.MaxCapacity;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var service = new EnrollmentService(new EnrollmentRepository(_context), _context);

        // Act
        var (success, message) = await service.EnrollStudentAsync(100, 100);

        // Assert
        Assert.False(success);
        Assert.Contains("đầy", message);
        Assert.Equal(0, await _context.Enrollments.CountAsync(e => e.CourseId == 100));
    }

    [Fact]
    public async Task EnrollStudentAsync_DangKyTrung_TuChoi()
    {
        // Arrange: học viên đã có enrollment trước đó
        _context.Enrollments.Add(new Enrollment
        {
            CourseId = 100, StudentId = 100, EnrolledAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var service = new EnrollmentService(new EnrollmentRepository(_context), _context);

        // Act
        var (success, message) = await service.EnrollStudentAsync(100, 100);

        // Assert: từ chối và không tạo thêm bản ghi
        Assert.False(success);
        Assert.Contains("đã đăng ký", message);
        Assert.Equal(1, await _context.Enrollments.CountAsync(e => e.CourseId == 100));
    }

    [Fact]
    public async Task EnrollStudentAsync_LoiGiuaChung_RollbackToanBo()
    {
        // Arrange: thay repository thật bằng Fake ném exception ở bước AddAsync
        // => mô phỏng lỗi giữa transaction sau khi đã qua các bước kiểm tra
        var service = new EnrollmentService(new ThrowingEnrollmentRepository(), _context);

        // Act
        var (success, message) = await service.EnrollStudentAsync(100, 100);

        // Assert: thất bại, VÀ sĩ số không bị trừ, không có enrollment nào được lưu
        Assert.False(success);
        Assert.Contains("Lỗi hệ thống", message);

        _context.ChangeTracker.Clear();
        var course = await _context.Courses.AsNoTracking().FirstAsync(c => c.Id == 100);
        Assert.Equal(1, course.CurrentEnrollment); // giữ nguyên như ban đầu
        Assert.Equal(0, await _context.Enrollments.CountAsync(e => e.CourseId == 100));
    }

    /// <summary>
    /// Fake repository mô phỏng "người dùng khác" commit trước trong lúc request
    /// hiện tại đang xử lý: bump Version của Course ngay trước khi lưu enrollment.
    /// </summary>
    private class ConcurrentWriteEnrollmentRepository : MiniCourseCatalog.Mvc.Repositories.Interfaces.IEnrollmentRepository
    {
        private readonly AppDbContext _context;
        private readonly EnrollmentRepository _inner;

        public ConcurrentWriteEnrollmentRepository(AppDbContext context)
        {
            _context = context;
            _inner = new EnrollmentRepository(context);
        }

        public Task<List<Enrollment>> GetAllWithDetailsReadOnlyAsync() =>
            _inner.GetAllWithDetailsReadOnlyAsync();

        public Task<bool> IsAlreadyEnrolledAsync(int courseId, int studentId) =>
            _inner.IsAlreadyEnrolledAsync(courseId, studentId);

        public async Task AddAsync(Enrollment enrollment)
        {
            // Request "kia" thắng cuộc đua: Version trong DB không còn khớp bản đã đọc
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE Courses SET Version = Version + 1 WHERE Id = 100");
            await _inner.AddAsync(enrollment);
        }
    }

    [Fact]
    public async Task EnrollStudentAsync_HaiRequestCungLuc_RequestSauBiTuChoi_KhongOversell()
    {
        // Arrange: repository mô phỏng request khác commit trước (Version bị đổi)
        var service = new EnrollmentService(new ConcurrentWriteEnrollmentRepository(_context), _context);

        // Act
        var (success, message) = await service.EnrollStudentAsync(100, 100);

        // Assert: bị từ chối bởi concurrency token, không lưu enrollment nào
        Assert.False(success);
        Assert.Contains("cùng lúc", message);

        _context.ChangeTracker.Clear();
        Assert.Equal(0, await _context.Enrollments.CountAsync(e => e.CourseId == 100));
    }

    [Fact]
    public async Task EnrollStudentAsync_KhoaHocKhongTonTai_TuChoi()
    {
        // Arrange
        var service = new EnrollmentService(new EnrollmentRepository(_context), _context);

        // Act
        var (success, message) = await service.EnrollStudentAsync(courseId: 999, studentId: 100);

        // Assert
        Assert.False(success);
        Assert.Contains("Không tìm thấy khóa học", message);
    }
}
