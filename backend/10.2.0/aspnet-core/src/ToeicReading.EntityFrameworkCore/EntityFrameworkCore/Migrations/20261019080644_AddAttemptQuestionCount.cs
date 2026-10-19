using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToeicReading.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAttemptQuestionCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalQuestionCount",
                table: "StudentExamAttempts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalQuestionCount",
                table: "StudentExamAttempts");
        }
    }
}
