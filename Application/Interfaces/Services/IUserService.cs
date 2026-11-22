using eventsynс.Domain;
using eventsynс.Domain.Entities;

namespace eventsynс.Application.Interfaces.Services;

public interface IUserService
{
    Task<User> GetMyProfileAsync(Guid currentUserId);
    Task<int> GetCountByOrganizerAsync(Guid currentUserId);
}