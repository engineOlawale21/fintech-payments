using FintechPayments.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FintechPayments.Infrastructure.Persistence;

public sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("Database__ConnectionString")
            ?? "Host=localhost;Port=5432;Database=fintech_payments;Username=fintech;Password=development_only";

        DbContextOptionsBuilder<PaymentsDbContext> builder = new();
        builder.UseNpgsql(connectionString, options =>
            options.MigrationsAssembly(typeof(PaymentsDbContext).Assembly.FullName));

        return new PaymentsDbContext(builder.Options);
    }
}
