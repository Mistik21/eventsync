using eventsynс.Application.Interfaces.Storage;

namespace EventSync.UnitTests.TestSupport;

internal sealed class InMemoryEventImageStorage : IEventImageStorage
{
    public Dictionary<string, string> Covers { get; } = [];

    public Task<string> UploadCoverAsync(Guid eventId, string dataUrl)
    {
        var key = $"events/{eventId}/covers/{Guid.NewGuid()}.jpg";
        Covers.Add(key, dataUrl);
        return Task.FromResult(key);
    }

    public string? GetCoverUrl(Guid eventId, string? key) =>
        key is not null && Covers.ContainsKey(key) ? $"https://storage.example.test/{key}" : null;

    public Task DeleteCoverAsync(Guid eventId, string? key)
    {
        if (key is not null) Covers.Remove(key);
        return Task.CompletedTask;
    }
}
