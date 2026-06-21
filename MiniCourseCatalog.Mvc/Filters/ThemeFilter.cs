using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MiniCourseCatalog.Mvc.Filters;

/// <summary>
/// Global action filter: reads theme from query param or cookie,
/// persists it in a 1-year cookie, and injects it into ViewData + action arguments.
/// </summary>
public class ThemeFilter : IActionFilter
{
    private static readonly CookieOptions _cookieOptions = new()
    {
        MaxAge     = TimeSpan.FromDays(365),
        HttpOnly   = false,
        SameSite   = SameSiteMode.Lax,
        IsEssential = true
    };

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var http       = context.HttpContext;
        var fromQuery  = http.Request.Query["theme"].ToString();
        var fromCookie = http.Request.Cookies["app-theme"];

        string theme;

        if (fromQuery is "dark" or "light")
        {
            theme = fromQuery;
            http.Response.Cookies.Append("app-theme", theme, _cookieOptions);
        }
        else if (fromCookie is "dark" or "light")
        {
            theme = fromCookie;
        }
        else
        {
            theme = "light";
        }

        // Inject into action argument so controllers receive correct value
        // (prevents controller redirects from resetting theme to "light")
        if (context.ActionArguments.ContainsKey("theme"))
            context.ActionArguments["theme"] = theme;

        if (context.Controller is Controller ctrl)
            ctrl.ViewData["Theme"] = theme;
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
