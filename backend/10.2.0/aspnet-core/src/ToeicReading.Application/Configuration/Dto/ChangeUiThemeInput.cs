using System.ComponentModel.DataAnnotations;

namespace ToeicReading.Configuration.Dto;

public class ChangeUiThemeInput
{
    [Required]
    [StringLength(32)]
    public string Theme { get; set; }
}
