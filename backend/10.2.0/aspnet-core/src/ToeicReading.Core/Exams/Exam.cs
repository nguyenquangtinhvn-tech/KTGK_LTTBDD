using Abp.Domain.Entities;
using System;
using System.Collections.Generic;

namespace ToeicReading.Exams
{
    public class Exam : Entity
    {
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int DurationMinutes { get; set; } = 75;

        public bool IsPublished { get; set; } = false;

        public DateTime CreationTime { get; set; } = DateTime.UtcNow;

        public ICollection<ReadingPassage> ReadingPassages { get; set; }
            = new List<ReadingPassage>();

        public ICollection<Question> Questions { get; set; }
            = new List<Question>();
    }
}