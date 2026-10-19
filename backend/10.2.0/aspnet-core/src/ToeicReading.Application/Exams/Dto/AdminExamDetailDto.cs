using System;
using System.Collections.Generic;

namespace ToeicReading.Exams.Dto
{
    /// <summary>Exam detail returned only to authorized administrators.</summary>
    public class AdminExamDetailDto
    {
        /// <summary>Database identifier of the exam.</summary>
        public int Id { get; set; }

        /// <summary>Exam title.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Exam description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Allowed completion time in minutes.</summary>
        public int DurationMinutes { get; set; }

        /// <summary>Creation time recorded by the database.</summary>
        public DateTime CreationTime { get; set; }

        /// <summary>Whether the exam is published.</summary>
        public bool IsPublished { get; set; }

        /// <summary>Reading passages belonging to the exam.</summary>
        public List<AdminReadingPassageDto> ReadingPassages { get; set; }
            = new List<AdminReadingPassageDto>();

        /// <summary>Questions and answer keys belonging to the exam.</summary>
        public List<AdminQuestionDto> Questions { get; set; }
            = new List<AdminQuestionDto>();
    }

    /// <summary>Reading passage details for an administrator.</summary>
    public class AdminReadingPassageDto
    {
        /// <summary>Database identifier of the passage.</summary>
        public int Id { get; set; }

        /// <summary>TOEIC reading part number.</summary>
        public int PartNumber { get; set; }

        /// <summary>Passage title.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Passage text.</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>Display order within the exam.</summary>
        public int OrderNumber { get; set; }
    }

    /// <summary>Question detail, including its correct answer for Admin use.</summary>
    public class AdminQuestionDto
    {
        /// <summary>Database identifier of the question.</summary>
        public int Id { get; set; }

        /// <summary>Linked reading passage, if applicable.</summary>
        public int? ReadingPassageId { get; set; }

        /// <summary>TOEIC reading part number.</summary>
        public int PartNumber { get; set; }

        /// <summary>Question number shown to learners.</summary>
        public int QuestionNumber { get; set; }

        /// <summary>Question text.</summary>
        public string QuestionText { get; set; } = string.Empty;

        /// <summary>Optional hint.</summary>
        public string Hint { get; set; } = string.Empty;

        /// <summary>Explanation of the correct answer.</summary>
        public string Explanation { get; set; } = string.Empty;

        /// <summary>Answer options and correctness flags.</summary>
        public List<AdminQuestionOptionDto> Options { get; set; }
            = new List<AdminQuestionOptionDto>();
    }

    /// <summary>Answer option details for an administrator.</summary>
    public class AdminQuestionOptionDto
    {
        /// <summary>Database identifier of the option.</summary>
        public int Id { get; set; }

        /// <summary>Option label, such as A or B.</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Option text.</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>Display order within the question.</summary>
        public int OrderNumber { get; set; }

        /// <summary>Whether this option is the correct answer.</summary>
        public bool IsCorrect { get; set; }
    }
}
