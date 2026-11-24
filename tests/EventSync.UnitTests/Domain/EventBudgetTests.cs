using eventsynс.Domain.Entities;
using EventSync.UnitTests.TestSupport;
using Xunit;

namespace EventSync.UnitTests.Domain;

public class EventBudgetTests
{
    private readonly User organizer = TestData.CreateUser(1);
    private readonly User firstAttendee = TestData.CreateUser(2);
    private readonly User secondAttendee = TestData.CreateUser(3);

    [Fact]
    public void Budget_RejectsNegativeAmountsAndCanChangeBeforeOpeningCollection()
    {
        var start = DateTime.UtcNow.AddDays(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => TestData.CreateEvent(organizer, start, -1));
        var meetup = TestData.CreateEvent(organizer, start, 100);

        Assert.Throws<ArgumentOutOfRangeException>(() => meetup.ChangeBudget(-1));
        Assert.Equal(100, meetup.Budget);

        meetup.ChangeBudget(150);
        meetup.OpenCollection();

        Assert.Throws<InvalidOperationException>(() => meetup.ChangeBudget(200));
        Assert.Equal(150, meetup.Budget);
        Assert.Equal(150, meetup.Debts[organizer.Id]);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(2, 1, 1, 0)]
    [InlineData(9, 3, 3, 3)]
    [InlineData(10, 4, 3, 3)]
    [InlineData(11, 4, 4, 3)]
    public void OpenCollection_DistributesWholeBudget(int budget, int first, int second, int third)
    {
        var meetup = CreateEvent(budget);

        meetup.OpenCollection();

        Assert.Equal(CollectionStatus.Open, meetup.CollectionStatus);
        Assert.Equal(0, meetup.CollectedBudget);
        AssertDebts(meetup, first, second, third);
        Assert.Equal(budget, meetup.Debts.Values.Sum());
    }

    [Fact]
    public void OpenCollection_CannotResetPreviouslyPaidDebt()
    {
        var meetup = CreateEvent(300);
        meetup.OpenCollection();
        meetup.PayDebt(organizer.Id, 100);

        Assert.Throws<InvalidOperationException>(() => meetup.OpenCollection());

        Assert.Equal(100, meetup.CollectedBudget);
        AssertDebts(meetup, 0, 100, 100);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(40, 60)]
    [InlineData(100, 0)]
    public void PayDebt_RecordsOnlyTheSpecifiedAmount(int payment, int remainingDebt)
    {
        var meetup = CreateEvent(300);
        meetup.OpenCollection();

        meetup.PayDebt(organizer.Id, payment);

        Assert.Equal(payment, meetup.CollectedBudget);
        AssertDebts(meetup, remainingDebt, 100, 100);
        Assert.Equal(300, meetup.Budget);
    }

    [Fact]
    public void PayDebt_RejectsNegativePaymentAndPreservesPreviousPayment()
    {
        var meetup = CreateEvent(300);
        meetup.OpenCollection();
        meetup.PayDebt(organizer.Id, 40);

        Assert.Throws<InvalidOperationException>(() => meetup.PayDebt(organizer.Id, -10));

        Assert.Equal(40, meetup.CollectedBudget);
        AssertDebts(meetup, 60, 100, 100);
    }

    [Theory]
    [InlineData(0, 101, 100)]
    [InlineData(40, 61, 60)]
    [InlineData(100, 1, 0)]
    public void PayDebt_RejectsPaymentAboveTheRemainingDebt(int alreadyPaid, int nextPayment, int remainingDebt)
    {
        var meetup = CreateEvent(300);
        meetup.OpenCollection();
        meetup.PayDebt(organizer.Id, alreadyPaid);

        Assert.Throws<InvalidOperationException>(() => meetup.PayDebt(organizer.Id, nextPayment));

        Assert.Equal(alreadyPaid, meetup.CollectedBudget);
        AssertDebts(meetup, remainingDebt, 100, 100);
    }

    [Theory]
    [InlineData(1, 1, 0, 0)]
    [InlineData(6, 2, 2, 2)]
    [InlineData(8, 3, 3, 2)]
    public void AddSupplementalDebts_AllocatesOnlyOverspendToPaidAndUnpaidParticipants(
        int overspend, int firstExtra, int secondExtra, int thirdExtra)
    {
        var meetup = CreateEvent(300, started: true);
        meetup.OpenCollection();
        meetup.PayDebt(organizer.Id, 100);
        TestData.SeedCost(meetup, 200);
        TestData.SeedCost(meetup, 100 + overspend);

        meetup.AddSupplementalDebts();

        Assert.Equal(300 + overspend, meetup.Budget);
        Assert.Equal(100, meetup.CollectedBudget);
        AssertDebts(meetup, firstExtra, 100 + secondExtra, 100 + thirdExtra);
        Assert.Equal(meetup.Budget, meetup.CollectedBudget + meetup.Debts.Values.Sum());
        Assert.Equal(CollectionStatus.Open, meetup.CollectionStatus);
    }

    [Fact]
    public void AddSupplementalDebts_RejectsRepeatEvenAfterPayingNewDebt()
    {
        var meetup = CreateEvent(300, started: true);
        meetup.OpenCollection();
        meetup.PayDebt(organizer.Id, 100);
        TestData.SeedCost(meetup, 330);
        meetup.AddSupplementalDebts();
        meetup.PayDebt(organizer.Id, 10);

        Assert.Throws<InvalidOperationException>(() => meetup.AddSupplementalDebts());

        Assert.Equal(330, meetup.Budget);
        Assert.Equal(110, meetup.CollectedBudget);
        AssertDebts(meetup, 0, 110, 110);
    }

    [Fact]
    public void AddSupplementalDebts_RejectsOverspendBeforeStart()
    {
        var meetup = CreateEvent(300);
        meetup.OpenCollection();
        meetup.AddCost(new Cost(Guid.NewGuid(), "Food", 330));

        Assert.Throws<InvalidOperationException>(() => meetup.AddSupplementalDebts());

        Assert.Equal(300, meetup.Budget);
        AssertDebts(meetup, 100, 100, 100);
    }

    [Fact]
    public void AddSupplementalDebts_RejectsClosedCollection()
    {
        var meetup = CreateEvent(300, started: true);
        TestData.SeedCost(meetup, 330);

        Assert.Throws<InvalidOperationException>(() => meetup.AddSupplementalDebts());

        Assert.Empty(meetup.Debts);
        Assert.Equal(CollectionStatus.NotStarted, meetup.CollectionStatus);
    }

    [Theory]
    [InlineData(299)]
    [InlineData(300)]
    public void AddSupplementalDebts_RequiresSpendingAboveBudget(int spent)
    {
        var meetup = CreateEvent(300, started: true);
        meetup.OpenCollection();
        TestData.SeedCost(meetup, spent);

        Assert.Throws<InvalidOperationException>(() => meetup.AddSupplementalDebts());

        Assert.Equal(300, meetup.Budget);
        AssertDebts(meetup, 100, 100, 100);
    }

    private Event CreateEvent(int budget, bool started = false) =>
        TestData.CreateEvent(organizer, DateTime.UtcNow.AddDays(started ? -1 : 1), budget,
            firstAttendee, secondAttendee);

    private void AssertDebts(Event meetup, int first, int second, int third)
    {
        Assert.Equal(3, meetup.Debts.Count);
        Assert.Equal(first, meetup.Debts[organizer.Id]);
        Assert.Equal(second, meetup.Debts[firstAttendee.Id]);
        Assert.Equal(third, meetup.Debts[secondAttendee.Id]);
    }
}
