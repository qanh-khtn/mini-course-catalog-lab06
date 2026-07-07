using Microsoft.EntityFrameworkCore;
using MiniCourseCatalog.Mvc.Data;
using MiniCourseCatalog.Mvc.Models;
using MiniCourseCatalog.Mvc.Services.Interfaces;
using System.Security.Claims;

namespace MiniCourseCatalog.Mvc.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(AppDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<AuditLogService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(string action, string entityName, string? entityId, string result, string? note = null)
    {
        try
        {
            var user = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Anonymous";
            var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            var log = new AuditLog
            {
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                UserName = user,
                IpAddress = ip,
                Result = result,
                CreatedAt = DateTime.UtcNow,
                Note = note
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for {Action} on {EntityName}", action, entityName);
        }
    }

    public IQueryable<AuditLog> GetQueryable()
    {
        return _context.AuditLogs.AsNoTracking();
    }
}
