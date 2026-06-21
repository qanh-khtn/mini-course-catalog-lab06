# Mini Training Center Course Catalog MVC — Lab05

Ứng dụng ASP.NET Core MVC quản lý danh mục khóa học cho một trung tâm đào tạo nhỏ. Project được phát triển tiếp từ Lab04 và nâng cấp theo yêu cầu **Lab05**: bổ sung **Soft Delete**, **Audit Fields**, **Optimistic Concurrency** (RowVersion), **Serilog logging**, **Health Checks** và **ProblemDetails API**.

> Nhánh thực hiện Lab05: `main`

## Chủ Đề

**Mini Training Center** — Hệ thống quản lý nội bộ trung tâm đào tạo: xem danh sách khóa học, xem chi tiết, tạo/sửa/xóa mềm, khôi phục từ thùng rác, thống kê, đăng ký khóa học, theo dõi sức khỏe hệ thống.

## Công Nghệ Sử Dụng

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core 10 + SQLite
- Dependency Injection, Options Pattern
- Repository Pattern, Service Layer
- Serilog (Console + File rolling daily)
- ASP.NET Core Health Checks
- xUnit (Unit Test với Fake Repository)
- Razor View Engine, Bootstrap 5, Chart.js

## Kiến Trúc & Luồng Xử Lý

```text
Controller → Service → Repository → DbContext → Database (SQLite)
```

- **Controller** chỉ nhận request và gọi Service, không query database trực tiếp.
- **Service** chứa logic nghiệp vụ, map dữ liệu sang ViewModel.
- **Repository** chịu trách nhiệm truy vấn EF Core.
- **DbContext** (`AppDbContext`) quản lý phiên làm việc với database, tự động set audit fields qua `SaveChangesAsync`.

## Cấu Trúc Thư Mục

```text
MiniCourseCatalog.Mvc
├── Controllers/          # CoursesController, CourseCategoriesController, EnrollmentsController, DataHealthController, HomeController
├── Data/
│   └── AppDbContext.cs   # DbSet, Fluent API mapping, Seed Data, Global Query Filter, SaveChangesAsync interceptor
├── Models/
│   ├── Course.cs         # + IsDeleted, DeletedAt, CreatedAt, UpdatedAt, RowVersion
│   ├── IAuditable.cs     # Interface audit fields
│   └── ISoftDeletable.cs # Interface soft delete
├── Repositories/
│   ├── Interfaces/       # ICourseRepository, IEnrollmentRepository, ...
│   └── *.cs
├── Services/
│   ├── Interfaces/       # ICourseService, IEnrollmentService, ...
│   └── *.cs
├── Options/
│   └── TrainingCenterConfig.cs
├── ViewModels/
│   ├── CourseEditViewModel.cs      # + RowVersion
│   ├── CourseDeleteViewModel.cs
│   └── CourseTrashItemViewModel.cs
├── Migrations/           # ..._AddCourseSoftDeleteAuditFields
├── Fakes/
│   └── FakeCourseRepository.cs
└── Views/
    └── Courses/
        ├── Edit.cshtml   # Form sửa + concurrency
        ├── Delete.cshtml # Xác nhận xóa mềm
        └── Trash.cshtml  # Thùng rác + khôi phục

MiniCourseCatalog.Tests/  # Project unit test (xUnit + Fake Repository)
```

## Mô Hình Dữ Liệu & Quan Hệ

Ứng dụng có **1 `AppDbContext`**, **4 Entity** và **3 Relationship**:

| Quan hệ | Loại | Ý nghĩa |
|---|---|---|
| `CourseCategory` → `Course` | One-to-Many | Một chuyên ngành có nhiều khóa học |
| `Course` → `Enrollment` | One-to-Many | Một khóa học có nhiều lượt đăng ký |
| `Student` → `Enrollment` | One-to-Many | Một học viên có nhiều lượt đăng ký |

`Enrollment` là bảng trung gian thể hiện quan hệ Many-to-Many giữa `Course` và `Student`.

## Các Yêu Cầu Lab05 Đã Thực Hiện

### Soft Delete & Audit Fields

- `ISoftDeletable`: interface khai báo `IsDeleted`, `DeletedAt`
- `IAuditable`: interface khai báo `CreatedAt`, `UpdatedAt`
- `Course` implement cả hai interface
- `AppDbContext.SaveChangesAsync` tự động set `CreatedAt`/`UpdatedAt` khi lưu
- Global query filter `HasQueryFilter(c => !c.IsDeleted)` — mặc định ẩn bản ghi đã xóa mềm
- `IgnoreQueryFilters()` dùng cho Trash/Restore để thấy bản ghi đã xóa

### Optimistic Concurrency (RowVersion)

- Trường `RowVersion` (`byte[]`) trên `Course` với `IsRowVersion()` trong Fluent API
- Edit action bẫy `DbUpdateConcurrencyException`, trả thông báo lỗi khi có xung đột
- CourseCode unique index (`IX_Courses_Code`) + `CodeExistsAsync` kiểm tra trùng mã

### CRUD đầy đủ + Soft Delete

| Tính năng | Route | Mô tả |
|---|---|---|
| Tạo mới | `POST /Courses/Create` | Validation mã duy nhất |
| Chỉnh sửa | `GET/POST /Courses/Edit/{id}` | RowVersion concurrency |
| Xóa mềm | `POST /Courses/Delete/{id}` | Đặt `IsDeleted = true` |
| Thùng rác | `GET /Courses/Trash` | Liệt kê bản ghi đã xóa |
| Khôi phục | `POST /Courses/Restore/{id}` | Đặt `IsDeleted = false` |

### Serilog Logging

- Ghi log ra Console và File (rolling daily, giữ 7 ngày)
- Structured request logging middleware
- Override level: `Microsoft.*` → Warning, `Microsoft.Hosting.Lifetime` → Information

### Health Checks

- `/health/live` — liveness probe, luôn trả `Healthy`
- `/health/ready` — readiness probe, kiểm tra kết nối database SQLite

### ProblemDetails API

- `GET /api/courses/{id}` — trả JSON khóa học hoặc ProblemDetails 404 có `traceId` + `timestamp`

### Migration

- `AddCourseSoftDeleteAuditFields` — thêm 5 cột: `IsDeleted`, `DeletedAt`, `CreatedAt`, `UpdatedAt`, `RowVersion`

## Kết Quả Chạy Ứng Dụng

| Trang | Route | Mô tả |
|---|---|---|
| Trang chủ | `/` | Dashboard tổng quan |
| Danh sách | `/Courses` | Chỉ hiển thị khóa chưa xóa (Global Filter) |
| Chi tiết | `/Courses/Detail/{id}` | Thông tin + audit timestamps |
| Tạo mới | `/Courses/Create` | Validation mã duy nhất |
| Chỉnh sửa | `/Courses/Edit/{id}` | RowVersion concurrency |
| Xóa mềm | `/Courses/Delete/{id}` | Xác nhận trước khi xóa |
| Thùng rác | `/Courses/Trash` | Xem + khôi phục bản ghi đã xóa |
| Đăng ký | `/Courses/Enroll` | Nghiệp vụ Transaction |
| Thống kê | `/Courses/Stats` | Doanh thu, sĩ số, biểu đồ Chart.js |
| Tìm kiếm | `/Courses/Search` | Lọc theo từ khóa và chuyên ngành |
| Data Health | `/DataHealth` | Kiểm tra migration, seed, tracking |
| Liveness | `/health/live` | Health check liveness |
| Readiness | `/health/ready` | Health check DB readiness |
| API | `/api/courses/{id}` | JSON + ProblemDetails |

## Hướng Dẫn Chạy Project

```powershell
# 1. Khôi phục & cập nhật database
cd MiniCourseCatalog.Mvc
dotnet ef database update

# 2. Chạy ứng dụng (từ root)
dotnet run --project MiniCourseCatalog.Mvc

# 3. Chạy unit test
dotnet test
```

Sau đó mở URL hiển thị trong terminal (ví dụ `http://localhost:5063`).

## Ghi Chú

- Dữ liệu lưu trong SQLite (`MiniCourseCatalog.db`), không mất khi tắt ứng dụng.
- Xóa mềm không xóa dữ liệu khỏi DB — có thể khôi phục bất cứ lúc nào từ `/Courses/Trash`.
- Log file được ghi tại thư mục `logs/` theo format `lab05-YYYYMMDD_NNN.txt`.
- Nếu trình duyệt chưa cập nhật CSS mới, nhấn `Ctrl + F5` để refresh mạnh.
