using System.ComponentModel.DataAnnotations;

namespace FintechPayments.Api.Contracts.Wallets;

public sealed record CreateWalletRequest(
    [Required, StringLength(3, MinimumLength = 3)] string Currency);
