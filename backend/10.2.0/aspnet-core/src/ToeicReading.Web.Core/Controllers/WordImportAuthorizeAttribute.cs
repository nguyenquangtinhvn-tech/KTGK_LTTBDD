using Abp.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;
using ToeicReading.Authorization;

namespace ToeicReading.Controllers
{
    /// <summary>Requires an authenticated user with the Admin Word-import permission.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WordImportAuthorizeAttribute : TypeFilterAttribute
    {
        /// <summary>Creates the Word-import authorization filter.</summary>
        public WordImportAuthorizeAttribute()
            : base(typeof(WordImportAuthorizationFilter))
        {
        }
    }

    /// <summary>Checks authentication and the exam-import permission for Word endpoints.</summary>
    public sealed class WordImportAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly IPermissionChecker _permissionChecker;

        /// <summary>Creates the filter with the ABP permission checker.</summary>
        public WordImportAuthorizationFilter(IPermissionChecker permissionChecker)
        {
            _permissionChecker = permissionChecker;
        }

        /// <summary>Rejects anonymous or unauthorized callers.</summary>
        /// <param name="context">The current authorization context.</param>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;
            if (httpContext.User?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (!await _permissionChecker.IsGrantedAsync(PermissionNames.Pages_Exams_Import))
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
