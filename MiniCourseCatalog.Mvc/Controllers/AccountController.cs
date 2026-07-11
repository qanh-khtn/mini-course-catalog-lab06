using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Services.Interfaces;
using MiniCourseCatalog.Mvc.ViewModels;

namespace MiniCourseCatalog.Mvc.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;
    private readonly IAuditLogService _auditLogService;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AccountController> logger, IAuditLogService auditLogService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: false, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in.", model.Email);
                await _auditLogService.LogAsync("Login", "ApplicationUser", model.Email, "Success");
                TempData["SuccessMessage"] = "Đăng nhập thành công!";
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return LocalRedirect(model.ReturnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
            else if (result.IsLockedOut)
            {
                await _auditLogService.LogAsync("AccountLockedOut", "ApplicationUser", model.Email, "Fail", "Tài khoản bị khóa tạm do đăng nhập sai quá nhiều lần");
                ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa tạm do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau vài phút.");
            }
            else
            {
                await _auditLogService.LogAsync("Login", "ApplicationUser", model.Email, "Fail", "Invalid credentials");
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng");
            }
        }
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Default to User role
                await _userManager.AddToRoleAsync(user, "User");
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("User {Email} registered and logged in.", model.Email);
                await _auditLogService.LogAsync("Register", "ApplicationUser", model.Email, "Success");
                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công!";
                return RedirectToAction("Index", "Home");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            await _auditLogService.LogAsync("Register", "ApplicationUser", model.Email, "Fail", "Validation failed");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userName = User.Identity?.Name;
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User {UserName} logged out.", userName);
        await _auditLogService.LogAsync("Logout", "ApplicationUser", userName, "Success");
        TempData["SuccessMessage"] = "Đăng xuất thành công!";
        return RedirectToAction("Login", "Account");
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> AccessDenied(string? attempted = null)
    {
        ViewData["AttemptedPath"] = attempted;
        ViewData["RequiredPolicy"] = ResolvePolicyFromPath(attempted);
        await _auditLogService.LogAsync("AccessDenied", "ApplicationUser", User.Identity?.Name, "Fail", attempted);
        return View();
    }

    private static string ResolvePolicyFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Unknown";

        var normalized = path.ToLowerInvariant();

        if (normalized.Contains("/courses/create") ||
            normalized.Contains("/courses/edit") ||
            normalized.Contains("/courses/delete") ||
            normalized.Contains("/courses/trash") ||
            normalized.Contains("/courses/restore") ||
            normalized.Contains("/courses/harddelete"))
            return "CanManageCourse";

        if (normalized.Contains("/courses/uploadthumbnail"))
            return "CanUploadCourseThumbnail";

        if (normalized.Contains("/auditlogs"))
            return "CanViewAuditLog";

        if (normalized.Contains("/courses/adjustseats"))
            return "CanAdjustSeats";

        if (normalized.StartsWith("/courses"))
            return "CanViewCourse";

        return "Unknown";
    }
}
