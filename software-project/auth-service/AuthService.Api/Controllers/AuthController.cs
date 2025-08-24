using AuthService.Api.Models;
using AuthService.Application.Features.Authentication.Commands.ClientCredentials;
using AuthService.Application.Features.Authentication.Commands.Login;
using AuthService.Application.Features.Authentication.Commands.RefreshToken;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Security;
using AuthService.Infrastructure.Security.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IKeyStore _keyStore;

    public AuthController(
       IMediator mediator,
       IKeyStore keyStore
    )
    {
        _mediator = mediator;
        _keyStore = keyStore;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<TokenResult>> Login([FromBody] LoginRequest dto)
    {
        var command = new LoginCommand(dto.Email, dto.Password);
        var tokens = await _mediator.Send(command);
        return Ok(tokens);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResult>> Refresh([FromBody] RefreshRequest dto)
    {
        var command = new RefreshTokenCommand(dto.RefreshToken, dto.ClientId);
        var tokens = await _mediator.Send(command);
        return Ok(tokens);
    }

    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<ActionResult<TokenResult>> Token([FromBody] ClientCredentialsRequest dto)
    {
        var command = new ClientCredentialsCommand(dto.ClientId, dto.ClientSecret);
        var tokens = await _mediator.Send(command);
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
    public ActionResult<JsonWebKeySet> PublicKeys()
    {
        var keyPairs = _keyStore.GetAllKeyPairs();
        var jwks = JwkConverter.ToJsonWebKeySet(keyPairs);
        return Ok(jwks);
    }
}
