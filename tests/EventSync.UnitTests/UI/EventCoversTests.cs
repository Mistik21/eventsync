using System.Security.Claims;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using eventsynс.Application.Services;
using eventsynс.UI.Controllers;
using eventsynс.UI.DTOs.Responses;
using EventSync.UnitTests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EventSync.UnitTests.UI;

public class EventCoversTests
{
    [Fact]
    public async Task CoverResponses_UseSignedUrlsForTheEventsOwnKey()
    {
        using var s3 = new RecordingS3Client();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ObjectStorage:Bucket"] = "test-covers" }).Build();
        var storage = new EventImageStorage(new Lazy<IAmazonS3>(() => s3), configuration);
        var organizer = TestData.CreateUser(1);
        var meetup = TestData.CreateEvent(organizer);
        var otherMeetup = TestData.CreateEvent(organizer);
        var key = await storage.UploadCoverAsync(meetup.Id, "data:image/jpeg;base64,/9j/2Q==");
        Assert.Equal(new byte[] { 0xff, 0xd8, 0xff, 0xd9 }, s3.UploadedCover);
        meetup.ChangeMainImage(key);
        otherMeetup.ChangeMainImage(key);
        var events = new InMemoryEventRepository(meetup, otherMeetup);
        var controller = new EventsController(new EventService(new InMemoryUserRepository(organizer), events, storage), storage)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, organizer.Id.ToString())], "test"))
                }
            }
        };

        var list = Assert.IsType<List<EventListItemResponse>>(
            Assert.IsType<OkObjectResult>((await controller.GetMyEvents()).Result).Value);
        var details = Assert.IsType<EventResponse>(Assert.IsType<OkObjectResult>(await controller.GetEvent(meetup.Id)).Value);
        var invitation = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(await controller.GetInvitation(meetup.Id)).Value);

        Assert.Null(list.Single(item => item.Id == otherMeetup.Id).MainImageUrl);
        Assert.Null(storage.GetCoverUrl(meetup.Id, "data:image/jpeg;base64,/9j/2Q=="));
        AssertSignedUrl(list.Single(item => item.Id == meetup.Id).MainImageUrl, key);
        AssertSignedUrl(details.MainImageUrl, key);
        AssertSignedUrl(invitation.GetProperty("MainImageUrl").GetString(), key);
    }

    private static void AssertSignedUrl(string? url, string key)
    {
        Assert.NotNull(url);
        var address = new Uri(url);
        var query = QueryHelpers.ParseQuery(address.Query);
        Assert.Equal(Uri.UriSchemeHttps, address.Scheme);
        Assert.Equal("s3.yandexcloud.net", address.Host);
        Assert.Equal($"/test-covers/{key}", address.AbsolutePath);
        Assert.Equal("AWS4-HMAC-SHA256", query["X-Amz-Algorithm"].ToString());
        Assert.Contains("/ru-central1/s3/aws4_request", query["X-Amz-Credential"].ToString());
        Assert.InRange(int.Parse(query["X-Amz-Expires"].ToString()), 1795, 1800);
    }

    private sealed class RecordingS3Client : AmazonS3Client
    {
        public byte[]? UploadedCover { get; private set; }

        public RecordingS3Client() : base("test-key-id", "test-secret", new AmazonS3Config
        {
            ServiceURL = "https://s3.yandexcloud.net",
            AuthenticationRegion = "ru-central1",
            ForcePathStyle = true
        })
        {
        }

        public override async Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken cancellationToken = default)
        {
            using var content = new MemoryStream();
            await request.InputStream.CopyToAsync(content, cancellationToken);
            UploadedCover = content.ToArray();
            return new PutObjectResponse();
        }
    }
}
