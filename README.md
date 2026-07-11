# Mini Training Center — Lab06 Final: Secure Course Catalog MVC

> **Final Mini Project** tích hợp Lab01–Lab05 + ASP.NET Core Identity + Authorization (Policy & Role) + Security Pack (Anti-Forgery, XSS, SQLi, Safe Upload, Audit Log, Health Check, ProblemDetails).
>
> Branch: `lab06-final-secure-course-center`

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
| **UI** | Bootstrap 5 + Bootstrap Icons + Chart.js |
| **Testing** | xUnit |

---

## Kiến trúc

```
Browser
  │
  ▼
Controller  ──── [ThemeFilter / AuthFilter]
  │
  ▼
Service (ICourseService, IAuditLogService, IFileUploadService, …)
  │
  ▼
Repository (ICourseRepository, ICourseCategoryRepository, …)
  │
  ▼
AppDbContext (EF Core)
  │
  ▼
SQLite Database (MiniCourseCatalog.db)
```

**Trách nhiệm từng lớp:**

| Lớp | Trách nhiệm |
|-----|------------|
| **Controller** | Nhận HTTP request, validate ViewModel, gọi Service, trả về View/Redirect. Không chứa logic nghiệp vụ. |
| **Service** | Xử lý nghiệp vụ, mapping Entity ↔ ViewModel, xử lý concurrency (RowVersion), gọi Repository. |
| **Repository** | Truy vấn database bằng EF Core/LINQ. Không expose IQueryable ra ngoài. |
| **AppDbContext** | Cấu hình model, migration, seeding. |

**Tại sao dùng DI (Dependency Injection)?** — Dễ test (mock interface), tách biệt vòng đời (Scoped per request), giảm coupling giữa các lớp.

---

## Cách chạy

```bash
# 1. Restore dependencies
dotnet restore

# 2. Tạo schema + bảng Identity + AuditLogs + seed dữ liệu
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
| `AddThumbnailToCourse` | Thêm cột `ThumbnailPath` cho Course |
| `AddAuditLogs` | Thêm bảng `AuditLogs` |
| `Lab06IdentitySecurityFinal` | Bảng Identity (Users, Roles, Claims...) + soft delete + RowVersion |

---

## Tài khoản demo (seed tự động qua `Data/DbInitializer.cs`)

| Email | Mật khẩu | Role |
|-------|----------|------|
| admin@coursecenter.test | Admin@123 | Admin |
| staff@coursecenter.test | Staff@123 | Staff |
| user@coursecenter.test | User@123 | User |

---

## Ma trận phân quyền

### 6 Policy được định nghĩa trong `Program.cs`

| Policy | Ý nghĩa | Roles được phép |
|--------|---------|----------------|
| `CanViewCourse` | Xem danh sách / chi tiết khóa học (trang quản lý) | Admin, Staff |
| `CanManageCourse` | Tạo, sửa, xóa mềm, restore khóa học | Admin |
| `CanAdjustSeats` | Điều chỉnh sĩ số (Feature 1) | Admin, Staff |
| `CanViewAuditLog` | Xem Audit Logs và Security Dashboard | Admin |
| `CanUploadCourseThumbnail` | Upload/thay thumbnail (Feature 2) | Admin |
| `CanEnrollCourse` | Đăng ký học viên vào khóa học | Mọi user đã đăng nhập |

### Bảng quyền theo chức năng

| Chức năng | URL | Bảo vệ bằng | Admin | Staff | User | Anonymous |
|-----------|-----|------------|-------|-------|------|-----------|
| Catalog công khai | `/Courses/Catalog` | `[AllowAnonymous]` | ✅ | ✅ | ✅ | ✅ |
| Xem danh sách khóa học | `/Courses` | `CanViewCourse` | ✅ | ✅ | ❌ | ❌ |
| Xem chi tiết | `/Courses/Detail/{id}` | `CanViewCourse` | ✅ | ✅ | ❌ | ❌ |
| Tạo khóa học | `/Courses/Create` | `CanManageCourse` | ✅ | ❌ | ❌ | ❌ |
| Sửa khóa học | `/Courses/Edit/{id}` | `CanManageCourse` | ✅ | ❌ | ❌ | ❌ |
| Xóa mềm (soft delete) | `/Courses/Delete/{id}` | `CanManageCourse` | ✅ | ❌ | ❌ | ❌ |
| Khôi phục (restore) | `/Courses/Trash` | `CanManageCourse` | ✅ | ❌ | ❌ | ❌ |
| **Xóa vĩnh viễn** | `/Courses/HardDelete` | **`[Authorize(Roles="Admin")]`** | ✅ | ❌ | ❌ | ❌ |
| Điều chỉnh sĩ số | `/Courses/AdjustSeats/{id}` | `CanAdjustSeats` | ✅ | ✅ | ❌ | ❌ |
| Upload thumbnail | `/Courses/UploadThumbnail` | `CanUploadCourseThumbnail` | ✅ | ❌ | ❌ | ❌ |
| Đăng ký học viên | `/Courses/Enroll` | `CanEnrollCourse` | ✅ | ✅ | ✅ | ❌ |
| Audit Logs | `/AuditLogs` | `CanViewAuditLog` | ✅ | ❌ | ❌ | ❌ |
| Security Dashboard | `/` (Admin view) | `CanViewAuditLog` | ✅ | ❌ | ❌ | ❌ |
| Health Check | `/health/live`, `/health/ready` | `[AllowAnonymous]` | ✅ | ✅ | ✅ | ✅ |
| API Search | `/api/courses/search` | `[AllowAnonymous]` | ✅ | ✅ | ✅ | ✅ |

---

## Cách kiểm thử phân quyền (gõ URL trực tiếp)

> **Quan trọng:** Luôn test bằng cách gõ URL trực tiếp, không chỉ dựa vào UI (vì nút có thể ẩn nhưng URL vẫn có thể truy cập).

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
   → Gõ: /Courses/Catalog
   → Kết quả mong đợi: Hiển thị danh sách khóa học (không redirect)
```

---

## Security Pack

| Biện pháp | Nằm ở file | Mô tả |
|-----------|-----------|-------|
| **Anti-Forgery Token** | Mọi form POST trong Views + `[ValidateAntiForgeryToken]` trong Controllers | Chống CSRF |
| **Razor Encoding (chống XSS)** | Toàn bộ Views (không dùng `Html.Raw` với user input) | Tất cả output qua `@` được HTML-encode tự động |
| **LINQ (chống SQL Injection)** | `Services/CourseService.cs`, `Repositories/` | Không nối chuỗi SQL; mọi query qua EF Core parameterized |
| **Safe Upload** | `Services/FileUploadService.cs` | Whitelist `.jpg/.jpeg/.png/.webp`; tối đa 2MB; tên file GUID; `FileMode.CreateNew` chống ghi đè; chống path traversal |
| **Cookie HttpOnly** | `.AspNetCore.Identity.Application` | Cookie session không truy cập được từ JavaScript |
| **Logout dùng POST** | `Views/Shared/_Layout.cshtml` + `AccountController.Logout()` | Chống CSRF trên logout |
| **Exception Handling** | `Program.cs` | Development: chi tiết exception; Production: trang lỗi chung, không lộ stack trace |
| **Theme Cookie an toàn** | `Controllers/ThemeController.cs` | `HttpOnly=false` (cần đọc từ JS), `SameSite=Lax`, `IsEssential=true`, `MaxAge=365 ngày` |

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
- **Tìm kiếm:** Lọc theo `User`, `Action`, `Result`, `DateFrom`, `DateTo` bằng LINQ + `AsNoTracking()`
- **Dashboard (trang chủ Admin):** 3 chỉ số: số AccessDenied trong ngày, thao tác nhạy cảm, upload thất bại

### Feature khuyến khích — API `/api/courses/search`

- **Route:** `GET /api/courses/search?keyword=...`
- **Validation:** `keyword` rỗng hoặc > 100 ký tự → `ValidationProblemDetails 400`
- **Not Found:** Không có kết quả → `ProblemDetails 404` có `errorCode=COURSE_SEARCH_EMPTY` + `traceId`

---

## Observability

| Endpoint | Mô tả |
|----------|-------|
| `/health/live` | Liveness — app đang chạy (luôn Healthy nếu process sống) |
| `/health/ready` | Readiness — kiểm tra kết nối DB SQLite |
| Logs | `logs/lab06-YYYYMMDD.txt` (Serilog rolling file) |
| ProblemDetails | Mọi API error trả về RFC 7807 JSON có `traceId` + `errorCode` |

---

## Ghi chú về sử dụng AI

Dự án này có sử dụng AI (GitHub Copilot / ChatGPT / Gemini) để hỗ trợ:
- Sinh boilerplate code (ViewModel, Repository pattern)
- Đề xuất cách xử lý concurrency với RowVersion
- Gợi ý cấu trúc Audit Log Service
- Refactor cơ chế theme (cookie-based single source of truth)

**Tác giả hiểu và có thể giải thích toàn bộ code**, bao gồm:
- Tại sao dùng `[ValidateAntiForgeryToken]` trên mọi POST action
- Cách `RowVersion` hoạt động trong EF Core (`DbUpdateConcurrencyException`)
- Tại sao `FileMode.CreateNew` an toàn hơn `FileMode.Create`
- Sự khác biệt giữa `[Authorize(Policy=...)]` và `[Authorize(Roles=...)]`
- Tại sao theme dùng cookie thay vì query string

---

## Tài liệu liên quan

- 📋 [TEST_CHECKLIST.md](./TEST_CHECKLIST.md) — Bảng kiểm thử đầy đủ để demo
- 🖼️ Ảnh minh chứng: `docs/images/lab06/` *(điền trong quá trình demo)*
