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
    public async Task<IActionResult> Search([FromQuery] string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword) || keyword.Length > 100)
        {
            var problemDetails = new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Yêu cầu không hợp lệ.",
                Detail = "Từ khóa tìm kiếm không được rỗng và tối đa 100 ký tự."
            };
            return BadRequest(problemDetails);
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
            .ToListAsync();

        if (!courses.Any())
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy dữ liệu.",
                Detail = $"Không có khóa học nào khớp với từ khóa '{keyword}'.",
                Instance = HttpContext.Request.Path
            };
            problemDetails.Extensions["errorCode"] = "COURSE_SEARCH_EMPTY";
            problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            return NotFound(problemDetails);
        }

        return Ok(courses);
    }
}
