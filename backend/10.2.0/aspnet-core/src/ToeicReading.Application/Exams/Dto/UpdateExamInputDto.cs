
using System.ComponentModel.DataAnnotations;

namespace ToeicReading.Exams.Dto
{
    public class UpdateExamInputDto
    {
        [Required(ErrorMessage = "Tên đề thi không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên đề thi tối đa 200 ký tự.")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(1, 300, ErrorMessage = "Thời gian làm bài phải từ 1 đến 300 phút.")]
        public int DurationMinutes { get; set; }

        public bool IsPublished { get; set; }
    }
}
