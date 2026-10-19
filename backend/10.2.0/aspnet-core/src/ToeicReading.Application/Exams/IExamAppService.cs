
using Abp.Application.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using ToeicReading.Exams.Dto;

namespace ToeicReading.Exams
{
    public interface IExamAppService : IApplicationService
    {
        Task<List<ExamListDto>> GetPublishedExamsAsync();

        Task<ExamDetailDto> GetExamDetailAsync(int id);

        Task<int> CreateQuestionAsync(CreateQuestionInputDto input);

        Task UpdateExamAsync(int id, UpdateExamInputDto input);

        Task<bool> SetPublicationStatusAsync(SetPublicationStatusInputDto input);
    }
}
