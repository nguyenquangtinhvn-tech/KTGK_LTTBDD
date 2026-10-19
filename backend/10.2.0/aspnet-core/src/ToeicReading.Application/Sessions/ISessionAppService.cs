using Abp.Application.Services;
using ToeicReading.Sessions.Dto;
using System.Threading.Tasks;

namespace ToeicReading.Sessions;

public interface ISessionAppService : IApplicationService
{
    Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();
}
