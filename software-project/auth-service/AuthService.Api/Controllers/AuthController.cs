using AuthService.Application.Features.Authentication.Commands.ChangePassword;
using AuthService.Application.Features.Authentication.Commands.ClientCredentials;
using AuthService.Application.Features.Authentication.Commands.ForgotPassword;
using AuthService.Application.Features.Authentication.Commands.Login;
using AuthService.Application.Features.Authentication.Commands.RefreshToken;
using AuthService.Application.Features.Authentication.Commands.ResetPassword;
using AuthService.Application.Features.Authentication.DTOs;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Security;
using AuthService.Infrastructure.Security.Models;
using HydroEspinaca.Shared.DTOs.Authentication;
using HydroEspinaca.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
    public async Task<ActionResult<TokenResultDto>> Login([FromBody] HydroEspinaca.Shared.DTOs.Authentication.LoginRequestDto dto)
    {
        var command = new LoginCommand(
            dto.Email,
            dto.Password,
            dto.ClientId,
            dto.SessionId,
            dto.IpAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString(),
            dto.UserAgent ?? HttpContext.Request.Headers.UserAgent.ToString(),
            dto.CsrfToken);
        var tokens = await _mediator.Send(command);
        return Ok(tokens);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResultDto>> Refresh([FromBody] HydroEspinaca.Shared.DTOs.Authentication.RefreshRequestDto dto)
    {
        var command = new RefreshTokenCommand(
            dto.RefreshToken,
            dto.ClientId,
            dto.SessionId,
            dto.IpAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString(),
            dto.UserAgent ?? HttpContext.Request.Headers.UserAgent.ToString());
        var tokens = await _mediator.Send(command);
        return Ok(tokens);
    }

    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<ActionResult<TokenResultDto>> Token([FromBody] HydroEspinaca.Shared.DTOs.Authentication.ClientCredentialsRequestDto dto)
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

    /// <summary>
    /// Changes the password for the authenticated user
    /// </summary>
    /// <param name="dto">Change password request</param>
    /// <returns>Result of password change operation</returns>
    [Authorize(AuthorizationScopes.PasswordChange)]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
    {
        var userId = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("User ID not found in token");
        }

        var command = new ChangePasswordCommand
        {
            UserId = userId,
            OldPassword = dto.OldPassword,
            NewPassword = dto.NewPassword
        };

        var result = await _mediator.Send(command);

        if (result.Success)
        {
            return Ok(new { message = "Password changed successfully" });
        }

        return BadRequest(new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Initiates the password reset process by sending a code to the user's email
    /// </summary>
    /// <param name="dto">Forgot password request</param>
    /// <returns>Result of the operation</returns>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        var command = new ForgotPasswordCommand { Email = dto.Email };
        var result = await _mediator.Send(command);

        if (result.Success)
        {
            return Ok(new { message = result.Message });
        }

        return BadRequest(new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Resets the password using a verification code received via email
    /// </summary>
    /// <param name="dto">Reset password request</param>
    /// <returns>Result of password reset operation</returns>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
    {
        var command = new ResetPasswordCommand
        {
            Email = dto.Email,
            Code = dto.Code,
            NewPassword = dto.NewPassword
        };

        var result = await _mediator.Send(command);

        if (result.Success)
        {
            return Ok(new { message = result.Message });
        }

        return BadRequest(new { error = result.ErrorMessage });
    }
}
