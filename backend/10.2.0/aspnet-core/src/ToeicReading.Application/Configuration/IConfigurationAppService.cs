using ToeicReading.Configuration.Dto;
using System.Threading.Tasks;

namespace ToeicReading.Configuration;

public interface IConfigurationAppService
{
    Task ChangeUiTheme(ChangeUiThemeInput input);
}
