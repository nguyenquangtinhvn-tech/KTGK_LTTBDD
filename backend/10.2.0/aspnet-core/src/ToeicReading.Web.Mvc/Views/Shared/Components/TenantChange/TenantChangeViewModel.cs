using Abp.AutoMapper;
using ToeicReading.Sessions.Dto;

namespace ToeicReading.Web.Views.Shared.Components.TenantChange;

[AutoMapFrom(typeof(GetCurrentLoginInformationsOutput))]
public class TenantChangeViewModel
{
    public TenantLoginInfoDto Tenant { get; set; }
}
