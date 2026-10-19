using Abp.Authorization;
using Abp.UI;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using ToeicReading.Authorization.Users;
using ToeicReading.EntityFrameworkCore;
using ToeicReading.Exams;
using ToeicReading.Exams.Dto;
using Xunit;

namespace ToeicReading.Tests.Exams;

public class ExamPublication_Tests : ToeicReadingTestBase
{
    private readonly IExamAppService _examAppService;

    public ExamPublication_Tests()
    {
        _examAppService = Resolve<IExamAppService>();
    }

    [Fact]
    public async Task Should_Require_At_Least_One_Question_To_Publish()
    {
        LoginAsHostAdmin();
        var examId = CreateExam();

        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _examAppService.SetPublicationStatusAsync(
                new SetPublicationStatusInputDto { Id = examId, IsPublished = true }));

        exception.Message.ShouldContain("ít nhất một câu hỏi");
        UsingDbContext(context =>
            context.Exams.Single(exam => exam.Id == examId).IsPublished.ShouldBeFalse());
    }

    [Fact]
    public async Task Should_Publish_And_Unpublish_A_Valid_Exam()
    {
        LoginAsHostAdmin();
        var examId = CreateExamWithValidQuestion();

        var published = await _examAppService.SetPublicationStatusAsync(
            new SetPublicationStatusInputDto { Id = examId, IsPublished = true });
        published.ShouldBeTrue();
        UsingDbContext(context =>
            context.Exams.Single(exam => exam.Id == examId).IsPublished.ShouldBeTrue());

        var unpublished = await _examAppService.SetPublicationStatusAsync(
            new SetPublicationStatusInputDto { Id = examId, IsPublished = false });
        unpublished.ShouldBeFalse();
        UsingDbContext(context =>
            context.Exams.Single(exam => exam.Id == examId).IsPublished.ShouldBeFalse());
    }

    [Fact]
    public async Task Should_Reject_Publication_Without_Exactly_One_Correct_Option()
    {
        LoginAsHostAdmin();
        var examId = CreateExamWithValidQuestion(correctOptionCount: 0);

        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _examAppService.SetPublicationStatusAsync(
                new SetPublicationStatusInputDto { Id = examId, IsPublished = true }));

        exception.Message.ShouldContain("chính xác một đáp án đúng");
        UsingDbContext(context =>
            context.Exams.Single(exam => exam.Id == examId).IsPublished.ShouldBeFalse());
    }

    [Fact]
    public async Task Should_Allow_Part_Six_Question_Represented_By_A_Passage_Gap()
    {
        LoginAsHostAdmin();
        var examId = CreateExamWithPartSixQuestion(
            "The membership offer is available ------- [131] days.");

        var published = await _examAppService.SetPublicationStatusAsync(
            new SetPublicationStatusInputDto { Id = examId, IsPublished = true });

        published.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Reject_Part_Six_Question_When_Its_Passage_Gap_Is_Missing()
    {
        LoginAsHostAdmin();
        var examId = CreateExamWithPartSixQuestion(
            "The membership offer is available for thirty days.");

        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _examAppService.SetPublicationStatusAsync(
                new SetPublicationStatusInputDto { Id = examId, IsPublished = true }));

        exception.Message.ShouldContain("QuestionText");
        exception.Message.ShouldContain("[131]");
    }

    [Fact]
    public async Task Should_Require_Exam_Management_Permission_To_Change_Publication()
    {
        var examId = CreateExamWithValidQuestion();
        var tenantId = AbpSession.TenantId;
        var userId = UsingDbContext(context =>
        {
            var user = new User
            {
                TenantId = tenantId,
                UserName = "publication-viewer",
                Name = "Publication",
                Surname = "Viewer",
                EmailAddress = "publication-viewer@example.com",
                Password = User.DefaultPassword,
                IsActive = true
            };
            user.SetNormalizedNames();
            context.Users.Add(user);
            context.SaveChanges();
            return user.Id;
        });
        AbpSession.UserId = userId;

        await Should.ThrowAsync<AbpAuthorizationException>(
            () => _examAppService.SetPublicationStatusAsync(
                new SetPublicationStatusInputDto { Id = examId, IsPublished = true }));
    }

    private int CreateExam()
    {
        return UsingDbContext(context =>
        {
            var exam = new Exam { Title = "Exam publication test" };
            context.Exams.Add(exam);
            context.SaveChanges();
            return exam.Id;
        });
    }

    private int CreateExamWithValidQuestion(int correctOptionCount = 1)
    {
        return UsingDbContext(context =>
        {
            var exam = new Exam { Title = "Exam publication test" };
            context.Exams.Add(exam);
            context.SaveChanges();

            var question = new Question
            {
                ExamId = exam.Id,
                PartNumber = 5,
                QuestionNumber = 1,
                QuestionText = "A valid question?"
            };
            context.Questions.Add(question);
            context.SaveChanges();

            var labels = new[] { "A", "B", "C", "D" };
            for (var index = 0; index < labels.Length; index++)
            {
                context.QuestionOptions.Add(new QuestionOption
                {
                    QuestionId = question.Id,
                    Label = labels[index],
                    Content = $"Option {labels[index]}",
                    IsCorrect = index < correctOptionCount,
                    OrderNumber = index + 1
                });
            }

            context.SaveChanges();
            return exam.Id;
        });
    }

    private int CreateExamWithPartSixQuestion(string passageContent)
    {
        return UsingDbContext(context =>
        {
            var exam = new Exam { Title = "Part 6 publication test" };
            context.Exams.Add(exam);
            context.SaveChanges();

            var passage = new ReadingPassage
            {
                ExamId = exam.Id,
                PartNumber = 6,
                Title = "Passage",
                Content = passageContent
            };
            context.ReadingPassages.Add(passage);
            context.SaveChanges();

            var question = new Question
            {
                ExamId = exam.Id,
                ReadingPassageId = passage.Id,
                PartNumber = 6,
                QuestionNumber = 131,
                QuestionText = string.Empty
            };
            context.Questions.Add(question);
            context.SaveChanges();

            var labels = new[] { "A", "B", "C", "D" };
            for (var index = 0; index < labels.Length; index++)
            {
                context.QuestionOptions.Add(new QuestionOption
                {
                    QuestionId = question.Id,
                    Label = labels[index],
                    Content = $"Option {labels[index]}",
                    IsCorrect = index == 0,
                    OrderNumber = index + 1
                });
            }

            context.SaveChanges();
            return exam.Id;
        });
    }
}
