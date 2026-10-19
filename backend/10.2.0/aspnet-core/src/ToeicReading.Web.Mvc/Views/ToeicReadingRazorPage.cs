using Abp.AspNetCore.Mvc.Views;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc.Razor.Internal;

namespace ToeicReading.Web.Views;

public abstract class ToeicReadingRazorPage<TModel> : AbpRazorPage<TModel>
{
    [RazorInject]
    public IAbpSession AbpSession { get; set; }

    protected ToeicReadingRazorPage()
    {
        LocalizationSourceName = ToeicReadingConsts.LocalizationSourceName;
    }
}
