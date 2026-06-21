using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MiniCourseCatalog.Mvc.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CourseCategories",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Các khóa học về lập trình, phần mềm và hệ thống máy tính", "Công nghệ Thông tin" },
                    { 2, "Các khóa học về trí tuệ nhân tạo và khoa học dữ liệu", "AI & Data Science" },
                    { 3, "Các khóa học ngôn ngữ quốc tế", "Ngoại Ngữ" },
                    { 4, "Các khóa học về tiếp thị và kinh doanh số", "Marketing" }
                });

            migrationBuilder.InsertData(
                table: "Students",
                columns: new[] { "Id", "Email", "FullName", "PhoneNumber" },
                values: new object[,]
                {
                    { 1, "an.nguyen@example.com", "Nguyễn Văn An", "0901234567" },
                    { 2, "bich.tran@example.com", "Trần Thị Bích", "0912345678" }
                });

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "Code", "CourseCategoryId", "CurrentEnrollment", "Instructor", "MaxCapacity", "Name", "StartDate", "TuitionFee" },
                values: new object[,]
                {
                    { 1, "PRG-201", 1, 28, "Cô Lê Thị Hoa", 30, "Lập Trình Hướng Đối Tượng C#", new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 2500000m },
                    { 2, "DATA-302", 2, 9, "Thầy Trần Đức Hùng", 25, "Nhập môn Khoa Học Dữ Liệu", new DateTime(2026, 9, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 3500000m },
                    { 3, "ENG-105", 3, 5, "Ms. Emily Smith", 20, "Tiếng Anh Giao Tiếp VSTEP B1", new DateTime(2026, 9, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 1800000m },
                    { 4, "DIG-101", 4, 8, "Cô Trần Thanh Mai", 30, "Digital Marketing Cơ Bản", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 2000000m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "CourseCategories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CourseCategories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "CourseCategories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "CourseCategories",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
