using Microsoft.EntityFrameworkCore;
using MiniCourseCatalog.Mvc.Models;

namespace MiniCourseCatalog.Mvc.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<CourseCategory> CourseCategories => Set<CourseCategory>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    // Giá trị RowVersion cố định cho seed data (SQLite không tự sinh rowversion).
    private static byte[] SeedRowVersion(int id) => new byte[] { 0, 0, 0, 0, 0, 0, 0, (byte)id };
    private static readonly DateTime SeedCreatedAt = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Local);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // CourseCategory 1:N Course
        modelBuilder.Entity<Course>()
            .HasOne(c => c.CourseCategory)
            .WithMany(cc => cc.Courses)
            .HasForeignKey(c => c.CourseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Course 1:N Enrollment
        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Student 1:N Enrollment
        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Course>()
            .Property(c => c.TuitionFee)
            .HasColumnType("decimal(18,2)");

        // CourseCode là mã định danh nghiệp vụ duy nhất
        modelBuilder.Entity<Course>()
            .HasIndex(c => c.Code)
            .IsUnique();

        // Optimistic concurrency cho luồng Enroll: UPDATE chỉ thành công khi Version chưa đổi
        modelBuilder.Entity<Course>()
            .Property(c => c.Version)
            .IsConcurrencyToken();

        // RowVersion: concurrency token cho Edit. ValueGeneratedNever vì SQLite không tự sinh —
        // ApplyAuditAndSoftDelete() gán Guid mới mỗi lần Add/Update.
        modelBuilder.Entity<Course>()
            .Property(c => c.RowVersion)
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        // Global query filter: danh sách/chi tiết mặc định bỏ qua bản ghi đã xóa mềm
        modelBuilder.Entity<Course>()
            .HasQueryFilter(c => !c.IsDeleted);

        // --- Seed Data (migration: SeedInitialData) ---
        modelBuilder.Entity<CourseCategory>().HasData(
            new CourseCategory { Id = 1, Name = "Công nghệ Thông tin", Description = "Các khóa học về lập trình, phần mềm và hệ thống máy tính" },
            new CourseCategory { Id = 2, Name = "AI & Data Science", Description = "Các khóa học về trí tuệ nhân tạo và khoa học dữ liệu" },
            new CourseCategory { Id = 3, Name = "Ngoại Ngữ", Description = "Các khóa học ngôn ngữ quốc tế" },
            new CourseCategory { Id = 4, Name = "Marketing", Description = "Các khóa học về tiếp thị và kinh doanh số" }
        );

        modelBuilder.Entity<Course>().HasData(
            new Course { Id = 1, Code = "PRG-201", Name = "Lập Trình Hướng Đối Tượng C#", Instructor = "Cô Lê Thị Hoa", TuitionFee = 2500000, CurrentEnrollment = 29, MaxCapacity = 30, StartDate = new DateTime(2026, 9, 10), CourseCategoryId = 1, CreatedAt = SeedCreatedAt, RowVersion = SeedRowVersion(1) },
            new Course { Id = 2, Code = "DATA-302", Name = "Nhập môn Khoa Học Dữ Liệu", Instructor = "Thầy Trần Đức Hùng", TuitionFee = 3500000, CurrentEnrollment = 9, MaxCapacity = 25, StartDate = new DateTime(2026, 9, 15), CourseCategoryId = 2, CreatedAt = SeedCreatedAt, RowVersion = SeedRowVersion(2) },
            new Course { Id = 3, Code = "ENG-105", Name = "Tiếng Anh Giao Tiếp VSTEP B1", Instructor = "Ms. Emily Smith", TuitionFee = 1800000, CurrentEnrollment = 5, MaxCapacity = 20, StartDate = new DateTime(2026, 9, 20), CourseCategoryId = 3, CreatedAt = SeedCreatedAt, RowVersion = SeedRowVersion(3) },
            new Course { Id = 4, Code = "DIG-101", Name = "Digital Marketing Cơ Bản", Instructor = "Cô Trần Thanh Mai", TuitionFee = 2000000, CurrentEnrollment = 8, MaxCapacity = 30, StartDate = new DateTime(2026, 10, 1), CourseCategoryId = 4, CreatedAt = SeedCreatedAt, RowVersion = SeedRowVersion(4) }
        );

        modelBuilder.Entity<Student>().HasData(
            new Student { Id = 1,  FullName = "Nguyễn Văn An",       Email = "an.nguyen@example.com",       PhoneNumber = "0901234567" },
            new Student { Id = 2,  FullName = "Trần Thị Bích",       Email = "bich.tran@example.com",       PhoneNumber = "0912345678" },
            new Student { Id = 3,  FullName = "Lê Minh Khoa",        Email = "khoa.le@example.com",         PhoneNumber = "0923456789" },
            new Student { Id = 4,  FullName = "Phạm Thùy Linh",      Email = "linh.pham@example.com",       PhoneNumber = "0934567890" },
            new Student { Id = 5,  FullName = "Hoàng Đức Thắng",     Email = "thang.hoang@example.com",     PhoneNumber = "0945678901" },
            new Student { Id = 6,  FullName = "Ngô Thị Hương",       Email = "huong.ngo@example.com",       PhoneNumber = "0956789012" },
            new Student { Id = 7,  FullName = "Vũ Quang Huy",        Email = "huy.vu@example.com",          PhoneNumber = "0967890123" },
            new Student { Id = 8,  FullName = "Đặng Thị Mai Anh",    Email = "maianh.dang@example.com",     PhoneNumber = "0978901234" },
            new Student { Id = 9,  FullName = "Bùi Tiến Dũng",       Email = "dung.bui@example.com",        PhoneNumber = "0989012345" },
            new Student { Id = 10, FullName = "Trịnh Ngọc Lan",      Email = "lan.trinh@example.com",       PhoneNumber = "0990123456" },
            new Student { Id = 11, FullName = "Đinh Văn Tùng",       Email = "tung.dinh@example.com",       PhoneNumber = "0901357924" },
            new Student { Id = 12, FullName = "Cao Thị Thanh Nga",   Email = "nga.cao@example.com",         PhoneNumber = "0912468035" },
            new Student { Id = 13, FullName = "Phan Minh Đức",       Email = "duc.phan@example.com",        PhoneNumber = "0923579146" },
            new Student { Id = 14, FullName = "Lý Thị Kim Oanh",     Email = "oanh.ly@example.com",         PhoneNumber = "0934680257" },
            new Student { Id = 15, FullName = "Nguyễn Thành Long",   Email = "long.nguyen2@example.com",    PhoneNumber = "0945791368" }
        );
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChanges();
    }

    /// <summary>
    /// Tự gán audit fields, chuyển xóa cứng thành xóa mềm và sinh RowVersion mới
    /// trước khi EF gửi lệnh xuống database.
    /// </summary>
    private void ApplyAuditAndSoftDelete()
    {
        var now = DateTime.Now;

        foreach (var entry in ChangeTracker.Entries())
        {
            // Chặn xóa cứng: chuyển Deleted -> Modified với IsDeleted = true
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable soft)
            {
                entry.State = EntityState.Modified;
                soft.IsDeleted = true;
                soft.DeletedAt = now;
            }

            if (entry.Entity is IAuditable audit)
            {
                if (entry.State == EntityState.Added)
                    audit.CreatedAt = now;
                else if (entry.State == EntityState.Modified)
                    audit.UpdatedAt = now;
            }

            // SQLite không tự sinh rowversion: gán Guid mới cho mỗi Add/Update
            if (entry.Entity is Course && entry.State is EntityState.Added or EntityState.Modified)
                entry.Property(nameof(Course.RowVersion)).CurrentValue = Guid.NewGuid().ToByteArray();
        }
    }
}
