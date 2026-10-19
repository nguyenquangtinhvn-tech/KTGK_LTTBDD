using Abp.Application.Services;
using ToeicReading.Authorization.Accounts.Dto;
using System.Threading.Tasks;

namespace ToeicReading.Authorization.Accounts;

public interface IAccountAppService : IApplicationService
{
    Task<IsTenantAvailableOutput> IsTenantAvailable(IsTenantAvailableInput input);

    Task<RegisterOutput> Register(RegisterInput input);
}
