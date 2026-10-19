using Abp.Domain.Entities;
using System;

namespace ToeicReading.Exams
{
    public class StudentExamAttempt : Entity
    {
        public long StudentUserId { get; set; }

        public int ExamId { get; set; }

        public DateTimeOffset StartedAtUtc { get; set; }

        public DateTimeOffset ExpiresAtUtc { get; set; }

        public DateTimeOffset? SubmittedAtUtc { get; set; }

        public bool IsSubmitted { get; set; }

        public int DurationSeconds { get; set; }

        public int? Score { get; set; }

        public int? CorrectCount { get; set; }

        public int? WrongCount { get; set; }

        public int? BlankCount { get; set; }

        public int TotalQuestionCount { get; set; }
    }
}
