using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using MiniCourseCatalog.Mvc.Data;
using MiniCourseCatalog.Mvc.Models;

namespace MiniCourseCatalog.Tests.Integration;

public class AuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();
                
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite(connection);
                });

                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.EnsureCreated();
                    if (!db.Courses.Any(c => c.Id == 1))
                    {
                        db.CourseCategories.Add(new CourseCategory { Id = 1, Name = "Cat" });
                        db.Courses.Add(new Course { Id = 1, Code = "TEST", Name = "Test", Instructor = "Test", TuitionFee = 1, MaxCapacity = 1, StartDate = DateTime.Now, CourseCategoryId = 1 });
                        db.SaveChanges();
                    }
                }

                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
                
                services.Configure<AuthenticationOptions>(o =>
                {
                    o.DefaultAuthenticateScheme = "Test";
                    o.DefaultChallengeScheme = "Test";
                });
            });
        });
    }

    [Fact]
    public async Task Anonymous_GetCourses_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Courses");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Staff_GetCreateCourse_AccessDenied()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("TestUser", "Staff");

        var response = await client.GetAsync("/Courses/Create");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Staff_GetAdjustSeats_Ok()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("TestUser", "Staff");

        var response = await client.GetAsync("/Courses/AdjustSeats/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_GetCreateCourse_Ok()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("TestUser", "Admin");

        var response = await client.GetAsync("/Courses/Create");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostDelete_NoAntiforgery_Returns400()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("TestUser", "Admin");
        
        var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("Id", "1") });
        var response = await client.PostAsync("/Courses/Delete/1", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetApiCourse9999_Returns404ProblemDetails()
    {
        var client = _factory.CreateClient();
        
        var response = await client.GetAsync("/api/courses/9999");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("errorCode", json);
        Assert.Contains("traceId", json);
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.Request.Headers.TryGetValue("TestUser", out var testUser))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim> { new Claim(ClaimTypes.Name, testUser!) };
        
        if (testUser == "Staff")
        {
            claims.Add(new Claim(ClaimTypes.Role, "Staff"));
        }
        else if (testUser == "Admin")
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Context.Response.Redirect("/Account/Login");
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Context.Response.Redirect("/Account/AccessDenied");
        return Task.CompletedTask;
    }
}
