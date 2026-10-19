using Abp.AspNetCore;
using Abp.AspNetCore.TestBase;
using Abp.Modules;
using Abp.Reflection.Extensions;
using ToeicReading.EntityFrameworkCore;
using ToeicReading.Web.Startup;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace ToeicReading.Web.Tests;

[DependsOn(
    typeof(ToeicReadingWebMvcModule),
    typeof(AbpAspNetCoreTestBaseModule)
)]
public class ToeicReadingWebTestModule : AbpModule
{
    public ToeicReadingWebTestModule(ToeicReadingEntityFrameworkModule abpProjectNameEntityFrameworkModule)
    {
        abpProjectNameEntityFrameworkModule.SkipDbContextRegistration = true;
    }

    public override void PreInitialize()
    {
        Configuration.UnitOfWork.IsTransactional = false; //EF Core InMemory DB does not support transactions.
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(ToeicReadingWebTestModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        IocManager.Resolve<ApplicationPartManager>()
            .AddApplicationPartsIfNotAddedBefore(typeof(ToeicReadingWebMvcModule).Assembly);
    }
}