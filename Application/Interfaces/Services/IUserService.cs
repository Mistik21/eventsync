using eventsynс.Domain;

namespace eventsynс.Application.Interfaces.Services;

public interface IUserService
{
    Task<User> GetMyProfileAsync(Guid currentUserId);
}