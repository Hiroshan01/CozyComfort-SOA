using CozyComfort.Domain.Entities;

namespace CozyComfort.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user);
}
