namespace eventsynс.Application.Interfaces.Storage;

public interface IEventImageStorage
{
    Task<string> UploadCoverAsync(Guid eventId, string dataUrl);
    string? GetCoverUrl(Guid eventId, string? key);
    Task DeleteCoverAsync(Guid eventId, string? key);
}
