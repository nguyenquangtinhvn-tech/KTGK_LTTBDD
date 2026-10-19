using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using ToeicReading.Authorization;

namespace ToeicReading;

[DependsOn(
    typeof(ToeicReadingCoreModule),
    typeof(AbpAutoMapperModule))]
public class ToeicReadingApplicationModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Authorization.Providers.Add<ToeicReadingAuthorizationProvider>();
    }

    public override void Initialize()
    {
        var thisAssembly = typeof(ToeicReadingApplicationModule).GetAssembly();

        IocManager.RegisterAssemblyByConvention(thisAssembly);

        Configuration.Modules.AbpAutoMapper().Configurators.Add(
            // Scan the assembly for classes which inherit from AutoMapper.Profile
            cfg => cfg.AddMaps(thisAssembly)
        );
    }
}
