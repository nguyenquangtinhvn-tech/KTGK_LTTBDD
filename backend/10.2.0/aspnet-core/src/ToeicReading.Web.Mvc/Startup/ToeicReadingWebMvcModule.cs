using Abp.Modules;
using Abp.Reflection.Extensions;
using ToeicReading.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ToeicReading.Web.Startup;

[DependsOn(typeof(ToeicReadingWebCoreModule))]
public class ToeicReadingWebMvcModule : AbpModule
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfigurationRoot _appConfiguration;

    public ToeicReadingWebMvcModule(IWebHostEnvironment env)
    {
        _env = env;
        _appConfiguration = env.GetAppConfiguration();
    }

    public override void PreInitialize()
    {
        Configuration.Navigation.Providers.Add<ToeicReadingNavigationProvider>();
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(ToeicReadingWebMvcModule).GetAssembly());
    }
}
