using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCourseCatalog.Mvc.Data;

namespace MiniCourseCatalog.Mvc.Controllers.Api;

[ApiController]
[Route("api/courses")]
public class ApiCoursesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ApiCoursesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery(Name = "q")] string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword) || keyword.Length > 100)
        {
            ModelState.AddModelError(nameof(keyword), "Từ khóa tìm kiếm không được rỗng và tối đa 100 ký tự.");
            // ValidationProblem() (thay vì BadRequest(new ValidationProblemDetails{...})) đi qua
            // IProblemDetailsService nên traceId/timestamp được CustomizeProblemDetails tự thêm.
            return ValidationProblem(statusCode: StatusCodes.Status400BadRequest, title: "Yêu cầu không hợp lệ.", modelStateDictionary: ModelState);
        }

        var keywordLower = keyword.Trim().ToLower();

        var courses = await _context.Courses
            .AsNoTracking()
            .Where(c => !c.IsDeleted && (c.Name.ToLower().Contains(keywordLower) || c.Code.ToLower().Contains(keywordLower)))
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Name,
                c.TuitionFee,
                c.AvailableSeats,
                c.MaxCapacity,
                c.StartDate,
                c.ThumbnailPath
            })
            .Take(10)
            .ToListAsync();

        if (!courses.Any())
        {
            // Problem() (thay vì NotFound(new ProblemDetails{...})) đi qua IProblemDetailsService
            // nên traceId/timestamp được CustomizeProblemDetails tự thêm, giống endpoint /api/courses/{id}.
            return Problem(
                title: "Không tìm thấy khóa học",
                detail: "Không có khóa học nào khớp với từ khóa tìm kiếm.",
                statusCode: StatusCodes.Status404NotFound,
                instance: $"/api/courses/search?q={keyword}",
                extensions: new Dictionary<string, object?> { ["errorCode"] = "COURSE_SEARCH_EMPTY" });
        }

        return Ok(courses);
    }
}
