namespace MiniCourseCatalog.Mvc.Models;

/// <summary>
/// Entity hỗ trợ xóa mềm. AppDbContext.SaveChangesAsync chặn lệnh xóa cứng
/// (EntityState.Deleted) và chuyển thành cập nhật IsDeleted = true + DeletedAt.
/// Global query filter !IsDeleted giúp danh sách chính không thấy bản ghi đã xóa.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
