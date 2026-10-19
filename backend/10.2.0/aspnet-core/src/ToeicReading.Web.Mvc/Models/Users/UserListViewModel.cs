using ToeicReading.Roles.Dto;
using System.Collections.Generic;

namespace ToeicReading.Web.Models.Users;

public class UserListViewModel
{
    public IReadOnlyList<RoleDto> Roles { get; set; }
}
