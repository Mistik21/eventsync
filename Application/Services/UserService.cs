using eventsynс.Application.Interfaces.Services;
using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories;

namespace eventsynс.Application.Services;

public class UserService: IUserService
{
    private readonly IUserRepository userRepository;
    private readonly IEventRepository eventRepository;
    
    public UserService(IUserRepository userRepository,IEventRepository eventRepository)
    {
        this.userRepository = userRepository;
        this.eventRepository = eventRepository;
    }
    
    public async Task<User> GetMyProfileAsync(Guid currentUserId)
    {
        return await userRepository.GetByIdAsync(currentUserId);
    }
    
    public async Task<int> GetCountByOrganizerAsync(Guid currentUserId)
    {
        return await eventRepository.CountByOrganizerAsync(currentUserId);
    }
}