using System.Diagnostics;
using eventsynс.Application.Models;
using eventsynс.Application.Models.Activity;

namespace eventsynс.Application.Interfaces.Services;

public interface IActivityService
{
    Task<List<Activity>> GetActivitiesAsync(Guid eventId, Guid currentUserId);
    Task<Activity> GetActivityAsync(Guid activityId, Guid currentUserId);

    Task<Guid> CreateActivityAsync(Guid eventId, ActivityCreationModel data, Guid currentUserId);

    Task UpdateActivityAsync(Guid activityId, ActivityUpdateModel data, Guid currentUserId);

    Task DeleteActivityAsync(Guid activityId, Guid currentUserId);

    Task JoinActivityAsync(Guid activityId, Guid currentUserId);
    Task LeaveActivityAsync(Guid activityId, Guid currentUserId);
}