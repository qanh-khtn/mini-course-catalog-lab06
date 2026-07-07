using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in.", model.Email);
                await _auditLogService.LogAsync("Login", "ApplicationUser", model.Email, "Success");
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return LocalRedirect(model.ReturnUrl);
                }
                return RedirectToAction("Index", "Home");
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
        return RedirectToAction("Login", "Account");
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> AccessDenied()
    {
        await _auditLogService.LogAsync("AccessDenied", "ApplicationUser", User.Identity?.Name, "Fail", "Attempted to access forbidden resource");
        return View();
    }
}
