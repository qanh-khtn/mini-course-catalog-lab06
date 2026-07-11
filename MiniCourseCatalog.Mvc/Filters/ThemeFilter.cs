using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MiniCourseCatalog.Mvc.Filters;

/// <summary>
/// Global action filter: reads theme from cookie and injects it into ViewData.
/// </summary>
public class ThemeFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var fromCookie = context.HttpContext.Request.Cookies["app-theme"];
        
        string theme = (fromCookie is "dark" or "light") ? fromCookie : "light";

        if (context.Controller is Controller ctrl)
        {
            ctrl.ViewData["Theme"] = theme;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
