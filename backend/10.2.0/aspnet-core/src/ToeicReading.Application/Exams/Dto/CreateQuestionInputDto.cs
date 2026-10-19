
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToeicReading.Exams.Dto
{
    public class CreateQuestionInputDto
    {
        [Range(1, int.MaxValue)]
        public int ExamId { get; set; }

        public int? ReadingPassageId { get; set; }

        [Range(5, 7)]
        public int PartNumber { get; set; }

        [Range(1, int.MaxValue)]
        public int QuestionNumber { get; set; }

        [Required]
        public string QuestionText { get; set; } = string.Empty;

        public string? Hint { get; set; }

        public string? Explanation { get; set; }

        public bool ShuffleOptions { get; set; }

        [MinLength(4)]
        public List<CreateQuestionOptionInputDto> Options { get; set; } = new();
    }

    public class CreateQuestionOptionInputDto
    {
        [Required]
        public string Label { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        public int OrderNumber { get; set; }
    }
}
