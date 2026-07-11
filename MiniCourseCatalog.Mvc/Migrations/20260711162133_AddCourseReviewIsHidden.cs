using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniCourseCatalog.Mvc.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseReviewIsHidden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "CourseReviews",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "CourseReviews");
        }
    }
}
