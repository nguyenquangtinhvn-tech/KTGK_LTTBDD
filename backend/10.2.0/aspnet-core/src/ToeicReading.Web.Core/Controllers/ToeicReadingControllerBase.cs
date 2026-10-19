using Abp.AspNetCore.Mvc.Controllers;
using Abp.IdentityFramework;
using Microsoft.AspNetCore.Identity;

namespace ToeicReading.Controllers
{
    public abstract class ToeicReadingControllerBase : AbpController
    {
        protected ToeicReadingControllerBase()
        {
            LocalizationSourceName = ToeicReadingConsts.LocalizationSourceName;
        }

        protected void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }
    }
}
