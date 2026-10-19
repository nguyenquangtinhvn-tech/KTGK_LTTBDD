using System.Collections.Generic;
using System.Linq;

namespace ToeicReading.Exams.Dto
{
    public class WordExamPreviewDto
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }

        public bool HasEndExam { get; set; }

        public List<WordReadingPassageDto> ReadingPassages { get; set; }
            = new List<WordReadingPassageDto>();

        public List<WordQuestionDto> Questions { get; set; }
            = new List<WordQuestionDto>();

        public List<WordImportIssueDto> Errors { get; set; }
            = new List<WordImportIssueDto>();

        public List<WordImportIssueDto> Warnings { get; set; }
            = new List<WordImportIssueDto>();

        public bool IsValid
        {
            get
            {
                return Errors.Count == 0 &&
                       Questions.All(question => question.Errors.Count == 0);
            }
        }
    }

    public class WordReadingPassageDto
    {
        public string TempId { get; set; } = string.Empty;

        public int PartNumber { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public int OrderNumber { get; set; }
    }

    public class WordQuestionDto
    {
        public int QuestionNumber { get; set; }

        public int PartNumber { get; set; }

        public string? ReadingPassageTempId { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public List<WordQuestionOptionDto> Options { get; set; }
            = new List<WordQuestionOptionDto>();

        public string Answer { get; set; } = string.Empty;

        public string Explanation { get; set; } = string.Empty;

        public string Hint { get; set; } = string.Empty;

        public List<WordImportIssueDto> Errors { get; set; }
            = new List<WordImportIssueDto>();
    }

    public class WordQuestionOptionDto
    {
        public string Label { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }

    public class WordImportIssueDto
    {
        public string Code { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public int? QuestionNumber { get; set; }
    }

    public class WordExamImportResultDto
    {
        public int ExamId { get; set; }

        public int PassageCount { get; set; }

        public int QuestionCount { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
