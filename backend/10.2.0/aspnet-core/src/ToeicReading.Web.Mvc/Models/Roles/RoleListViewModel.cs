using ToeicReading.Roles.Dto;
using System.Collections.Generic;

namespace ToeicReading.Web.Models.Roles;

public class RoleListViewModel
{
    public IReadOnlyList<PermissionDto> Permissions { get; set; }
}
