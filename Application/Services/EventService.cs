using eventsynс.Application.Access;
using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Models.Event;
using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories;

namespace eventsynс.Application.Services;

public class EventService : IEventService
{
    private readonly IUserRepository userRepository;
    private readonly IEventRepository eventRepository;
    private readonly IEventImageStorage imageStorage;

    public EventService(IUserRepository userRepository, IEventRepository eventRepository, IEventImageStorage imageStorage)
    {
        this.userRepository = userRepository;
        this.eventRepository = eventRepository;
        this.imageStorage = imageStorage;
    }

    public async Task<List<Event>> GetMyEventsAsync(Guid currentUserId)
    {
        return await eventRepository.ListForOrganizerOrParticipantAsync(currentUserId);
    }

    public async Task<Event> GetEventAsync(Guid eventId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        EventAccess.RequireParticipant(eventDetails, currentUserId);
        return eventDetails;
    }

    public async Task CreateEventAsync(EventCreationModel data, Guid currentUserId)
    {
        var eventId = Guid.NewGuid();
        var user = await userRepository.GetByIdAsync(currentUserId);
        var eventNew = new Event(
            eventId,
            data.Title,
            data.Description,
            "",
            data.EventStartTime,
            data.EventLocation,
            data.Budget,
            currentUserId);
        eventNew.ChangeMainImage(
            string.IsNullOrEmpty(data.MainImageUrl)
                ? string.Empty : await imageStorage.UploadCoverAsync(eventId, data.MainImageUrl));
        eventNew.AddParticipant(user);
        eventRepository.AddEvent(eventNew);
        await eventRepository.SavedAsync();
    }

    public async Task UpdateEventAsync(Guid eventId, EventUpdateModel data, Guid currentUserId)
    {
        string? previousCover = null;
        await eventRepository.SaveEventChangesAsync(eventId, async eventDetails =>
        {
            EventAccess.RequireOrganizer(eventDetails, currentUserId);
            eventDetails.ChangeTitle(data.Title);
            eventDetails.ChangeDescription(data.Description);
            eventDetails.ChangeBudget(data.Budget);
            eventDetails.ChangeStartTime(data.EventStartTime);
            eventDetails.ChangeLocation(data.EventLocation);

            var cover = data.MainImageUrl;
            if (cover == null) return;
            previousCover = eventDetails.MainImageUrl;
            eventDetails.ChangeMainImage(
                cover.Length == 0
                    ? string.Empty
                    : await imageStorage.UploadCoverAsync(eventId, cover));
        });

        await imageStorage.DeleteCoverAsync(eventId, previousCover);
    }

    public async Task<bool> DeleteEventAsync(Guid eventId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        EventAccess.RequireOrganizer(eventDetails, currentUserId);
        eventRepository.RemoveEvent(eventDetails);
        await eventRepository.SavedAsync();
        await imageStorage.DeleteCoverAsync(eventId, eventDetails.MainImageUrl);
        return true;
    }

    public async Task<(Event Event, string? OrganizerName)> GetInvitationAsync(Guid eventId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        var organizer = await userRepository.GetByIdAsync(eventDetails.Organizer);
        return (eventDetails, organizer.Name);
    }

    public Task JoinEventAsync(Guid eventId, Guid currentUserId)
    {
        return eventRepository.SaveEventChangesAsync(eventId, async eventDetails =>
        {
            var user = await userRepository.GetByIdAsync(currentUserId);
            eventDetails.AddParticipant(user);
        });
    }

    public Task LeaveEventAsync(Guid eventId, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public async Task<List<User>> GetParticipantsAsync(Guid eventId, Guid currentUserId)
    {
        var eventDetails = await GetEventAsync(eventId, currentUserId);
        return eventDetails.Participants.ToList();
    }

    public Task RemoveParticipantAsync(Guid eventId, Guid userId, Guid currentUserId)
    {
        throw new NotImplementedException();
    }

    public Task PayMyDebtAsync(Guid eventId, Guid currentUserId, int amount)
    {
        return eventRepository.SaveEventChangesAsync(eventId, eventDetails =>
        {
            EventAccess.RequireParticipant(eventDetails, currentUserId);
            eventDetails.PayDebt(currentUserId, amount);
            return Task.CompletedTask;
        });
    }

    public Task OpenCollectionAsync(Guid eventId, Guid currentUserId)
    {
        return eventRepository.SaveEventChangesAsync(eventId, eventDetails =>
        {
            EventAccess.RequireOrganizer(eventDetails, currentUserId);
            eventDetails.OpenCollection();
            return Task.CompletedTask;
        });
    }

    public Task AddSupplementalDebtsAsync(Guid eventId, Guid currentUserId)
    {
        return eventRepository.SaveEventChangesAsync(eventId,  eventDetails =>
        {
            EventAccess.RequireOrganizer(eventDetails, currentUserId);
            eventDetails.AddSupplementalDebts();
            return Task.CompletedTask;
        });
    }
}
