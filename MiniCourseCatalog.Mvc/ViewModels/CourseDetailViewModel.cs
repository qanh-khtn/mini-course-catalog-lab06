namespace MiniCourseCatalog.Mvc.ViewModels;

public class CourseDetailViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Instructor { get; set; } = "";
    public decimal TuitionFee { get; set; }
    public int CurrentEnrollment { get; set; }
    public int MaxCapacity { get; set; }
    public DateTime StartDate { get; set; }

    public string TuitionFeeText => $"{TuitionFee:N0} VND";
    public string StartDateText => StartDate.ToString("dd/MM/yyyy");
    public decimal TotalRevenue => TuitionFee * CurrentEnrollment;
    public string TotalRevenueText => $"{TotalRevenue:N0} VND";

    public string CourseStatus => CurrentEnrollment >= MaxCapacity ? "Đã đầy lớp" :
                                  (MaxCapacity - CurrentEnrollment <= 3 ? "Sắp kín chỗ" :
                                  (CurrentEnrollment < 10 ? "Lớp vắng học viên" : "Còn chỗ đăng ký"));

    public string OperationalSuggestion
    {
        get
        {
            if (CurrentEnrollment >= MaxCapacity)
                return "Lớp học đã đạt tối đa sĩ số. Vui lòng dừng nhận học viên và chuẩn bị phòng học.";
            if (MaxCapacity - CurrentEnrollment <= 3)
                return $"Lớp chỉ còn {MaxCapacity - CurrentEnrollment} chỗ trống. Đẩy mạnh liên hệ danh sách đợi.";
            if (CurrentEnrollment < 10)
                return "Lớp đang có ít học viên đăng ký. Cần đẩy mạnh tư vấn tuyển sinh hoặc xem xét điều chỉnh lịch khai giảng.";
            return "Sĩ số đang ở mức ổn định, tiếp tục tuyển sinh bình thường.";
        }
    }
}
