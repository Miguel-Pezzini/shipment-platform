using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShipmentPlatform.Infrastructure.Auth;
using ShipmentPlatform.Infrastructure.Options;

namespace ShipmentPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IJwtTokenService jwtTokenService,
    IOptions<AuthOptions> authOptions) : ControllerBase
{
    public record LoginRequest(string Username, string Password);
    public record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);

    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        var demo = authOptions.Value.DemoUser;
        if (!string.Equals(request.Username, demo.Username, StringComparison.Ordinal)
            || !string.Equals(request.Password, demo.Password, StringComparison.Ordinal))
        {
            return Unauthorized(new { error = "Invalid username or password." });
        }

        var (token, expires) = jwtTokenService.CreateToken(request.Username);
        return Ok(new LoginResponse(token, expires));
    }
}
