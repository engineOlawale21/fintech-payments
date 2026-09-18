using FintechPayments.Api.Authentication;
using FintechPayments.Api.Contracts.Wallets;
using FintechPayments.Application.Wallets;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Wallets;
using FintechPayments.Api.Contracts.Ledger;
using FintechPayments.Application.Ledger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FintechPayments.Application.Abstractions.Persistence;

namespace FintechPayments.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/wallets")]
public sealed class WalletsController(
    WalletService wallets, LedgerService ledger, IWalletQuery walletQuery) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<WalletResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateWalletRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out Guid ownerId))
        {
            return Unauthorized();
        }

        try
        {
            CreateWalletResult result = await wallets.CreateAsync(
                ownerId, request.Currency, cancellationToken);

            if (!result.Succeeded)
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "A wallet already exists for this currency.",
                    extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode });
            }

            WalletResponse response = WalletResponse.FromWallet(result.Wallet!);
            return CreatedAtAction(nameof(GetById), new { walletId = response.Id }, response);
        }
        catch (DomainException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Wallet request is invalid.",
                detail: exception.Message,
                extensions: new Dictionary<string, object?> { ["code"] = "validation_failed" });
        }
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WalletResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out Guid ownerId))
        {
            return Unauthorized();
        }

        IReadOnlyList<WalletReadModel> owned = await walletQuery.ListOwnedAsync(ownerId, cancellationToken);
        return Ok(owned.Select(WalletResponse.FromReadModel));
    }

    [HttpGet("{walletId:guid}")]
    [ProducesResponseType<WalletResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid walletId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out Guid ownerId))
        {
            return Unauthorized();
        }

        WalletReadModel? wallet = await walletQuery.FindOwnedAsync(walletId, ownerId, cancellationToken);
        return wallet is null ? NotFound() : Ok(WalletResponse.FromReadModel(wallet));
    }

    [HttpGet("{walletId:guid}/ledger")]
    [ProducesResponseType<LedgerPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLedger(
        Guid walletId,
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out Guid ownerId))
        {
            return Unauthorized();
        }

        try
        {
            WalletLedgerResult result = await ledger.GetWalletLedgerAsync(
                walletId, ownerId, cursor, pageSize, cancellationToken);
            if (!result.Exists)
            {
                return NotFound();
            }

            LedgerPageResponse response = new(
                result.Entries.Select(LedgerEntryResponse.FromEntry).ToArray(),
                result.NextCursor);
            return Ok(response);
        }
        catch (DomainException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ledger request is invalid.",
                detail: exception.Message,
                extensions: new Dictionary<string, object?> { ["code"] = "validation_failed" });
        }
    }
}
