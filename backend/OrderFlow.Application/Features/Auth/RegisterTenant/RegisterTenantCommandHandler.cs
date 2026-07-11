using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Features.Auth.RegisterTenant;

public class RegisterTenantCommandHandler
    : IRequestHandler<RegisterTenantCommand, RegisterTenantResult>
{
    private readonly IAppDbContext _db;
    private readonly ITokenService _tokenService;

    public RegisterTenantCommandHandler(IAppDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<RegisterTenantResult> Handle(
        RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters: en el registro no hay tenant en contexto todavía,
        // y el slug debe ser único globalmente, no por tenant.
        var slugTaken = await _db.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Slug == request.Slug, cancellationToken);

        if (slugTaken)
            throw new ConflictException($"El slug '{request.Slug}' ya está en uso.");

        var tenant = new Tenant
        {
            Name = request.CompanyName,
            Slug = request.Slug
        };

        var admin = new User
        {
            TenantId = tenant.Id, // explícito: aún no hay TenantProvider con valor
            Tenant = tenant,
            Email = request.AdminEmail.ToLowerInvariant(),
            FullName = request.AdminFullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Admin
        };

        _db.Tenants.Add(tenant);
        _db.Users.Add(admin);
        await _db.SaveChangesAsync(cancellationToken);

        return new RegisterTenantResult(tenant.Id, _tokenService.GenerateToken(admin));
    }
}