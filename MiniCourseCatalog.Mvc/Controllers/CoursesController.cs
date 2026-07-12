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
    private readonly IFileUploadService _fileUploadService;
    private readonly IAuditLogService _auditLogService;

    public CoursesController(
        ICourseService courseService,
        IEnrollmentService enrollmentService,
        IStudentService studentService,
        IFileUploadService fileUploadService,
        IAuditLogService auditLogService)
    {
        _courseService = courseService;
        _enrollmentService = enrollmentService;
        _studentService = studentService;
        _fileUploadService = fileUploadService;
        _auditLogService = auditLogService;
    }

    [Authorize(Policy = "CanViewCourse")]
    public async Task<IActionResult> Index(string keyword = "", string category = "", int page = 1)
    {
        page = Math.Max(1, page);
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

        int pageSize = 12;
        var totalItems = courseItems.Count;
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var pagedItems = courseItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var pagedCourses = new PaginationViewModel<CourseListItemViewModel>
        {
            Items = pagedItems,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        var viewModel = new CourseIndexViewModel
        {
            Courses = pagedCourses,
            Categories = categories,
            Keyword = keyword,
            Category = category,
            TotalCoursesBeforeFilter = rawCourses.Count
        };

        return View(viewModel);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Detail(int id)
    {

        var course = await _courseService.GetByIdAsync(id);
        if (course == null)
            return NotFound($"Không thể tìm thấy thông tin khóa học với mã ID = {id}");

        var reviews = await _courseService.GetReviewsAsync(id);
        reviews = reviews.Where(r => !r.IsHidden).ToList();
        var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

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
            StartDate = course.StartDate,
            Reviews = reviews,
            ReviewCount = reviews.Count,
            AverageRating = averageRating
        };

        return View(detailVm);
    }

    [HttpPost]
    [Authorize(Policy = "CanEnrollCourse")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(AddReviewViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            TempData["ErrorMessage"] = firstError ?? "Dữ liệu đánh giá không hợp lệ.";
            return RedirectToAction(nameof(Detail), new { id = model.CourseId });
        }

        var course = await _courseService.GetByIdAsync(model.CourseId);
        if (course == null)
        {
            return NotFound();
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var success = await _courseService.AddReviewAsync(model.CourseId, userId, model.Rating, model.Comment ?? string.Empty);
        if (!success)
        {
            TempData["ErrorMessage"] = "Bạn đã đánh giá khóa học này rồi.";
        }
        else
        {
            TempData["SuccessMessage"] = "Đánh giá của bạn đã được gửi thành công!";
        }

        return RedirectToAction(nameof(Detail), new { id = model.CourseId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HideReview(int id)
    {
        var success = await _courseService.HideReviewAsync(id);
        if (success)
        {
            await _auditLogService.LogAsync(
                action: "HideReview",
                entityName: "CourseReview",
                entityId: id.ToString(),
                result: "Success"
            );
            TempData["SuccessMessage"] = "Đã ẩn đánh giá.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không tìm thấy đánh giá.";
        }
        
        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            return Redirect(referer);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "CanViewCourse")]
    public async Task<IActionResult> Stats()
    {

        var statsVm = await _courseService.GetStatsAsync();
        return View(statsVm);
    }

    // Lab05 điểm cộng: xuất danh sách khóa học ra file CSV (UTF-8 BOM để Excel đọc đúng tiếng Việt)
    [HttpGet]
    [Authorize(Policy = "CanViewCourse")]
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
    [Authorize(Policy = "CanViewCourse")]
    public async Task<IActionResult> Filter(int? categoryId, decimal? minFee, decimal? maxFee)
    {

        var categories = await _courseService.GetCourseCategoriesAsync();
        var vm = new CourseFilterViewModel
        {
            CategoryId = categoryId,
            MinFee = minFee,
            MaxFee = maxFee,
            Categories = categories,
            HasSearched = categoryId.HasValue || minFee.HasValue || maxFee.HasValue
        };

        if (vm.HasSearched)
            vm.Results = await _courseService.FilterAsync(categoryId, minFee, maxFee);

        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = "CanViewCourse")]
    public async Task<IActionResult> Search(string keyword = "", string category = "")
    {

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
            Categories = await _courseService.GetCategoryNamesAsync(),
            Results = results
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = "CanManageCourse")]
    public async Task<IActionResult> Create()
    {

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
    [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Create(CourseCreateViewModel viewModel)
    {

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

        string? thumbnailPath = null;
        if (viewModel.Thumbnail != null)
        {
            try
            {
                thumbnailPath = await _fileUploadService.SaveCourseThumbnailAsync(viewModel.Thumbnail);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(viewModel.Thumbnail), ex.Message);
                await PopulateCategoryOptionsAsync(viewModel);
                return View(viewModel);
            }
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
            StartDate = viewModel.StartDate,
            ThumbnailPath = thumbnailPath
        };

        await _courseService.AddAsync(course);
        await _auditLogService.LogAsync("CreateCourse", "Course", course.Code, "Success");

        TempData["SuccessMessage"] = "Khóa học đã được tạo thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = "CanManageEnrollment")]
    public async Task<IActionResult> Enroll()
    {

        var vm = await BuildEnrollViewModelAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageEnrollment")]
    public async Task<IActionResult> Enroll(EnrollViewModel viewModel)
    {

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
            return RedirectToAction(nameof(Enroll));
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
    public async Task<IActionResult> Edit(int id)
    {

        var viewModel = await _courseService.GetForEditAsync(id);
        if (viewModel == null)
            return NotFound($"Không thể tìm thấy khóa học với mã ID = {id}");

        await PopulateCategoryOptionsAsync(viewModel);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageCourse")]
    public async Task<IActionResult> Edit(int id, CourseEditViewModel viewModel)
    {

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
                await _auditLogService.LogAsync("EditCourse", "Course", viewModel.Code, "Success");
                TempData["SuccessMessage"] = "Cập nhật khóa học thành công.";
                return RedirectToAction(nameof(Index));
            
            case CourseUpdateResult.NotFound:
                await _auditLogService.LogAsync("EditCourse", "Course", viewModel.Code, "Fail", "Course not found");
                TempData["ErrorMessage"] = "Không tìm thấy khóa học cần cập nhật.";
                return RedirectToAction(nameof(Index));
            
            case CourseUpdateResult.ConcurrencyConflict:
                await _auditLogService.LogAsync("EditCourse", "Course", viewModel.Code, "Fail", "Concurrency conflict");
                ModelState.AddModelError(string.Empty, "Dữ liệu đã bị người khác thay đổi trước đó. Vui lòng kiểm tra lại.");
                await PopulateCategoryOptionsAsync(viewModel);
                return View(viewModel);
            
            default:
                await PopulateCategoryOptionsAsync(viewModel);
                return View(viewModel);
        }
    }

    // ---------- Lab06 Feature 2: Thay thumbnail fault-tolerant ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanUploadCourseThumbnail")]
    [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> UploadThumbnail(int id, IFormFile thumbnail)
    {

        var course = await _courseService.GetByIdAsync(id);
        if (course == null)
            return NotFound($"Không thể tìm thấy khóa học với mã ID = {id}");

        var oldPath = course.ThumbnailPath;
        string? newPath = null;

        try
        {
            newPath = await _fileUploadService.SaveCourseThumbnailAsync(thumbnail);
        }
        catch (InvalidOperationException ex)
        {
            await _auditLogService.LogAsync("ReplaceCourseThumbnail", "Course", id.ToString(), "Fail", ex.Message);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _courseService.UpdateThumbnailAsync(id, newPath);
        if (result == CourseUpdateResult.Success)
        {
            await _auditLogService.LogAsync("ReplaceCourseThumbnail", "Course", id.ToString(), "Success");
            if (!string.IsNullOrEmpty(oldPath))
                _fileUploadService.DeleteFile(oldPath);
            TempData["SuccessMessage"] = "Ảnh thumbnail đã được cập nhật thành công.";
        }
        else
        {
            if (!string.IsNullOrEmpty(newPath))
                _fileUploadService.DeleteFile(newPath);
            await _auditLogService.LogAsync("ReplaceCourseThumbnail", "Course", id.ToString(), "Fail", "Database update failed");
            TempData["ErrorMessage"] = "Lỗi khi lưu vào cơ sở dữ liệu. Ảnh cũ được giữ nguyên.";
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    // ---------- Lab05: Delete confirmation -> soft delete ----------

    [HttpGet]
    [Authorize(Policy = "CanManageCourse")]
    public async Task<IActionResult> Delete(int id)
    {

        var viewModel = await _courseService.GetForDeleteAsync(id);
        if (viewModel == null)
            return NotFound($"Không thể tìm thấy khóa học với mã ID = {id}");

        return View(viewModel);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageCourse")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {

        var success = await _courseService.SoftDeleteAsync(id);
        if (success)
        {
            await _auditLogService.LogAsync("DeleteCourse", "Course", id.ToString(), "Success");
            TempData["SuccessMessage"] = "Khóa học đã được chuyển vào Thùng rác.";
        }
        else
        {
            await _auditLogService.LogAsync("DeleteCourse", "Course", id.ToString(), "Fail", "Not found");
            TempData["ErrorMessage"] = "Không tìm thấy khóa học để xóa.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ---------- Lab05 Feature 2: Điều chỉnh sĩ số (RowVersion) ----------

    [HttpGet]
    [Authorize(Policy = "CanAdjustSeats")]
    public async Task<IActionResult> AdjustSeats(int id)
    {

        var viewModel = await _courseService.GetForAdjustSeatsAsync(id);
        if (viewModel == null)
            return NotFound($"Không thể tìm thấy khóa học với mã ID = {id}");

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanAdjustSeats")]
    public async Task<IActionResult> AdjustSeats(int id, CourseAdjustSeatsViewModel viewModel)
    {

        if (id != viewModel.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(viewModel);

        var result = await _courseService.AdjustSeatsAsync(viewModel);
        switch (result)
        {
            case CourseAdjustResult.Success:
                await _auditLogService.LogAsync("AdjustSeats", "Course", viewModel.Code, "Success");
                TempData["SuccessMessage"] = "Đã cập nhật sĩ số thành công.";
                return RedirectToAction(nameof(Index));
            
            case CourseAdjustResult.NotFound:
                await _auditLogService.LogAsync("AdjustSeats", "Course", viewModel.Code, "Fail", "Not found");
                TempData["ErrorMessage"] = "Không tìm thấy khóa học.";
                return RedirectToAction(nameof(Index));
            
            case CourseAdjustResult.ExceedsCapacity:
                await _auditLogService.LogAsync("AdjustSeats", "Course", viewModel.Code, "Fail", "Exceeds capacity");
                ModelState.AddModelError("NewEnrollment", "Sĩ số không được vượt quá sức chứa tối đa.");
                return View(viewModel);

            case CourseAdjustResult.ConcurrencyConflict:
                await _auditLogService.LogAsync("AdjustSeats", "Course", viewModel.Code, "Fail", "Concurrency conflict");
                ModelState.AddModelError(string.Empty, "Dữ liệu đã bị người khác thay đổi (Last-Save-Wins). Vui lòng F5 để làm mới trang trước khi nhập lại.");
                var fresh = await _courseService.GetForAdjustSeatsAsync(viewModel.Id);
                if (fresh != null)
                {
                    viewModel.CurrentEnrollment = fresh.CurrentEnrollment;
                    viewModel.MaxCapacity = fresh.MaxCapacity;
                    viewModel.RowVersion = fresh.RowVersion;
                }
                return View(viewModel);
            
            default:
                return View(viewModel);
        }
    }

    // ---------- Lab05: Trash + Restore ----------

    [HttpGet]
    [Authorize(Policy = "CanManageCourse")]
    public async Task<IActionResult> Trash()
    {

        var items = await _courseService.GetTrashAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageCourse")]
    public async Task<IActionResult> Restore(int id)
    {

        var ok = await _courseService.RestoreAsync(id);
        if (ok)
        {
            await _auditLogService.LogAsync("RestoreCourse", "Course", id.ToString(), "Success");
            TempData["SuccessMessage"] = "Đã khôi phục khóa học về danh sách hoạt động.";
        }
        else
        {
            await _auditLogService.LogAsync("RestoreCourse", "Course", id.ToString(), "Fail", "Not found");
            TempData["ErrorMessage"] = "Không tìm thấy khóa học để khôi phục.";
        }
        return RedirectToAction(nameof(Trash));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // HardDelete là thao tác phá hủy không thể hoàn tác — gắn cứng với role Admin
    // thay vì policy nghiệp vụ để đảm bảo chỉ Admin thực sự mới xóa được vĩnh viễn.
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> HardDelete(int id)
    {

        var ok = await _courseService.HardDeleteAsync(id);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
            ? "Đã xóa vĩnh viễn khóa học khỏi hệ thống."
            : "Không tìm thấy khóa học trong thùng rác.";
        return RedirectToAction(nameof(Trash));
    }

    [HttpGet]
    [Authorize(Policy = "CanViewAuditLog")]
    public async Task<IActionResult> AuditLog()
    {

        var items = await _courseService.GetAuditLogAsync();
        return View(items);
    }

    [AllowAnonymous]
    public IActionResult Welcome() =>
        Content("Hệ thống quản lý đào tạo Mini Training Center xin chào học viên!");

    /// <summary>
    /// Trang danh mục khóa học công khai — không yêu cầu đăng nhập.
    /// Hiển thị toàn bộ khóa học chưa bị soft-delete kèm thumbnail và thông tin cơ bản.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Catalog(int page = 1)
    {
        var model = await _courseService.GetCatalogAsync(page);
        return View(model);
    }

    [Authorize(Policy = "CanViewCourse")]
    public async Task<IActionResult> CourseJson() =>
        Json(await _courseService.GetAllAsync());

    [Authorize(Policy = "CanViewCourse")]
    public IActionResult GoToList() =>
        RedirectToAction(nameof(Index));

    [AllowAnonymous]
    public IActionResult Force404() =>
        NotFound("Đây là trang phản hồi mẫu 404 thử nghiệm từ hệ thống.");

    [AllowAnonymous]
    public IActionResult CategoryInfo() =>
        Content("Xem danh mục tại /DataHealth");

    private async Task PopulateCategoryOptionsAsync(CourseCreateViewModel viewModel)
    {
        var categories = await _courseService.GetCourseCategoriesAsync();
        viewModel.CategoryOptions = categories
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToList();
    }



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
