namespace FintechPayments.Api.Contracts.Authentication;

public sealed record AuthenticationResponse(
    Guid UserId,
    string Email,
    string Role,
    string AccessToken,
    DateTimeOffset ExpiresAt);
