using FintechPayments.Domain.Wallets;
using FintechPayments.Domain.Users;
using FintechPayments.Domain.Ledger;
using FintechPayments.Domain.Transfers;
using FintechPayments.Domain.Payments;
using FintechPayments.Domain.Webhooks;
using FintechPayments.Domain.Reconciliation;
using FintechPayments.Domain.Settlements;
using FintechPayments.Domain.Auditing;
using Microsoft.EntityFrameworkCore;

namespace FintechPayments.Infrastructure.Persistence;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
    : DbContext(options)
{
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<User> Users => Set<User>();
    public DbSet<LedgerTransaction> LedgerTransactions => Set<LedgerTransaction>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<ReconciliationRun> ReconciliationRuns => Set<ReconciliationRun>();
    public DbSet<ReconciliationItem> ReconciliationItems => Set<ReconciliationItem>();
    public DbSet<SettlementBatch> SettlementBatches => Set<SettlementBatch>();
    public DbSet<SettlementItem> SettlementItems => Set<SettlementItem>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }
}
