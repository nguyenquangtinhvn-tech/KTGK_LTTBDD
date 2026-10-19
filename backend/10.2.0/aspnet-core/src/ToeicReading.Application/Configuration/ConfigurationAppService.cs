using Abp.Authorization;
using Abp.Runtime.Session;
using ToeicReading.Configuration.Dto;
using System.Threading.Tasks;

namespace ToeicReading.Configuration;

[AbpAuthorize]
public class ConfigurationAppService : ToeicReadingAppServiceBase, IConfigurationAppService
{
    public async Task ChangeUiTheme(ChangeUiThemeInput input)
    {
        await SettingManager.ChangeSettingForUserAsync(AbpSession.ToUserIdentifier(), AppSettingNames.UiTheme, input.Theme);
    }
}
