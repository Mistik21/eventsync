using System.Diagnostics;
using eventsynс.Application.Models;
using eventsynс.Application.Models.Activity;
using Activity = eventsynс.Domain.Entities.Activity;

namespace eventsynс.Application.Interfaces.Events;

public interface IActivityService
{
    Task<List<Domain.Entities.Activity>> GetActivitiesAsync(Guid eventId, Guid currentUserId);

    Task CreateActivityAsync(Guid eventId, ActivityCreationModel data, Guid currentUserId);

    Task UpdateActivityAsync(Guid eventId,Guid activityId, ActivityUpdateModel data, Guid currentUserId);

    Task DeleteActivityAsync(Guid eventId,Guid activityId, Guid currentUserId);

    Task JoinActivityAsync(Guid eventId,Guid activityId, Guid currentUserId);
    Task LeaveActivityAsync(Guid eventId,Guid activityId, Guid currentUserId);
}