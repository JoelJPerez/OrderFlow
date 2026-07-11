using MediatR;

namespace OrderFlow.Application.Features.Auth.RegisterTenant;

public record RegisterTenantCommand(
    string CompanyName,
    string Slug,
    string AdminEmail,
    string AdminFullName,
    string Password) : IRequest<RegisterTenantResult>;

public record RegisterTenantResult(Guid TenantId, string Token);