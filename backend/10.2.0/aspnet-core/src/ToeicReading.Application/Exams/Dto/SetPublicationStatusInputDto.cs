using System.ComponentModel.DataAnnotations;

namespace ToeicReading.Exams.Dto
{
    public class SetPublicationStatusInputDto
    {
        [Range(1, int.MaxValue)]
        public int Id { get; set; }

        public bool IsPublished { get; set; }
    }
}
