using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCourseCatalog.Mvc.Services.Interfaces;
using MiniCourseCatalog.Mvc.ViewModels;

namespace MiniCourseCatalog.Mvc.Controllers;

[Authorize(Policy = "CanViewAuditLog")]
public class AuditLogsController : Controller
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task<IActionResult> Index(AuditLogSearchViewModel model, string theme = "light")
    {
        ViewData["Theme"] = theme;

        var query = _auditLogService.GetQueryable();

        if (!string.IsNullOrWhiteSpace(model.Keyword))
        {
            var keyword = model.Keyword.Trim().ToLower();
            query = query.Where(a => 
                (a.UserName != null && a.UserName.ToLower().Contains(keyword)) ||
                a.Action.ToLower().Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(model.Result))
        {
            query = query.Where(a => a.Result == model.Result);
        }

        if (model.FromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt.Date >= model.FromDate.Value.Date);
        }

        if (model.ToDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt.Date <= model.ToDate.Value.Date);
        }

        model.Logs = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();

        return View(model);
    }
}
