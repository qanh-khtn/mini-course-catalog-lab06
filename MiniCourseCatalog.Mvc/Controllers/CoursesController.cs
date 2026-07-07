using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Services.Interfaces;
using MiniCourseCatalog.Mvc.ViewModels;

namespace MiniCourseCatalog.Mvc.Controllers;

public class CoursesController : Controller
{
    private readonly ICourseService _courseService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IStudentService _studentService;

    public CoursesController(
        ICourseService courseService,
        IEnrollmentService enrollmentService,
        IStudentService studentService)
    {
        _courseService = courseService;
        _enrollmentService = enrollmentService;
        _studentService = studentService;
    }

    public async Task<IActionResult> Index(string keyword = "", string category = "", string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        var rawCourses = await _courseService.GetAllAsync();
        var categories = rawCourses
            .Select(c => c.CourseCategory.Name)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        var filtered = rawCourses.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(keyword))
            filtered = filtered.Where(c =>
                c.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                c.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                c.Instructor.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(category))
            filtered = filtered.Where(c =>
                string.Equals(c.CourseCategory.Name, category, StringComparison.OrdinalIgnoreCase));

        var courseItems = filtered.Select(c => new CourseListItemViewModel
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

        var viewModel = new CourseIndexViewModel
        {
            Courses = courseItems,
            Categories = categories,
            Keyword = keyword,
            Category = category,
            Theme = theme,
            TotalCoursesBeforeFilter = rawCourses.Count
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Detail(int id, string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        var course = await _courseService.GetByIdAsync(id);
        if (course == null)
            return NotFound($"Không thể tìm thấy thông tin khóa học với mã ID = {id}");

        var detailVm = new CourseDetailViewModel
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

        return View(detailVm);
    }

    public async Task<IActionResult> Stats(string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        var statsVm = await _courseService.GetStatsAsync();
        return View(statsVm);
    }

    // Lab05 điểm cộng: xuất danh sách khóa học ra file CSV (UTF-8 BOM để Excel đọc đúng tiếng Việt)
    [HttpGet]
    public async Task<IActionResult> Export()
    {
        var csv = await _courseService.ExportCoursesCsvAsync();
        var preamble = Encoding.UTF8.GetPreamble();           // BOM
        var content = Encoding.UTF8.GetBytes(csv);
        var bytes = preamble.Concat(content).ToArray();

        var fileName = $"khoa-hoc-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> Filter(int? categoryId, decimal? minFee, decimal? maxFee, string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        var categories = await _courseService.GetCourseCategoriesAsync();
        var vm = new CourseFilterViewModel
        {
            CategoryId = categoryId,
            MinFee = minFee,
            MaxFee = maxFee,
            Theme = theme,
            Categories = categories,
            HasSearched = categoryId.HasValue || minFee.HasValue || maxFee.HasValue
        };

        if (vm.HasSearched)
            vm.Results = await _courseService.FilterAsync(categoryId, minFee, maxFee);

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string keyword = "", string category = "", string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        var results = (await _courseService.SearchAsync(keyword, category))
            .Select(c => new CourseListItemViewModel
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Category = c.CourseCategory.Name,
                Instructor = c.Instructor,
                TuitionFee = c.TuitionFee,
                CurrentEnrollment = c.CurrentEnrollment,
                MaxCapacity = c.MaxCapacity
            })
            .ToList();

        var viewModel = new CourseSearchViewModel
        {
            Keyword = keyword,
            Category = category,
            Theme = theme,
            Categories = await _courseService.GetCategoryNamesAsync(),
            Results = results
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = "CanManageCourse")]
    public async Task<IActionResult> Create(string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        var categories = await _courseService.GetCourseCategoriesAsync();
        var viewModel = new CourseCreateViewModel
        {
            StartDate = DateTime.Today,
            MaxCapacity = 20,
            CategoryOptions = categories
                .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
                .ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageCourse")]
    public async Task<IActionResult> Create(CourseCreateViewModel viewModel, string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        // Custom validation nghiệp vụ: CourseCode là mã định danh duy nhất, không được trùng
        if (await _courseService.CodeExistsAsync(viewModel.Code))
        {
            ModelState.AddModelError(
                nameof(viewModel.Code),
                $"Mã khóa học '{viewModel.Code}' đã tồn tại. Vui lòng dùng mã khác.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoryOptionsAsync(viewModel);
            return View(viewModel);
        }

        var course = new Course
        {
            Code = viewModel.Code,
            Name = viewModel.Name,
            CourseCategoryId = viewModel.CourseCategoryId,
            Instructor = viewModel.Instructor,
            TuitionFee = viewModel.TuitionFee,
            CurrentEnrollment = viewModel.CurrentEnrollment,
            MaxCapacity = viewModel.MaxCapacity,
            StartDate = viewModel.StartDate
        };

        await _courseService.AddAsync(course);
        TempData["SuccessMessage"] = $"Đã thêm khóa học '{course.Name}' thành công.";
        return RedirectToAction(nameof(Index), new { theme });
    }

    [HttpGet]
    [Authorize(Policy = "CanEnrollCourse")]
    public async Task<IActionResult> Enroll(string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        var vm = await BuildEnrollViewModelAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEnrollCourse")]
    public async Task<IActionResult> Enroll(EnrollViewModel viewModel, string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        if (!ModelState.IsValid)
        {
            var fresh = await BuildEnrollViewModelAsync();
            fresh.CourseId = viewModel.CourseId;
            fresh.StudentId = viewModel.StudentId;
            return View(fresh);
        }

        var (success, message) = await _enrollmentService.EnrollStudentAsync(viewModel.CourseId, viewModel.StudentId);

        if (success)
        {
            // PRG: redirect sau khi ghi thành công — toast hiện ở trang mới, F5 không submit lại form
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Enroll), new { theme });
        }

        // Thất bại (hết chỗ / trùng / lỗi concurrency): giữ thông báo inline trên form
        var vm = await BuildEnrollViewModelAsync();
        vm.CourseId = viewModel.CourseId;
        vm.StudentId = viewModel.StudentId;
        vm.IsSuccess = success;
        vm.ResultMessage = message;

        return View(vm);
    }

    // ---------- Lab05: Edit (có RowVersion) ----------

    [HttpGet]
    [Authorize(Policy = "CanManageCourse")]
    public async Task<IActionResult> Edit(int id, string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        var viewModel = await _courseService.GetForEditAsync(id);
        if (viewModel == null)
            return NotFound($"Không thể tìm thấy khóa học với mã ID = {id}");

        await PopulateCategoryOptionsAsync(viewModel);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageCourse")]
    public async Task<IActionResult> Edit(int id, CourseEditViewModel viewModel, string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        if (id != viewModel.Id)
            return NotFound();

        // CourseCode duy nhất — bỏ qua chính bản ghi đang sửa
        if (await _courseService.CodeExistsAsync(viewModel.Code, viewModel.Id))
        {
            ModelState.AddModelError(
                nameof(viewModel.Code),
                $"Mã khóa học '{viewModel.Code}' đã tồn tại ở khóa học khác.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoryOptionsAsync(viewModel);
            return View(viewModel);
        }

        var result = await _courseService.UpdateAsync(viewModel);
        switch (result)
        {
            case CourseUpdateResult.Success:
                TempData["SuccessMessage"] = $"Đã cập nhật khóa học '{viewModel.Name}'.";
                return RedirectToAction(nameof(Index), new { theme });

            case CourseUpdateResult.NotFound:
                return NotFound();

            default: // ConcurrencyConflict
                ModelState.AddModelError(string.Empty,
                    "Dữ liệu đã được người khác cập nhật trong lúc bạn đang sửa. Vui lòng tải lại trang và thử lại.");
                await PopulateCategoryOptionsAsync(viewModel);
                return View(viewModel);
        }
    }

    // ---------- Lab05: Delete confirmation -> soft delete ----------

    [HttpGet]
    [Authorize(Policy = "CanManageCourse")]
    public async Task<IActionResult> Delete(int id, string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        var viewModel = await _courseService.GetForDeleteAsync(id);
        if (viewModel == null)
            return NotFound($"Không thể tìm thấy khóa học với mã ID = {id}");

        return View(viewModel);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageCourse")]
    public async Task<IActionResult> DeleteConfirmed(int id, string theme = "light")
    {
        theme = NormalizeTheme(theme);

        var ok = await _courseService.SoftDeleteAsync(id);
        if (!ok)
            return NotFound();

        TempData["SuccessMessage"] = "Đã chuyển khóa học vào thùng rác (xóa mềm).";
        return RedirectToAction(nameof(Index), new { theme });
    }

    // ---------- Lab05 Feature 2: Điều chỉnh sĩ số (RowVersion) ----------

    [HttpGet]
    [Authorize(Policy = "CanAdjustSeats")]
    public async Task<IActionResult> AdjustSeats(int id, string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        var viewModel = await _courseService.GetForAdjustSeatsAsync(id);
        if (viewModel == null)
            return NotFound($"Không thể tìm thấy khóa học với mã ID = {id}");

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanAdjustSeats")]
    public async Task<IActionResult> AdjustSeats(int id, CourseAdjustSeatsViewModel viewModel, string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        if (id != viewModel.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(viewModel);

        var result = await _courseService.AdjustSeatsAsync(viewModel);
        switch (result)
        {
            case CourseAdjustResult.Success:
                TempData["SuccessMessage"] = $"Đã cập nhật sĩ số khóa học '{viewModel.Name}' thành {viewModel.NewEnrollment}.";
                return RedirectToAction(nameof(Index));

            case CourseAdjustResult.NotFound:
                return NotFound();

            case CourseAdjustResult.ExceedsCapacity:
                ModelState.AddModelError(nameof(viewModel.NewEnrollment),
                    $"Sĩ số mới ({viewModel.NewEnrollment}) không được vượt quá sức chứa ({viewModel.MaxCapacity}).");
                return View(viewModel);

            default: // ConcurrencyConflict
                ModelState.AddModelError(string.Empty,
                    "Dữ liệu đã được người khác cập nhật trong lúc bạn đang chỉnh. Vui lòng tải lại trang và thử lại.");
                var fresh = await _courseService.GetForAdjustSeatsAsync(viewModel.Id);
                if (fresh != null)
                {
                    viewModel.CurrentEnrollment = fresh.CurrentEnrollment;
                    viewModel.MaxCapacity = fresh.MaxCapacity;
                    viewModel.RowVersion = fresh.RowVersion;
                }
                return View(viewModel);
        }
    }

    // ---------- Lab05: Trash + Restore ----------

    [HttpGet]
    [Authorize(Policy = "CanManageCourse")]
    public async Task<IActionResult> Trash(string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        var items = await _courseService.GetTrashAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageCourse")]
    public async Task<IActionResult> Restore(int id, string theme = "light")
    {
        theme = NormalizeTheme(theme);

        var ok = await _courseService.RestoreAsync(id);
        TempData["SuccessMessage"] = ok
            ? "Đã khôi phục khóa học về danh sách hoạt động."
            : "Không tìm thấy khóa học để khôi phục.";
        return RedirectToAction(nameof(Trash), new { theme });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageCourse")]
    public async Task<IActionResult> HardDelete(int id, string theme = "light")
    {
        theme = NormalizeTheme(theme);

        var ok = await _courseService.HardDeleteAsync(id);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
            ? "Đã xóa vĩnh viễn khóa học khỏi hệ thống."
            : "Không tìm thấy khóa học trong thùng rác.";
        return RedirectToAction(nameof(Trash), new { theme });
    }

    [HttpGet]
    [Authorize(Policy = "CanViewAuditLog")]
    public async Task<IActionResult> AuditLog(string theme = "light")
    {
        theme = NormalizeTheme(theme);
        ViewData["Theme"] = theme;

        var items = await _courseService.GetAuditLogAsync();
        return View(items);
    }

    public IActionResult Welcome() =>
        Content("Hệ thống quản lý đào tạo Mini Training Center xin chào học viên!");

    public async Task<IActionResult> CourseJson() =>
        Json(await _courseService.GetAllAsync());

    public IActionResult GoToList() =>
        RedirectToAction(nameof(Index));

    public IActionResult Force404() =>
        NotFound("Đây là trang phản hồi mẫu 404 thử nghiệm từ hệ thống.");

    public IActionResult CategoryInfo() =>
        Content("Xem danh mục tại /DataHealth");

    private async Task PopulateCategoryOptionsAsync(CourseCreateViewModel viewModel)
    {
        var categories = await _courseService.GetCourseCategoriesAsync();
        viewModel.CategoryOptions = categories
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToList();
    }

    private static string NormalizeTheme(string theme) =>
        string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light";

    private async Task<EnrollViewModel> BuildEnrollViewModelAsync()
    {
        var courses = await _courseService.GetAllAsync();
        var students = await _studentService.GetAllAsync();
        return new EnrollViewModel
        {
            CourseOptions = courses
                .Select(c => new SelectListItem($"{c.Code} – {c.Name} ({c.CurrentEnrollment}/{c.MaxCapacity})", c.Id.ToString()))
                .ToList(),
            StudentOptions = students
                .Select(s => new SelectListItem($"{s.FullName} ({s.Email})", s.Id.ToString()))
                .ToList()
        };
    }
}
