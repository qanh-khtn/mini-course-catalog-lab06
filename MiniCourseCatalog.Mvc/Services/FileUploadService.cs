using MiniCourseCatalog.Mvc.Services.Interfaces;

namespace MiniCourseCatalog.Mvc.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileUploadService> _logger;

    public FileUploadService(IWebHostEnvironment env, ILogger<FileUploadService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string subFolder = "uploads", int maxSizeMb = 5, string[]? allowedExtensions = null)
    {
        if (file == null || file.Length == 0) return string.Empty;

        // 1. Kiểm tra dung lượng
        var sizeInMb = file.Length / (1024.0 * 1024.0);
        if (sizeInMb > maxSizeMb)
        {
            _logger.LogWarning("Upload bị từ chối do vượt quá {MaxSize}MB (thực tế {Size}MB).", maxSizeMb, sizeInMb);
            throw new InvalidOperationException($"Dung lượng file vượt quá {maxSizeMb}MB.");
        }

        // 2. Kiểm tra phần mở rộng (chống upload script/shell)
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var defaultAllowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var allowed = allowedExtensions ?? defaultAllowed;
        
        if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext))
        {
            _logger.LogWarning("Upload bị từ chối do phần mở rộng '{Ext}' không hợp lệ.", ext);
            throw new InvalidOperationException($"Chỉ cho phép tải lên file ảnh ({string.Join(", ", allowed)}).");
        }

        // 3. Đổi tên file ngẫu nhiên để chống ghi đè và ẩn tên file thật
        var newFileName = $"{Guid.NewGuid():N}{ext}";
        var folderPath = Path.Combine(_env.WebRootPath, subFolder);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var filePath = Path.Combine(folderPath, newFileName);

        try
        {
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            
            _logger.LogInformation("File {FileName} được lưu thành công tại {Path}", file.FileName, filePath);
            return $"/{subFolder}/{newFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu file upload: {Message}", ex.Message);
            return string.Empty;
        }
    }

    public bool DeleteFile(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return false;

        try
        {
            // Bảo vệ directory traversal (tránh truyền string như /../..)
            if (relativePath.Contains(".."))
            {
                _logger.LogWarning("Phát hiện directory traversal attempt: {Path}", relativePath);
                return false;
            }

            var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa file {Path}: {Message}", relativePath, ex.Message);
        }
        return false;
    }
}
