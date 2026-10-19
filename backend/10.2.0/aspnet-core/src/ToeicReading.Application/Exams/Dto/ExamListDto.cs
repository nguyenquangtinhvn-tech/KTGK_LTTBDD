namespace ToeicReading.Exams.Dto
{
    public class ExamListDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int DurationMinutes { get; set; }

        public int QuestionCount { get; set; }
    }
}