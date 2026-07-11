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
            .Take(10)
            .ToListAsync();

        return Ok(courses);
    }
}
