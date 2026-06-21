using System.ComponentModel.DataAnnotations;

namespace MiniCourseCatalog.Mvc.Options;

public class TrainingCenterConfig
{
    public const string SectionName = "TrainingCenterConfig";

    [Range(1, 100, ErrorMessage = "LowSeatThreshold phải nằm trong khoảng 1–100.")]
    public int LowSeatThreshold { get; set; } = 3;

    [Required(AllowEmptyStrings = false, ErrorMessage = "CenterName không được để trống.")]
    [StringLength(100)]
    public string CenterName { get; set; } = "Mini Training Center";
}
