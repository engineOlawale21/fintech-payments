using System.ComponentModel.DataAnnotations;

namespace FintechPayments.Api.Contracts.Authentication;

public sealed record LoginRequest(
    [Required, EmailAddress, MaxLength(320)] string Email,
    [Required, MaxLength(128)] string Password);
