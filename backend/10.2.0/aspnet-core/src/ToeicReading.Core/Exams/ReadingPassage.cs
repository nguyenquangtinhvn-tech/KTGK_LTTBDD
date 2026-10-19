using Abp.Domain.Entities;
using System.Collections.Generic;

namespace ToeicReading.Exams
{
    public class ReadingPassage : Entity
    {
        public int ExamId { get; set; }

        public int PartNumber { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public int OrderNumber { get; set; }

        public Exam Exam { get; set; } = null!;

        public ICollection<Question> Questions { get; set; }
            = new List<Question>();
    }
}