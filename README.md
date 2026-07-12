# Mini Training Center — Lab06 Final: Secure Course Catalog MVC

> **Final Mini Project** tích hợp Lab01–Lab05 + ASP.NET Core Identity + Authorization (Policy & Role) + Security Pack (Anti-Forgery, XSS, SQLi, Safe Upload, Audit Log, Health Check, ProblemDetails).
>
> Do phạm vi Lab06 Final mở rộng đáng kể so với Lab05 (Identity, Authorization, Security Pack), dự án được tách thành **repository riêng** thay vì tiếp tục trên repo Lab05. Nhánh làm việc: `main`.

---

## Công nghệ sử dụng

| Thành phần | Phiên bản / Gói |
|-----------|-----------------|
| **Runtime** | .NET 10 |
| **Framework** | ASP.NET Core MVC |
| **ORM** | EF Core 10 + SQLite (`Microsoft.EntityFrameworkCore.Sqlite`) |
| **Identity** | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` |
| **Logging** | Serilog (`Serilog.AspNetCore`, `Serilog.Sinks.File`) |
| **Health Checks** | `AspNetCore.HealthChecks.Sqlite` |
| **API Docs** | ProblemDetails (built-in .NET 10) |
| **Rate Limiting** | `Microsoft.AspNetCore.RateLimiting` (built-in .NET 10) |
| **UI** | Bootstrap 5 + Bootstrap Icons + Chart.js, design token riêng (`tokens.css`) |
| **Testing** | xUnit (18 unit test + 18 integration test qua `WebApplicationFactory`) |

---

## Kiến trúc

```
Browser
  │
  ▼
Controller  ──── [ThemeFilter / SecurityHeadersMiddleware / RateLimiter]
  │
  ▼
Service (ICourseService, IEnrollmentService, IAuditLogService, IFileUploadService, …)
  │
  ▼
Repository (ICourseRepository, ICourseCategoryRepository, ICourseReviewRepository, …)
  │
  ▼
AppDbContext : IdentityDbContext<ApplicationUser> (EF Core)
  │
  ▼
SQLite Database (MiniCourseCatalog.db)
```

**Trách nhiệm từng lớp:**

| Lớp | Trách nhiệm |
|-----|------------|
| **Controller** | Nhận HTTP request, validate ViewModel, gọi Service, trả về View/Redirect. Không chứa logic nghiệp vụ. |
| **Service** | Xử lý nghiệp vụ, mapping Entity ↔ ViewModel, xử lý concurrency (RowVersion), transaction (đăng ký học viên), gọi Repository. |
| **Repository** | Truy vấn database bằng EF Core/LINQ. Không expose IQueryable ra ngoài. |
| **AppDbContext** | Cấu hình model (Identity + nghiệp vụ), Global Query Filter, migration, seeding, interceptor audit fields/soft delete. |

**Tại sao dùng DI (Dependency Injection)?** — Dễ test (thay repository thật bằng `FakeCourseRepository`/`ThrowingCourseRepository` trong bộ test), tách biệt vòng đời (Scoped per request), giảm coupling giữa các lớp — không có Service nào tự `new` Repository bên trong method.

---

## Cách chạy

```bash
# 1. Restore dependencies
dotnet restore

# 2. Tạo schema + bảng Identity + AuditLogs + CourseReviews + seed dữ liệu
#    (tự động chạy DbInitializer khi app khởi động lần đầu)
dotnet ef database update

# 3. Chạy ứng dụng (HTTPS)
dotnet run --launch-profile https

# 4. Truy cập
# https://localhost:<port>     (xem cổng trong launchSettings.json)
```

### Reset database

```bash
# Xóa file database rồi chạy lại migrations
Remove-Item MiniCourseCatalog.Mvc\MiniCourseCatalog.db -Force
dotnet ef database update
```

### Danh sách migrations đã có

| Migration | Mô tả |
|-----------|-------|
| `InitialCreate` | Schema ban đầu (Course, CourseCategory, Student, Enrollment) |
| `SeedInitialData` | Seed dữ liệu mẫu Lab04 |
| `AddMoreStudents` | Bổ sung dữ liệu học viên mẫu |
| `AddCourseConcurrencyToken` | Thêm cột `RowVersion` (concurrency token) cho Course |
| `AddCourseSoftDeleteAuditFields` | Thêm `IsDeleted`, `DeletedAt`, `CreatedAt`, `UpdatedAt` |
| `Lab06IdentitySecurityFinal` | Bảng Identity (`AspNetUsers`, `AspNetRoles`...) — chuyển `AppDbContext` sang `IdentityDbContext<ApplicationUser>` |
| `AddThumbnailToCourse` | Thêm cột `ThumbnailPath` cho Course |
| `AddAuditLogs` | Thêm bảng `AuditLogs` |
| `AddCourseReviews` | Thêm bảng `CourseReviews` (tính năng Đánh giá & Bình luận) |
| `AddCourseDescription` | Thêm cột `Description` cho Course |
| `AddCourseReviewIsHidden` | Thêm cột `IsHidden` cho CourseReview (Admin ẩn đánh giá vi phạm) |

---

## Tài khoản demo (seed tự động qua `Data/DbInitializer.cs`)

| Email | Mật khẩu | Role |
|-------|----------|------|
| admin@coursecenter.test | Admin@123 | Admin |
| staff@coursecenter.test | Staff@123 | Staff |
| user@coursecenter.test | User@123 | User |

---

## Ma trận phân quyền

### 8 Policy được định nghĩa trong `Program.cs`

| Policy | Ý nghĩa | Roles được phép |
|--------|---------|----------------|
| `CanViewCourse` | Xem danh sách/chi tiết/thống kê khóa học (trang quản lý) | Admin, Staff |
| `CanManageCourse` | Tạo, sửa, xóa mềm, restore khóa học | Admin |
| `CanAdjustSeats` | Điều chỉnh sĩ số (Feature 1 — tách khỏi quyền sửa học phí) | Admin, Staff |
| `CanViewAuditLog` | Xem Audit Logs và Security Dashboard | Admin |
| `CanUploadCourseThumbnail` | Upload/thay thumbnail (Feature 2) | Admin |
| `CanEnrollCourse` | Viết đánh giá (review) khóa học | Mọi user đã đăng nhập |
| `CanManageEnrollment` | Dùng công cụ đăng ký học viên vào khóa học (`/Courses/Enroll`) | Admin, Staff |
| `CanManageUsers` | Quản lý vai trò tài khoản (`/UserManagement`) | Admin |

> **Lưu ý kiến trúc:** `CanEnrollCourse` ban đầu dùng chung cho cả công cụ đăng ký học viên lẫn tính năng viết đánh giá. Sau khi rà soát, route `/Courses/Enroll` (vốn giống công cụ tuyển sinh của nhân viên hơn là tự đăng ký của học viên) đã tách sang policy riêng `CanManageEnrollment` (chỉ Admin/Staff), còn `CanEnrollCourse` giữ nguyên cho tính năng viết đánh giá (mọi user đã đăng nhập). Hướng đúng nghĩa lâu dài — liên kết 1–1 `ApplicationUser` ↔ `Student` để `User` tự đăng ký cho chính mình — được ghi ở mục "Hướng phát triển" trong báo cáo.

### Bảng quyền theo chức năng

| Chức năng | URL | Bảo vệ bằng | Admin | Staff | User | Anonymous |
|-----------|-----|------------|-------|-------|------|-----------|
| Catalog công khai | `/Courses/Catalog` | `[AllowAnonymous]` | ✅ | ✅ | ✅ | ✅ |
| Xem danh sách khóa học (quản lý) | `/Courses` | `CanViewCourse` | ✅ | ✅ | ❌ | ❌ |
| Xem chi tiết + đánh giá | `/Courses/Detail/{id}` | `[AllowAnonymous]` | ✅ | ✅ | ✅ | ✅ |
| Viết đánh giá khóa học | `/Courses/AddReview` (POST) | `CanEnrollCourse` | ✅ | ✅ | ✅ | ❌ |
| Ẩn đánh giá vi phạm | `/Courses/HideReview/{id}` (POST) | `[Authorize(Roles="Admin")]` | ✅ | ❌ | ❌ | ❌ |
| Tạo khóa học | `/Courses/Create` | `CanManageCourse` | ✅ | ❌ | ❌ | ❌ |
| Sửa khóa học | `/Courses/Edit/{id}` | `CanManageCourse` | ✅ | ❌ | ❌ | ❌ |
| Xóa mềm (soft delete) | `/Courses/Delete/{id}` | `CanManageCourse` | ✅ | ❌ | ❌ | ❌ |
| Khôi phục (restore) | `/Courses/Trash`, `/Courses/Restore/{id}` | `CanManageCourse` | ✅ | ❌ | ❌ | ❌ |
| **Xóa vĩnh viễn** | `/Courses/HardDelete/{id}` | **`[Authorize(Roles="Admin")]`** | ✅ | ❌ | ❌ | ❌ |
| Điều chỉnh sĩ số | `/Courses/AdjustSeats/{id}` | `CanAdjustSeats` | ✅ | ✅ | ❌ | ❌ |
| Upload thumbnail | `/Courses/UploadThumbnail/{id}` | `CanUploadCourseThumbnail` | ✅ | ❌ | ❌ | ❌ |
| Công cụ đăng ký học viên | `/Courses/Enroll` | `CanManageEnrollment` | ✅ | ✅ | ❌ | ❌ |
| Lịch sử đăng ký | `/Enrollments/History` | `CanViewCourse` | ✅ | ✅ | ❌ | ❌ |
| Chuyên ngành | `/CourseCategories` | `[Authorize]` (mọi role đã đăng nhập) | ✅ | ✅ | ✅ | ❌ |
| Quản lý vai trò người dùng | `/UserManagement` | `CanManageUsers` | ✅ | ❌ | ❌ | ❌ |
| Audit Logs | `/AuditLogs` | `CanViewAuditLog` | ✅ | ❌ | ❌ | ❌ |
| Data Health (chẩn đoán nội bộ) | `/DataHealth` | `[Authorize(Roles="Admin")]` | ✅ | ❌ | ❌ | ❌ |
| Health Check | `/health/live`, `/health/ready` | `[AllowAnonymous]` | ✅ | ✅ | ✅ | ✅ |
| API lấy khóa học theo id | `/api/courses/{id}` | Công khai (Minimal API, không gắn `RequireAuthorization`) | ✅ | ✅ | ✅ | ✅ |
| API Search autocomplete | `/api/courses/search?q=...` | `[AllowAnonymous]` | ✅ | ✅ | ✅ | ✅ |

---

## Cách kiểm thử phân quyền (gõ URL trực tiếp)

> **Quan trọng:** Luôn test bằng cách gõ URL trực tiếp, không chỉ dựa vào UI (vì nút có thể ẩn nhưng URL vẫn có thể truy cập). Bộ 18 integration test trong `MiniCourseCatalog.Tests/Integration/AuthorizationTests.cs` tự động hóa toàn bộ các kịch bản dưới đây bằng `WebApplicationFactory`.

```
1. Đăng nhập Staff (staff@coursecenter.test / Staff@123)
   → Gõ: /Courses/Create
   → Kết quả mong đợi: Trang AccessDenied (403)

2. Đăng nhập Staff
   → Gõ: /Courses/Edit/1
   → Kết quả mong đợi: Trang AccessDenied (403)

3. Đăng nhập Staff
   → Gõ: /Courses/AdjustSeats/1
   → Kết quả mong đợi: Hiển thị form điều chỉnh sĩ số (được phép)

4. Đăng nhập Staff
   → POST /Courses/HardDelete với id=1
   → Kết quả mong đợi: AccessDenied (role Admin required)

5. Chưa đăng nhập (Anonymous)
   → Gõ: /Courses
   → Kết quả mong đợi: Redirect về /Account/Login?ReturnUrl=%2FCourses

6. Đăng nhập User (user@coursecenter.test / User@123)
   → Gõ: /AuditLogs
   → Kết quả mong đợi: Trang AccessDenied (403)

7. Chưa đăng nhập
   → Gõ: /Courses/Catalog hoặc /Courses/Detail/1
   → Kết quả mong đợi: Hiển thị công khai (không redirect)

8. Đăng nhập User
   → Gõ: /Courses/Enroll
   → Kết quả mong đợi: Trang AccessDenied (403) — công cụ đăng ký chỉ dành cho Admin/Staff

9. Đăng nhập User
   → POST /UserManagement/ChangeRole với userId của chính mình
   → Kết quả mong đợi: Bị chặn ở CanManageUsers policy (403) trước khi chạm logic tự-hạ-quyền

10. Đăng nhập Admin, thử tự đổi role chính mình sang User qua request trực tiếp
    → Kết quả mong đợi: Bị Controller chặn (chống tự hạ quyền), ghi AuditLog ChangeUserRole/Fail
```

---

## Security Pack

| Biện pháp | Nằm ở file | Mô tả |
|-----------|-----------|-------|
| **Anti-Forgery Token** | Mọi form POST trong Views + `[ValidateAntiForgeryToken]` trong Controllers | Chống CSRF |
| **Razor Encoding (chống XSS)** | Toàn bộ Views (không dùng `Html.Raw` với user input) | Tất cả output qua `@` được HTML-encode tự động; kiểm thử thực nghiệm bằng payload `<script>alert(1)</script>` ở ô đánh giá khóa học |
| **LINQ (chống SQL Injection)** | `Services/CourseService.cs`, `Controllers/Api/ApiCoursesController.cs` | Không nối chuỗi SQL; mọi query qua EF Core parameterized |
| **Safe Upload** | `Services/FileUploadService.cs` | Whitelist `.jpg/.jpeg/.png/.webp`; tối đa 2MB; tên file GUID; `FileMode.CreateNew` chống ghi đè; chống path traversal |
| **Cookie Identity siết chặt** | `Program.cs` → `ConfigureApplicationCookie` | `HttpOnly=true`, `SecurePolicy=Always`, `SameSite=Lax` |
| **CSP với nonce riêng từng request** | `Middleware/SecurityHeadersMiddleware.cs` | Sinh nonce ngẫu nhiên mỗi request, gắn header `Content-Security-Policy` + `X-Content-Type-Options` + `X-Frame-Options` + `Referrer-Policy` + `Permissions-Policy` |
| **Rate Limiting đăng nhập, phân vùng theo IP** | `Program.cs` → `AddRateLimiter` policy `"login"` | `RateLimitPartition.GetFixedWindowLimiter` khóa theo `RemoteIpAddress`, trả 429, ghi Audit `LoginRateLimited` |
| **Identity Account Lockout** | `Program.cs` → `Lockout.MaxFailedAccessAttempts=5`, `DefaultLockoutTimeSpan=5 phút` | Khóa tạm tài khoản sau 5 lần sai liên tiếp, ghi Audit `AccountLockedOut` |
| **Logout dùng POST** | `Views/Shared/_Layout.cshtml` + `AccountController.Logout()` | Chống CSRF trên logout |
| **Exception Handling** | `Program.cs` | Development: chi tiết exception; Production: trang lỗi chung, không lộ stack trace |
| **Chống Overposting** | `ViewModels/*ViewModel.cs` (Course, AdjustSeats, AddReview...) | Mọi Create/Edit/Review đều bind qua ViewModel chỉ phơi bày đúng field cho phép, không bind thẳng Entity |

---

## 3 Feature câu 2

### Feature 1 — Tách quyền học phí vs điều chỉnh sĩ số

- **Mục tiêu:** Staff chỉ được điều chỉnh số chỗ, không được chạm vào học phí
- **Policy:** `CanAdjustSeats` = Admin + Staff
- **Route:** `GET/POST /Courses/AdjustSeats/{id}`
- **Layer:** `CourseAdjustSeatsViewModel` chỉ nhận `NewEnrollment` + `RowVersion` → chống overposting (TuitionFee không có trong ViewModel)
- **Xử lý concurrency:** `DbUpdateConcurrencyException` → báo lỗi RowVersion conflict
- **Audit:** Ghi `AdjustSeats` vào AuditLogs khi thành công/thất bại

### Feature 2 — Thay thumbnail an toàn (Fault-Tolerant)

- **Mục tiêu:** Thay ảnh không làm mất ảnh cũ nếu có lỗi
- **Layer:** `Services/FileUploadService.cs` + `CoursesController.UploadThumbnail`
- **Thứ tự an toàn:** validate → lưu file mới (tên GUID) → cập nhật DB → **chỉ sau đó** mới xóa ảnh cũ
- **Nếu lỗi ở bất kỳ bước nào:** giữ ảnh cũ, xóa file mới (nếu đã lưu)
- **Audit:** Ghi `ReplaceCourseThumbnail` vào AuditLogs

### Feature 3 — Audit Log Search + Security Dashboard

- **Mục tiêu:** Admin tra cứu lịch sử + theo dõi chỉ số bảo mật
- **Layer:** `AuditLogsController`, `Services/AuditLogService.cs`
- **Tìm kiếm:** Lọc theo `User`, `ActionName`, `Result`, `FromDate`, `ToDate` bằng LINQ + `AsNoTracking()`
- **Dashboard (`/`):** 3 chỉ số: số AccessDenied trong ngày, thao tác nhạy cảm, upload thất bại

### Feature khuyến khích — API `/api/courses/search`

- **Route:** `GET /api/courses/search?q=...`
- **Validation:** `q` rỗng hoặc > 100 ký tự → `ValidationProblemDetails 400`
- **Not Found:** Không có kết quả → `ProblemDetails 404`

---

## Tính năng làm thêm (ngoài yêu cầu bắt buộc)

- **Thiết kế lại giao diện:** design token riêng (`wwwroot/css/tokens.css`), 1 accent màu duy nhất, font Outfit/JetBrains Mono, hỗ trợ đầy đủ Dark/Light theme.
- **Đánh giá & Bình luận khóa học (`CourseReview`):** mọi user đã đăng nhập viết đánh giá 1–5 sao + bình luận; Admin ẩn đánh giá vi phạm (`IsHidden`); minh chứng XSS-safe qua Razor encoding.
- **Quản lý vai trò người dùng (`/UserManagement`):** Admin đổi role User ↔ Staff, có cơ chế chống tự hạ quyền (Admin không tự hạ quyền chính mình, kể cả qua request trực tiếp).
- **Integration Test tự động cho Authorization:** 18 test dùng `WebApplicationFactory<Program>`, kiểm thử redirect/AccessDenied/200 theo từng role qua HTTP thật (không chỉ unit test service-layer).
- **Phân trang** cho danh sách khóa học và Audit Log.

Chi tiết đầy đủ (kèm ảnh minh chứng) nằm trong báo cáo nộp kèm (`BaoCao_Lab06.tex` → PDF), không đưa vào repo để tránh trùng vai trò.

---

## Observability

| Endpoint | Mô tả |
|----------|-------|
| `/health/live` | Liveness — app đang chạy (luôn Healthy nếu process sống) |
| `/health/ready` | Readiness — kiểm tra kết nối DB SQLite qua `AddDbContextCheck<AppDbContext>` |
| Logs | `logs/lab06-YYYYMMDD.txt` (Serilog rolling file, giữ 7 ngày gần nhất) |
| ProblemDetails | Mọi API error trả về RFC 7807 JSON có `traceId`; endpoint `/api/courses/{id}` có thêm `errorCode=COURSE_NOT_FOUND` khi 404 |

---

## Ghi chú về sử dụng AI

Dự án này có sử dụng AI (GitHub Copilot / ChatGPT / Gemini / Claude) để hỗ trợ:
- Sinh boilerplate code (ViewModel, Repository pattern)
- Đề xuất cách xử lý concurrency với RowVersion
- Gợi ý cấu trúc Audit Log Service
- Rà soát bảo mật độc lập (phát hiện và vá lỗ hổng overposting ở `AddReview`, rate limiter bucket toàn cục, open redirect ở `HideReview`)

**Tác giả hiểu và có thể giải thích toàn bộ code**, bao gồm:
- Tại sao dùng `[ValidateAntiForgeryToken]` trên mọi POST action
- Cách `RowVersion` hoạt động trong EF Core (`DbUpdateConcurrencyException`)
- Tại sao `FileMode.CreateNew` an toàn hơn `FileMode.Create`
- Sự khác biệt giữa `[Authorize(Policy=...)]` và `[Authorize(Roles=...)]`
- Vì sao `CanEnrollCourse` và `CanManageEnrollment` là hai policy tách biệt dù tên gần giống nhau
- Tại sao chỉ xóa ảnh cũ sau khi file mới và database đã cập nhật thành công (Feature 2)

Mọi báo cáo tự thuật "đã hoàn thành" từ AI đều được tự kiểm chứng lại bằng build + test (`dotnet test`) và khai thác thử nghiệm trực tiếp trước khi chấp nhận, không dùng làm bằng chứng cuối cùng.

---

## Tài liệu liên quan

- 📋 [TEST_CHECKLIST.md](./TEST_CHECKLIST.md) — Bảng kiểm thử đầy đủ (37 trường hợp, có kết quả thực tế)
- 🖼️ Ảnh minh chứng đầy đủ nằm trong báo cáo PDF nộp kèm (không đưa vào repo)
