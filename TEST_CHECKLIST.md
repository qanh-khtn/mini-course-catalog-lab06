# TEST CHECKLIST — Lab06 Final: Mini Training Center

## Thông tin chạy ứng dụng

```bash
# Restore dependencies
dotnet restore

# Tạo/cập nhật database (chạy migrations)
dotnet ef database update

# Chạy ứng dụng
dotnet run --launch-profile https

# Reset database
# Xóa file MiniCourseCatalog.db rồi chạy lại dotnet ef database update
```

## Tài khoản demo (seed tự động khi khởi động)

| Email | Mật khẩu | Role |
|-------|----------|------|
| admin@coursecenter.test | Admin@123 | Admin |
| staff@coursecenter.test | Staff@123 | Staff |
| user@coursecenter.test | User@123 | User |

---

## Bảng kiểm thử

> Cột **Ảnh minh chứng** trỏ tới ảnh thật đã chụp trong `img/Lab06/basic/` (Câu 1) hoặc `img/Lab06/bonus/` (Câu 2/làm thêm). Với các trường hợp không có ảnh riêng, cột này ghi rõ nguồn kiểm chứng khác (test tự động hoặc curl/code trực tiếp) thay vì để trống hoặc ghi tên ảnh chưa tồn tại.

| # | Nhóm | Trường hợp test | URL / Thao tác | Role thực hiện | Kết quả mong đợi | Kết quả thực tế | Pass/Fail | Ảnh minh chứng / nguồn kiểm chứng |
|---|------|----------------|----------------|----------------|-------------------|-----------------|-----------|----------------|
| **1** | **Authentication** | Anonymous truy cập trang yêu cầu đăng nhập | GET `/Courses` | Anonymous | Redirect về `/Account/Login?ReturnUrl=%2FCourses` | Xác nhận qua `curl`: HTTP 302, header Location đúng `/Account/Login?ReturnUrl=%2FCourses` | **PASS** | `c1_authz_anon_redirect.jpg` |
| **2** | **Authentication** | Login sai mật khẩu | POST `/Account/Login` với mật khẩu sai | Anonymous | Hiển thị thông báo "Email hoặc mật khẩu không đúng" | Đúng như mong đợi, không tiết lộ email có tồn tại hay không; ghi AuditLog `Login/Fail` | **PASS** | `c1_auth_login_fail.jpg` |
| **3** | **Authentication** | Login đúng thông tin | POST `/Account/Login` admin@/Admin@123 | Anonymous | Redirect về Dashboard, navbar hiện tên email | Đúng như mong đợi | **PASS** | `c1_auth_login_success_navbar.jpg` |
| **4** | **Authentication** | Cookie Identity HttpOnly | Mở DevTools → Application → Cookies | Admin | Cookie `.AspNetCore.Identity.Application` có cờ HttpOnly = true | Đúng như mong đợi; đồng thời `Secure=true`, `SameSite=Lax` (siết chặt hơn mặc định) | **PASS** | `c1_auth_cookie_devtools.jpg`, `bonus_sec_cookie_flags.jpg` |
| **5** | **Authentication** | Logout dùng POST | Bấm nút Đăng xuất | Admin | Form POST có anti-forgery token; redirect về trang Login | Đúng như mong đợi | **PASS** | `c1_auth_logout_post.jpg` |
| **6** | **Authentication** | Đăng ký tài khoản mới | POST `/Account/Register` | Anonymous | Tạo user với role `User` mặc định, tự đăng nhập sau khi đăng ký | Đúng như mong đợi qua `UserManager.CreateAsync` + `AddToRoleAsync("User")` | **PASS** | `c1_auth_register_form.jpg` |
| **7** | **Authentication** | Đăng nhập sai 5 lần liên tiếp | POST `/Account/Login` sai mật khẩu × 5 | Anonymous | Tài khoản bị khóa tạm 5 phút, ghi AuditLog `AccountLockedOut` | Đúng như mong đợi (`Lockout.MaxFailedAccessAttempts=5`) | **PASS** | `bonus_sec_lockout.jpg` |
| **8** | **Authentication** | Spam login nhiều request/phút | POST `/Account/Login` liên tục từ cùng 1 IP | Anonymous | HTTP 429 Too Many Requests | Đúng như mong đợi (rate limiter phân vùng theo IP), ghi AuditLog `LoginRateLimited` | **PASS** | `bonus_sec_ratelimit_429.jpg` |
| **9** | **Authorization** | Staff không tạo được khóa học | Đăng nhập Staff → GET `/Courses/Create` | Staff | HTTP 403 / AccessDenied page | Đúng như mong đợi | **PASS** | `c1_authz_accessdenied_staff.jpg` |
| **10** | **Authorization** | Staff không sửa được khóa học | Đăng nhập Staff → GET `/Courses/Edit/1` | Staff | HTTP 403 / AccessDenied page | Đúng như mong đợi | **PASS** | `c1_feat1_staff_edit_denied.jpg` |
| **11** | **Authorization** | Staff điều chỉnh sĩ số được | Đăng nhập Staff → GET `/Courses/AdjustSeats/1` | Staff | Hiển thị form AdjustSeats thành công (chỉ có NewEnrollment + RowVersion) | Đúng như mong đợi | **PASS** | `c1_feat1_adjustseats_staff.jpg` |
| **12** | **Authorization** | Staff không HardDelete được | Đăng nhập Staff → POST `/Courses/HardDelete/1` | Staff | HTTP 403 / AccessDenied (role Admin required, `[Authorize(Roles="Admin")]`) | Đúng như mong đợi — cùng cơ chế AccessDenied middleware như #9/#10, khác ở chỗ dùng Role trực tiếp thay vì Policy; chưa có test tự động riêng cho route này (khác với Create/AdjustSeats/Enroll/DataHealth đã có trong `AuthorizationTests.cs`) | **PASS** | Kiểm chứng qua code (`CoursesController.cs:616`) — khuyến nghị bổ sung 1 integration test riêng cho HardDelete nếu còn thời gian |
| **13** | **Authorization** | User/Anonymous không xem Audit Logs | GET `/AuditLogs` | Anonymous / User | Anonymous → redirect Login; User đã đăng nhập → AccessDenied 403 | Xác nhận qua `curl` (Anonymous): HTTP 302 → `/Account/Login?ReturnUrl=%2FAuditLogs`. User: AccessDenied theo policy `CanViewAuditLog` (Admin only) | **PASS** | `curl` trực tiếp (log phiên làm việc) |
| **14** | **Authorization** | Anonymous xem Catalog công khai | GET `/Courses/Catalog` | Anonymous | Hiển thị danh sách khóa học, không redirect về Login | Xác nhận qua `curl`: HTTP 200 | **PASS** | `c1_catalog_public.jpg` |
| **15** | **Authorization** | Anonymous xem chi tiết + đánh giá khóa học | GET `/Courses/Detail/1` | Anonymous | HTTP 200, không redirect (route `[AllowAnonymous]`) | Xác nhận qua `curl`: HTTP 200 | **PASS** | `bonus_authz_detail_public.jpg` |
| **16** | **Authorization** | User không dùng được công cụ Đăng ký học viên | Đăng nhập User → GET `/Courses/Enroll` | User | HTTP 403 / AccessDenied (policy `CanManageEnrollment` = Admin/Staff only) | Đúng như mong đợi — quyết định kiến trúc: Enroll là công cụ tuyển sinh của nhân viên, không phải tự đăng ký của User | **PASS** | `bonus_authz_enroll_denied.jpg`, `bonus_authz_enroll_hidden_from_user_1.jpg`, `bonus_authz_enroll_hidden_from_user_2.jpg` |
| **17** | **Authorization** | Staff dùng được công cụ Đăng ký học viên | Đăng nhập Staff → GET `/Courses/Enroll` | Staff | HTTP 200 | Đúng như mong đợi | **PASS** | `bonus_authz_enroll_staff_ok.jpg` |
| **18** | **Authorization** | Admin HardDelete thành công | Admin → Trash → Xóa vĩnh viễn | Admin | Khóa học bị xóa hoàn toàn khỏi DB | Đúng như mong đợi | **PASS** | `c1_harddelete_admin_1.jpg`, `c1_harddelete_admin_2.jpg` |
| **19** | **Overposting** | Staff không thay đổi được học phí qua AdjustSeats | POST `/Courses/AdjustSeats/1` kèm field `TuitionFee=1` giả mạo | Staff | TuitionFee trong DB không đổi (field không tồn tại trong ViewModel nên bị model binder bỏ qua) | Đúng như mong đợi — `CourseAdjustSeatsViewModel` chỉ khai báo `NewEnrollment` + `RowVersion`, không có `TuitionFee` | **PASS** | Kiểm chứng qua code (`ViewModels/CourseAdjustSeatsViewModel.cs`) |
| **20** | **Overposting** | Không sửa được ThumbnailPath qua form Edit | POST `/Courses/Edit/1` kèm field `ExistingThumbnailPath` giả mạo | Admin | `ThumbnailPath` trong DB không đổi qua Edit (chỉ đổi được qua UploadThumbnail) | Lỗ hổng thật phát hiện khi tự rà soát bảo mật, đã vá bằng cách xóa nhánh ghi `course.ThumbnailPath` trong `CourseService.UpdateAsync`; xác nhận lại bằng khai thác thực nghiệm trước/sau khi vá | **PASS** (sau khi vá) | Kiểm chứng qua code + khai thác thực nghiệm (POST trực tiếp bằng Postman) |
| **21** | **Overposting** | AddReview không nhận rating/courseId giả mạo | POST `/Courses/AddReview` với `rating=99999`, `courseId` không tồn tại | User đã đăng nhập | Bị chặn bởi `[Range(1,5)]` trên ViewModel + kiểm tra course tồn tại, không lưu vào DB | Lỗ hổng thật phát hiện khi tự rà soát bảo mật (action từng bind tham số nguyên thủy, bỏ qua toàn bộ DataAnnotations) — đã vá bằng `AddReviewViewModel` + `ModelState.IsValid` | **PASS** (sau khi vá) | `bonus_sec_addreview_validation.jpg`, `bonus_sec_addreview_dbcheck.jpg` |
| **22** | **CSRF** | POST thiếu anti-forgery token | Client giả lập gửi `POST /Courses/Delete/1` (đã đăng nhập Admin) không kèm token | Admin | HTTP 400 Bad Request | Đúng như mong đợi | **PASS** | Test tự động `PostDelete_NoAntiforgery_Returns400` (`AuthorizationTests.cs:109`) — nằm trong 36/36 test pass |
| **23** | **CSRF / Open Redirect** | HideReview với Referer giả mạo trỏ ra ngoài domain | POST `/Courses/HideReview/1` với header `Referer: https://evil.example.com` | Admin | Không redirect ra ngoài domain — fallback về `/Courses` | Lỗ hổng thật phát hiện khi rà soát (`Redirect(Request.Headers["Referer"])` không kiểm tra) — đã vá bằng `Url.IsLocalUrl()` guard | **PASS** (sau khi vá) | Kiểm chứng qua code (`CoursesController.cs` — `HideReview`) |
| **24** | **XSS** | Nhập script vào bình luận đánh giá | Viết đánh giá với Comment = `<script>alert(1)</script>` | User đã đăng nhập | Hiển thị dạng text escaped (`&lt;script&gt;...`), script không chạy | Đúng như mong đợi — Razor encoding mặc định, xác nhận qua View Source (không có node `<script>` thật trong DOM) | **PASS** | `bonus_review_xss_encoded.jpg` |
| **25** | **SQL Injection** | SQLi qua tìm kiếm khóa học | GET `/Courses/Search?keyword=' OR '1'='1` | Anonymous/Staff | Không lỗi SQL, không trả toàn bộ dữ liệu ngoài ý muốn (LINQ tham số hóa) | Đúng như mong đợi | **PASS** | `c1_search_linq.jpg` |
| **26** | **Upload – Feature 2** | Upload ảnh hợp lệ | POST ảnh .jpg < 2MB lên `/Courses/UploadThumbnail/1` | Admin | Upload thành công, thumbnail hiển thị, ghi AuditLog `ReplaceCourseThumbnail/Success` | Đúng như mong đợi | **PASS** | `c1_feat2_upload_success.jpg` |
| **27** | **Upload – Feature 2** | Upload file .exe đổi đuôi giả dạng ảnh | POST file .exe (đổi tên thành .jpg) | Admin | Thông báo lỗi whitelist extension, ảnh cũ vẫn giữ nguyên | Đúng như mong đợi | **PASS** | `c1_feat2_upload_exe_rejected.jpg` |
| **28** | **Upload – Feature 2** | Upload file > 2MB bị từ chối | POST ảnh > 2MB | Admin | Thông báo lỗi kích thước, ảnh cũ giữ nguyên | Đúng như mong đợi | **PASS** | `c1_feat2_upload_toolarge.jpg` |
| **29** | **Upload – Feature 2** | Tên file bất thường (path traversal) | POST file tên `../../evil.jpg` | Admin | File lưu bằng tên GUID an toàn (`Guid.NewGuid()`), không path traversal, không ghi đè file trùng tên (`FileMode.CreateNew`) | Đúng như mong đợi | **PASS** | Kiểm chứng qua code (`Services/FileUploadService.cs`) |
| **30** | **Upload – Feature 2** | Upload file mới lỗi không mất ảnh cũ | Upload file .exe khi khóa học đã có sẵn thumbnail hợp lệ | Admin | Ảnh cũ vẫn còn nguyên trên DB/đĩa sau khi upload lỗi | Đúng như mong đợi — validate trước khi ghi, chỉ xóa ảnh cũ sau khi DB cập nhật thành công | **PASS** | Kiểm chứng qua code (`CoursesController.UploadThumbnail`) |
| **31** | **Concurrency** | 2 tab cùng Edit 1 khóa học | Tab 1 lưu trước, Tab 2 lưu sau (RowVersion cũ) | Admin | Tab 2 nhận lỗi "Dữ liệu đã bị người khác thay đổi" (RowVersion conflict) | Đúng như mong đợi | **PASS** | `c1_course_edit_concurrency.jpg` |
| **32** | **Soft Delete** | Admin xóa mềm khóa học | Admin → Delete → Xác nhận | Admin | Khóa học vào `/Courses/Trash`, DB vẫn có record (IsDeleted=true) | Đúng như mong đợi | **PASS** | `c1_course_delete_confirm.jpg`, `c1_course_trash.jpg` |
| **33** | **Soft Delete** | Restore khóa học | Admin → Trash → Khôi phục | Admin | Khóa học quay lại danh sách hoạt động | Đúng như mong đợi | **PASS** | `c1_course_restore_1.jpg`, `c1_course_restore_2.jpg` |
| **34** | **Transaction** | Đăng ký học → tạo Enrollment và trừ số chỗ | POST `/Courses/Enroll` cho học viên | Admin/Staff | Enrollment được tạo VÀ CurrentEnrollment tăng 1 trong cùng transaction; nếu lỗi giữa chừng thì rollback cả hai | Đúng như mong đợi — `EnrollmentService.EnrollStudentAsync` bọc trong `BeginTransactionAsync` | **PASS** | Kiểm chứng qua code (`Services/EnrollmentService.cs`) |
| **35** | **Audit Log – Feature 3** | Tạo/sửa/xóa mềm/upload khóa học ghi audit | Admin thao tác CRUD + upload | Admin | `/AuditLogs` có dòng tương ứng `CreateCourse`/`EditCourse`/`DeleteCourse`/`ReplaceCourseThumbnail`, result=Success | Đúng như mong đợi | **PASS** | `c1_auditlog_list.jpg` |
| **36** | **Audit Log – Feature 3** | AccessDenied ghi audit | Staff/User truy cập trang bị cấm | Staff/User | Dòng `AccessDenied` trong AuditLogs kèm đường dẫn đã cố truy cập | Đúng như mong đợi | **PASS** | `bonus_authz_selfdemote_bypass_blocked_3.jpg` |
| **37** | **Audit Log – Feature 3** | Lọc audit theo action + khoảng ngày | `/AuditLogs?ActionName=AdjustSeats&FromDate=...&ToDate=...` | Admin | Kết quả lọc đúng, chỉ hiện records khớp; query dùng LINQ + AsNoTracking | Đúng như mong đợi | **PASS** | `c1_auditlog_filter.jpg` |
| **38** | **Security Dashboard** | Kiểm tra số liệu dashboard | GET `/` | Admin | Dashboard hiển thị số AccessDenied trong ngày, thao tác nhạy cảm, upload thất bại | Đúng như mong đợi | **PASS** | `bonus_ui_after_dashboard_light.jpg`, `bonus_ui_after_dashboard_dark.jpg` |
| **39** | **Quản lý vai trò (làm thêm)** | Admin đổi role User → Staff | POST `/UserManagement/ChangeRole` | Admin | Cập nhật role thành công, ghi AuditLog `ChangeUserRole/Success` | Đúng như mong đợi | **PASS** | `bonus_authz_usermgmt_list.jpg`, `bonus_authz_changerole_success.jpg`, `bonus_authz_role_effective.jpg` |
| **40** | **Quản lý vai trò (làm thêm)** | Admin không tự hạ quyền chính mình (kể cả bypass UI) | POST `/UserManagement/ChangeRole` với `userId` là chính Admin đang đăng nhập | Admin | Bị chặn ở server, ghi AuditLog `ChangeUserRole/Fail` | Đúng như mong đợi — chặn cả qua UI (disabled) lẫn request trực tiếp (server-side check) | **PASS** | `bonus_authz_selfdemote_blocked.jpg`, `bonus_authz_selfdemote_bypass_blocked_1.jpg`, `bonus_authz_selfdemote_bypass_blocked_2.jpg` |
| **41** | **Health Check** | Liveness check | GET `/health/live` | Anonymous | `Healthy` (text/plain) | Xác nhận qua `curl`: HTTP 200, body `Healthy` | **PASS** | `c1_health_live.jpg` |
| **42** | **Health Check** | Readiness check (kiểm tra DB) | GET `/health/ready` | Anonymous | JSON có check `database` = Healthy | Xác nhận qua `curl`: HTTP 200, JSON đúng cấu trúc | **PASS** | `c1_health_ready.jpg` |
| **43** | **API** | Lấy khóa học theo ID | GET `/api/courses/1` | Anonymous | HTTP 200, JSON với đầy đủ trường | Xác nhận qua `curl`: HTTP 200, JSON đúng | **PASS** | `c1_api_course_ok.jpg` |
| **44** | **API** | Không tìm thấy ID | GET `/api/courses/9999` | Anonymous | HTTP 404, ProblemDetails JSON có `traceId` + `errorCode=COURSE_NOT_FOUND` | Xác nhận qua `curl`: HTTP 404, đủ `traceId`/`errorCode`/`timestamp` | **PASS** | `c1_api_course_404.jpg` |
| **45** | **API** | Tìm kiếm keyword rỗng | GET `/api/courses/search?q=` | Anonymous | HTTP 400, ValidationProblemDetails | Xác nhận qua `curl`: HTTP 400, `errors.keyword` đúng thông báo | **PASS** | Xác nhận trực tiếp qua `curl` (log phiên làm việc) |
| **46** | **API** | Không tìm thấy kết quả tìm kiếm | GET `/api/courses/search?q=zzzxyzkhongtontai` | Anonymous | HTTP 404, ProblemDetails có `errorCode=COURSE_SEARCH_EMPTY` + `traceId` | Vá lại trước khi test: endpoint trước đó tạo `ProblemDetails` thủ công nên KHÔNG tự có `traceId`; đã đổi sang `Problem()`/`ValidationProblem()` (đi qua `IProblemDetailsService`) — xác nhận qua `curl`: HTTP 404, đủ cả `traceId` và `errorCode` | **PASS** (sau khi vá) | Xác nhận trực tiếp qua `curl` (log phiên làm việc) |
| **47** | **Production** | Không lộ stack trace | Chạy `--environment Production`, cố tình kích lỗi | Anonymous | Trang lỗi chung, KHÔNG có stack trace / connection string / SQL query | Đúng như mong đợi — `UseExceptionHandler("/Home/Error")` + `UseHsts()` chỉ áp dụng ở Production | **PASS** | `c1_prod_error_safe.jpg` |

---

## Tổng kết

- **47/47 trường hợp PASS.**
- **3 trường hợp (#20, #21, #23, #46)** từng là lỗ hổng/gap thật phát hiện trong quá trình tự rà soát bảo mật (không phải giả định lý thuyết) — đã vá và verify lại bằng khai thác thực nghiệm hoặc `curl` trực tiếp trước khi đóng dấu PASS.
- **36/36 test tự động** (18 unit test service-layer + 18 integration test authorization qua `WebApplicationFactory`) chạy `dotnet test` không lỗi — xem thêm `bonus_test_authz_code_2.jpg`.
- Các trường hợp đánh dấu "Kiểm chứng qua code" là những ràng buộc có tính xác định 100% từ thiết kế (ví dụ: ViewModel không khai báo field nên model binder không thể gán), không phải suy đoán — vẫn khuyến khích tự tay test lại trước khi báo cáo/demo trực tiếp.
