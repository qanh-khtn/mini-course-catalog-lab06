using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.ViewModels;

namespace MiniCourseCatalog.Mvc.Services.Interfaces;

/// <summary>Kết quả thao tác cập nhật khóa học (dùng cho Edit có RowVersion).</summary>
public enum CourseUpdateResult
{
    Success,
    NotFound,
    ConcurrencyConflict
}

/// <summary>Kết quả điều chỉnh sĩ số (Feature 2 — có RowVersion).</summary>
public enum CourseAdjustResult
{
    Success,
    NotFound,
    ConcurrencyConflict,
    ExceedsCapacity
}

public interface ICourseService
{
    Task<List<Course>> GetAllAsync();
    Task<Course?> GetByIdAsync(int id);
    Task<CourseStatsViewModel> GetStatsAsync();
    Task AddAsync(Course course);
    Task<List<Course>> SearchAsync(string keyword, string category);
    Task<List<string>> GetCategoryNamesAsync();
    Task<List<CourseCategory>> GetCourseCategoriesAsync();
    Task<bool> ExistsSameClassAsync(string code, string instructor, DateTime startDate);
    Task<List<CourseListItemViewModel>> FilterAsync(int? categoryId, decimal? minFee, decimal? maxFee);

    // --- Lab05: CRUD an toàn + soft delete ---
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<CourseEditViewModel?> GetForEditAsync(int id);
    Task<CourseDeleteViewModel?> GetForDeleteAsync(int id);
    Task<CourseUpdateResult> UpdateAsync(CourseEditViewModel viewModel);
    Task<bool> SoftDeleteAsync(int id);
    Task<List<CourseTrashItemViewModel>> GetTrashAsync();
    Task<bool> RestoreAsync(int id);

    // --- Lab05 Feature 2: điều chỉnh sĩ số có RowVersion ---
    Task<CourseAdjustSeatsViewModel?> GetForAdjustSeatsAsync(int id);
    Task<CourseAdjustResult> AdjustSeatsAsync(CourseAdjustSeatsViewModel viewModel);

    // --- Lab05 điểm cộng: xóa vĩnh viễn từ Trash ---
    Task<bool> HardDeleteAsync(int id);

    // --- Lab05 điểm cộng: nhật ký kiểm tra audit fields ---
    Task<List<CourseAuditLogItemViewModel>> GetAuditLogAsync();

    // --- Lab05 điểm cộng: xuất CSV danh sách khóa học đang hoạt động ---
    Task<string> ExportCoursesCsvAsync();
}
