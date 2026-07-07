using MiniCourseCatalog.Mvc.Models;

namespace MiniCourseCatalog.Mvc.Services.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(string action, string entityName, string? entityId, string result, string? note = null);
    IQueryable<AuditLog> GetQueryable();
}
