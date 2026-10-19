using Abp.Authorization;
using ToeicReading.Authorization.Roles;
using ToeicReading.Authorization.Users;

namespace ToeicReading.Authorization;

public class PermissionChecker : PermissionChecker<Role, User>
{
    public PermissionChecker(UserManager userManager)
        : base(userManager)
    {
    }
}
