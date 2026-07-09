using Microsoft.AspNetCore.Http;
using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Infrastructure.Services;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User
                .FindFirst("tenant_id")?.Value;

            return Guid.TryParse(claim, out var tenantId) ? tenantId : null;
        }
    }
}