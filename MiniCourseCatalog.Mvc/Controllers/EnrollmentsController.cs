using Microsoft.AspNetCore.Mvc;
using MiniCourseCatalog.Mvc.Services.Interfaces;

namespace MiniCourseCatalog.Mvc.Controllers;

public class EnrollmentsController : Controller
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    public async Task<IActionResult> History()
    {
        var enrollments = await _enrollmentService.GetAllEnrollmentsAsync();
        return View(enrollments);
    }
}
