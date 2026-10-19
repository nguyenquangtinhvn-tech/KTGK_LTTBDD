using Abp.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using Shouldly;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using ToeicReading.Authorization;
using ToeicReading.Controllers;
using Xunit;

namespace ToeicReading.Web.Tests.Controllers
{
    public class WordImportAuthorizationFilterTests
    {
        [Fact]
        public async Task DevelopmentLoopbackAnonymousUser_ShouldRequireAuthentication()
        {
            var permissionChecker = Substitute.For<IPermissionChecker>();
            var filter = CreateFilter(permissionChecker);
            var context = CreateContext("localhost", IPAddress.IPv6Loopback, isAuthenticated: false);

            await filter.OnAuthorizationAsync(context);

            context.Result.ShouldBeOfType<UnauthorizedResult>();
            await permissionChecker.DidNotReceive()
                .IsGrantedAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task DevelopmentNonLocalHost_ShouldRequireAuthentication()
        {
            var permissionChecker = Substitute.For<IPermissionChecker>();
            var filter = CreateFilter(permissionChecker);
            var context = CreateContext("admin.example.com", IPAddress.Loopback, isAuthenticated: false);

            await filter.OnAuthorizationAsync(context);

            context.Result.ShouldBeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task Production_ShouldRequireAuthentication()
        {
            var permissionChecker = Substitute.For<IPermissionChecker>();
            var filter = CreateFilter(permissionChecker);
            var context = CreateContext("localhost", IPAddress.Loopback, isAuthenticated: false);

            await filter.OnAuthorizationAsync(context);

            context.Result.ShouldBeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task ProductionAuthenticatedUser_ShouldRequireImportPermission()
        {
            var permissionChecker = Substitute.For<IPermissionChecker>();
            permissionChecker.IsGrantedAsync(PermissionNames.Pages_Exams_Import)
                .Returns(Task.FromResult(false));
            var filter = CreateFilter(permissionChecker);
            var context = CreateContext("localhost", IPAddress.Loopback, isAuthenticated: true);

            await filter.OnAuthorizationAsync(context);

            context.Result.ShouldBeOfType<ForbidResult>();
            await permissionChecker.Received(1)
                .IsGrantedAsync(PermissionNames.Pages_Exams_Import);
        }

        [Fact]
        public async Task AuthenticatedUserWithImportPermission_ShouldBeAllowed()
        {
            var permissionChecker = Substitute.For<IPermissionChecker>();
            permissionChecker.IsGrantedAsync(PermissionNames.Pages_Exams_Import)
                .Returns(Task.FromResult(true));
            var filter = CreateFilter(permissionChecker);
            var context = CreateContext("localhost", IPAddress.Loopback, isAuthenticated: true);

            await filter.OnAuthorizationAsync(context);

            context.Result.ShouldBeNull();
            await permissionChecker.Received(1)
                .IsGrantedAsync(PermissionNames.Pages_Exams_Import);
        }

        private static WordImportAuthorizationFilter CreateFilter(
            IPermissionChecker permissionChecker)
        {
            return new WordImportAuthorizationFilter(permissionChecker);
        }

        private static AuthorizationFilterContext CreateContext(
            string host,
            IPAddress remoteAddress,
            bool isAuthenticated)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(host, 44311);
            httpContext.Connection.RemoteIpAddress = remoteAddress;
            if (isAuthenticated)
            {
                httpContext.User = new ClaimsPrincipal(
                    new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin") }, "test"));
            }

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor());
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }
    }
}
