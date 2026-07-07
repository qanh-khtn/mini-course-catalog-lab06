namespace MiniCourseCatalog.Mvc.ViewModels;

public class SecurityDashboardViewModel
{
    public int AccessDeniedCountToday { get; set; }
    public int SensitiveActionsCountToday { get; set; }
    public int FailedUploadsCountToday { get; set; }
}
