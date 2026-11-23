using System.Diagnostics;
using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Models.Activity;
using eventsynс.Domain.Repositories;
using Activity = eventsynс.Domain.Entities.Activity;

namespace eventsynс.Application.Services;

public class ActivityService : IActivityService
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

    public async Task CreateActivityAsync(Guid eventId, ActivityCreationModel data, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        var user = await userRepository.GetByIdAsync(currentUserId);
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

    public Task UpdateActivityAsync(Guid eventId, Guid activityId, ActivityUpdateModel data, Guid currentUserId)
    {
        return eventRepository.SaveActivityChangesAsync(eventId, activityId, eventDetails =>
        {
            if (!eventDetails.IsParticipant(currentUserId)) throw new UnauthorizedAccessException();
            var activity = eventDetails.GetRequiredActivity(activityId);
            if (!activity.IsCreator(currentUserId)) throw new UnauthorizedAccessException();
            activity.ChangeTitle(data.Title);
            activity.ChangeDescription(data.Description);
            activity.ChangeTime(data.ActivityStartTime, data.ActivityEndTime);
            activity.ChangeMaxParticipants(data.MaxParticipants);
            return Task.CompletedTask;
        });
    }

    public async Task DeleteActivityAsync(Guid eventId, Guid activityId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        if (!eventDetails.IsParticipant(currentUserId)) throw new UnauthorizedAccessException();
        var activity = eventDetails.GetRequiredActivity(activityId);
        if (!activity.IsCreator(currentUserId)) throw new UnauthorizedAccessException();
        eventDetails.RemoveActivity(activityId);
        eventRepository.RemoveActivity(activity);
        await eventRepository.SavedAsync();
    }

    public Task JoinActivityAsync(Guid eventId, Guid activityId, Guid currentUserId)
    {
        return eventRepository.SaveActivityChangesAsync(eventId, activityId, async eventDetails =>
        {
            var user = await userRepository.GetByIdAsync(currentUserId);
            if (!eventDetails.IsParticipant(currentUserId)) throw new UnauthorizedAccessException();
            var activity = eventDetails.GetRequiredActivity(activityId);
            activity.AddParticipant(user);
        });
    }

    public Task LeaveActivityAsync(Guid eventId, Guid activityId, Guid currentUserId)
    {
        return eventRepository.SaveActivityChangesAsync(eventId, activityId, eventDetails =>
        {
            if (!eventDetails.IsParticipant(currentUserId)) throw new UnauthorizedAccessException();
            var activity = eventDetails.GetRequiredActivity(activityId);
            activity.RemoveParticipant(currentUserId);
            return Task.CompletedTask;
        });
    }
}
