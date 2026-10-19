using ToeicReading.Roles.Dto;
using System.Collections.Generic;

namespace ToeicReading.Web.Models.Common;

public interface IPermissionsEditViewModel
{
    List<FlatPermissionDto> Permissions { get; set; }
}