using System.ComponentModel.DataAnnotations;

namespace MiniCourseCatalog.Mvc.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [StringLength(100, ErrorMessage = "Mật khẩu phải từ {2} đến {1} ký tự.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? FullName { get; set; }
}
