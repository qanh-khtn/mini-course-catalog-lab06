using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiniCourseCatalog.Mvc.Controllers;

public class ThemeController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public IActionResult Toggle(string? returnUrl)
    {
        var currentTheme = Request.Cookies["app-theme"];
        var newTheme = currentTheme == "dark" ? "light" : "dark";

        Response.Cookies.Append("app-theme", newTheme, new CookieOptions
        {
            MaxAge = TimeSpan.FromDays(365),
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            HttpOnly = false
        });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        
        return RedirectToAction("Index", "Home");
    }
}
