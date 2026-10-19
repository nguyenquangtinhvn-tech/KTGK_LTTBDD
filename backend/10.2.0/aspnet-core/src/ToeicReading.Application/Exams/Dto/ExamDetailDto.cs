using System.Collections.Generic;

namespace ToeicReading.Exams.Dto
{
    public class ExamDetailDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int DurationMinutes { get; set; }

        public List<ReadingPassageDto> ReadingPassages { get; set; }
            = new List<ReadingPassageDto>();

        public List<QuestionDto> Questions { get; set; }
            = new List<QuestionDto>();
    }

    public class ReadingPassageDto
    {
        public int Id { get; set; }

        public int PartNumber { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public int OrderNumber { get; set; }
    }

    public class QuestionDto
    {
        public int Id { get; set; }

        public int? ReadingPassageId { get; set; }

        public int PartNumber { get; set; }

        public int QuestionNumber { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public string? Hint { get; set; }

        public List<QuestionOptionDto> Options { get; set; }
            = new List<QuestionOptionDto>();
    }

    public class QuestionOptionDto
    {
        public int Id { get; set; }

        public string Label { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public int OrderNumber { get; set; }
    }
}