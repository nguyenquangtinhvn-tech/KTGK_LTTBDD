using Abp.Application.Services;
using ToeicReading.MultiTenancy.Dto;

namespace ToeicReading.MultiTenancy;

public interface ITenantAppService : IAsyncCrudAppService<TenantDto, int, PagedTenantResultRequestDto, CreateTenantDto, TenantDto>
{
}

