using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiniCourseCatalog.Mvc.Models;

namespace MiniCourseCatalog.Mvc.Controllers;

public class HomeController : Controller
{
    public IActionResult Index(string theme = "light")
    {
        ViewData["Theme"] = NormalizeTheme(theme);
        return View();
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
    private static string NormalizeTheme(string theme)
    {
        return string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light";
    }
}
