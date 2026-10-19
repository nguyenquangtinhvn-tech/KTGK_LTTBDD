using System;
using System.Collections.Generic;

namespace ToeicReading.Exams.Dto
{
    public class SaveStudentExamAnswerInputDto
    {
        public int AttemptId { get; set; }

        public int QuestionId { get; set; }

        public int? SelectedOptionId { get; set; }
    }

    public class StudentExamAttemptDto
    {
        public int AttemptId { get; set; }

        public int ExamId { get; set; }

        public string ExamTitle { get; set; } = string.Empty;

        public DateTimeOffset StartedAtUtc { get; set; }

        public DateTimeOffset ExpiresAtUtc { get; set; }

        public int RemainingSeconds { get; set; }

        public bool IsSubmitted { get; set; }

        public StudentExamResultDto? Result { get; set; }

        public List<StudentAttemptPassageDto> ReadingPassages { get; set; } = new();

        public List<StudentAttemptQuestionDto> Questions { get; set; } = new();

        public List<StudentAttemptAnswerDto> Answers { get; set; } = new();
    }

    public class StudentAttemptPassageDto
    {
        public int Id { get; set; }

        public int PartNumber { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public int OrderNumber { get; set; }
    }

    public class StudentAttemptQuestionDto
    {
        public int Id { get; set; }

        public int? ReadingPassageId { get; set; }

        public int PartNumber { get; set; }

        public int QuestionNumber { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public List<StudentAttemptOptionDto> Options { get; set; } = new();
    }

    public class StudentAttemptOptionDto
    {
        public int Id { get; set; }

        public string Label { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }

    public class StudentAttemptAnswerDto
    {
        public int QuestionId { get; set; }

        public int? SelectedOptionId { get; set; }
    }

    public class StudentExamResultDto
    {
        public int AttemptId { get; set; }

        public int ExamId { get; set; }

        public string ExamTitle { get; set; } = string.Empty;

        public DateTimeOffset StartedAtUtc { get; set; }

        public DateTimeOffset SubmittedAtUtc { get; set; }

        public int DurationSeconds { get; set; }

        public int Score { get; set; }

        public int CorrectCount { get; set; }

        public int WrongCount { get; set; }

        public int BlankCount { get; set; }

        public int TotalQuestionCount { get; set; }

        public decimal AccuracyPercent { get; set; }
    }

    public class StudentExamHistoryItemDto
    {
        public int AttemptId { get; set; }

        public int ExamId { get; set; }

        public string ExamTitle { get; set; } = string.Empty;

        public DateTimeOffset StartedAtUtc { get; set; }

        public DateTimeOffset SubmittedAtUtc { get; set; }

        public int DurationSeconds { get; set; }

        public int Score { get; set; }

        public int CorrectCount { get; set; }

        public int WrongCount { get; set; }

        public int BlankCount { get; set; }

        public int TotalQuestionCount { get; set; }

        public decimal AccuracyPercent { get; set; }
    }
}
