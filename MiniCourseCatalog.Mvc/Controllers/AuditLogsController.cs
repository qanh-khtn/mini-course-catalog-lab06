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

    public async Task<IActionResult> Index(AuditLogSearchViewModel model)
    {

        var query = _auditLogService.GetQueryable();

        if (!string.IsNullOrWhiteSpace(model.User))
        {
            var user = model.User.Trim().ToLower();
            query = query.Where(a =>
                a.UserName != null && a.UserName.ToLower().Contains(user));
        }

        if (!string.IsNullOrWhiteSpace(model.ActionName))
        {
            var action = model.ActionName.Trim();
            query = query.Where(a => a.Action == action);
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
