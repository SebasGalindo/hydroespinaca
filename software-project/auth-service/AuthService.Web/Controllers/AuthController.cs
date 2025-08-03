using AuthService.Application.DTOs;
using AuthService.Application.UseCases;
using AuthService.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthenticateUserUseCase _authUser;
    private readonly RefreshTokenUseCase _refresh;
    private readonly ClientCredentialsUseCase _clientCreds;
    private readonly ValidateTokenUseCase _validate;
    private readonly IKeyStore _keyStore;

    public AuthController(
        AuthenticateUserUseCase authUser,
        RefreshTokenUseCase refresh,
        ClientCredentialsUseCase clientCreds,
        ValidateTokenUseCase validate,
        IKeyStore keyStore)
    {
        _authUser = authUser;
        _refresh = refresh;
        _clientCreds = clientCreds;
        _validate = validate;
        _keyStore = keyStore;
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        var tokens = await _authUser.ExecuteAsync(dto);
        return Ok(tokens);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponseDto>> Refresh([FromBody] RefreshRequestDto dto)
    {
        var tokens = await _refresh.ExecuteAsync(dto);
        return Ok(tokens);
    }

    [HttpPost("token")]
    public async Task<ActionResult<TokenResponseDto>> Token([FromBody] ClientCredentialsRequestDto dto)
    {
        var tokens = await _clientCreds.ExecuteAsync(dto.ClientId, dto.ClientSecret);
        return Ok(tokens);
    }

    [HttpGet("validate")]
    public async Task<ActionResult<bool>> Validate([FromHeader(Name = "Authorization")] string authHeader)
    {
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            return BadRequest("Missing or invalid Authorization header.");

        var token = authHeader.Substring("Bearer ".Length).Trim();
        var isValid = await _validate.ExecuteAsync(token);
        return Ok(isValid);
    }

    [HttpGet("keys/public")]
    public ActionResult<string> PublicKey()
    {
        var pub = _keyStore.GetPublicKey();
        return Ok(pub);
    }
}
