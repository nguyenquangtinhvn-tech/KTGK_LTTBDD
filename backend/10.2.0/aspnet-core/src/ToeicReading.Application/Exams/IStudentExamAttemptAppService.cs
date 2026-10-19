using Abp.Application.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using ToeicReading.Exams.Dto;

namespace ToeicReading.Exams
{
    public interface IStudentExamAttemptAppService : IApplicationService
    {
        Task<StudentExamAttemptDto> CreateAttemptAsync(int examId);

        Task<StudentExamAttemptDto> GetAttemptAsync(int attemptId);

        Task<StudentExamAttemptDto> SaveAnswerAsync(SaveStudentExamAnswerInputDto input);

        Task<StudentExamResultDto> SubmitAttemptAsync(int attemptId);

        Task<List<StudentExamHistoryItemDto>> GetMyAttemptHistoryAsync();
    }
}
