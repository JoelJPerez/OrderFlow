using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}