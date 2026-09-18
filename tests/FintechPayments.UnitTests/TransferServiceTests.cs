using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Transfers;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Transfers;

namespace FintechPayments.UnitTests;

public sealed class TransferServiceTests
{
    [Fact]
    public async Task CreateValidatesMoneyBeforeCallingPersistence()
    {
        FakeTransferGateway gateway = new();
        TransferService service = new(gateway, gateway);

        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            10.001m, "USD", "invalid-precision", "key-1", CancellationToken.None));
        Assert.False(gateway.WasCalled);
    }

    [Fact]
    public async Task CreatePassesValidatedCommandToAtomicExecutor()
    {
        FakeTransferGateway gateway = new();
        TransferService service = new(gateway, gateway);

        TransferExecutionResult result = await service.CreateAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            10.25m, "usd", "valid-command", "key-2", CancellationToken.None);

        Assert.True(gateway.WasCalled);
        Assert.Equal("USD", gateway.Money.Currency.Code);
        Assert.Equal(10.25m, gateway.Money.Amount);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task CreateRequiresIdempotencyKey()
    {
        FakeTransferGateway gateway = new();
        TransferService service = new(gateway, gateway);

        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            10m, "USD", "missing-key", "", CancellationToken.None));
        Assert.False(gateway.WasCalled);
    }

    [Fact]
    public async Task IdenticalCommandsProduceIdenticalFingerprint()
    {
        FakeTransferGateway gateway = new();
        TransferService service = new(gateway, gateway);
        Guid actor = Guid.NewGuid();
        Guid source = Guid.NewGuid();
        Guid destination = Guid.NewGuid();

        await service.CreateAsync(actor, source, destination, 10m, "USD", "same", "first", CancellationToken.None);
        string firstHash = gateway.RequestHash;
        await service.CreateAsync(actor, source, destination, 10.00m, "usd", "same", "second", CancellationToken.None);

        Assert.Equal(firstHash, gateway.RequestHash);
        Assert.Equal(64, gateway.RequestHash.Length);
    }

    private sealed class FakeTransferGateway : ITransferExecutor, ITransferReader
    {
        public bool WasCalled { get; private set; }
        public Money Money { get; private set; }
        public string RequestHash { get; private set; } = string.Empty;

        public Task<TransferExecutionResult> ExecuteAsync(
            Guid actorId,
            Guid sourceWalletId,
            Guid destinationWalletId,
            Money money,
            string reference,
            string idempotencyKey,
            string requestHash,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            Money = money;
            RequestHash = requestHash;
            return Task.FromResult(TransferExecutionResult.Failure("test_result"));
        }

        public Task<Transfer?> FindAccessibleAsync(
            Guid transferId,
            Guid actorId,
            CancellationToken cancellationToken) =>
            Task.FromResult<Transfer?>(null);
    }
}
