using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Uow;
using ToeicReading.Authorization;
using ToeicReading.Authorization.Roles;
using ToeicReading.Authorization.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ToeicReading.Web.Host.Startup
{
    /// <summary>Seeds opt-in Development sample accounts through ABP identity services.</summary>
    public sealed class SampleAccountSeeder : ITransientDependency
    {
        private const string StudentRoleName = "Student";

        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly UserManager _userManager;
        private readonly RoleManager _roleManager;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IPermissionManager _permissionManager;
        private readonly ILogger<SampleAccountSeeder> _logger;

        /// <summary>Creates the sample-account seeder with the configured ABP identity services.</summary>
        public SampleAccountSeeder(
            IUnitOfWorkManager unitOfWorkManager,
            UserManager userManager,
            RoleManager roleManager,
            IPasswordHasher<User> passwordHasher,
            IPermissionManager permissionManager,
            ILogger<SampleAccountSeeder> logger)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _passwordHasher = passwordHasher;
            _permissionManager = permissionManager;
            _logger = logger;
        }

        /// <summary>Idempotently creates or updates the Development sample accounts.</summary>
        /// <param name="configuration">Host configuration containing opt-in seed settings and passwords.</param>
        public async Task SeedAsync(IConfiguration configuration)
        {
            if (!configuration.GetValue<bool>("SampleAccounts:SeedOnStartup"))
            {
                return;
            }

            var adminPassword = configuration["SampleAccounts:AdminPassword"];
            var studentPassword = configuration["SampleAccounts:StudentPassword"];
            if (string.IsNullOrWhiteSpace(adminPassword) ||
                string.IsNullOrWhiteSpace(studentPassword))
            {
                throw new InvalidOperationException(
                    "Development sample-account seeding requires SampleAccounts:AdminPassword and SampleAccounts:StudentPassword environment variables.");
            }

            using (var unitOfWork = _unitOfWorkManager.Begin())
            {
                await _userManager.InitializeOptionsAsync(null);
                var adminRole = await EnsureRoleAsync(
                    _roleManager,
                    StaticRoleNames.Host.Admin,
                    "Administrator");
                var studentRole = await EnsureRoleAsync(
                    _roleManager,
                    StudentRoleName,
                    "Student");

                await EnsureRolePermissionAsync(
                    _roleManager,
                    _permissionManager,
                    adminRole,
                    PermissionNames.Pages_Exams_Import,
                    true);
                await EnsureRolePermissionAsync(
                    _roleManager,
                    _permissionManager,
                    studentRole,
                    PermissionNames.Pages_Exams_Import,
                    false);

                var admin = await EnsureUserAsync(
                    _userManager,
                    _passwordHasher,
                    "admin",
                    "Admin",
                    "admin@toeicreading.local",
                    adminPassword,
                    configuration.GetValue<bool>("SampleAccounts:ResetExistingPasswords"));
                var student = await EnsureUserAsync(
                    _userManager,
                    _passwordHasher,
                    "student",
                    "Student",
                    "student@toeicreading.local",
                    studentPassword,
                    configuration.GetValue<bool>("SampleAccounts:ResetExistingPasswords"));

                await EnsureRoleMembershipAsync(_userManager, admin, adminRole);
                await RemoveAdminPermissionRolesAsync(_userManager, _roleManager, student);
                await EnsureRoleMembershipAsync(_userManager, student, studentRole);

                await unitOfWork.CompleteAsync();
                _logger.LogInformation(
                    "Development sample accounts are ready (admin user id {AdminUserId}, student user id {StudentUserId}).",
                    admin.Id,
                    student.Id);
            }
        }

        private static async Task<Role> EnsureRoleAsync(
            RoleManager roleManager,
            string roleName,
            string displayName)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                return role;
            }

            role = new Role(null, roleName, displayName)
            {
                IsStatic = roleName == StaticRoleNames.Host.Admin,
                IsDefault = false
            };
            EnsureIdentitySuccess(
                await roleManager.CreateAsync(role),
                "Could not create the " + roleName + " role");
            return role;
        }

        private static async Task EnsureRolePermissionAsync(
            RoleManager roleManager,
            IPermissionManager permissionManager,
            Role role,
            string permissionName,
            bool shouldBeGranted)
        {
            var grantedPermissions = (await roleManager.GetGrantedPermissionsAsync(role)).ToList();
            var isGranted = grantedPermissions.Any(item => item.Name == permissionName);
            if (isGranted == shouldBeGranted)
            {
                return;
            }

            var permissions = permissionManager.GetAllPermissions();
            var permission = permissions.FirstOrDefault(item => item.Name == permissionName);
            if (permission == null)
            {
                throw new InvalidOperationException(
                    "The configured permission does not exist: " + permissionName);
            }

            if (shouldBeGranted)
            {
                grantedPermissions.Add(permission);
            }
            else
            {
                grantedPermissions.RemoveAll(item => item.Name == permissionName);
            }

            await roleManager.SetGrantedPermissionsAsync(role, grantedPermissions);
        }

        private async Task<User> EnsureUserAsync(
            UserManager userManager,
            IPasswordHasher<User> passwordHasher,
            string userName,
            string name,
            string emailAddress,
            string password,
            bool resetExistingPassword)
        {
            var user = await userManager.FindByNameAsync(userName);
            var isNew = user == null;
            if (isNew)
            {
                user = new User
                {
                    TenantId = null,
                    UserName = userName,
                    Name = name,
                    Surname = name,
                    EmailAddress = emailAddress,
                    IsEmailConfirmed = true,
                    IsActive = true
                };
                user.SetNormalizedNames();
                EnsureIdentitySuccess(
                    await userManager.CreateAsync(user, password),
                    "Could not create the " + userName + " account");
                return user;
            }

            var changed = false;
            if (!user.IsActive)
            {
                user.IsActive = true;
                changed = true;
            }

            if (!user.IsEmailConfirmed)
            {
                user.IsEmailConfirmed = true;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(user.Name))
            {
                user.Name = name;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(user.Surname))
            {
                user.Surname = name;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(user.EmailAddress))
            {
                user.EmailAddress = emailAddress;
                changed = true;
            }

            if (resetExistingPassword)
            {
                user.Password = passwordHasher.HashPassword(user, password);
                user.SecurityStamp = Guid.NewGuid().ToString("N");
                changed = true;
            }

            if (changed)
            {
                user.SetNormalizedNames();
                EnsureIdentitySuccess(
                    await userManager.UpdateAsync(user),
                    "Could not update the " + userName + " account");
            }

            return user;
        }

        private static async Task EnsureRoleMembershipAsync(
            UserManager userManager,
            User user,
            Role role)
        {
            if (!await userManager.IsInRoleAsync(user, role.Name))
            {
                EnsureIdentitySuccess(
                    await userManager.AddToRoleAsync(user, role.Name),
                    "Could not assign the " + role.Name + " role");
            }
        }

        private static async Task RemoveAdminPermissionRolesAsync(
            UserManager userManager,
            RoleManager roleManager,
            User user)
        {
            var roleNames = await userManager.GetRolesAsync(user);
            foreach (var roleName in roleNames)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role == null)
                {
                    continue;
                }

                var permissions = await roleManager.GetGrantedPermissionsAsync(role);
                if (permissions.Any(permission =>
                    permission.Name == PermissionNames.Pages_Exams_Import))
                {
                    EnsureIdentitySuccess(
                        await userManager.RemoveFromRoleAsync(user, role.Name),
                        "Could not remove the administrative role from the student account");
                }
            }
        }

        private static void EnsureIdentitySuccess(IdentityResult result, string operation)
        {
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    operation + ": " +
                    string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }
    }
}
