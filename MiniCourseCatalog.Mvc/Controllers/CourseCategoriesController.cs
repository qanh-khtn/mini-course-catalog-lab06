using Microsoft.AspNetCore.Mvc;
using MiniCourseCatalog.Mvc.Services.Interfaces;
using MiniCourseCatalog.Mvc.ViewModels;

namespace MiniCourseCatalog.Mvc.Controllers;

public class CourseCategoriesController : Controller
{
    private readonly ICourseService _courseService;

    public CourseCategoriesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _courseService.GetCourseCategoriesAsync();
        var allCourses = await _courseService.GetAllAsync();

        var vm = categories.Select(cat => new CourseCategoryViewModel
        {
            Id = cat.Id,
            Name = cat.Name,
            Description = cat.Description ?? "",
            Courses = allCourses
                .Where(c => c.CourseCategoryId == cat.Id)
                .Select(c => new CourseListItemViewModel
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Category = cat.Name,
                    Instructor = c.Instructor,
                    TuitionFee = c.TuitionFee,
                    CurrentEnrollment = c.CurrentEnrollment,
                    MaxCapacity = c.MaxCapacity
                }).ToList()
        }).ToList();

        return View(vm);
    }
}
