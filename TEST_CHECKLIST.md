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

| # | Nhóm | Trường hợp test | URL / Thao tác | Role thực hiện | Kết quả mong đợi | Kết quả thực tế | Pass/Fail | Ảnh minh chứng |
|---|------|----------------|----------------|----------------|-------------------|-----------------|-----------|----------------|
| **1** | **Authentication** | Anonymous truy cập trang yêu cầu đăng nhập | GET `/Courses` | Anonymous | Redirect về `/Account/Login?ReturnUrl=%2FCourses` | | | c1_anon_redirect.png |
| **2** | **Authentication** | Login sai mật khẩu | POST `/Account/Login` với mật khẩu sai | Anonymous | Hiển thị thông báo "Email hoặc mật khẩu không đúng" | | | c2_login_fail.png |
| **3** | **Authentication** | Login đúng thông tin | POST `/Account/Login` admin@/Admin@123 | Anonymous | Redirect về Dashboard, navbar hiện tên email | | | c3_login_success.png |
| **4** | **Authentication** | Cookie Identity HttpOnly | Mở DevTools → Application → Cookies | Admin | Cookie `.AspNetCore.Identity.Application` có cờ HttpOnly = true | | | c4_cookie_httponly.png |
| **5** | **Authentication** | Logout dùng POST | Bấm nút Đăng xuất | Admin | Form POST có anti-forgery token; redirect về trang Login | | | c5_logout_post.png |
| **6** | **Authentication** | Giữ nguyên theme sau login | Bật dark mode → Logout → Login lại | Admin | Theme vẫn dark sau khi login thành công | | | c6_theme_persist.png |
| **7** | **Authorization** | Staff không tạo được khóa học | Đăng nhập Staff → GET `/Courses/Create` | Staff | HTTP 403 / AccessDenied page | | | c7_staff_create_denied.png |
| **8** | **Authorization** | Staff không sửa được khóa học | Đăng nhập Staff → GET `/Courses/Edit/1` | Staff | HTTP 403 / AccessDenied page | | | c8_staff_edit_denied.png |
| **9** | **Authorization** | Staff điều chỉnh sĩ số được | Đăng nhập Staff → GET `/Courses/AdjustSeats/1` | Staff | Hiển thị form AdjustSeats thành công | | | c9_staff_adjustseats_ok.png |
| **10** | **Authorization** | Staff không HardDelete được | Đăng nhập Staff → POST `/Courses/HardDelete` | Staff | HTTP 403 / AccessDenied (role Admin required) | | | c10_staff_harddelete_denied.png |
| **11** | **Authorization** | User/Anonymous không xem Audit Logs | GET `/AuditLogs` | Anonymous / User | HTTP 403 / AccessDenied | | | c11_auditlog_denied.png |
| **12** | **Authorization** | Anonymous xem Catalog công khai | GET `/Courses/Catalog` | Anonymous | Hiển thị danh sách khóa học, không redirect về Login | | | c12_catalog_anon.png |
| **13** | **Authorization** | Admin HardDelete thành công | Admin → Trash → Xóa vĩnh viễn | Admin | Khóa học bị xóa hoàn toàn khỏi DB | | | c13_admin_harddelete.png |
| **14** | **Overposting** | Staff không thay đổi được học phí qua AdjustSeats | POST `/Courses/AdjustSeats/1` kèm field `TuitionFee=1` | Staff | TuitionFee trong DB không thay đổi | | | c14_overposting.png |
| **15** | **CSRF** | POST thiếu anti-forgery token | Dùng Postman POST `/Courses/Delete/1` không có token | Admin | HTTP 400 Bad Request | | | c15_csrf_block.png |
| **16** | **XSS** | Nhập script vào tên khóa học | Tạo khóa học với Name = `<script>alert(1)</script>` | Admin | Hiển thị dạng text escaped, script không chạy | | | c16_xss_safe.png |
| **17** | **SQL Injection** | SQLi qua search | GET `/Courses/Search?keyword=' OR '1'='1` | Anonymous/Staff | Không lỗi SQL, không trả toàn bộ dữ liệu ngoài ý muốn | | | c17_sqli_safe.png |
| **18** | **Upload – Feature 2** | Upload ảnh hợp lệ | POST ảnh .jpg < 2MB lên `/Courses/Edit/1` | Admin | Upload thành công, thumbnail hiển thị | | | c18_upload_ok.png |
| **19** | **Upload – Feature 2** | Upload file .exe bị từ chối | POST file .exe | Admin | Thông báo lỗi, ảnh cũ vẫn giữ nguyên | | | c19_upload_exe_denied.png |
| **20** | **Upload – Feature 2** | Upload file > 2MB bị từ chối | POST ảnh > 2MB | Admin | Thông báo lỗi kích thước, ảnh cũ giữ nguyên | | | c20_upload_size_denied.png |
| **21** | **Upload – Feature 2** | Tên file bất thường (path traversal) | POST file tên `../../evil.jpg` | Admin | File lưu bằng tên GUID an toàn, không path traversal | | | c21_upload_safe_name.png |
| **22** | **Concurrency** | 2 tab cùng Edit 1 khóa học | Tab 1 lưu trước, Tab 2 lưu sau | Admin | Tab 2 nhận lỗi "Dữ liệu đã bị người khác thay đổi" (RowVersion conflict) | | | c22_concurrency.png |
| **23** | **Soft Delete** | Admin xóa mềm khóa học | Admin → Delete → Xác nhận | Admin | Khóa học vào `/Courses/Trash`, DB vẫn có record (IsDeleted=true) | | | c23_soft_delete.png |
| **24** | **Soft Delete** | Restore khóa học | Admin → Trash → Khôi phục | Admin | Khóa học quay lại danh sách hoạt động | | | c24_restore.png |
| **25** | **Transaction** | Đăng ký học → tạo Enrollment và trừ số chỗ | POST Enroll cho học viên | Admin/Staff | Enrollment được tạo VÀ AvailableSeats giảm 1 | | | c25_enroll_transaction.png |
| **26** | **Audit Log – Feature 3** | Tạo khóa học ghi audit | Admin tạo course mới | Admin | `/AuditLogs` có dòng action=CreateCourse, result=Success | | | c26_audit_create.png |
| **27** | **Audit Log – Feature 3** | Edit khóa học ghi audit | Admin sửa course | Admin | Dòng action=EditCourse trong AuditLogs | | | c27_audit_edit.png |
| **28** | **Audit Log – Feature 3** | AccessDenied ghi audit | Staff truy cập trang bị cấm | Staff | Dòng action=AccessDenied trong AuditLogs | | | c28_audit_accessdenied.png |
| **29** | **Audit Log – Feature 3** | Lọc audit theo action + khoảng ngày | `/AuditLogs?Action=Login&DateFrom=...&DateTo=...` | Admin | Kết quả lọc đúng, chỉ hiện records khớp | | | c29_audit_filter.png |
| **30** | **Audit Log – Feature 3** | Không có kết quả tìm kiếm | Lọc với điều kiện không khớp | Admin | Hiển thị thông báo "Không tìm thấy..." thay vì bảng rỗng | | | c30_audit_empty.png |
| **31** | **Security Dashboard** | Kiểm tra số liệu dashboard | GET `/` | Admin | Dashboard hiển thị số AccessDenied trong ngày, thao tác nhạy cảm, upload thất bại | | | c31_dashboard.png |
| **32** | **Health Check** | Liveness check | GET `/health/live` | Anonymous | JSON `{"status":"Healthy"}` | | | c32_health_live.png |
| **33** | **Health Check** | Readiness check (kiểm tra DB) | GET `/health/ready` | Anonymous | JSON có check database = Healthy | | | c33_health_ready.png |
| **34** | **API** | Lấy khóa học theo ID | GET `/api/courses/1` | Anonymous | HTTP 200, JSON với đầy đủ trường | | | c34_api_get.png |
| **35** | **API** | Không tìm thấy ID | GET `/api/courses/9999` | Anonymous | HTTP 404, ProblemDetails JSON có `traceId` + `errorCode` | | | c35_api_404.png |
| **36** | **API** | Tìm kiếm keyword rỗng | GET `/api/courses/search?keyword=` | Anonymous | HTTP 400, ValidationProblemDetails | | | c36_api_400_empty.png |
| **37** | **API** | Không tìm thấy kết quả | GET `/api/courses/search?keyword=xyzkhongton` | Anonymous | HTTP 404, ProblemDetails có `errorCode=COURSE_SEARCH_EMPTY` + `traceId` | | | c37_api_404_search.png |
| **38** | **Production** | Không lộ stack trace | Chạy `--environment Production`, gây lỗi 500 | Anonymous | Trang lỗi chung, KHÔNG có stack trace / connection string | | | c38_prod_no_stacktrace.png |
