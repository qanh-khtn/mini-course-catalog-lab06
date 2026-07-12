using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Services.Interfaces;
using MiniCourseCatalog.Mvc.ViewModels;

namespace MiniCourseCatalog.Mvc.Controllers;

[Authorize(Policy = "CanManageUsers")]
public class UserManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;

    public UserManagementController(UserManager<ApplicationUser> userManager, IAuditLogService auditLogService)
    {
        _userManager = userManager;
        _auditLogService = auditLogService;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.AsNoTracking().ToListAsync();
        var model = new List<UserManagementViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            model.Add(new UserManagementViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName,
                Department = user.Department,
                CurrentRole = roles.FirstOrDefault() ?? "User"
            });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string userId, string newRole)
    {
        var validRoles = new[] { "Admin", "Staff", "User" };
        if (!validRoles.Contains(newRole))
        {
            TempData["ErrorMessage"] = "Role không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var userToChange = await _userManager.FindByIdAsync(userId);
        if (userToChange == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
            return RedirectToAction(nameof(Index));
        }

        var currentUser = User.Identity?.Name;
        if (string.Equals(userToChange.UserName, currentUser, StringComparison.OrdinalIgnoreCase) && newRole != "Admin")
        {
            await _auditLogService.LogAsync("ChangeUserRole", "ApplicationUser", userId, "Fail", $"Cố ý tự hạ quyền bởi {currentUser}");
            TempData["ErrorMessage"] = "Không thể tự hạ quyền của chính mình.";
            return RedirectToAction(nameof(Index));
        }

        var currentRoles = await _userManager.GetRolesAsync(userToChange);
        var oldRole = currentRoles.FirstOrDefault() ?? "None";

        if (oldRole != newRole)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(userToChange, currentRoles);
            if (!removeResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa role cũ.";
                return RedirectToAction(nameof(Index));
            }

            var addResult = await _userManager.AddToRoleAsync(userToChange, newRole);
            if (addResult.Succeeded)
            {
                await _auditLogService.LogAsync("ChangeUserRole", "ApplicationUser", userId, "Success", $"Đổi role từ {oldRole} sang {newRole} bởi {currentUser}");
                TempData["SuccessMessage"] = "Cập nhật quyền thành công!";
            }
            else
            {
                await _auditLogService.LogAsync("ChangeUserRole", "ApplicationUser", userId, "Fail", $"Lỗi khi gán role mới bởi {currentUser}");
                TempData["ErrorMessage"] = "Lỗi khi gán role mới.";
            }
        }
        else
        {
            TempData["SuccessMessage"] = "Role không thay đổi.";
        }

        return RedirectToAction(nameof(Index));
    }
}
