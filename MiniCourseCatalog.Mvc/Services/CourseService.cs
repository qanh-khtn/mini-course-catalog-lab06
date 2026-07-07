using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Options;
using MiniCourseCatalog.Mvc.Repositories.Interfaces;
using MiniCourseCatalog.Mvc.Services.Interfaces;
using MiniCourseCatalog.Mvc.ViewModels;

namespace MiniCourseCatalog.Mvc.Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseCategoryRepository _categoryRepository;
    private readonly TrainingCenterConfig _config;
    private readonly ILogger<CourseService> _logger;

    public CourseService(
        ICourseRepository courseRepository,
        ICourseCategoryRepository categoryRepository,
        IOptions<TrainingCenterConfig> config,
        ILogger<CourseService>? logger = null)
    {
        _courseRepository = courseRepository;
        _categoryRepository = categoryRepository;
        _config = config.Value;
        _logger = logger ?? NullLogger<CourseService>.Instance;
    }

    public async Task<List<Course>> GetAllAsync() =>
        await _courseRepository.GetAllReadOnlyAsync();

    public async Task<Course?> GetByIdAsync(int id) =>
        await _courseRepository.GetByIdReadOnlyAsync(id);

    public async Task<CourseStatsViewModel> GetStatsAsync()
    {
        var courses = await _courseRepository.GetAllReadOnlyAsync();
        var threshold = _config.LowSeatThreshold;

        var totalCourses = courses.Count;
        var totalStudents = courses.Sum(c => c.CurrentEnrollment);
        var totalRevenue = courses.Sum(c => c.TuitionFee * c.CurrentEnrollment);
        var fullCourses = courses.Count(c => c.CurrentEnrollment >= c.MaxCapacity);
        var pendingCourses = courses.Count(c =>
            c.CurrentEnrollment > 0 &&
            c.CurrentEnrollment < c.MaxCapacity &&
            c.MaxCapacity - c.CurrentEnrollment <= threshold);

        var totalCapacity = courses.Sum(c => c.MaxCapacity);
        double overallFillRate = totalCapacity > 0
            ? Math.Round((double)totalStudents / totalCapacity * 100, 1) : 0;

        var topInstructor = courses
            .GroupBy(c => c.Instructor)
            .OrderByDescending(g => g.Sum(c => c.CurrentEnrollment))
            .Select(g => g.Key)
            .FirstOrDefault() ?? "Chưa có";

        var categoryStats = courses
            .GroupBy(c => c.CourseCategory.Name)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var studentCount = g.Sum(c => c.CurrentEnrollment);
                var capacity = g.Sum(c => c.MaxCapacity);
                return new CategoryStatsViewModel
                {
                    Category = g.Key,
                    CourseCount = g.Count(),
                    StudentCount = studentCount,
                    Capacity = capacity,
                    Revenue = g.Sum(c => c.TuitionFee * c.CurrentEnrollment),
                    FillRate = capacity > 0 ? Math.Round((double)studentCount / capacity * 100, 1) : 0
                };
            })
            .ToList();

        return new CourseStatsViewModel
        {
            TotalCourses = totalCourses,
            TotalStudents = totalStudents,
            TotalExpectedRevenue = totalRevenue,
            FullCoursesCount = fullCourses,
            PendingCoursesCount = pendingCourses,
            OverallFillRate = overallFillRate,
            TopInstructor = topInstructor,
            CategoryStats = categoryStats
        };
    }

    public async Task AddAsync(Course course)
    {
        await _courseRepository.AddAsync(course);
        await _courseRepository.SaveChangesAsync();
        _logger.LogInformation("Course created. CourseId={CourseId}, Code={Code}", course.Id, course.Code);
    }

    public async Task<List<Course>> SearchAsync(string keyword, string category)
    {
        var courses = await _courseRepository.GetAllReadOnlyAsync();
        var query = courses.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(c =>
                c.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                c.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                c.Instructor.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(c =>
                string.Equals(c.CourseCategory.Name, category, StringComparison.OrdinalIgnoreCase));

        return query.ToList();
    }

    public async Task<List<string>> GetCategoryNamesAsync()
    {
        var courses = await _courseRepository.GetAllReadOnlyAsync();
        return courses.Select(c => c.CourseCategory.Name).Distinct().OrderBy(c => c).ToList();
    }

    public async Task<List<CourseCategory>> GetCourseCategoriesAsync() =>
        await _categoryRepository.GetAllReadOnlyAsync();

    public async Task<bool> ExistsSameClassAsync(string code, string instructor, DateTime startDate) =>
        await _courseRepository.ExistsSameClassAsync(code, instructor, startDate);

    public async Task<List<CourseListItemViewModel>> FilterAsync(int? categoryId, decimal? minFee, decimal? maxFee)
    {
        var courses = await _courseRepository.FilterAsync(categoryId, minFee, maxFee);
        return courses.Select(c => new CourseListItemViewModel
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Category = c.CourseCategory.Name,
            Instructor = c.Instructor,
            TuitionFee = c.TuitionFee,
            CurrentEnrollment = c.CurrentEnrollment,
            MaxCapacity = c.MaxCapacity
        }).ToList();
    }

    // ---------- Lab05: CRUD an toàn + soft delete ----------

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null) =>
        _courseRepository.CodeExistsAsync(code, excludeId);

    public async Task<CourseEditViewModel?> GetForEditAsync(int id)
    {
        var course = await _courseRepository.GetByIdReadOnlyAsync(id);
        if (course == null) return null;

        return new CourseEditViewModel
        {
            Id = course.Id,
            Code = course.Code,
            Name = course.Name,
            CourseCategoryId = course.CourseCategoryId,
            Instructor = course.Instructor,
            TuitionFee = course.TuitionFee,
            CurrentEnrollment = course.CurrentEnrollment,
            MaxCapacity = course.MaxCapacity,
            StartDate = course.StartDate,
            ExistingThumbnailPath = course.ThumbnailPath,
            RowVersion = Convert.ToBase64String(course.RowVersion)
        };
    }

    public async Task<CourseDeleteViewModel?> GetForDeleteAsync(int id)
    {
        var course = await _courseRepository.GetByIdReadOnlyAsync(id);
        if (course == null) return null;

        return new CourseDeleteViewModel
        {
            Id = course.Id,
            Code = course.Code,
            Name = course.Name,
            Category = course.CourseCategory.Name,
            Instructor = course.Instructor,
            TuitionFee = course.TuitionFee,
            CurrentEnrollment = course.CurrentEnrollment,
            MaxCapacity = course.MaxCapacity,
            StartDate = course.StartDate
        };
    }

    public async Task<CourseUpdateResult> UpdateAsync(CourseEditViewModel viewModel)
    {
        // Lấy bản ghi đang hoạt động (tracked) để EF theo dõi thay đổi
        var course = await _courseRepository.GetByIdAsync(viewModel.Id);
        if (course == null) return CourseUpdateResult.NotFound;

        course.Code = viewModel.Code.Trim();
        course.Name = viewModel.Name.Trim();
        course.CourseCategoryId = viewModel.CourseCategoryId;
        course.Instructor = viewModel.Instructor.Trim();
        course.TuitionFee = viewModel.TuitionFee;
        course.CurrentEnrollment = viewModel.CurrentEnrollment;
        course.MaxCapacity = viewModel.MaxCapacity;
        course.StartDate = viewModel.StartDate;
        
        // Update ThumbnailPath if changed
        if (viewModel.ExistingThumbnailPath != course.ThumbnailPath)
        {
            course.ThumbnailPath = viewModel.ExistingThumbnailPath;
        }

        // So phiên bản user thấy lúc mở form với phiên bản hiện tại trong DB
        byte[] originalRowVersion;
        try
        {
            originalRowVersion = Convert.FromBase64String(viewModel.RowVersion);
        }
        catch (FormatException)
        {
            return CourseUpdateResult.ConcurrencyConflict;
        }
        _courseRepository.SetOriginalRowVersion(course, originalRowVersion);

        try
        {
            await _courseRepository.SaveChangesAsync();
            _logger.LogInformation("Course updated. CourseId={CourseId}, Code={Code}", course.Id, course.Code);
            return CourseUpdateResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict updating course. CourseId={CourseId}", viewModel.Id);
            return CourseUpdateResult.ConcurrencyConflict;
        }
    }

    public async Task<bool> SoftDeleteAsync(int id)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        if (course == null) return false;

        // Soft delete: đánh dấu IsDeleted thay vì xóa record. (Interceptor ApplyAuditAndSoftDelete
        // còn là lưới an toàn chặn mọi lệnh Remove() xóa cứng vô tình.)
        course.IsDeleted = true;
        course.DeletedAt = DateTime.Now;
        await _courseRepository.SaveChangesAsync();
        _logger.LogWarning("Course soft deleted. CourseId={CourseId}, Code={Code}", course.Id, course.Code);
        return true;
    }

    public async Task<List<CourseTrashItemViewModel>> GetTrashAsync()
    {
        var deleted = await _courseRepository.GetTrashReadOnlyAsync();
        return deleted.Select(c => new CourseTrashItemViewModel
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Category = c.CourseCategory.Name,
            DeletedAt = c.DeletedAt
        }).ToList();
    }

    public async Task<bool> RestoreAsync(int id)
    {
        var course = await _courseRepository.GetByIdIncludingDeletedAsync(id);
        if (course == null || !course.IsDeleted) return false;

        course.IsDeleted = false;
        course.DeletedAt = null;
        await _courseRepository.SaveChangesAsync();
        _logger.LogInformation("Course restored. CourseId={CourseId}, Code={Code}", course.Id, course.Code);
        return true;
    }

    // ---------- Lab05 Feature 2: điều chỉnh sĩ số có RowVersion ----------

    public async Task<CourseAdjustSeatsViewModel?> GetForAdjustSeatsAsync(int id)
    {
        var course = await _courseRepository.GetByIdReadOnlyAsync(id);
        if (course == null) return null;

        return new CourseAdjustSeatsViewModel
        {
            Id = course.Id,
            Code = course.Code,
            Name = course.Name,
            MaxCapacity = course.MaxCapacity,
            CurrentEnrollment = course.CurrentEnrollment,
            NewEnrollment = course.CurrentEnrollment,
            RowVersion = Convert.ToBase64String(course.RowVersion)
        };
    }

    public async Task<CourseAdjustResult> AdjustSeatsAsync(CourseAdjustSeatsViewModel viewModel)
    {
        var course = await _courseRepository.GetByIdAsync(viewModel.Id);
        if (course == null) return CourseAdjustResult.NotFound;

        // Nghiệp vụ: sĩ số mới không được vượt sức chứa (và không âm — đã chặn bằng Range ở ViewModel)
        if (viewModel.NewEnrollment > course.MaxCapacity)
            return CourseAdjustResult.ExceedsCapacity;

        course.CurrentEnrollment = viewModel.NewEnrollment;

        byte[] originalRowVersion;
        try
        {
            originalRowVersion = Convert.FromBase64String(viewModel.RowVersion);
        }
        catch (FormatException)
        {
            return CourseAdjustResult.ConcurrencyConflict;
        }
        _courseRepository.SetOriginalRowVersion(course, originalRowVersion);

        try
        {
            await _courseRepository.SaveChangesAsync();
            _logger.LogInformation(
                "Course seats adjusted. CourseId={CourseId}, Code={Code}, NewEnrollment={NewEnrollment}",
                course.Id, course.Code, course.CurrentEnrollment);
            return CourseAdjustResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict adjusting seats. CourseId={CourseId}", viewModel.Id);
            return CourseAdjustResult.ConcurrencyConflict;
        }
    }

    // ---------- Lab05 điểm cộng: nhật ký kiểm tra audit fields ----------

    public async Task<List<CourseAuditLogItemViewModel>> GetAuditLogAsync()
    {
        var courses = await _courseRepository.GetAllIncludingDeletedReadOnlyAsync();
        return courses.Select(c => new CourseAuditLogItemViewModel
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Instructor = c.Instructor,
            IsDeleted = c.IsDeleted,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            DeletedAt = c.DeletedAt
        }).ToList();
    }

    // ---------- Lab05 điểm cộng: xóa vĩnh viễn từ Trash ----------

    public async Task<bool> HardDeleteAsync(int id)
    {
        var trash = await _courseRepository.GetTrashReadOnlyAsync();
        if (!trash.Any(c => c.Id == id)) return false;

        await _courseRepository.HardDeleteAsync(id);
        _logger.LogWarning("Course hard deleted permanently. CourseId={CourseId}", id);
        return true;
    }

    // ---------- Lab05 điểm cộng: xuất CSV ----------

    public async Task<string> ExportCoursesCsvAsync()
    {
        var courses = await _courseRepository.GetAllReadOnlyAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Mã,Tên khóa học,Chuyên ngành,Giảng viên,Học phí,Sĩ số,Sức chứa,Ngày khai giảng");

        foreach (var c in courses.OrderBy(c => c.Code))
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(c.Code),
                EscapeCsv(c.Name),
                EscapeCsv(c.CourseCategory.Name),
                EscapeCsv(c.Instructor),
                c.TuitionFee.ToString("0", CultureInfo.InvariantCulture),
                c.CurrentEnrollment.ToString(CultureInfo.InvariantCulture),
                c.MaxCapacity.ToString(CultureInfo.InvariantCulture),
                c.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }

        _logger.LogInformation("Courses exported to CSV. Count={Count}", courses.Count);
        return sb.ToString();
    }

    // Bọc trong dấu ngoặc kép nếu trường chứa dấu phẩy/ngoặc kép/xuống dòng (chuẩn RFC 4180)
    private static string EscapeCsv(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        return field;
    }
}
