using Abp.UI;
using Abp.Authorization;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using ToeicReading.EntityFrameworkCore;
using ToeicReading.Exams;
using ToeicReading.Exams.Dto;
using Xunit;

namespace ToeicReading.Tests.Exams;

public class AdminExamEditing_Tests : ToeicReadingTestBase
{
    private readonly IWordExamImportService _importService;

    public AdminExamEditing_Tests()
    {
        _importService = Resolve<IWordExamImportService>();
    }

    [Fact]
    public async Task Should_Update_Exam_Passage_Question_And_Options_Without_Duplicating_Rows()
    {
        LoginAsHostAdmin();
        var ids = CreateExam();

        var updated = await _importService.UpdateAdminExamAsync(ids.ExamId, BuildUpdate(ids));

        updated.ShouldBeTrue();
        UsingDbContext(context =>
        {
            context.Exams.Single(item => item.Id == ids.ExamId).Title.ShouldBe("Updated title");
            context.ReadingPassages.Single(item => item.Id == ids.PassageId).Content.ShouldBe("Updated passage");
            var question = context.Questions.Single(item => item.Id == ids.QuestionId);
            question.QuestionText.ShouldBe("Updated question?");
            context.QuestionOptions.Count(option => option.QuestionId == ids.QuestionId).ShouldBe(4);
            context.QuestionOptions.Single(option =>
                option.QuestionId == ids.QuestionId && option.IsCorrect).Label.ShouldBe("B");
        });
    }

    [Fact]
    public async Task Should_Reject_Editing_Published_Exam()
    {
        LoginAsHostAdmin();
        var ids = CreateExam(isPublished: true);

        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _importService.UpdateAdminExamAsync(ids.ExamId, BuildUpdate(ids)));

        exception.Message.ShouldContain("tắt xuất bản");
    }

    [Fact]
    public async Task Should_Reject_Editing_Exam_With_Attempt_History()
    {
        LoginAsHostAdmin();
        var ids = CreateExam(withAttempt: true);

        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _importService.UpdateAdminExamAsync(ids.ExamId, BuildUpdate(ids)));

        exception.Message.ShouldContain("đã có lượt thi");
    }

    [Fact]
    public async Task Should_Require_Admin_Permission_To_Edit_Exam()
    {
        LoginAsHostAdmin();
        var ids = CreateExam();
        AbpSession.UserId = null;

        await Should.ThrowAsync<AbpAuthorizationException>(
            () => _importService.UpdateAdminExamAsync(ids.ExamId, BuildUpdate(ids)));
    }

    [Fact]
    public async Task Should_Add_Question_And_New_Passage_Using_Client_Link()
    {
        LoginAsHostAdmin();
        var ids = CreateExam();
        var update = BuildUpdate(ids);
        update.ReadingPassages.Add(new UpdateAdminReadingPassageInputDto
        {
            ClientKey = "new-passage",
            PartNumber = 6,
            Title = "Part 6 passage",
            Content = "Please read the note. [2]",
            OrderNumber = 1
        });
        update.Questions.Add(new UpdateAdminQuestionInputDto
        {
            PartNumber = 6,
            QuestionNumber = 2,
            ReadingPassageClientKey = "new-passage",
            Options =
            {
                new UpdateAdminQuestionOptionInputDto { Label = "A", Content = "Option A", OrderNumber = 1, IsCorrect = true },
                new UpdateAdminQuestionOptionInputDto { Label = "B", Content = "Option B", OrderNumber = 2 },
                new UpdateAdminQuestionOptionInputDto { Label = "C", Content = "Option C", OrderNumber = 3 },
                new UpdateAdminQuestionOptionInputDto { Label = "D", Content = "Option D", OrderNumber = 4 }
            }
        });

        await _importService.UpdateAdminExamAsync(ids.ExamId, update);

        UsingDbContext(context =>
        {
            var addedQuestion = context.Questions.Single(question =>
                question.ExamId == ids.ExamId && question.QuestionNumber == 2);
            var addedPassage = context.ReadingPassages.Single(passage =>
                passage.ExamId == ids.ExamId && passage.PartNumber == 6);
            addedQuestion.ReadingPassageId.ShouldBe(addedPassage.Id);
            context.QuestionOptions.Count(option => option.QuestionId == addedQuestion.Id).ShouldBe(4);
        });
    }

    private (int ExamId, int PassageId, int QuestionId, int[] OptionIds) CreateExam(
        bool isPublished = false,
        bool withAttempt = false)
    {
        return UsingDbContext(context =>
        {
            var exam = new Exam
            {
                Title = "Original title",
                DurationMinutes = 75,
                IsPublished = isPublished
            };
            context.Exams.Add(exam);
            context.SaveChanges();

            var passage = new ReadingPassage
            {
                ExamId = exam.Id,
                PartNumber = 7,
                Title = "Original passage",
                Content = "Original passage content",
                OrderNumber = 1
            };
            context.ReadingPassages.Add(passage);
            context.SaveChanges();

            var question = new Question
            {
                ExamId = exam.Id,
                ReadingPassageId = passage.Id,
                PartNumber = 7,
                QuestionNumber = 1,
                QuestionText = "Original question?"
            };
            context.Questions.Add(question);
            context.SaveChanges();

            var optionIds = new int[4];
            var labels = new[] { "A", "B", "C", "D" };
            for (var index = 0; index < labels.Length; index++)
            {
                var option = new QuestionOption
                {
                    QuestionId = question.Id,
                    Label = labels[index],
                    Content = $"Original {labels[index]}",
                    IsCorrect = index == 0,
                    OrderNumber = index + 1
                };
                context.QuestionOptions.Add(option);
                context.SaveChanges();
                optionIds[index] = option.Id;
            }

            if (withAttempt)
            {
                context.StudentExamAttempts.Add(new StudentExamAttempt
                {
                    StudentUserId = AbpSession.UserId!.Value,
                    ExamId = exam.Id,
                    StartedAtUtc = DateTimeOffset.UtcNow,
                    ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(75),
                    IsSubmitted = true,
                    SubmittedAtUtc = DateTimeOffset.UtcNow,
                    TotalQuestionCount = 1
                });
                context.SaveChanges();
            }

            return (exam.Id, passage.Id, question.Id, optionIds);
        });
    }

    private static UpdateAdminExamInputDto BuildUpdate(
        (int ExamId, int PassageId, int QuestionId, int[] OptionIds) ids)
    {
        return new UpdateAdminExamInputDto
        {
            Title = "Updated title",
            Description = "Updated description",
            DurationMinutes = 80,
            ReadingPassages =
            {
                new UpdateAdminReadingPassageInputDto
                {
                    Id = ids.PassageId,
                    PartNumber = 7,
                    Title = "Updated passage title",
                    Content = "Updated passage",
                    OrderNumber = 1
                }
            },
            Questions =
            {
                new UpdateAdminQuestionInputDto
                {
                    Id = ids.QuestionId,
                    PartNumber = 7,
                    QuestionNumber = 1,
                    ReadingPassageId = ids.PassageId,
                    QuestionText = "Updated question?",
                    Options =
                    {
                        new UpdateAdminQuestionOptionInputDto { Id = ids.OptionIds[0], Label = "A", Content = "Updated A", OrderNumber = 1 },
                        new UpdateAdminQuestionOptionInputDto { Id = ids.OptionIds[1], Label = "B", Content = "Updated B", OrderNumber = 2, IsCorrect = true },
                        new UpdateAdminQuestionOptionInputDto { Id = ids.OptionIds[2], Label = "C", Content = "Updated C", OrderNumber = 3 },
                        new UpdateAdminQuestionOptionInputDto { Id = ids.OptionIds[3], Label = "D", Content = "Updated D", OrderNumber = 4 }
                    }
                }
            }
        };
    }
}
