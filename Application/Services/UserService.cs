using eventsynс.Application.Interfaces.Services;
using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories;

namespace eventsynс.Application.Services;

public class UserService: IUserService
{
    private readonly IUserRepository userRepository;
    
    public UserService(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }
    
    public async Task<User?> GetMyProfileAsync(Guid currentUserId)
    {
        return await userRepository.GetByIdAsync(currentUserId);
    }
}