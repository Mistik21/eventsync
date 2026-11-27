using eventsynс.Application.Interfaces.Users;
using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories.Interfaces;

namespace eventsynс.Application.Services.Users;

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
    
    public async Task<int> GetCountByParticipantAsync(Guid userId)
    {
        return await eventRepository.CountByParticipantAsync(userId);
    }
}