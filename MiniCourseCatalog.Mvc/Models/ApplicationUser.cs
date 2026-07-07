using Microsoft.AspNetCore.Identity;

namespace MiniCourseCatalog.Mvc.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? Department { get; set; }
}
