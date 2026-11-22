using System.Diagnostics;
using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Models.Activity;
using eventsynс.Domain.Repositories;
using Activity = eventsynс.Domain.Entities.Activity;

namespace eventsynс.Application.Services;

public class ActivityService: IActivityService
{
    private readonly IEventRepository eventRepository;
    private readonly IUserRepository userRepository;

    public ActivityService(IEventRepository eventRepository, IUserRepository userRepository)
    {
        this.eventRepository = eventRepository;
        this.userRepository = userRepository;
    }
    
    public async Task<List<Activity>> GetActivitiesAsync(Guid eventId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        if (!eventDetails.IsParticipant(currentUserId)) throw new UnauthorizedAccessException();
        return eventDetails.Activities.ToList();
    }

    public Task<Activity> GetActivityAsync(Guid activityId, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public async Task CreateActivityAsync(Guid eventId, ActivityCreationModel data, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        var user= await userRepository.GetByIdAsync(currentUserId);
        if (!eventDetails.IsParticipant(currentUserId)) throw new UnauthorizedAccessException();
        var activity = new Activity(
                Guid.Empty,
            data.Title,
            data.Description,
            currentUserId,
            data.ActivityStartTime,
            data.ActivityEndTime,
            data.MaxParticipants
        );
        activity.AddParticipant(user);
        eventDetails.AddActivity(activity);
        await eventRepository.SavedAsync();
    }

    public Task UpdateActivityAsync(Guid activityId, ActivityUpdateModel data, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public Task DeleteActivityAsync(Guid activityId, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public Task JoinActivityAsync(Guid activityId, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public Task LeaveActivityAsync(Guid activityId, Guid currentUserId)
    {
        throw new NotImplementedException();
    }
}