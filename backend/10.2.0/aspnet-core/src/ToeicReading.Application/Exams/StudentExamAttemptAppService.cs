using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToeicReading.Exams.Dto;

namespace ToeicReading.Exams
{
    [AbpAuthorize]
    public class StudentExamAttemptAppService : ToeicReadingAppServiceBase, IStudentExamAttemptAppService
    {
        private const string StudentRoleName = "Student";

        private readonly IRepository<Exam, int> _examRepository;
        private readonly IRepository<ReadingPassage, int> _passageRepository;
        private readonly IRepository<Question, int> _questionRepository;
        private readonly IRepository<QuestionOption, int> _optionRepository;
        private readonly IRepository<StudentExamAttempt, int> _attemptRepository;
        private readonly IRepository<StudentExamAttemptAnswer, int> _answerRepository;

        public StudentExamAttemptAppService(
            IRepository<Exam, int> examRepository,
            IRepository<ReadingPassage, int> passageRepository,
            IRepository<Question, int> questionRepository,
            IRepository<QuestionOption, int> optionRepository,
            IRepository<StudentExamAttempt, int> attemptRepository,
            IRepository<StudentExamAttemptAnswer, int> answerRepository)
        {
            _examRepository = examRepository;
            _passageRepository = passageRepository;
            _questionRepository = questionRepository;
            _optionRepository = optionRepository;
            _attemptRepository = attemptRepository;
            _answerRepository = answerRepository;
        }

        public async Task<StudentExamAttemptDto> CreateAttemptAsync(int examId)
        {
            var user = await GetStudentUserAsync();
            var exam = await _examRepository.GetAll()
                .FirstOrDefaultAsync(item => item.Id == examId);
            if (exam == null)
            {
                throw new UserFriendlyException("Không tìm thấy đề thi.");
            }

            var existingAttempt = await _attemptRepository.GetAll()
                .FirstOrDefaultAsync(attempt =>
                    attempt.StudentUserId == user.Id &&
                    attempt.ExamId == examId &&
                    !attempt.IsSubmitted);

            if (existingAttempt != null)
            {
                if (existingAttempt.ExpiresAtUtc <= DateTimeOffset.UtcNow)
                {
                    await CompleteAttemptAsync(existingAttempt);
                    await CurrentUnitOfWork.SaveChangesAsync();
                }
                return await BuildAttemptDtoAsync(existingAttempt);
            }

            if (!exam.IsPublished)
            {
                throw new UserFriendlyException("Đề thi chưa được xuất bản.");
            }

            var startedAt = DateTimeOffset.UtcNow;
            var attempt = new StudentExamAttempt
            {
                StudentUserId = user.Id,
                ExamId = exam.Id,
                StartedAtUtc = startedAt,
                ExpiresAtUtc = startedAt.AddMinutes(exam.DurationMinutes)
            };

            await _attemptRepository.InsertAsync(attempt);
            await CurrentUnitOfWork.SaveChangesAsync();
            return await BuildAttemptDtoAsync(attempt);
        }

        public async Task<StudentExamAttemptDto> GetAttemptAsync(int attemptId)
        {
            var user = await GetStudentUserAsync();
            var attempt = await GetOwnedAttemptAsync(attemptId, user.Id);
            if (!attempt.IsSubmitted && attempt.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                await CompleteAttemptAsync(attempt);
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            return await BuildAttemptDtoAsync(attempt);
        }

        public async Task<StudentExamAttemptDto> SaveAnswerAsync(SaveStudentExamAnswerInputDto input)
        {
            if (input == null || input.AttemptId <= 0 || input.QuestionId <= 0)
            {
                throw new UserFriendlyException("Thông tin câu trả lời không hợp lệ.");
            }

            var user = await GetStudentUserAsync();
            var attempt = await GetOwnedAttemptAsync(input.AttemptId, user.Id);
            if (attempt.IsSubmitted)
            {
                throw new UserFriendlyException("Lượt thi đã nộp, không thể thay đổi câu trả lời.");
            }
            if (attempt.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                await CompleteAttemptAsync(attempt);
                await CurrentUnitOfWork.SaveChangesAsync();
                return await BuildAttemptDtoAsync(attempt);
            }

            var questionBelongsToExam = await _questionRepository.GetAll()
                .AnyAsync(question =>
                    question.Id == input.QuestionId &&
                    question.ExamId == attempt.ExamId);
            if (!questionBelongsToExam)
            {
                throw new UserFriendlyException("Câu hỏi không thuộc đề thi của lượt làm bài này.");
            }

            if (input.SelectedOptionId.HasValue)
            {
                var optionBelongsToQuestion = await _optionRepository.GetAll()
                    .AnyAsync(option =>
                        option.Id == input.SelectedOptionId.Value &&
                        option.QuestionId == input.QuestionId);
                if (!optionBelongsToQuestion)
                {
                    throw new UserFriendlyException("Phương án trả lời không thuộc câu hỏi này.");
                }
            }

            var existingAnswer = await _answerRepository.GetAll()
                .FirstOrDefaultAsync(answer =>
                    answer.StudentExamAttemptId == attempt.Id &&
                    answer.QuestionId == input.QuestionId);

            if (!input.SelectedOptionId.HasValue)
            {
                if (existingAnswer != null)
                {
                    await _answerRepository.DeleteAsync(existingAnswer);
                    await CurrentUnitOfWork.SaveChangesAsync();
                }
            }
            else if (existingAnswer == null)
            {
                await _answerRepository.InsertAsync(new StudentExamAttemptAnswer
                {
                    StudentExamAttemptId = attempt.Id,
                    QuestionId = input.QuestionId,
                    SelectedOptionId = input.SelectedOptionId,
                    LastSavedAtUtc = DateTimeOffset.UtcNow
                });
                await CurrentUnitOfWork.SaveChangesAsync();
            }
            else
            {
                existingAnswer.SelectedOptionId = input.SelectedOptionId;
                existingAnswer.LastSavedAtUtc = DateTimeOffset.UtcNow;
                await _answerRepository.UpdateAsync(existingAnswer);
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            return await BuildAttemptDtoAsync(attempt);
        }

        public async Task<StudentExamResultDto> SubmitAttemptAsync(int attemptId)
        {
            var user = await GetStudentUserAsync();
            var attempt = await GetOwnedAttemptAsync(attemptId, user.Id);
            if (!attempt.IsSubmitted)
            {
                await CompleteAttemptAsync(attempt);
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            return await BuildResultDtoAsync(attempt);
        }

        public async Task<List<StudentExamHistoryItemDto>> GetMyAttemptHistoryAsync()
        {
            var user = await GetStudentUserAsync();
            var attempts = await (
                from attempt in _attemptRepository.GetAll()
                join exam in _examRepository.GetAll() on attempt.ExamId equals exam.Id
                where attempt.StudentUserId == user.Id && attempt.IsSubmitted
                orderby attempt.SubmittedAtUtc descending
                select new StudentExamHistoryItemDto
                {
                    AttemptId = attempt.Id,
                    ExamId = exam.Id,
                    ExamTitle = exam.Title,
                    StartedAtUtc = attempt.StartedAtUtc,
                    SubmittedAtUtc = attempt.SubmittedAtUtc!.Value,
                    DurationSeconds = attempt.DurationSeconds,
                    Score = attempt.Score ?? 0,
                    CorrectCount = attempt.CorrectCount ?? 0,
                    WrongCount = attempt.WrongCount ?? 0,
                    BlankCount = attempt.BlankCount ?? 0,
                    TotalQuestionCount = attempt.TotalQuestionCount,
                    AccuracyPercent = attempt.TotalQuestionCount == 0
                        ? 0
                        : Math.Round(
                            (decimal)(attempt.CorrectCount ?? 0) * 100 /
                            attempt.TotalQuestionCount,
                            2)
                }).ToListAsync();

            return attempts;
        }

        private async Task<ToeicReading.Authorization.Users.User> GetStudentUserAsync()
        {
            var user = await GetCurrentUserAsync();
            if (!await UserManager.IsInRoleAsync(user, StudentRoleName))
            {
                throw new AbpAuthorizationException("Chức năng này chỉ dành cho tài khoản Student.");
            }

            return user;
        }

        private async Task<StudentExamAttempt> GetOwnedAttemptAsync(int attemptId, long userId)
        {
            var attempt = await _attemptRepository.GetAll()
                .FirstOrDefaultAsync(item =>
                    item.Id == attemptId &&
                    item.StudentUserId == userId);
            if (attempt == null)
            {
                throw new UserFriendlyException("Không tìm thấy lượt thi của tài khoản hiện tại.");
            }

            return attempt;
        }

        private async Task CompleteAttemptAsync(StudentExamAttempt attempt)
        {
            var questions = await _questionRepository.GetAll()
                .Where(question => question.ExamId == attempt.ExamId)
                .ToListAsync();
            var questionIds = questions.Select(question => question.Id).ToList();
            var answers = await _answerRepository.GetAll()
                .Where(answer =>
                    answer.StudentExamAttemptId == attempt.Id &&
                    questionIds.Contains(answer.QuestionId) &&
                    answer.SelectedOptionId.HasValue)
                .ToListAsync();
            var selectedOptionIds = answers
                .Select(answer => answer.SelectedOptionId!.Value)
                .ToList();
            var correctOptionIds = await _optionRepository.GetAll()
                .Where(option =>
                    selectedOptionIds.Contains(option.Id) &&
                    option.IsCorrect)
                .Select(option => option.Id)
                .ToListAsync();

            var correctCount = answers.Count(answer =>
                answer.SelectedOptionId.HasValue &&
                correctOptionIds.Contains(answer.SelectedOptionId.Value));
            var answeredCount = answers.Count;
            var submittedAt = DateTimeOffset.UtcNow;
            var actualDuration = Math.Max(
                0,
                (int)Math.Min(
                    (submittedAt - attempt.StartedAtUtc).TotalSeconds,
                    (attempt.ExpiresAtUtc - attempt.StartedAtUtc).TotalSeconds));

            attempt.IsSubmitted = true;
            attempt.SubmittedAtUtc = submittedAt;
            attempt.DurationSeconds = actualDuration;
            attempt.Score = correctCount;
            attempt.CorrectCount = correctCount;
            attempt.WrongCount = answeredCount - correctCount;
            attempt.BlankCount = Math.Max(0, questions.Count - answeredCount);
            attempt.TotalQuestionCount = questions.Count;
            await _attemptRepository.UpdateAsync(attempt);
        }

        private async Task<StudentExamAttemptDto> BuildAttemptDtoAsync(StudentExamAttempt attempt)
        {
            var exam = await _examRepository.GetAsync(attempt.ExamId);
            var passages = await _passageRepository.GetAll()
                .Where(passage => passage.ExamId == exam.Id)
                .OrderBy(passage => passage.OrderNumber)
                .Select(passage => new StudentAttemptPassageDto
                {
                    Id = passage.Id,
                    PartNumber = passage.PartNumber,
                    Title = passage.Title,
                    Content = passage.Content,
                    OrderNumber = passage.OrderNumber
                })
                .ToListAsync();
            var questions = await _questionRepository.GetAll()
                .Where(question => question.ExamId == exam.Id)
                .OrderBy(question => question.PartNumber)
                .ThenBy(question => question.QuestionNumber)
                .Select(question => new StudentAttemptQuestionDto
                {
                    Id = question.Id,
                    ReadingPassageId = question.ReadingPassageId,
                    PartNumber = question.PartNumber,
                    QuestionNumber = question.QuestionNumber,
                    QuestionText = question.QuestionText
                })
                .ToListAsync();

            var questionIds = questions.Select(question => question.Id).ToList();
            var options = await _optionRepository.GetAll()
                .Where(option => questionIds.Contains(option.QuestionId))
                .OrderBy(option => option.OrderNumber)
                .Select(option => new
                {
                    option.Id,
                    option.QuestionId,
                    option.Label,
                    option.Content
                })
                .ToListAsync();
            foreach (var question in questions)
            {
                question.Options = options
                    .Where(option => option.QuestionId == question.Id)
                    .Select(option => new StudentAttemptOptionDto
                    {
                        Id = option.Id,
                        Label = option.Label,
                        Content = option.Content
                    })
                    .ToList();
            }

            var answers = await _answerRepository.GetAll()
                .Where(answer => answer.StudentExamAttemptId == attempt.Id)
                .Select(answer => new StudentAttemptAnswerDto
                {
                    QuestionId = answer.QuestionId,
                    SelectedOptionId = answer.SelectedOptionId
                })
                .ToListAsync();

            var result = attempt.IsSubmitted
                ? await BuildResultDtoAsync(attempt)
                : null;

            return new StudentExamAttemptDto
            {
                AttemptId = attempt.Id,
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                StartedAtUtc = attempt.StartedAtUtc,
                ExpiresAtUtc = attempt.ExpiresAtUtc,
                RemainingSeconds = (int)Math.Max(
                    0,
                    (attempt.ExpiresAtUtc - DateTimeOffset.UtcNow).TotalSeconds),
                IsSubmitted = attempt.IsSubmitted,
                Result = result,
                ReadingPassages = passages,
                Questions = questions,
                Answers = answers
            };
        }

        private async Task<StudentExamResultDto> BuildResultDtoAsync(StudentExamAttempt attempt)
        {
            var exam = await _examRepository.GetAsync(attempt.ExamId);
            var correctCount = attempt.CorrectCount ?? 0;
            var questionCount = attempt.TotalQuestionCount;

            return new StudentExamResultDto
            {
                AttemptId = attempt.Id,
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                StartedAtUtc = attempt.StartedAtUtc,
                SubmittedAtUtc = attempt.SubmittedAtUtc ?? DateTimeOffset.UtcNow,
                DurationSeconds = attempt.DurationSeconds,
                Score = attempt.Score ?? correctCount,
                CorrectCount = correctCount,
                WrongCount = attempt.WrongCount ?? 0,
                BlankCount = attempt.BlankCount ?? 0,
                TotalQuestionCount = questionCount,
                AccuracyPercent = questionCount == 0
                    ? 0
                    : Math.Round((decimal)correctCount * 100 / questionCount, 2)
            };
        }
    }
}
