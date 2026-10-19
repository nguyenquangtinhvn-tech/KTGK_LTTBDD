using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.UI;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using ToeicReading.Authorization.Roles;
using ToeicReading.Authorization.Users;
using ToeicReading.Exams;
using ToeicReading.Exams.Dto;
using Xunit;

namespace ToeicReading.Tests.Exams;

public class StudentExamAttempt_Tests : ToeicReadingTestBase
{
    private readonly IStudentExamAttemptAppService _attemptService;

    public StudentExamAttempt_Tests()
    {
        _attemptService = Resolve<IStudentExamAttemptAppService>();
    }

    [Fact]
    public async Task Should_Create_Resume_Save_And_Submit_Student_Attempt()
    {
        var examId = CreateExam(isPublished: true);
        LoginAsStudent();

        var started = await _attemptService.CreateAttemptAsync(examId);
        started.IsSubmitted.ShouldBeFalse();
        started.Questions.Count.ShouldBe(2);
        started.Questions[0].Options.Count.ShouldBe(4);
        started.Answers.ShouldBeEmpty();

        var resumed = await _attemptService.CreateAttemptAsync(examId);
        resumed.AttemptId.ShouldBe(started.AttemptId);

        await Should.ThrowAsync<UserFriendlyException>(
            () => _attemptService.SaveAnswerAsync(new()
            {
                AttemptId = started.AttemptId,
                QuestionId = started.Questions[0].Id,
                SelectedOptionId = started.Questions[1].Options[0].Id
            }));

        await _attemptService.SaveAnswerAsync(new()
        {
            AttemptId = started.AttemptId,
            QuestionId = started.Questions[0].Id,
            SelectedOptionId = started.Questions[0].Options[1].Id
        });
        await _attemptService.SaveAnswerAsync(new()
        {
            AttemptId = started.AttemptId,
            QuestionId = started.Questions[1].Id,
            SelectedOptionId = started.Questions[1].Options[0].Id
        });

        var result = await _attemptService.SubmitAttemptAsync(started.AttemptId);

        result.TotalQuestionCount.ShouldBe(2);
        result.CorrectCount.ShouldBe(1);
        result.WrongCount.ShouldBe(1);
        result.BlankCount.ShouldBe(0);
        result.Score.ShouldBe(1);
        result.AccuracyPercent.ShouldBe(50m);

        var submittedAgain = await _attemptService.SubmitAttemptAsync(started.AttemptId);
        submittedAgain.CorrectCount.ShouldBe(result.CorrectCount);

        await Should.ThrowAsync<UserFriendlyException>(
            () => _attemptService.SaveAnswerAsync(new()
            {
                AttemptId = started.AttemptId,
                QuestionId = started.Questions[0].Id,
                SelectedOptionId = started.Questions[0].Options[0].Id
            }));

        var history = await _attemptService.GetMyAttemptHistoryAsync();
        history.Count.ShouldBe(1);
        history[0].AttemptId.ShouldBe(started.AttemptId);
        history[0].AccuracyPercent.ShouldBe(50m);
    }

    [Fact]
    public async Task Should_Reject_Draft_Exam()
    {
        var examId = CreateExam(isPublished: false);
        LoginAsStudent();

        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _attemptService.CreateAttemptAsync(examId));

        exception.Message.ShouldContain("chưa được xuất bản");
    }

    [Fact]
    public async Task Should_Reject_Access_To_Another_Students_Attempt()
    {
        var examId = CreateExam(isPublished: true);
        var ownerId = CreateStudent();
        var otherStudentId = CreateStudent("student-other");
        AbpSession.UserId = ownerId;
        var attempt = await _attemptService.CreateAttemptAsync(examId);
        AbpSession.UserId = otherStudentId;

        await Should.ThrowAsync<UserFriendlyException>(
            () => _attemptService.GetAttemptAsync(attempt.AttemptId));
    }

    [Fact]
    public async Task Should_Require_Student_Role()
    {
        LoginAsHostAdmin();
        var examId = CreateExam(isPublished: true);

        await Should.ThrowAsync<AbpAuthorizationException>(
            () => _attemptService.CreateAttemptAsync(examId));
    }

    [Fact]
    public async Task Should_Auto_Submit_Expired_Attempts()
    {
        var examId = CreateExam(isPublished: true);
        var studentId = CreateStudent();
        var attemptId = UsingDbContext(context =>
        {
            var now = System.DateTimeOffset.UtcNow;
            var attempt = context.StudentExamAttempts.Add(new StudentExamAttempt
            {
                StudentUserId = studentId,
                ExamId = examId,
                StartedAtUtc = now.AddMinutes(-90),
                ExpiresAtUtc = now.AddMinutes(-15),
                IsSubmitted = false
            }).Entity;
            context.SaveChanges();
            return attempt.Id;
        });
        AbpSession.UserId = studentId;

        var result = await _attemptService.GetAttemptAsync(attemptId);

        result.IsSubmitted.ShouldBeTrue();
        result.Result.ShouldNotBeNull();
        result.Result!.BlankCount.ShouldBe(2);
        result.Result.TotalQuestionCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Not_Unpublish_Exam_While_Student_Attempt_Is_Active()
    {
        var examId = CreateExam(isPublished: true);
        LoginAsStudent();
        await _attemptService.CreateAttemptAsync(examId);
        LoginAsHostAdmin();
        var examService = Resolve<IExamAppService>();

        var statusException = await Should.ThrowAsync<UserFriendlyException>(
            () => examService.SetPublicationStatusAsync(new SetPublicationStatusInputDto
            {
                Id = examId,
                IsPublished = false
            }));
        statusException.Message.ShouldContain("vẫn đang làm bài");

        var updateException = await Should.ThrowAsync<UserFriendlyException>(
            () => examService.UpdateExamAsync(examId, new UpdateExamInputDto
            {
                Title = "Attempt lifecycle test",
                DurationMinutes = 75,
                IsPublished = false
            }));
        updateException.Message.ShouldContain("vẫn đang làm bài");
    }

    private void LoginAsStudent()
    {
        AbpSession.UserId = CreateStudent();
    }

    private long CreateStudent(string userName = "attempt-student")
    {
        return UsingDbContext(context =>
        {
            var tenantId = AbpSession.TenantId;
            var role = context.Roles.FirstOrDefault(item =>
                item.TenantId == tenantId && item.Name == "Student");
            if (role == null)
            {
                role = context.Roles.Add(
                    new Role(tenantId, "Student", "Student")).Entity;
                context.SaveChanges();
            }

            var user = new User
            {
                TenantId = tenantId,
                UserName = userName,
                Name = "Test",
                Surname = "Student",
                EmailAddress = userName + "@example.test",
                Password = User.DefaultPassword,
                IsActive = true
            };
            user.SetNormalizedNames();
            context.Users.Add(user);
            context.SaveChanges();
            context.UserRoles.Add(new UserRole(tenantId, user.Id, role.Id));
            context.SaveChanges();
            return user.Id;
        });
    }

    private int CreateExam(bool isPublished)
    {
        return UsingDbContext(context =>
        {
            var exam = new Exam
            {
                Title = "Attempt lifecycle test",
                DurationMinutes = 75,
                IsPublished = isPublished
            };
            context.Exams.Add(exam);
            context.SaveChanges();

            for (var questionNumber = 1; questionNumber <= 2; questionNumber++)
            {
                var question = new Question
                {
                    ExamId = exam.Id,
                    PartNumber = 5,
                    QuestionNumber = questionNumber,
                    QuestionText = $"Test question {questionNumber}?"
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
            }

            context.SaveChanges();
            return exam.Id;
        });
    }
}
