using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Application.Features.Auth.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IAppDbContext _db;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(IAppDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters: en el login no hay token aún → no hay tenant en contexto.
        // El email identifica al usuario globalmente en este flujo.
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.Email == request.Email.ToLowerInvariant() && !u.IsDeleted,
                cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales inválidas.");

        return new LoginResult(
            _tokenService.GenerateToken(user),
            user.FullName,
            user.Role.ToString());
    }
}
