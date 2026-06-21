using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCourseCatalog.Mvc.Data;
using MiniCourseCatalog.Mvc.ViewModels;

namespace MiniCourseCatalog.Mvc.Controllers;

public class DataHealthController : Controller
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public DataHealthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new DataHealthViewModel
        {
            DatabasePath = _configuration.GetConnectionString("DefaultConnection") ?? "N/A"
        };

        try
        {
            vm.CanConnect = await _context.Database.CanConnectAsync();

            if (vm.CanConnect)
            {
                vm.CourseCategoryCount = await _context.CourseCategories.CountAsync();
                vm.CourseCount = await _context.Courses.CountAsync();
                vm.StudentCount = await _context.Students.CountAsync();
                vm.EnrollmentCount = await _context.Enrollments.CountAsync();

                var applied = await _context.Database.GetAppliedMigrationsAsync();
                var list = applied.ToList();
                vm.MigrationApplied = list.Any();
                vm.LastMigration = list.LastOrDefault() ?? "Chưa có";
            }
        }
        catch (Exception ex)
        {
            vm.CanConnect = false;
            vm.DbError = ex.Message;
        }

        return View(vm);
    }
}
