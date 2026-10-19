using Abp.AspNetCore.Mvc.ViewComponents;

namespace ToeicReading.Web.Views;

public abstract class ToeicReadingViewComponent : AbpViewComponent
{
    protected ToeicReadingViewComponent()
    {
        LocalizationSourceName = ToeicReadingConsts.LocalizationSourceName;
    }
}
