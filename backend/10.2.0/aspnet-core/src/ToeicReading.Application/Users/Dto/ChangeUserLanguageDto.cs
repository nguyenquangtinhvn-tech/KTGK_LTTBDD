using System.ComponentModel.DataAnnotations;

namespace ToeicReading.Users.Dto;

public class ChangeUserLanguageDto
{
    [Required]
    public string LanguageName { get; set; }
}