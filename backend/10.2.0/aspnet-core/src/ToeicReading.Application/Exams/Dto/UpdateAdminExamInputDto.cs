using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToeicReading.Exams.Dto
{
    /// <summary>Full editable content of an unpublished exam.</summary>
    public class UpdateAdminExamInputDto
    {
        /// <summary>Exam title.</summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Optional exam description.</summary>
        [StringLength(4000)]
        public string? Description { get; set; }

        /// <summary>Exam duration in minutes.</summary>
        [Range(1, 300)]
        public int DurationMinutes { get; set; }

        /// <summary>Passages to retain or add. Omitted existing passages are removed.</summary>
        [Required]
        public List<UpdateAdminReadingPassageInputDto> ReadingPassages { get; set; } = new();

        /// <summary>Questions to retain, add, or remove.</summary>
        [Required]
        public List<UpdateAdminQuestionInputDto> Questions { get; set; } = new();
    }

    /// <summary>Editable passage data. A non-positive ID denotes a new passage.</summary>
    public class UpdateAdminReadingPassageInputDto
    {
        /// <summary>Existing passage ID, or zero for a new passage.</summary>
        public int Id { get; set; }

        /// <summary>Request-local identifier used to link a new passage to questions.</summary>
        public string? ClientKey { get; set; }

        /// <summary>TOEIC part number (6 or 7).</summary>
        [Range(6, 7)]
        public int PartNumber { get; set; }

        /// <summary>Passage title.</summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Passage content.</summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>Display order within the part.</summary>
        [Range(1, int.MaxValue)]
        public int OrderNumber { get; set; }
    }

    /// <summary>Editable question and its four answer options.</summary>
    public class UpdateAdminQuestionInputDto
    {
        /// <summary>Existing question ID, or zero for a new question.</summary>
        public int Id { get; set; }

        /// <summary>TOEIC part number.</summary>
        [Range(5, 7)]
        public int PartNumber { get; set; }

        /// <summary>Question number shown to learners.</summary>
        [Range(1, int.MaxValue)]
        public int QuestionNumber { get; set; }

        /// <summary>Existing passage ID when this question uses a saved passage.</summary>
        public int? ReadingPassageId { get; set; }

        /// <summary>Request-local passage key when linking to a new passage.</summary>
        public string? ReadingPassageClientKey { get; set; }

        /// <summary>Question text, or empty for a Part 6 gap represented in its passage.</summary>
        public string QuestionText { get; set; } = string.Empty;

        /// <summary>Optional hint.</summary>
        public string? Hint { get; set; }

        /// <summary>Optional answer explanation.</summary>
        public string? Explanation { get; set; }

        /// <summary>Answer choices; exactly A, B, C, and D are required.</summary>
        public List<UpdateAdminQuestionOptionInputDto> Options { get; set; } = new();
    }

    /// <summary>An editable answer choice.</summary>
    public class UpdateAdminQuestionOptionInputDto
    {
        /// <summary>Existing option ID, or zero for a new option.</summary>
        public int Id { get; set; }

        /// <summary>Choice label, A through D.</summary>
        [Required]
        public string Label { get; set; } = string.Empty;

        /// <summary>Choice text.</summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>Display order within the question.</summary>
        public int OrderNumber { get; set; }

        /// <summary>Whether this is the sole correct choice.</summary>
        public bool IsCorrect { get; set; }
    }
}
