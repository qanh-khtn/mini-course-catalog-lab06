namespace MiniCourseCatalog.Mvc.ViewModels;

/// <summary>
/// ViewModel dành cho trang Catalog công khai — chỉ chứa thông tin public, không lộ audit fields.
/// </summary>
public class CourseCatalogItemViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Instructor { get; set; } = "";
    public decimal TuitionFee { get; set; }
    public int AvailableSeats { get; set; }
    public DateTime StartDate { get; set; }
    public string? ThumbnailPath { get; set; }

    public string TuitionFeeText => $"{TuitionFee:N0} VNĐ";
    public string StartDateText => StartDate.ToString("dd/MM/yyyy");
    public bool IsFull => AvailableSeats <= 0;
}
