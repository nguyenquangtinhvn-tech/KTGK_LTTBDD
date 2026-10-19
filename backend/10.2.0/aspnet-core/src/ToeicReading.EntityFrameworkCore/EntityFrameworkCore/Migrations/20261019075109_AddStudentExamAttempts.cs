using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToeicReading.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentExamAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentExamAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentUserId = table.Column<long>(type: "bigint", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: true),
                    CorrectCount = table.Column<int>(type: "int", nullable: true),
                    WrongCount = table.Column<int>(type: "int", nullable: true),
                    BlankCount = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentExamAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentExamAttempts_AbpUsers_StudentUserId",
                        column: x => x.StudentUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentExamAttempts_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentExamAttemptAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentExamAttemptId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    SelectedOptionId = table.Column<int>(type: "int", nullable: true),
                    LastSavedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentExamAttemptAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentExamAttemptAnswers_QuestionOptions_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "QuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentExamAttemptAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentExamAttemptAnswers_StudentExamAttempts_StudentExamAttemptId",
                        column: x => x.StudentExamAttemptId,
                        principalTable: "StudentExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAttemptAnswers_QuestionId",
                table: "StudentExamAttemptAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAttemptAnswers_SelectedOptionId",
                table: "StudentExamAttemptAnswers",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAttemptAnswers_StudentExamAttemptId_QuestionId",
                table: "StudentExamAttemptAnswers",
                columns: new[] { "StudentExamAttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAttempts_ExamId",
                table: "StudentExamAttempts",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAttempts_StudentUserId_ExamId",
                table: "StudentExamAttempts",
                columns: new[] { "StudentUserId", "ExamId" },
                unique: true,
                filter: "[IsSubmitted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAttempts_StudentUserId_SubmittedAtUtc",
                table: "StudentExamAttempts",
                columns: new[] { "StudentUserId", "SubmittedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentExamAttemptAnswers");

            migrationBuilder.DropTable(
                name: "StudentExamAttempts");
        }
    }
}
