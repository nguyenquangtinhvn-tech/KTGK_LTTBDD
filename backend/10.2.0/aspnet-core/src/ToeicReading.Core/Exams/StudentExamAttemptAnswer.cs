using Abp.Domain.Entities;
using System;

namespace ToeicReading.Exams
{
    public class StudentExamAttemptAnswer : Entity
    {
        public int StudentExamAttemptId { get; set; }

        public int QuestionId { get; set; }

        public int? SelectedOptionId { get; set; }

        public DateTimeOffset LastSavedAtUtc { get; set; }
    }
}
