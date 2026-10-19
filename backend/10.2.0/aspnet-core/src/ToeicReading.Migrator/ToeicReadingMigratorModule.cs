using Abp.Events.Bus;
using Abp.Modules;
using Abp.Reflection.Extensions;
using ToeicReading.Configuration;
using ToeicReading.EntityFrameworkCore;
using ToeicReading.Migrator.DependencyInjection;
using Castle.MicroKernel.Registration;
using Microsoft.Extensions.Configuration;

namespace ToeicReading.Migrator;

[DependsOn(typeof(ToeicReadingEntityFrameworkModule))]
public class ToeicReadingMigratorModule : AbpModule
{
    private readonly IConfigurationRoot _appConfiguration;

    public ToeicReadingMigratorModule(ToeicReadingEntityFrameworkModule abpProjectNameEntityFrameworkModule)
    {
        abpProjectNameEntityFrameworkModule.SkipDbSeed = true;

        _appConfiguration = AppConfigurations.Get(
            typeof(ToeicReadingMigratorModule).GetAssembly().GetDirectoryPathOrNull()
        );
    }

    public override void PreInitialize()
    {
        Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(
            ToeicReadingConsts.ConnectionStringName
        );

        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        Configuration.ReplaceService(
            typeof(IEventBus),
            () => IocManager.IocContainer.Register(
                Component.For<IEventBus>().Instance(NullEventBus.Instance)
            )
        );
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(ToeicReadingMigratorModule).GetAssembly());
        ServiceCollectionRegistrar.Register(IocManager);
    }
}
