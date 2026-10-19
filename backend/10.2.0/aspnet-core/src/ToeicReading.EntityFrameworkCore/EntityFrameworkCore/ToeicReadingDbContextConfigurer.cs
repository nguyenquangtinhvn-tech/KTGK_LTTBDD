using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace ToeicReading.EntityFrameworkCore;

public static class ToeicReadingDbContextConfigurer
{
    public static void Configure(DbContextOptionsBuilder<ToeicReadingDbContext> builder, string connectionString)
    {
        builder.UseSqlServer(connectionString);
    }

    public static void Configure(DbContextOptionsBuilder<ToeicReadingDbContext> builder, DbConnection connection)
    {
        builder.UseSqlServer(connection);
    }
}
