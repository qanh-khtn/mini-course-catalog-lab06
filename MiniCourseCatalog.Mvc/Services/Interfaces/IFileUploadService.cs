namespace MiniCourseCatalog.Mvc.Services.Interfaces;

public interface IFileUploadService
{
    /// <summary>
    /// Validate và lưu file an toàn.
    /// </summary>
    /// <param name="file">File upload</param>
    /// <param name="subFolder">Tên thư mục con (ví dụ: "thumbnails")</param>
    /// <param name="maxSizeMb">Dung lượng tối đa (MB)</param>
    /// <param name="allowedExtensions">Danh sách đuôi mở rộng cho phép (ví dụ: .jpg, .png)</param>
    /// <returns>Đường dẫn tương đối của file đã lưu, hoặc chuỗi rỗng nếu thất bại</returns>
    Task<string> UploadFileAsync(IFormFile file, string subFolder = "uploads", int maxSizeMb = 5, string[]? allowedExtensions = null);

    /// <summary>
    /// Xóa file trên đĩa
    /// </summary>
    bool DeleteFile(string relativePath);
}
