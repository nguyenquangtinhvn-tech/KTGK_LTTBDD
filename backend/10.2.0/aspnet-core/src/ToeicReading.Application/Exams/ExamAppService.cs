
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToeicReading.Authorization;
using ToeicReading.Exams.Dto;

namespace ToeicReading.Exams
{
    public class ExamAppService : ToeicReadingAppServiceBase, IExamAppService
    {
        private readonly IRepository<Exam, int> _examRepository;
        private readonly IRepository<ReadingPassage, int> _passageRepository;
        private readonly IRepository<Question, int> _questionRepository;
        private readonly IRepository<QuestionOption, int> _optionRepository;
        private readonly IRepository<StudentExamAttempt, int> _attemptRepository;

        public ExamAppService(
            IRepository<Exam, int> examRepository,
            IRepository<ReadingPassage, int> passageRepository,
            IRepository<Question, int> questionRepository,
            IRepository<QuestionOption, int> optionRepository,
            IRepository<StudentExamAttempt, int> attemptRepository)
        {
            _examRepository = examRepository;
            _passageRepository = passageRepository;
            _questionRepository = questionRepository;
            _optionRepository = optionRepository;
            _attemptRepository = attemptRepository;
        }

        // =========================================================
        // 1. LẤY DANH SÁCH ĐỀ ĐÃ CÔNG BỐ
        // API: GET /api/services/app/Exam/GetPublishedExams
        // =========================================================
        public async Task<List<ExamListDto>> GetPublishedExamsAsync()
        {
            var exams = await _examRepository.GetAll()
                .Where(x => x.IsPublished)
                .OrderByDescending(x => x.CreationTime)
                .ToListAsync();

            return exams.Select(x => new ExamListDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                DurationMinutes = x.DurationMinutes,
                QuestionCount = _questionRepository.GetAll()
                    .Count(question => question.ExamId == x.Id)
            }).ToList();
        }

        // =========================================================
        // 2. LẤY CHI TIẾT ĐỀ THI
        // API: GET /api/services/app/Exam/GetExamDetail?id=1
        // =========================================================
        public async Task<ExamDetailDto> GetExamDetailAsync(int id)
        {
            var exam = await _examRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsPublished);

            if (exam == null)
            {
                throw new UserFriendlyException(
                    "Không tìm thấy đề thi hoặc đề thi chưa được công bố.");
            }

            var passages = await _passageRepository.GetAll()
                .Where(x => x.ExamId == id)
                .OrderBy(x => x.OrderNumber)
                .ToListAsync();

            var questions = await _questionRepository.GetAll()
                .Where(x => x.ExamId == id)
                .OrderBy(x => x.PartNumber)
                .ThenBy(x => x.QuestionNumber)
                .ToListAsync();

            var questionIds = questions.Select(x => x.Id).ToList();

            var options = await _optionRepository.GetAll()
                .Where(x => questionIds.Contains(x.QuestionId))
                .OrderBy(x => x.OrderNumber)
                .ToListAsync();

            return new ExamDetailDto
            {
                Id = exam.Id,
                Title = exam.Title,
                Description = exam.Description,
                DurationMinutes = exam.DurationMinutes,

                ReadingPassages = passages.Select(x => new ReadingPassageDto
                {
                    Id = x.Id,
                    PartNumber = x.PartNumber,
                    Title = x.Title,
                    Content = x.Content,
                    ImageUrl = x.ImageUrl,
                    OrderNumber = x.OrderNumber
                }).ToList(),

                Questions = questions.Select(x => new QuestionDto
                {
                    Id = x.Id,
                    ReadingPassageId = x.ReadingPassageId,
                    PartNumber = x.PartNumber,
                    QuestionNumber = x.QuestionNumber,
                    QuestionText = x.QuestionText,
                    Hint = x.Hint,

                    Options = options
                        .Where(o => o.QuestionId == x.Id)
                        .Select(o => new QuestionOptionDto
                        {
                            Id = o.Id,
                            Label = o.Label,
                            Content = o.Content,
                            OrderNumber = o.OrderNumber
                        }).ToList()
                }).ToList()
            };
        }

        
        // =========================================================
        // 4. CẬP NHẬT THÔNG TIN ĐỀ THI
        // API: PUT /api/services/app/Exam/UpdateExam?id=1
        // =========================================================
        [AbpAuthorize(PermissionNames.Pages_Exams_Import)]
        public async Task UpdateExamAsync(
            int id,
            UpdateExamInputDto input)
        {
            if (input == null)
            {
                throw new UserFriendlyException(
                    "Dữ liệu cập nhật không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(input.Title))
            {
                throw new UserFriendlyException(
                    "Tên đề thi không được để trống.");
            }

            if (input.Title.Trim().Length > 200)
            {
                throw new UserFriendlyException(
                    "Tên đề thi không được vượt quá 200 ký tự.");
            }

            if (input.DurationMinutes < 1 ||
                input.DurationMinutes > 300)
            {
                throw new UserFriendlyException(
                    "Thời gian làm bài phải từ 1 đến 300 phút.");
            }

            // Tìm đề thi, kể cả đề chưa công bố
            var exam = await _examRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (exam == null)
            {
                throw new UserFriendlyException(
                    "Không tìm thấy đề thi cần cập nhật.");
            }

            var statusChanged = input.IsPublished != exam.IsPublished;
            if (!exam.IsPublished && input.IsPublished)
            {
                await ValidateExamCanBePublishedAsync(id);
            }
            else if (exam.IsPublished && !input.IsPublished)
            {
                await EnsureNoActiveAttemptsAsync(id);
            }

            var metadataChanged =
                !string.Equals(exam.Title, input.Title.Trim(), StringComparison.Ordinal) ||
                !string.Equals(exam.Description, input.Description?.Trim(), StringComparison.Ordinal) ||
                exam.DurationMinutes != input.DurationMinutes;
            if (metadataChanged && (exam.IsPublished || statusChanged))
            {
                throw new UserFriendlyException(
                    "Không thể chỉnh sửa nội dung cùng lúc với thay đổi xuất bản. Hãy lưu trạng thái trước, sau đó chỉnh sửa đề ở trạng thái bản nháp.");
            }
            if (metadataChanged && await _attemptRepository.GetAll()
                    .AnyAsync(attempt => attempt.ExamId == id))
            {
                throw new UserFriendlyException(
                    "Không thể chỉnh sửa đề đã có lượt thi để bảo toàn kết quả và lịch sử.");
            }

            // Cập nhật thông tin
            exam.Title = input.Title.Trim();
            exam.Description = input.Description?.Trim();
            exam.DurationMinutes = input.DurationMinutes;
            exam.IsPublished = input.IsPublished;

            await _examRepository.UpdateAsync(exam);
            await CurrentUnitOfWork.SaveChangesAsync();
        }

        [AbpAuthorize(PermissionNames.Pages_Exams_Import)]
        public async Task<bool> SetPublicationStatusAsync(SetPublicationStatusInputDto input)
        {
            if (input == null || input.Id <= 0)
            {
                throw new UserFriendlyException("Mã đề thi không hợp lệ.");
            }

            var exam = await _examRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == input.Id);

            if (exam == null)
            {
                throw new UserFriendlyException("Không tìm thấy đề thi cần cập nhật.");
            }

            if (exam.IsPublished == input.IsPublished)
            {
                return exam.IsPublished;
            }

            if (input.IsPublished)
            {
                await ValidateExamCanBePublishedAsync(input.Id);
            }
            else
            {
                await EnsureNoActiveAttemptsAsync(input.Id);
            }

            exam.IsPublished = input.IsPublished;
            await _examRepository.UpdateAsync(exam);
            await CurrentUnitOfWork.SaveChangesAsync();

            return exam.IsPublished;
        }

        private async Task EnsureNoActiveAttemptsAsync(int examId)
        {
            var hasActiveAttempts = await _attemptRepository.GetAll()
                .AnyAsync(attempt =>
                    attempt.ExamId == examId &&
                    !attempt.IsSubmitted);
            if (hasActiveAttempts)
            {
                throw new UserFriendlyException(
                    "Không thể hủy xuất bản khi sinh viên vẫn đang làm bài thi.");
            }
        }

        private async Task ValidateExamCanBePublishedAsync(int examId)
        {
            var questions = await _questionRepository.GetAll()
                .Where(question => question.ExamId == examId)
                .ToListAsync();

            if (questions.Count == 0)
            {
                throw new UserFriendlyException(
                    "Chưa thể kích hoạt: đề thi phải có ít nhất một câu hỏi.");
            }

            var questionIds = questions.Select(question => question.Id).ToList();
            var options = await _optionRepository.GetAll()
                .Where(option => questionIds.Contains(option.QuestionId))
                .ToListAsync();
            var passages = await _passageRepository.GetAll()
                .Where(passage => passage.ExamId == examId)
                .ToListAsync();

            foreach (var question in questions)
            {
                if (question.PartNumber != 5 &&
                    question.PartNumber != 6 &&
                    question.PartNumber != 7)
                {
                    throw new UserFriendlyException(
                        $"Chưa thể kích hoạt: câu {question.QuestionNumber} có PartNumber không hợp lệ.");
                }

                ReadingPassage? questionPassage = null;
                if (question.PartNumber is 6 or 7)
                {
                    questionPassage = passages.FirstOrDefault(passage =>
                        passage.Id == question.ReadingPassageId &&
                        passage.PartNumber == question.PartNumber &&
                        !string.IsNullOrWhiteSpace(passage.Content));

                    if (questionPassage == null)
                    {
                        throw new UserFriendlyException(
                            $"Chưa thể kích hoạt: câu {question.QuestionNumber} thiếu bài đọc hợp lệ (ReadingPassageId/Content).");
                    }
                }

                if ((question.PartNumber == 5 || question.PartNumber == 7) &&
                    string.IsNullOrWhiteSpace(question.QuestionText))
                {
                    throw new UserFriendlyException(
                        $"Chưa thể kích hoạt: câu {question.QuestionNumber} thiếu nội dung câu hỏi (QuestionText).");
                }

                if (question.PartNumber == 6 &&
                    string.IsNullOrWhiteSpace(question.QuestionText) &&
                    !System.Text.RegularExpressions.Regex.IsMatch(
                        questionPassage!.Content,
                        $@"\[\s*{question.QuestionNumber}\s*\]",
                        System.Text.RegularExpressions.RegexOptions.CultureInvariant))
                {
                    throw new UserFriendlyException(
                        $"Chưa thể kích hoạt: câu {question.QuestionNumber} thiếu nội dung câu hỏi (QuestionText) hoặc chỗ trống [{question.QuestionNumber}] trong bài đọc (Content).");
                }

                var questionOptions = options
                    .Where(option => option.QuestionId == question.Id)
                    .ToList();
                var labels = questionOptions
                    .Select(option => option.Label?.Trim().ToUpperInvariant())
                    .ToList();

                if (questionOptions.Count != 4 ||
                    labels.Distinct(StringComparer.Ordinal).Count() != 4 ||
                    !new[] { "A", "B", "C", "D" }.All(labels.Contains))
                {
                    throw new UserFriendlyException(
                        $"Chưa thể kích hoạt: câu {question.QuestionNumber} phải có đủ phương án A, B, C, D và không trùng nhãn.");
                }

                if (questionOptions.Any(option => string.IsNullOrWhiteSpace(option.Content)))
                {
                    var missingOption = questionOptions.First(option =>
                        string.IsNullOrWhiteSpace(option.Content));
                    throw new UserFriendlyException(
                        $"Chưa thể kích hoạt: câu {question.QuestionNumber}, phương án {missingOption.Label} thiếu nội dung (Content).");
                }

                if (questionOptions.Count(option => option.IsCorrect) != 1)
                {
                    throw new UserFriendlyException(
                        $"Chưa thể kích hoạt: câu {question.QuestionNumber} phải có chính xác một đáp án đúng (IsCorrect).");
                }
            }
        }


        // =========================================================
        // 3. TẠO CÂU HỎI VÀ 4 LỰA CHỌN
        // API: POST /api/services/app/Exam/CreateQuestion
        // =========================================================
        [AbpAuthorize(PermissionNames.Pages_Exams_Import)]
        public async Task<int> CreateQuestionAsync(
            CreateQuestionInputDto input)
        {
            if (input == null)
            {
                throw new UserFriendlyException(
                    "Dữ liệu câu hỏi không hợp lệ.");
            }

            // Kiểm tra đề thi tồn tại
            var exam = await _examRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == input.ExamId);

            if (exam == null)
            {
                throw new UserFriendlyException(
                    "Không tìm thấy đề thi.");
            }

            // Kiểm tra Part
            if (input.PartNumber != 5 &&
                input.PartNumber != 6 &&
                input.PartNumber != 7)
            {
                throw new UserFriendlyException(
                    "Phần thi chỉ được là Part 5, Part 6 hoặc Part 7.");
            }

            // Kiểm tra nội dung
            if (string.IsNullOrWhiteSpace(input.QuestionText))
            {
                throw new UserFriendlyException(
                    "Vui lòng nhập nội dung câu hỏi.");
            }

            // Kiểm tra đủ 4 lựa chọn
            if (input.Options == null ||
                input.Options.Count != 4)
            {
                throw new UserFriendlyException(
                    "Mỗi câu hỏi phải có đúng 4 lựa chọn A, B, C, D.");
            }

            // Kiểm tra nội dung lựa chọn
            if (input.Options.Any(x =>
                string.IsNullOrWhiteSpace(x.Label) ||
                string.IsNullOrWhiteSpace(x.Content)))
            {
                throw new UserFriendlyException(
                    "Nhãn và nội dung của các lựa chọn không được để trống.");
            }

            // Chuẩn hóa nhãn trước khi kiểm tra
            var normalizedOptions = input.Options
                .Select(x => new
                {
                    Label = x.Label.Trim().ToUpperInvariant(),
                    Content = x.Content.Trim(),
                    x.IsCorrect,
                    x.OrderNumber
                })
                .ToList();

            var validLabels = new[] { "A", "B", "C", "D" };

            if (normalizedOptions.Any(x =>
                !validLabels.Contains(x.Label)))
            {
                throw new UserFriendlyException(
                    "Nhãn lựa chọn chỉ được là A, B, C hoặc D.");
            }

            if (normalizedOptions
                .Select(x => x.Label)
                .Distinct()
                .Count() != 4)
            {
                throw new UserFriendlyException(
                    "Các nhãn A, B, C, D không được trùng nhau.");
            }

            // Phải có chính xác một đáp án đúng
            if (normalizedOptions.Count(x => x.IsCorrect) != 1)
            {
                throw new UserFriendlyException(
                    "Vui lòng chọn chính xác một đáp án đúng.");
            }

            // Part 5 không gắn bài đọc.
            // Part 6 và Part 7 bắt buộc gắn bài đọc cùng đề và cùng Part.
            int? passageId = null;

            if (input.PartNumber == 5)
            {
                if (input.ReadingPassageId.HasValue)
                {
                    throw new UserFriendlyException(
                        "Part 5 không cần chọn bài đọc.");
                }
            }
            else
            {
                if (!input.ReadingPassageId.HasValue)
                {
                    throw new UserFriendlyException(
                        "Part 6 và Part 7 phải chọn bài đọc.");
                }

                var passage = await _passageRepository.GetAll()
                    .FirstOrDefaultAsync(x =>
                        x.Id == input.ReadingPassageId.Value &&
                        x.ExamId == input.ExamId &&
                        x.PartNumber == input.PartNumber);

                if (passage == null)
                {
                    throw new UserFriendlyException(
                        "Bài đọc không tồn tại hoặc không thuộc đề thi/phần thi đã chọn.");
                }

                passageId = passage.Id;
            }

            // Không cho trùng số câu trong cùng đề
            var questionExists = await _questionRepository.GetAll()
                .AnyAsync(x =>
                    x.ExamId == input.ExamId &&
                    x.QuestionNumber == input.QuestionNumber);

            if (questionExists)
            {
                throw new UserFriendlyException(
                    "Số câu hỏi này đã tồn tại trong đề thi.");
            }

            // Tạo câu hỏi
            var question = new Question
            {
                ExamId = input.ExamId,
                ReadingPassageId = passageId,
                PartNumber = input.PartNumber,
                QuestionNumber = input.QuestionNumber,
                QuestionText = input.QuestionText.Trim(),
                Hint = input.Hint,
                Explanation = input.Explanation,
                ShuffleOptions = input.ShuffleOptions
            };

            await _questionRepository.InsertAsync(question);

            // Lưu câu hỏi để lấy ID
            await CurrentUnitOfWork.SaveChangesAsync();

            // Lưu các lựa chọn, giữ nguyên đáp án đúng người dùng đã chọn
            foreach (var option in normalizedOptions.OrderBy(x => x.OrderNumber))
            {
                var optionEntity = new QuestionOption
                {
                    QuestionId = question.Id,
                    Label = option.Label,
                    Content = option.Content,
                    IsCorrect = option.IsCorrect,
                    OrderNumber = option.OrderNumber
                };

                await _optionRepository.InsertAsync(optionEntity);
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            return question.Id;
        }
    }
}
