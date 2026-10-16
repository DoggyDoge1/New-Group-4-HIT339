using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Habibalib.Security;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "CatalogueApiKey";
    private readonly IConfiguration configuration;

    public ApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration)
        : base(options, logger, encoder) => this.configuration = configuration;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expected = configuration["PublicApi:ApiKey"];
        if (string.IsNullOrWhiteSpace(expected))
            return Task.FromResult(AuthenticateResult.Fail("API access has not been configured."));

        if (!Request.Headers.TryGetValue("X-Api-Key", out var supplied) || supplied.Count != 1)
            return Task.FromResult(AuthenticateResult.Fail("An X-Api-Key header is required."));

        // Compare hashes so timing does not reveal individual characters in the key.
        var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied.ToString()));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Catalogue client") }, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "ApiKey";
        return Response.WriteAsJsonAsync(new { error = "Supply a valid X-Api-Key header." });
    }
}
