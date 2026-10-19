using Abp.Zero.EntityFrameworkCore;
using ToeicReading.Authorization.Roles;
using ToeicReading.Authorization.Users;
using ToeicReading.MultiTenancy;
using ToeicReading.Exams;
using Microsoft.EntityFrameworkCore;
using System;

namespace ToeicReading.EntityFrameworkCore;

public class ToeicReadingDbContext
    : AbpZeroDbContext<Tenant, Role, User, ToeicReadingDbContext>
{
    public DbSet<Exam> Exams { get; set; }

    public DbSet<ReadingPassage> ReadingPassages { get; set; }

    public DbSet<Question> Questions { get; set; }

    public DbSet<QuestionOption> QuestionOptions { get; set; }

    public DbSet<StudentExamAttempt> StudentExamAttempts { get; set; }

    public DbSet<StudentExamAttemptAnswer> StudentExamAttemptAnswers { get; set; }

    public ToeicReadingDbContext(
        DbContextOptions<ToeicReadingDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StudentExamAttempt>(entity =>
        {
            entity.HasIndex(attempt => new { attempt.StudentUserId, attempt.ExamId })
                .IsUnique()
                .HasFilter("[IsSubmitted] = 0");
            entity.HasIndex(attempt => new { attempt.StudentUserId, attempt.SubmittedAtUtc });
            entity.HasOne<Exam>()
                .WithMany()
                .HasForeignKey(attempt => attempt.ExamId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ToeicReading.Authorization.Users.User>()
                .WithMany()
                .HasForeignKey(attempt => attempt.StudentUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StudentExamAttemptAnswer>(entity =>
        {
            entity.HasIndex(answer => new
                {
                    answer.StudentExamAttemptId,
                    answer.QuestionId
                })
                .IsUnique();
            entity.HasOne<StudentExamAttempt>()
                .WithMany()
                .HasForeignKey(answer => answer.StudentExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Question>()
                .WithMany()
                .HasForeignKey(answer => answer.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<QuestionOption>()
                .WithMany()
                .HasForeignKey(answer => answer.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}