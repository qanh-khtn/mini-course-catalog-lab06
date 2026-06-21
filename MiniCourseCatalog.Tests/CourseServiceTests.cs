using Microsoft.Extensions.Options;
using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Options;
using MiniCourseCatalog.Mvc.Services;
using MiniCourseCatalog.Tests.Fakes;

namespace MiniCourseCatalog.Tests;

/// <summary>
/// Unit test cho CourseService dùng Fake Repository (không cần database).
/// Mỗi test theo 3 bước Arrange — Act — Assert (Câu 3.14).
/// </summary>
public class CourseServiceTests
{
    private static CourseCategory CatIT  => new() { Id = 1, Name = "Công nghệ Thông tin" };
    private static CourseCategory CatLang => new() { Id = 3, Name = "Ngoại Ngữ" };

    private static Course MakeCourse(int id, string code, string name, string instructor,
        decimal fee, int enrolled, int capacity, CourseCategory category) =>
        new()
        {
            Id = id, Code = code, Name = name, Instructor = instructor,
            TuitionFee = fee, CurrentEnrollment = enrolled, MaxCapacity = capacity,
            StartDate = new DateTime(2026, 9, 10),
            CourseCategoryId = category.Id, CourseCategory = category
        };

    private static CourseService CreateService(
        IEnumerable<Course> courses, int lowSeatThreshold = 3)
    {
        var courseRepo = new FakeCourseRepository(courses);
        var categoryRepo = new FakeCourseCategoryRepository(new[] { CatIT, CatLang });
        var options = Microsoft.Extensions.Options.Options.Create(new TrainingCenterConfig
        {
            LowSeatThreshold = lowSeatThreshold,
            CenterName = "Test Center"
        });
        return new CourseService(courseRepo, categoryRepo, options);
    }

    [Fact]
    public async Task GetStatsAsync_TinhDungDoanhThuVaSiSo()
    {
        // Arrange: 2 khóa học, doanh thu = học phí x sĩ số hiện tại
        var courses = new[]
        {
            MakeCourse(1, "PRG-201", "OOP C#", "Cô Hoa", 2_000_000, 10, 20, CatIT),
            MakeCourse(2, "ENG-105", "Tiếng Anh B1", "Ms. Smith", 1_000_000, 5, 10, CatLang)
        };
        var service = CreateService(courses);

        // Act
        var stats = await service.GetStatsAsync();

        // Assert
        Assert.Equal(2, stats.TotalCourses);
        Assert.Equal(15, stats.TotalStudents);
        Assert.Equal(25_000_000m, stats.TotalExpectedRevenue); // 10*2tr + 5*1tr
        Assert.Equal(50.0, stats.OverallFillRate);             // 15/30 chỗ
    }

    [Fact]
    public async Task GetStatsAsync_DemKhoaSapDayTheoNguong_OptionsPattern()
    {
        // Arrange: khóa còn 2 chỗ trống, ngưỡng LowSeatThreshold = 3
        // => phải được tính là "sắp đầy" (PendingCoursesCount)
        var courses = new[]
        {
            MakeCourse(1, "PRG-201", "OOP C#", "Cô Hoa", 2_000_000, 18, 20, CatIT),  // còn 2 chỗ
            MakeCourse(2, "ENG-105", "Tiếng Anh B1", "Ms. Smith", 1_000_000, 1, 10, CatLang) // còn 9 chỗ
        };
        var service = CreateService(courses, lowSeatThreshold: 3);

        // Act
        var stats = await service.GetStatsAsync();

        // Assert: chỉ khóa còn 2 chỗ nằm trong ngưỡng
        Assert.Equal(1, stats.PendingCoursesCount);
    }

    [Fact]
    public async Task GetStatsAsync_DoiNguongTrongConfig_KetQuaThayDoi_KhongSuaCode()
    {
        // Arrange: cùng dữ liệu, chỉ đổi giá trị threshold như đổi appsettings.json
        var courses = new[]
        {
            MakeCourse(1, "PRG-201", "OOP C#", "Cô Hoa", 2_000_000, 11, 20, CatIT) // còn 9 chỗ
        };

        // Act
        var statsThreshold3 = await CreateService(courses, lowSeatThreshold: 3).GetStatsAsync();
        var statsThreshold10 = await CreateService(courses, lowSeatThreshold: 10).GetStatsAsync();

        // Assert: ngưỡng 3 => chưa sắp đầy; ngưỡng 10 => sắp đầy
        Assert.Equal(0, statsThreshold3.PendingCoursesCount);
        Assert.Equal(1, statsThreshold10.PendingCoursesCount);
    }

    [Fact]
    public async Task SearchAsync_TimTheoKeywordVaChuyenNganh()
    {
        // Arrange
        var courses = new[]
        {
            MakeCourse(1, "PRG-201", "OOP C#", "Cô Hoa", 2_000_000, 10, 20, CatIT),
            MakeCourse(2, "ENG-105", "Tiếng Anh B1", "Ms. Smith", 1_000_000, 5, 10, CatLang)
        };
        var service = CreateService(courses);

        // Act
        var byKeyword = await service.SearchAsync("prg", "");
        var byCategory = await service.SearchAsync("", "Ngoại Ngữ");
        var noMatch = await service.SearchAsync("python", "");

        // Assert
        Assert.Single(byKeyword);
        Assert.Equal("PRG-201", byKeyword[0].Code);
        Assert.Single(byCategory);
        Assert.Equal("ENG-105", byCategory[0].Code);
        Assert.Empty(noMatch);
    }

    [Fact]
    public async Task AddAsync_GoiRepositoryVaLuuThayDoi()
    {
        // Arrange
        var courseRepo = new FakeCourseRepository();
        var service = new CourseService(
            courseRepo,
            new FakeCourseCategoryRepository(),
            Microsoft.Extensions.Options.Options.Create(new TrainingCenterConfig()));
        var newCourse = MakeCourse(0, "NEW-001", "Khóa mới", "GV Mới", 1_500_000, 0, 25, CatIT);

        // Act
        await service.AddAsync(newCourse);

        // Assert: course đã vào repository và SaveChanges được gọi
        var all = await courseRepo.GetAllReadOnlyAsync();
        Assert.Single(all);
        Assert.True(courseRepo.SaveChangesCalled);
    }

    // ===== Lab05: Soft Delete & Restore =====

    [Fact]
    public async Task SoftDeleteAsync_DanhDauIsDeletedVaLuuThayDoi()
    {
        // Arrange
        var course = MakeCourse(1, "PRG-201", "OOP C#", "Cô Hoa", 2_000_000, 10, 20, CatIT);
        var courseRepo = new FakeCourseRepository(new[] { course });
        var service = new CourseService(courseRepo, new FakeCourseCategoryRepository(),
            Microsoft.Extensions.Options.Options.Create(new TrainingCenterConfig()));

        // Act
        var result = await service.SoftDeleteAsync(1);

        // Assert
        Assert.True(result);
        Assert.True(course.IsDeleted);
        Assert.NotNull(course.DeletedAt);
        Assert.True(courseRepo.SaveChangesCalled);
    }

    [Fact]
    public async Task SoftDeleteAsync_KhongTonTai_TraVeFalse()
    {
        // Arrange
        var service = CreateService(Array.Empty<Course>());

        // Act
        var result = await service.SoftDeleteAsync(99);

        // Assert: không tìm thấy course => false, không có exception
        Assert.False(result);
    }

    [Fact]
    public async Task RestoreAsync_BoIsDeletedVaLuuThayDoi()
    {
        // Arrange: course đang ở trạng thái đã xóa mềm
        var course = MakeCourse(1, "PRG-201", "OOP C#", "Cô Hoa", 2_000_000, 10, 20, CatIT);
        course.IsDeleted = true;
        course.DeletedAt = DateTime.Now.AddDays(-1);
        var courseRepo = new FakeCourseRepository(new[] { course });
        var service = new CourseService(courseRepo, new FakeCourseCategoryRepository(),
            Microsoft.Extensions.Options.Options.Create(new TrainingCenterConfig()));

        // Act
        var result = await service.RestoreAsync(1);

        // Assert
        Assert.True(result);
        Assert.False(course.IsDeleted);
        Assert.Null(course.DeletedAt);
        Assert.True(courseRepo.SaveChangesCalled);
    }

    // ===== Lab05 Feature 2: AdjustSeats =====

    [Fact]
    public async Task AdjustSeatsAsync_CapNhatSiSoThanhCong()
    {
        // Arrange
        var course = MakeCourse(1, "PRG-201", "OOP C#", "Cô Hoa", 2_000_000, 10, 20, CatIT);
        course.RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var courseRepo = new FakeCourseRepository(new[] { course });
        var service = new CourseService(courseRepo, new FakeCourseCategoryRepository(),
            Microsoft.Extensions.Options.Options.Create(new TrainingCenterConfig()));

        var vm = new MiniCourseCatalog.Mvc.ViewModels.CourseAdjustSeatsViewModel
        {
            Id = 1, Code = "PRG-201", Name = "OOP C#",
            MaxCapacity = 20, CurrentEnrollment = 10, NewEnrollment = 15,
            RowVersion = Convert.ToBase64String(course.RowVersion)
        };

        // Act
        var result = await service.AdjustSeatsAsync(vm);

        // Assert
        Assert.Equal(MiniCourseCatalog.Mvc.Services.Interfaces.CourseAdjustResult.Success, result);
        Assert.Equal(15, course.CurrentEnrollment);
        Assert.True(courseRepo.SaveChangesCalled);
    }

    [Fact]
    public async Task AdjustSeatsAsync_VuotSucChua_TraVeExceedsCapacity()
    {
        // Arrange: MaxCapacity = 20, NewEnrollment = 25 => ExceedsCapacity
        var course = MakeCourse(1, "PRG-201", "OOP C#", "Cô Hoa", 2_000_000, 10, 20, CatIT);
        course.RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var courseRepo = new FakeCourseRepository(new[] { course });
        var service = new CourseService(courseRepo, new FakeCourseCategoryRepository(),
            Microsoft.Extensions.Options.Options.Create(new TrainingCenterConfig()));

        var vm = new MiniCourseCatalog.Mvc.ViewModels.CourseAdjustSeatsViewModel
        {
            Id = 1, Code = "PRG-201", Name = "OOP C#",
            MaxCapacity = 20, CurrentEnrollment = 10, NewEnrollment = 25,
            RowVersion = Convert.ToBase64String(course.RowVersion)
        };

        // Act
        var result = await service.AdjustSeatsAsync(vm);

        // Assert
        Assert.Equal(MiniCourseCatalog.Mvc.Services.Interfaces.CourseAdjustResult.ExceedsCapacity, result);
        Assert.Equal(10, course.CurrentEnrollment); // không thay đổi
    }

    [Fact]
    public async Task AdjustSeatsAsync_XungDotConcurrency_TraVeConcurrencyConflict()
    {
        // Arrange: ThrowingCourseRepository ném DbUpdateConcurrencyException khi SaveChangesAsync
        var course = MakeCourse(1, "PRG-201", "OOP C#", "Cô Hoa", 2_000_000, 10, 20, CatIT);
        course.RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var throwingRepo = new ThrowingCourseRepository(new[] { course });
        var service = new CourseService(throwingRepo, new FakeCourseCategoryRepository(),
            Microsoft.Extensions.Options.Options.Create(new TrainingCenterConfig()));

        var vm = new MiniCourseCatalog.Mvc.ViewModels.CourseAdjustSeatsViewModel
        {
            Id = 1, Code = "PRG-201", Name = "OOP C#",
            MaxCapacity = 20, CurrentEnrollment = 10, NewEnrollment = 15,
            RowVersion = Convert.ToBase64String(course.RowVersion)
        };

        // Act
        var result = await service.AdjustSeatsAsync(vm);

        // Assert: concurrency exception được bắt và trả về enum thay vì throw ra ngoài
        Assert.Equal(MiniCourseCatalog.Mvc.Services.Interfaces.CourseAdjustResult.ConcurrencyConflict, result);
    }

    // ===== Lab05 điểm cộng: Export CSV =====

    [Fact]
    public async Task ExportCoursesCsvAsync_TaoCSVDungDinhDang()
    {
        // Arrange
        var courses = new[]
        {
            MakeCourse(1, "ENG-105", "Tiếng Anh B1", "Ms. Smith", 1_000_000, 5, 10, CatLang),
            MakeCourse(2, "PRG-201", "OOP C#",       "Cô Hoa",   2_000_000, 10, 20, CatIT)
        };
        var service = CreateService(courses);

        // Act
        var csv = await service.ExportCoursesCsvAsync();

        // Assert: có header và 2 dòng dữ liệu, sắp xếp theo Code
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length); // 1 header + 2 data rows
        Assert.StartsWith("Mã,Tên khóa học", lines[0]);
        Assert.Contains("ENG-105", lines[1]); // sắp xếp: E trước P
        Assert.Contains("PRG-201", lines[2]);
        Assert.Contains("1000000", lines[1]); // học phí không có dấu chấm
        Assert.Contains("2000000", lines[2]);
    }
}
