using Abp.Domain.Entities;

namespace ToeicReading.Exams
{
    public class QuestionOption : Entity
    {
        public int QuestionId { get; set; }

        public string Label { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public bool IsCorrect { get; set; } = false;

        public int OrderNumber { get; set; }

        public Question Question { get; set; } = null!;
    }
}