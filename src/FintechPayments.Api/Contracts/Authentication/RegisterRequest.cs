using System.ComponentModel.DataAnnotations;

namespace FintechPayments.Api.Contracts.Authentication;

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(320)] string Email,
    [Required, MinLength(12), MaxLength(128)] string Password);
