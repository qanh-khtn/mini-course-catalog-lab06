using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MiniCourseCatalog.Mvc.Data;
using MiniCourseCatalog.Mvc.Filters;
using MiniCourseCatalog.Mvc.Middleware;
using MiniCourseCatalog.Mvc.Options;
using MiniCourseCatalog.Mvc.Repositories;
using MiniCourseCatalog.Mvc.Repositories.Interfaces;
using MiniCourseCatalog.Mvc.Services;
using MiniCourseCatalog.Mvc.Services.Interfaces;
using MiniCourseCatalog.Mvc.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog: structured log ra Console + File (logs/lab06-yyyyMMdd.txt, giữ 7 ngày) ---
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/lab06-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new ThemeFilter());
});

// ProblemDetails chuẩn hóa response lỗi API: tự thêm traceId + timestamp để dò log
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Extensions["timestamp"] = DateTimeOffset.Now;
    };
});

// Options Pattern — validate bằng Data Annotation ngay khi khởi động:
// config sai (vd LowSeatThreshold = -5) thì app từ chối chạy thay vì lỗi ngầm lúc runtime
builder.Services.AddOptions<TrainingCenterConfig>()
    .Bind(builder.Configuration.GetSection(TrainingCenterConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// EF Core + SQLite (Scoped by default)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity & Cookie Auth
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o => {
    o.Password.RequiredLength = 6;
    o.Password.RequireDigit = true;
    o.Password.RequireUppercase = false;
    o.Password.RequireNonAlphanumeric = false;

    // Lockout: khóa tạm tài khoản sau nhiều lần đăng nhập sai liên tiếp — chống brute-force mật khẩu
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    o.Lockout.AllowedForNewUsers = true;
}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

// Rate Limiting: chặn brute-force đăng nhập ở tầng request, độc lập với Identity Lockout
// (Lockout khóa theo TÀI KHOẢN cụ thể; Rate Limiter chặn theo TẦN SUẤT request bất kể có
// đoán trúng email tồn tại hay không — hai lớp phòng thủ bổ trợ nhau).
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var auditLogService = context.HttpContext.RequestServices.GetRequiredService<IAuditLogService>();
        await auditLogService.LogAsync("LoginRateLimited", "ApplicationUser", null, "Fail",
            $"Quá nhiều yêu cầu đăng nhập từ {context.HttpContext.Connection.RemoteIpAddress}");
    };
});

builder.Services.ConfigureApplicationCookie(o => {
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromHours(2);
    o.Events.OnRedirectToAccessDenied = context =>
    {
        var path = context.Request.Path + context.Request.QueryString;
        context.Response.Redirect($"/Account/AccessDenied?attempted={Uri.EscapeDataString(path)}");
        return Task.CompletedTask;
    };
});
builder.Services.AddHttpContextAccessor();

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanViewCourse", policy => policy.RequireRole("Admin", "Staff"));
    options.AddPolicy("CanManageCourse", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanAdjustSeats", policy => policy.RequireRole("Admin", "Staff"));
    options.AddPolicy("CanViewAuditLog", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanUploadCourseThumbnail", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanEnrollCourse", policy => policy.RequireAuthenticatedUser());
});

// Health Checks: liveness (process còn sống) + readiness (có kiểm tra database)
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Ứng dụng đang chạy."), tags: new[] { "live" })
    .AddDbContextCheck<AppDbContext>("database", tags: new[] { "ready" });

// Repositories — Scoped
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<ICourseCategoryRepository, CourseCategoryRepository>();

// Services — Scoped
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

var app = builder.Build();

// Khởi tạo Seed Data (Identity)
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await DbInitializer.SeedIdentityAsync(userManager, roleManager);
}

if (app.Environment.IsDevelopment())
{
    // Development: thấy stack trace chi tiết để debug
    app.UseDeveloperExceptionPage();
}
else
{
    // Production: trang lỗi an toàn, KHÔNG lộ stack trace / connection string cho user
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();

// Security headers + CSP nonce — đăng ký sớm để áp dụng cho MỌI response, kể cả static files
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Health Check endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    // Feature 3: trả JSON gồm status tổng + danh sách checks + mô tả ngắn (không lộ chi tiết kỹ thuật nhạy cảm)
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description ?? e.Value.Status.ToString()
            })
        };
        await context.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(payload,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }
});

// API mẫu: trả JSON khóa học theo id; không tìm thấy -> ProblemDetails 404 (có traceId)
app.MapGet("/api/courses/{id:int}", async (int id, AppDbContext db) =>
{
    var course = await db.Courses
        .AsNoTracking()
        .Where(c => c.Id == id)
        .Select(c => new
        {
            c.Id,
            c.Code,
            c.Name,
            c.Instructor,
            c.TuitionFee,
            c.CurrentEnrollment,
            c.MaxCapacity,
            c.StartDate
        })
        .FirstOrDefaultAsync();

    if (course is null)
    {
        return Results.Problem(
            type: "https://minicourse.example/problems/course-not-found",
            title: "Course not found",
            detail: $"Không tìm thấy khóa học với id = {id}.",
            statusCode: StatusCodes.Status404NotFound,
            instance: $"/api/courses/{id}",
            // Feature 3: errorCode để client/log phân loại lỗi (traceId vẫn được thêm tự động qua CustomizeProblemDetails)
            extensions: new Dictionary<string, object?> { ["errorCode"] = "COURSE_NOT_FOUND" });
    }

    return Results.Ok(course);
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();

// Cho phép WebApplicationFactory<Program> trong MiniCourseCatalog.Tests thấy được entry point
// (top-level statements sinh class Program internal theo mặc định — cần khai báo partial + public
// để assembly test có thể tham chiếu).
public partial class Program { }
