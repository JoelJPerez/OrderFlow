using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Features.Auth.Login;
using OrderFlow.Application.Features.Auth.RegisterTenant;

namespace OrderFlow.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register-tenant")]
    public async Task<ActionResult<RegisterTenantResult>> RegisterTenant(
        RegisterTenantCommand command, CancellationToken ct)
        => Ok(await _sender.Send(command, ct));

    [HttpPost("login")]
    public async Task<ActionResult<LoginResult>> Login(
        LoginCommand command, CancellationToken ct)
        => Ok(await _sender.Send(command, ct));
}
