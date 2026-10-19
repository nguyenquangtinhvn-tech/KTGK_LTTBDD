using Abp.Domain.Entities;
using System.Collections.Generic;

namespace ToeicReading.Exams
{
    public class Question : Entity
    {
        public int ExamId { get; set; }

        public int? ReadingPassageId { get; set; }

        public int PartNumber { get; set; }

        public int QuestionNumber { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public string? Hint { get; set; }

        public string? Explanation { get; set; }

        public bool ShuffleOptions { get; set; } = false;

        public Exam Exam { get; set; } = null!;

        public ReadingPassage? ReadingPassage { get; set; }

        public ICollection<QuestionOption> Options { get; set; }
            = new List<QuestionOption>();
    }
}