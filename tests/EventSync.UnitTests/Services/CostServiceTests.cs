using eventsynс.Application.Models.Cost;
using eventsynс.Application.Services.Events;
using eventsynс.Domain.Entities;
using EventSync.UnitTests.TestSupport;
using Xunit;

namespace EventSync.UnitTests.Services;

public class CostServiceTests
{
    private readonly User organizer = TestData.CreateUser(1);
    private readonly User attendee = TestData.CreateUser(2);
    private readonly InMemoryEventRepository events = new();

    [Fact]
    public async Task GetBudget_ReturnsPlannedBudgetBeforeCollectionOpens()
    {
        var meetup = AddEvent(openCollection: false);

        var budget = await new CostService(events).GetBudgetAsync(meetup.Id, attendee.Id);

        Assert.Equal(100, budget.Budget);
        Assert.Equal(100, budget.RemainingBudget);
        Assert.Equal(0, budget.CollectedBudget);
        Assert.Equal(0, budget.SpentAmount);
        Assert.Empty(budget.Debts);
        Assert.Empty(budget.Costs);
        Assert.Equal(0, events.SaveCount);
    }

    [Fact]
    public async Task GetBudget_ReturnsCollectedFundsExpensesAndRemainingPlannedBudget()
    {
        var meetup = AddEvent();
        meetup.PayDebt(organizer.Id, 50);
        var food = TestData.SeedCost(meetup, 80);
        var drinks = TestData.SeedCost(meetup, 40, "Drinks");

        var budget = await new CostService(events).GetBudgetAsync(meetup.Id, attendee.Id);

        Assert.Equal(100, budget.Budget);
        Assert.Equal(50, budget.CollectedBudget);
        Assert.Equal(120, budget.SpentAmount);
        Assert.Equal(-20, budget.RemainingBudget);
        Assert.Equal(0, budget.Debts[organizer.Id]);
        Assert.Equal(50, budget.Debts[attendee.Id]);
        Assert.Equal(new[] { food, drinks }, budget.Costs);
        Assert.Equal(0, events.SaveCount);
    }

    [Fact]
    public async Task GetBudget_RejectsOutsider()
    {
        var meetup = AddEvent();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => new CostService(events).GetBudgetAsync(meetup.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateCost_OrganizerAddsExpenseThroughEventChange()
    {
        var meetup = AddEvent();

        await new CostService(events).CreateCostAsync(meetup.Id, new CostCreationModel("Food", 120), organizer.Id);

        var cost = Assert.Single(meetup.Expenses);
        Assert.Equal("Food", cost.Title);
        Assert.Equal(120, cost.Amount);
        Assert.Equal(120, meetup.SpentAmount());
        AssertSavedEventChange(meetup);
    }

    [Fact]
    public async Task UpdateCost_OrganizerChangesOnlyRequestedExpenseThroughEventChange()
    {
        var meetup = AddEvent();
        var food = TestData.SeedCost(meetup, 30);
        var drinks = TestData.SeedCost(meetup, 20, "Drinks");

        await new CostService(events).UpdateCostAsync(meetup.Id, food.Id, new CostUpdateModel(40), organizer.Id);

        Assert.Equal(40, food.Amount);
        Assert.Equal(20, drinks.Amount);
        Assert.Equal(60, meetup.SpentAmount());
        AssertSavedEventChange(meetup);
    }

    [Fact]
    public async Task DeleteCost_OrganizerRemovesOnlyRequestedExpenseThroughEventChange()
    {
        var meetup = AddEvent();
        var food = TestData.SeedCost(meetup, 30);
        var drinks = TestData.SeedCost(meetup, 20, "Drinks");

        await new CostService(events).DeleteCostAsync(meetup.Id, food.Id, organizer.Id);

        Assert.Same(drinks, Assert.Single(meetup.Expenses));
        Assert.Same(food, Assert.Single(events.RemovedCosts));
        Assert.Equal(20, meetup.SpentAmount());
        AssertSavedEventChange(meetup);
    }

    [Fact]
    public async Task CostChanges_RequireOrganizerEvenWhenRequesterIsParticipant()
    {
        var meetup = AddEvent();
        var cost = TestData.SeedCost(meetup, 30);
        var service = new CostService(events);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateCostAsync(meetup.Id, new CostCreationModel("Drinks", 20), attendee.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.UpdateCostAsync(meetup.Id, cost.Id, new CostUpdateModel(40), attendee.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.DeleteCostAsync(meetup.Id, cost.Id, attendee.Id));

        AssertUnchangedCost(meetup, cost, 30);
    }

    [Fact]
    public async Task CostChanges_RejectAllMutationsAfterEventStarts()
    {
        var meetup = AddEvent(started: true);
        var cost = TestData.SeedCost(meetup, 30);
        var service = new CostService(events);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateCostAsync(meetup.Id, new CostCreationModel("Drinks", 20), organizer.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateCostAsync(meetup.Id, cost.Id, new CostUpdateModel(40), organizer.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteCostAsync(meetup.Id, cost.Id, organizer.Id));

        AssertUnchangedCost(meetup, cost, 30);
    }

    [Fact]
    public async Task CreateCost_RejectsClosedCollection()
    {
        var meetup = AddEvent(openCollection: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new CostService(events).CreateCostAsync(meetup.Id, new CostCreationModel("Food", 30), organizer.Id));

        Assert.Empty(meetup.Expenses);
        Assert.Equal(0, events.SaveCount);
    }

    [Fact]
    public async Task CostChanges_RejectNegativeAmountsWithoutSaving()
    {
        var meetup = AddEvent();
        var cost = TestData.SeedCost(meetup, 30);
        var service = new CostService(events);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.CreateCostAsync(meetup.Id, new CostCreationModel("Food", -1), organizer.Id));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.UpdateCostAsync(meetup.Id, cost.Id, new CostUpdateModel(-1), organizer.Id));

        AssertUnchangedCost(meetup, cost, 30);
    }

    [Fact]
    public async Task UpdateAndDeleteCost_RejectCostOutsideRequestedEvent()
    {
        var meetup = AddEvent();
        var otherEvent = AddEvent();
        var cost = TestData.SeedCost(otherEvent, 30);
        var service = new CostService(events);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateCostAsync(meetup.Id, cost.Id, new CostUpdateModel(40), organizer.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.DeleteCostAsync(meetup.Id, cost.Id, organizer.Id));

        Assert.Empty(meetup.Expenses);
        AssertUnchangedCost(otherEvent, cost, 30);
    }

    private Event AddEvent(bool started = false, bool openCollection = true)
    {
        var meetup = TestData.CreateEvent(organizer, DateTime.UtcNow.AddDays(started ? -1 : 1), 100, attendee);
        if (openCollection)
        {
            meetup.OpenCollection();
        }
        events.AddEvent(meetup);
        return meetup;
    }

    private void AssertSavedEventChange(Event meetup)
    {
        Assert.Equal(meetup.Id, Assert.Single(events.EventChangeRequests));
        Assert.Equal(1, events.SaveCount);
    }

    private void AssertUnchangedCost(Event meetup, Cost cost, int amount)
    {
        Assert.Same(cost, Assert.Single(meetup.Expenses));
        Assert.Equal(amount, cost.Amount);
        Assert.Empty(events.RemovedCosts);
        Assert.Equal(0, events.SaveCount);
    }
}
