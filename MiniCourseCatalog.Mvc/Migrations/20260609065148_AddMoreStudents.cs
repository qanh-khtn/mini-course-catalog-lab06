using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MiniCourseCatalog.Mvc.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreStudents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 1,
                column: "CurrentEnrollment",
                value: 29);

            migrationBuilder.InsertData(
                table: "Students",
                columns: new[] { "Id", "Email", "FullName", "PhoneNumber" },
                values: new object[,]
                {
                    { 3, "khoa.le@example.com", "Lê Minh Khoa", "0923456789" },
                    { 4, "linh.pham@example.com", "Phạm Thùy Linh", "0934567890" },
                    { 5, "thang.hoang@example.com", "Hoàng Đức Thắng", "0945678901" },
                    { 6, "huong.ngo@example.com", "Ngô Thị Hương", "0956789012" },
                    { 7, "huy.vu@example.com", "Vũ Quang Huy", "0967890123" },
                    { 8, "maianh.dang@example.com", "Đặng Thị Mai Anh", "0978901234" },
                    { 9, "dung.bui@example.com", "Bùi Tiến Dũng", "0989012345" },
                    { 10, "lan.trinh@example.com", "Trịnh Ngọc Lan", "0990123456" },
                    { 11, "tung.dinh@example.com", "Đinh Văn Tùng", "0901357924" },
                    { 12, "nga.cao@example.com", "Cao Thị Thanh Nga", "0912468035" },
                    { 13, "duc.phan@example.com", "Phan Minh Đức", "0923579146" },
                    { 14, "oanh.ly@example.com", "Lý Thị Kim Oanh", "0934680257" },
                    { 15, "long.nguyen2@example.com", "Nguyễn Thành Long", "0945791368" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.UpdateData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 1,
                column: "CurrentEnrollment",
                value: 28);
        }
    }
}
