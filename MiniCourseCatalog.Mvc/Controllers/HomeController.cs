using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Services.Interfaces;
using MiniCourseCatalog.Mvc.ViewModels;

namespace MiniCourseCatalog.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly IAuditLogService _auditLogService;

    public HomeController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task<IActionResult> Index()
    {

        var model = new SecurityDashboardViewModel();

        if (User.IsInRole("Admin"))
        {
            var today = DateTime.UtcNow.Date;
            var query = _auditLogService.GetQueryable();

            model.AccessDeniedCountToday = await query
                .CountAsync(a => a.Action == "AccessDenied" && a.CreatedAt.Date == today);

            var sensitiveActions = new[] { "CreateCourse", "EditCourse", "DeleteCourse", "RestoreCourse", "AdjustSeats", "ReplaceCourseThumbnail" };
            model.SensitiveActionsCountToday = await query
                .CountAsync(a => sensitiveActions.Contains(a.Action) && a.CreatedAt.Date == today);

            model.FailedUploadsCountToday = await query
                .CountAsync(a => a.Action == "ReplaceCourseThumbnail" && a.Result == "Fail" && a.CreatedAt.Date == today);
        }

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
