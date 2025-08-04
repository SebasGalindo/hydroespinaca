using AuthService.Application.DTOs;
using AuthService.Application.UseCases;
using AuthService.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthenticateUserUseCase _authUser;
    private readonly RefreshTokenUseCase _refresh;
    private readonly ClientCredentialsUseCase _clientCreds;
    private readonly IKeyStore _keyStore;

    public AuthController(
        AuthenticateUserUseCase authUser,
        RefreshTokenUseCase refresh,
        ClientCredentialsUseCase clientCreds,
        IKeyStore keyStore)
    {
        _authUser = authUser;
        _refresh = refresh;
        _clientCreds = clientCreds;
        _keyStore = keyStore;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        var tokens = await _authUser.ExecuteAsync(dto);
        return Ok(tokens);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponseDto>> Refresh([FromBody] RefreshRequestDto dto)
    {
        var tokens = await _refresh.ExecuteAsync(dto);
        return Ok(tokens);
    }

    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<ActionResult<TokenResponseDto>> Token([FromBody] ClientCredentialsRequestDto dto)
    {
        var tokens = await _clientCreds.ExecuteAsync(dto.ClientId, dto.ClientSecret);
        return Ok(tokens);
    }

    [Authorize]
    [HttpGet("validate")]
    public ActionResult<bool> Validate()
    {
        return Ok(true);
    }

    [AllowAnonymous]
    [HttpGet("keys/public")]
    public ActionResult<string> PublicKey()
    {
        var pub = _keyStore.GetPublicKey();
        return Ok(pub);
    }
}
