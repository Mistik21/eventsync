namespace eventsynс.Application.Interfaces.Services;

public interface IEventImageStorage
{
    Task<string> UploadCoverAsync(Guid eventId, string dataUrl);
    string? GetCoverUrl(Guid eventId, string? key);
    Task DeleteCoverAsync(Guid eventId, string? key);
}
