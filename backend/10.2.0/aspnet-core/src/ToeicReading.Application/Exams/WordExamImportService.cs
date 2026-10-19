using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToeicReading.Authorization;
using ToeicReading.Exams.Dto;

namespace ToeicReading.Exams
{
    public interface IWordExamImportService
    {
        Task<List<AdminExamListItemDto>> GetAdminExamsAsync();

        Task<AdminExamDetailDto> GetAdminExamDetailAsync(int id);

        Task<bool> UpdateAdminExamAsync(int id, UpdateAdminExamInputDto input);

        Task<WordExamPreviewDto> ParseWordAsync(string fileName, byte[] content);

        Task<WordExamImportResultDto> ImportWordAsync(WordExamPreviewDto preview);
    }

    public class WordExamImportService : IWordExamImportService, ITransientDependency
    {
        private readonly IWordExamImportParser _parser;
        private readonly IRepository<Exam, int> _examRepository;
        private readonly IRepository<ReadingPassage, int> _passageRepository;
        private readonly IRepository<Question, int> _questionRepository;
        private readonly IRepository<QuestionOption, int> _optionRepository;
        private readonly IRepository<StudentExamAttempt, int> _attemptRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public WordExamImportService(
            IWordExamImportParser parser,
            IRepository<Exam, int> examRepository,
            IRepository<ReadingPassage, int> passageRepository,
            IRepository<Question, int> questionRepository,
            IRepository<QuestionOption, int> optionRepository,
            IRepository<StudentExamAttempt, int> attemptRepository,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _parser = parser;
            _examRepository = examRepository;
            _passageRepository = passageRepository;
            _questionRepository = questionRepository;
            _optionRepository = optionRepository;
            _attemptRepository = attemptRepository;
            _unitOfWorkManager = unitOfWorkManager;
        }

        [AbpAuthorize(PermissionNames.Pages_Exams_Import)]
        public async Task<List<AdminExamListItemDto>> GetAdminExamsAsync()
        {
            using (var unitOfWork = _unitOfWorkManager.Begin())
            {
                var exams = await _examRepository.GetAll()
                    .OrderByDescending(exam => exam.CreationTime)
                    .Select(exam => new AdminExamListItemDto
                    {
                        Id = exam.Id,
                        Title = exam.Title,
                        Description = exam.Description,
                        DurationMinutes = exam.DurationMinutes,
                        QuestionCount = _questionRepository.GetAll()
                            .Count(question => question.ExamId == exam.Id),
                        CreationTime = exam.CreationTime,
                        IsPublished = exam.IsPublished
                    })
                    .ToListAsync();

                await unitOfWork.CompleteAsync();
                return exams;
            }
        }

        [AbpAuthorize(PermissionNames.Pages_Exams_Import)]
        public async Task<AdminExamDetailDto> GetAdminExamDetailAsync(int id)
        {
            using (var unitOfWork = _unitOfWorkManager.Begin())
            {
                var exam = await _examRepository.GetAll()
                    .Where(item => item.Id == id)
                    .Select(item => new AdminExamDetailDto
                    {
                        Id = item.Id,
                        Title = item.Title,
                        Description = item.Description,
                        DurationMinutes = item.DurationMinutes,
                        CreationTime = item.CreationTime,
                        IsPublished = item.IsPublished
                    })
                    .FirstOrDefaultAsync();

                if (exam == null)
                {
                    await unitOfWork.CompleteAsync();
                    return null;
                }

                exam.ReadingPassages = await _passageRepository.GetAll()
                    .Where(passage => passage.ExamId == id)
                    .OrderBy(passage => passage.OrderNumber)
                    .Select(passage => new AdminReadingPassageDto
                    {
                        Id = passage.Id,
                        PartNumber = passage.PartNumber,
                        Title = passage.Title,
                        Content = passage.Content,
                        OrderNumber = passage.OrderNumber
                    })
                    .ToListAsync();

                var questions = await _questionRepository.GetAll()
                    .Where(question => question.ExamId == id)
                    .OrderBy(question => question.PartNumber)
                    .ThenBy(question => question.QuestionNumber)
                    .ToListAsync();
                var questionIds = questions.Select(question => question.Id).ToList();
                var options = await _optionRepository.GetAll()
                    .Where(option => questionIds.Contains(option.QuestionId))
                    .OrderBy(option => option.OrderNumber)
                    .ToListAsync();

                exam.Questions = questions.Select(question => new AdminQuestionDto
                {
                    Id = question.Id,
                    ReadingPassageId = question.ReadingPassageId,
                    PartNumber = question.PartNumber,
                    QuestionNumber = question.QuestionNumber,
                    QuestionText = question.QuestionText,
                    Hint = question.Hint,
                    Explanation = question.Explanation,
                    Options = options
                        .Where(option => option.QuestionId == question.Id)
                        .Select(option => new AdminQuestionOptionDto
                        {
                            Id = option.Id,
                            Label = option.Label,
                            Content = option.Content,
                            OrderNumber = option.OrderNumber,
                            IsCorrect = option.IsCorrect
                        })
                        .ToList()
                }).ToList();

                await unitOfWork.CompleteAsync();
                return exam;
            }
        }

        [AbpAuthorize(PermissionNames.Pages_Exams_Import)]
        public async Task<bool> UpdateAdminExamAsync(int id, UpdateAdminExamInputDto input)
        {
            if (id <= 0 || input == null)
            {
                throw new Abp.UI.UserFriendlyException("Thông tin đề thi không hợp lệ.");
            }

            var title = input.Title?.Trim();
            if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
            {
                throw new Abp.UI.UserFriendlyException("Tên đề thi phải có từ 1 đến 200 ký tự.");
            }
            if (input.DurationMinutes < 1 || input.DurationMinutes > 300)
            {
                throw new Abp.UI.UserFriendlyException("Thời lượng phải từ 1 đến 300 phút.");
            }
            if (input.Description is { Length: > 4000 })
            {
                throw new Abp.UI.UserFriendlyException("Mô tả đề thi không được vượt quá 4000 ký tự.");
            }

            using (var unitOfWork = _unitOfWorkManager.Begin(
                       new UnitOfWorkOptions { IsTransactional = true }))
            {
                var exam = await _examRepository.GetAll()
                    .FirstOrDefaultAsync(item => item.Id == id);
                if (exam == null)
                {
                    await unitOfWork.CompleteAsync();
                    return false;
                }

                if (exam.IsPublished)
                {
                    throw new Abp.UI.UserFriendlyException(
                        "Không thể chỉnh sửa đề đã xuất bản. Hãy tắt xuất bản trước khi sửa.");
                }

                if (await _attemptRepository.GetAll().AnyAsync(attempt => attempt.ExamId == id))
                {
                    throw new Abp.UI.UserFriendlyException(
                        "Không thể chỉnh sửa đề đã có lượt thi. Dữ liệu này được giữ nguyên để bảo toàn kết quả và lịch sử.");
                }

                if (input.ReadingPassages == null || input.Questions == null || input.Questions.Count == 0)
                {
                    throw new Abp.UI.UserFriendlyException("Đề thi phải có ít nhất một câu hỏi.");
                }

                var existingPassages = await _passageRepository.GetAll()
                    .Where(passage => passage.ExamId == id)
                    .ToListAsync();
                var existingQuestions = await _questionRepository.GetAll()
                    .Where(question => question.ExamId == id)
                    .ToListAsync();
                var existingQuestionIds = existingQuestions.Select(question => question.Id).ToList();
                var existingOptions = existingQuestionIds.Count == 0
                    ? new List<QuestionOption>()
                    : await _optionRepository.GetAll()
                        .Where(option => existingQuestionIds.Contains(option.QuestionId))
                        .ToListAsync();

                ValidateAdminExamUpdate(input, existingPassages, existingQuestions, existingOptions);

                exam.Title = title;
                exam.Description = string.IsNullOrWhiteSpace(input.Description)
                    ? null
                    : input.Description.Trim();
                exam.DurationMinutes = input.DurationMinutes;
                await _examRepository.UpdateAsync(exam);

                var passagesByClientKey = new Dictionary<string, ReadingPassage>(StringComparer.Ordinal);
                foreach (var passageInput in input.ReadingPassages)
                {
                    ReadingPassage passage;
                    if (passageInput.Id > 0)
                    {
                        passage = existingPassages.Single(item => item.Id == passageInput.Id);
                    }
                    else
                    {
                        passage = new ReadingPassage { ExamId = id };
                        await _passageRepository.InsertAsync(passage);
                    }

                    passage.PartNumber = passageInput.PartNumber;
                    passage.Title = passageInput.Title.Trim();
                    passage.Content = passageInput.Content.Trim();
                    passage.OrderNumber = passageInput.OrderNumber;
                    if (passageInput.Id <= 0)
                    {
                        passagesByClientKey.Add(passageInput.ClientKey!.Trim(), passage);
                    }
                }
                await _unitOfWorkManager.Current.SaveChangesAsync();

                var passageIdsInRequest = input.ReadingPassages
                    .Where(passage => passage.Id > 0)
                    .Select(passage => passage.Id)
                    .ToHashSet();
                var passagesToDelete = existingPassages
                    .Where(passage => !passageIdsInRequest.Contains(passage.Id))
                    .ToList();

                var questionIdsInRequest = input.Questions
                    .Where(question => question.Id > 0)
                    .Select(question => question.Id)
                    .ToHashSet();
                var questionsToDelete = existingQuestions
                    .Where(question => !questionIdsInRequest.Contains(question.Id))
                    .ToList();
                foreach (var question in questionsToDelete)
                {
                    foreach (var option in existingOptions.Where(option => option.QuestionId == question.Id))
                    {
                        await _optionRepository.DeleteAsync(option);
                    }
                    await _questionRepository.DeleteAsync(question);
                }

                var savedQuestions = new Dictionary<UpdateAdminQuestionInputDto, Question>();
                foreach (var questionInput in input.Questions)
                {
                    var question = questionInput.Id > 0
                        ? existingQuestions.Single(item => item.Id == questionInput.Id)
                        : new Question { ExamId = id };

                    question.PartNumber = questionInput.PartNumber;
                    question.QuestionNumber = questionInput.QuestionNumber;
                    question.QuestionText = questionInput.QuestionText.Trim();
                    question.Hint = string.IsNullOrWhiteSpace(questionInput.Hint)
                        ? null
                        : questionInput.Hint.Trim();
                    question.Explanation = string.IsNullOrWhiteSpace(questionInput.Explanation)
                        ? null
                        : questionInput.Explanation.Trim();
                    question.ReadingPassageId = questionInput.ReadingPassageId;
                    if (!string.IsNullOrWhiteSpace(questionInput.ReadingPassageClientKey))
                    {
                        question.ReadingPassageId =
                            passagesByClientKey[questionInput.ReadingPassageClientKey.Trim()].Id;
                    }

                    if (questionInput.Id <= 0)
                    {
                        await _questionRepository.InsertAsync(question);
                    }
                    else
                    {
                        await _questionRepository.UpdateAsync(question);
                    }
                    savedQuestions.Add(questionInput, question);
                }
                await _unitOfWorkManager.Current.SaveChangesAsync();

                foreach (var pair in savedQuestions)
                {
                    var question = pair.Value;
                    var optionIdsInRequest = pair.Key.Options
                        .Where(option => option.Id > 0)
                        .Select(option => option.Id)
                        .ToHashSet();
                    foreach (var option in existingOptions.Where(option =>
                                 option.QuestionId == question.Id &&
                                 !optionIdsInRequest.Contains(option.Id)))
                    {
                        await _optionRepository.DeleteAsync(option);
                    }

                    foreach (var optionInput in pair.Key.Options)
                    {
                        var option = optionInput.Id > 0
                            ? existingOptions.Single(item => item.Id == optionInput.Id)
                            : new QuestionOption { QuestionId = question.Id };
                        option.Label = optionInput.Label.Trim().ToUpperInvariant();
                        option.Content = optionInput.Content.Trim();
                        option.OrderNumber = optionInput.OrderNumber;
                        option.IsCorrect = optionInput.IsCorrect;
                        if (optionInput.Id <= 0)
                        {
                            await _optionRepository.InsertAsync(option);
                        }
                        else
                        {
                            await _optionRepository.UpdateAsync(option);
                        }
                    }
                }

                var retainedPassageIds = input.ReadingPassages
                    .Where(passage => passage.Id > 0)
                    .Select(passage => passage.Id)
                    .Concat(passagesByClientKey.Values.Select(passage => passage.Id))
                    .ToHashSet();
                foreach (var passage in passagesToDelete.Where(item => !retainedPassageIds.Contains(item.Id)))
                {
                    await _passageRepository.DeleteAsync(passage);
                }

                await unitOfWork.CompleteAsync();
                return true;
            }
        }

        private static void ValidateAdminExamUpdate(
            UpdateAdminExamInputDto input,
            IReadOnlyCollection<ReadingPassage> existingPassages,
            IReadOnlyCollection<Question> existingQuestions,
            IReadOnlyCollection<QuestionOption> existingOptions)
        {
            var passagesById = existingPassages.ToDictionary(passage => passage.Id);
            var passageIds = new HashSet<int>();
            var passageKeys = new HashSet<string>(StringComparer.Ordinal);
            var passageOrders = new HashSet<string>(StringComparer.Ordinal);
            foreach (var passage in input.ReadingPassages)
            {
                if (passage.PartNumber is not (6 or 7) ||
                    string.IsNullOrWhiteSpace(passage.Title) ||
                    passage.Title.Trim().Length > 200 ||
                    string.IsNullOrWhiteSpace(passage.Content) ||
                    passage.OrderNumber < 1)
                {
                    throw new Abp.UI.UserFriendlyException("Mỗi bài đọc phải có Part 6/7, tiêu đề, nội dung và thứ tự hợp lệ.");
                }
                if (!passageOrders.Add($"{passage.PartNumber}:{passage.OrderNumber}"))
                {
                    throw new Abp.UI.UserFriendlyException(
                        $"Thứ tự bài đọc Part {passage.PartNumber} không được trùng.");
                }

                if (passage.Id > 0)
                {
                    if (!passagesById.ContainsKey(passage.Id) || !passageIds.Add(passage.Id))
                    {
                        throw new Abp.UI.UserFriendlyException("Bài đọc không thuộc đề thi này hoặc bị lặp.");
                    }
                }
                else if (string.IsNullOrWhiteSpace(passage.ClientKey) ||
                         !passageKeys.Add(passage.ClientKey.Trim()))
                {
                    throw new Abp.UI.UserFriendlyException("Mã tạm của bài đọc mới không hợp lệ hoặc bị lặp.");
                }
            }

            var questionsById = existingQuestions.ToDictionary(question => question.Id);
            var seenQuestionIds = new HashSet<int>();
            var seenNumbers = new HashSet<int>();
            var optionIds = existingOptions.ToDictionary(option => option.Id);
            foreach (var question in input.Questions)
            {
                if (question.PartNumber is not (5 or 6 or 7) ||
                    question.QuestionNumber < 1 ||
                    !seenNumbers.Add(question.QuestionNumber))
                {
                    throw new Abp.UI.UserFriendlyException("Part và số thứ tự câu hỏi phải hợp lệ, không được trùng.");
                }
                if (question.Id > 0 &&
                    (!questionsById.ContainsKey(question.Id) || !seenQuestionIds.Add(question.Id)))
                {
                    throw new Abp.UI.UserFriendlyException("Câu hỏi không thuộc đề thi này hoặc bị lặp.");
                }

                UpdateAdminReadingPassageInputDto? passageInput = null;
                if (question.PartNumber == 5)
                {
                    if (question.ReadingPassageId.HasValue ||
                        !string.IsNullOrWhiteSpace(question.ReadingPassageClientKey))
                    {
                        throw new Abp.UI.UserFriendlyException($"Câu {question.QuestionNumber}: Part 5 không được gắn bài đọc.");
                    }
                }
                else
                {
                    if (question.ReadingPassageId.HasValue)
                    {
                        passageInput = input.ReadingPassages.FirstOrDefault(item =>
                            item.Id == question.ReadingPassageId.Value && item.Id > 0);
                    }
                    else if (!string.IsNullOrWhiteSpace(question.ReadingPassageClientKey))
                    {
                        var clientKey = question.ReadingPassageClientKey.Trim();
                        passageInput = input.ReadingPassages.FirstOrDefault(item =>
                            item.Id <= 0 && item.ClientKey == clientKey);
                    }

                    if (passageInput == null ||
                        (passageInput.Id > 0 && !passagesById.ContainsKey(passageInput.Id)) ||
                        passageInput.PartNumber != question.PartNumber)
                    {
                        throw new Abp.UI.UserFriendlyException(
                            $"Câu {question.QuestionNumber}: hãy chọn bài đọc thuộc đúng đề và Part.");
                    }
                }

                if (question.Options == null || question.Options.Count != 4)
                {
                    throw new Abp.UI.UserFriendlyException($"Câu {question.QuestionNumber}: cần đủ 4 phương án A, B, C, D.");
                }

                var labels = new HashSet<string>(StringComparer.Ordinal);
                var seenOptionIds = new HashSet<int>();
                foreach (var option in question.Options)
                {
                    var label = option.Label?.Trim().ToUpperInvariant();
                    if (label is not ("A" or "B" or "C" or "D") ||
                        !labels.Add(label) ||
                        string.IsNullOrWhiteSpace(option.Content))
                    {
                        throw new Abp.UI.UserFriendlyException(
                            $"Câu {question.QuestionNumber}: phương án A/B/C/D phải đủ, không trùng và có nội dung.");
                    }
                    if (option.Id > 0 &&
                        (!optionIds.TryGetValue(option.Id, out var existingOption) ||
                         existingOption.QuestionId != question.Id ||
                         !seenOptionIds.Add(option.Id)))
                    {
                        throw new Abp.UI.UserFriendlyException(
                            $"Câu {question.QuestionNumber}: phương án không thuộc câu hỏi này hoặc bị lặp.");
                    }
                }
                if (!new[] { "A", "B", "C", "D" }.All(labels.Contains) ||
                    question.Options.Count(option => option.IsCorrect) != 1)
                {
                    throw new Abp.UI.UserFriendlyException(
                        $"Câu {question.QuestionNumber}: cần đủ phương án A/B/C/D và chính xác một đáp án đúng.");
                }

                if (question.PartNumber is 5 or 7 && string.IsNullOrWhiteSpace(question.QuestionText))
                {
                    throw new Abp.UI.UserFriendlyException($"Câu {question.QuestionNumber}: thiếu nội dung câu hỏi.");
                }
                if (question.PartNumber == 6 &&
                    string.IsNullOrWhiteSpace(question.QuestionText) &&
                    !System.Text.RegularExpressions.Regex.IsMatch(
                        passageInput?.Content ?? string.Empty,
                        $@"\[\s*{question.QuestionNumber}\s*\]",
                        System.Text.RegularExpressions.RegexOptions.CultureInvariant))
                {
                    throw new Abp.UI.UserFriendlyException(
                        $"Câu {question.QuestionNumber}: cần nội dung câu hỏi hoặc chỗ trống [{question.QuestionNumber}] trong bài đọc.");
                }
            }
        }

        public Task<WordExamPreviewDto> ParseWordAsync(string fileName, byte[] content)
        {
            return Task.FromResult(_parser.Parse(fileName, content));
        }

        public async Task<WordExamImportResultDto> ImportWordAsync(WordExamPreviewDto preview)
        {
            var errors = _parser.Validate(preview);
            if (errors.Count > 0)
            {
                throw new WordExamValidationException(errors);
            }

            var title = preview.Title.Trim();
            using (var unitOfWork = _unitOfWorkManager.Begin())
            {
                var duplicateTitle = await _examRepository.GetAll()
                    .AnyAsync(exam => exam.Title == title);
                if (duplicateTitle)
                {
                    throw new WordExamDuplicateException(
                        $"Đã có đề thi tên \"{title}\". Hãy kiểm tra danh sách đề thi hoặc đổi tiêu đề trước khi nhập.");
                }

                var exam = new Exam
                {
                    Title = title,
                    Description = string.IsNullOrWhiteSpace(preview.Description)
                        ? null
                        : preview.Description.Trim(),
                    DurationMinutes = preview.DurationMinutes,
                    IsPublished = false
                };

                await _examRepository.InsertAsync(exam);
                await _unitOfWorkManager.Current.SaveChangesAsync();

                var passagesByTempId = new Dictionary<string, ReadingPassage>(StringComparer.Ordinal);
                foreach (var passageDto in preview.ReadingPassages.OrderBy(passage => passage.OrderNumber))
                {
                    var passage = new ReadingPassage
                    {
                        ExamId = exam.Id,
                        PartNumber = passageDto.PartNumber,
                        Title = passageDto.Title.Trim(),
                        Content = passageDto.Content.Trim(),
                        OrderNumber = passageDto.OrderNumber
                    };
                    await _passageRepository.InsertAsync(passage);
                    passagesByTempId.Add(passageDto.TempId, passage);
                }
                await _unitOfWorkManager.Current.SaveChangesAsync();

                var questionDtos = preview.Questions
                    .OrderBy(question => question.PartNumber)
                    .ThenBy(question => question.QuestionNumber)
                    .ToList();
                var questions = new List<Question>();
                foreach (var questionDto in questionDtos)
                {
                    var passageId = questionDto.PartNumber == 5
                        ? (int?)null
                        : passagesByTempId[questionDto.ReadingPassageTempId].Id;
                    var question = new Question
                    {
                        ExamId = exam.Id,
                        ReadingPassageId = passageId,
                        PartNumber = questionDto.PartNumber,
                        QuestionNumber = questionDto.QuestionNumber,
                        QuestionText = questionDto.QuestionText.Trim(),
                        Hint = string.IsNullOrWhiteSpace(questionDto.Hint) ? null : questionDto.Hint.Trim(),
                        Explanation = string.IsNullOrWhiteSpace(questionDto.Explanation) ? null : questionDto.Explanation.Trim(),
                        ShuffleOptions = false
                    };
                    await _questionRepository.InsertAsync(question);
                    questions.Add(question);
                }
                await _unitOfWorkManager.Current.SaveChangesAsync();

                foreach (var pair in questions.Zip(
                             questionDtos,
                             (question, questionDto) => new { question, questionDto }))
                {
                    foreach (var optionDto in pair.questionDto.Options.OrderBy(option => option.Label, StringComparer.Ordinal))
                    {
                        var label = optionDto.Label.Trim().ToUpperInvariant();
                        await _optionRepository.InsertAsync(new QuestionOption
                        {
                            QuestionId = pair.question.Id,
                            Label = label,
                            Content = optionDto.Content.Trim(),
                            IsCorrect = label == pair.questionDto.Answer.Trim().ToUpperInvariant(),
                            OrderNumber = label[0] - 'A' + 1
                        });
                    }
                }
                await _unitOfWorkManager.Current.SaveChangesAsync();

                await unitOfWork.CompleteAsync();

                return new WordExamImportResultDto
                {
                    ExamId = exam.Id,
                    PassageCount = preview.ReadingPassages.Count,
                    QuestionCount = preview.Questions.Count,
                    Message = "Đã nhập đề thi thành công ở trạng thái bản nháp."
                };
            }
        }
    }

    public class WordExamValidationException : Exception
    {
        public WordExamValidationException(IReadOnlyList<WordImportIssueDto> errors)
            : base("Dữ liệu đề thi chưa hợp lệ, vui lòng kiểm tra các lỗi.")
        {
            Errors = errors;
        }

        public IReadOnlyList<WordImportIssueDto> Errors { get; }
    }

    public class WordExamDuplicateException : Exception
    {
        public WordExamDuplicateException(string message)
            : base(message)
        {
        }
    }
}
