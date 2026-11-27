using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using eventsynс.Application.Interfaces.Services;

namespace eventsynс.Application.Services;

public class EventImageStorage : IEventImageStorage
{
    private const string JpegDataUrlPrefix = "data:image/jpeg;base64,";

    private readonly Lazy<IAmazonS3> s3;
    private readonly string bucket;

    public EventImageStorage(Lazy<IAmazonS3> s3, IConfiguration configuration)
    {
        this.s3 = s3;
        bucket = configuration["ObjectStorage:Bucket"]!;
    }

    public async Task<string> UploadCoverAsync(Guid eventId, string dataUrl)
    {
        var jpeg = Convert.FromBase64String(dataUrl[JpegDataUrlPrefix.Length..]);
        var key = $"{CoverPrefix(eventId)}{Guid.NewGuid()}.jpg";
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await s3.Value.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key =  key,
            InputStream = new MemoryStream(jpeg),
            ContentType = "image/jpeg",
            UseChunkEncoding = false
        }, timeout.Token);

        return key;
    }

    public string? GetCoverUrl(Guid eventId, string? key)
    {
        if (!IsCoverKey(eventId, key)) return null;

        return s3.Value.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Protocol = Protocol.HTTPS,
            Expires = DateTime.UtcNow.AddMinutes(30)
        });
    }

    public async Task DeleteCoverAsync(Guid eventId, string? key)
    {
        if (!IsCoverKey(eventId, key))
            return;

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await s3.Value.DeleteObjectAsync(bucket, key, timeout.Token);
    }

    private static string CoverPrefix(Guid eventId) => $"events/{eventId}/covers/";

    private static bool IsCoverKey(Guid eventId, string? key)
    {
        var prefix = CoverPrefix(eventId);
        const string extension = ".jpg";
        return key is not null && key.Length == prefix.Length + 36 + extension.Length
            && key.StartsWith(prefix, StringComparison.Ordinal)
            && key.EndsWith(extension, StringComparison.Ordinal)
            && Guid.TryParseExact(key.AsSpan(prefix.Length, 36), "D", out _);
    }
}
