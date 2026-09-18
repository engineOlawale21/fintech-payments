using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FintechPayments.IntegrationTests;

public sealed class ApiFoundationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public ApiFoundationTests(WebApplicationFactory<Program> factory)
    {
        client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Database:ConnectionString", "Host=test;Database=test");
            builder.UseSetting("Redis:ConnectionString", "test:6379");
            builder.UseSetting("Jwt:Issuer", "FintechPayments.Tests");
            builder.UseSetting("Jwt:Audience", "FintechPayments.Tests.Client");
            builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-at-least-32-characters");
            builder.UseSetting("Testing:SkipDependencyConnectivityChecks", "true");
            builder.UseSetting("Webhooks:MockProvider:Secret", "integration-test-webhook-secret-at-least-32-characters");
        }).CreateClient();
    }

    [Fact]
    public async Task PaymentWebhookRejectsMissingSignature()
    {
        using StringContent content = new("{}", System.Text.Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/webhooks/payments/mock-provider", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocumentIsPublished()
    {
        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string document = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/v1/system/info", document, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CurrentUserRequiresAuthentication()
    {
        HttpResponseMessage response = await client.GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegistrationRequestUsesConstructorParameterValidation()
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = "not-an-email", password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/wallets")]
    [InlineData("/api/v1/wallets/00000000-0000-0000-0000-000000000001")]
    public async Task WalletReadsRequireAuthentication(string path)
    {
        HttpResponseMessage response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/transfers/00000000-0000-0000-0000-000000000001")]
    public async Task TransferReadsRequireAuthentication(string path)
    {
        HttpResponseMessage response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReconciliationRequiresOperationsAuthentication()
    {
        HttpResponseMessage response = await client.GetAsync(
            "/api/v1/reconciliations/00000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SettlementsRequireOperationsAuthentication()
    {
        HttpResponseMessage response = await client.GetAsync("/api/v1/settlements");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuditEventsRequireOperationsAuthentication()
    {
        HttpResponseMessage response = await client.GetAsync("/api/v1/audit-events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpointsReturnHealthy(string path)
    {
        HttpResponseMessage response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequestCorrelationIdIsReturnedToTheCaller()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/system/info");
        request.Headers.Add("X-Correlation-ID", "integration-test-42");

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("integration-test-42", response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task UnhandledExceptionReturnsSafeProblemDetails()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/system/failure-probe");
        request.Headers.Add("X-Correlation-ID", "failure-test-42");

        HttpResponseMessage response = await client.SendAsync(request);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("An unexpected error occurred.", problem.Title);
        Assert.Equal("failure-test-42", problem.Extensions["correlationId"]?.ToString());
        Assert.DoesNotContain(
            "Integration-test exception probe",
            problem.Detail ?? string.Empty,
            StringComparison.Ordinal);
    }
}
