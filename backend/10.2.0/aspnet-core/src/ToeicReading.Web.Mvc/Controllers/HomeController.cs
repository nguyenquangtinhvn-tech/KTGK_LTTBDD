using Abp.AspNetCore.Mvc.Authorization;
using ToeicReading.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace ToeicReading.Web.Controllers;

[AbpMvcAuthorize]
public class HomeController : ToeicReadingControllerBase
{
    public ActionResult Index()
    {
        return View();
    }
}
