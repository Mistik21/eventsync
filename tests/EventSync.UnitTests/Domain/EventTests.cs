using EventSync.UnitTests.TestSupport;
using Xunit;

namespace EventSync.UnitTests.Domain;

public class EventTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void ChangeStartTime_KeepsActivityOffsets(int shiftDays)
    {
        var organizer = TestData.CreateUser(1);
        var meetup = TestData.CreateEvent(organizer);
        var first = TestData.CreateActivity(organizer, meetup.EventStartTime.AddHours(1));
        var second = TestData.CreateActivity(organizer, meetup.EventStartTime.AddHours(4));
        meetup.AddActivity(first);
        meetup.AddActivity(second);
        var newStart = meetup.EventStartTime.AddDays(shiftDays);

        meetup.ChangeStartTime(newStart);

        Assert.Equal(newStart, meetup.EventStartTime);
        Assert.Equal(newStart.AddHours(1), first.АctivityStartTime);
        Assert.Equal(newStart.AddHours(2), first.АctivityEndTime);
        Assert.Equal(newStart.AddHours(4), second.АctivityStartTime);
        Assert.Equal(newStart.AddHours(5), second.АctivityEndTime);
    }
}
