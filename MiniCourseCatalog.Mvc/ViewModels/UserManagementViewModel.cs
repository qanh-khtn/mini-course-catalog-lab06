namespace MiniCourseCatalog.Mvc.ViewModels;

public class UserManagementViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Department { get; set; }
    public string CurrentRole { get; set; } = "User";
}
