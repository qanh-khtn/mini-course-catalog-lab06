namespace MiniCourseCatalog.Mvc.Common;

/// <summary>
/// Toàn bộ timestamp trong DB được lưu là UTC. SQLite provider không giữ DateTimeKind
/// (luôn trả về Unspecified khi đọc lại), nên FromUtc() chủ động ép Kind=Utc trước khi quy đổi.
/// Dùng để hiển thị giờ theo múi Việt Nam/Bangkok (UTC+7, không có DST).
/// </summary>
public static class VietnamTime
{
    public static readonly TimeZoneInfo TimeZone = ResolveTimeZone();

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);

    public static DateTime Today => Now.Date;

    public static DateTime FromUtc(DateTime utcValue)
    {
        var utc = DateTime.SpecifyKind(utcValue, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone);
    }

    /// <summary>
    /// Khoảng [StartUtc, EndUtc) theo UTC ứng với 1 ngày lịch Việt Nam hiện tại — dùng để lọc
    /// "hôm nay" đúng múi giờ VN trong LINQ-to-SQL (so sánh UTC trực tiếp, dịch được sang SQL).
    /// </summary>
    public static (DateTime StartUtc, DateTime EndUtc) TodayRangeUtc()
    {
        var startLocal = Today;
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified), TimeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startLocal.AddDays(1), DateTimeKind.Unspecified), TimeZone);
        return (startUtc, endUtc);
    }

    private static TimeZoneInfo ResolveTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); }
        catch (TimeZoneNotFoundException)
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); } // Windows ID fallback
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("ICT", TimeSpan.FromHours(7), "Indochina Time (UTC+7)", "ICT");
            }
        }
    }
}
