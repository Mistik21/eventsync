using System.Security.Claims;
using eventsynс.Application.Models.Cost;
using eventsynс.Application.Services;
using eventsynс.Domain.Entities;
using eventsynс.UI.Controllers;
using eventsynс.UI.DTOs.Requests;
using eventsynс.UI.DTOs.Requests.Costs;
using eventsynс.UI.DTOs.Requests.Events;
using eventsynс.UI.DTOs.Responses;
using EventSync.UnitTests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace EventSync.UnitTests.UI;

public class BudgetControllerTests
{
    private readonly User organizer = TestData.CreateUser(1);
    private readonly User attendee = TestData.CreateUser(2);
    private readonly InMemoryEventRepository events = new();

    [Fact]
    public async Task CreateAndUpdateEvent_PreserveBudgetFromRequests()
    {
        var controller = EventsControllerFor(organizer.Id);
        var location = new LocationRequest("Park", "56.84,60.61");
        var create = new CreateEventRequest("Picnic", "", TestData.EventStart, location, 100, null);

        Assert.IsType<OkResult>(await controller.CreateEvent(create));
        var meetup = Assert.Single(events.Events);
        Assert.Equal(100, meetup.Budget);

        var update = new UpdateEventRequest("Picnic", "", TestData.EventStart, location, 120, null);

        Assert.IsType<OkResult>(await controller.UpdateEvent(update, meetup.Id));
        Assert.Equal(120, meetup.Budget);
    }

    [Fact]
    public async Task GetBudgetAndGetEvent_ReturnBudgetSummaryIncludingOverspend()
    {
        var meetup = AddEvent();
        meetup.OpenCollection();
        meetup.PayDebt(organizer.Id, 50);
        var cost = TestData.SeedCost(meetup, 125);

        var budgetResult = Assert.IsType<OkObjectResult>(await CostsControllerFor(attendee.Id).GetBudget(meetup.Id));
        var budget = Assert.IsType<BudgetResponse>(budgetResult.Value);
        var eventResult = Assert.IsType<OkObjectResult>(await EventsControllerFor(attendee.Id).GetEvent(meetup.Id));
        var eventResponse = Assert.IsType<EventResponse>(eventResult.Value);

        Assert.Equal(100, budget.Budget);
        Assert.Equal(50, budget.CollectedBudget);
        Assert.Equal(125, budget.SpentAmount);
        Assert.Equal(-25, budget.RemainingBudget);
        Assert.Equal(new CostResponse(cost.Id, cost.Title, cost.Amount), Assert.Single(budget.Costs));
        Assert.Equal(0, budget.Debts[organizer.Id]);
        Assert.Equal(50, budget.Debts[attendee.Id]);
        Assert.Equal(budget.Budget, eventResponse.Budget);
        Assert.Equal(budget.RemainingBudget, eventResponse.RemainingBudget);
        Assert.Equal(CollectionStatus.Open, eventResponse.CollectionStatus);
    }

    [Fact]
    public async Task CreateUpdateAndDeleteCost_ApplyAmountsToRequestedExpense()
    {
        var meetup = AddEvent();
        meetup.OpenCollection();
        var controller = CostsControllerFor(organizer.Id);

        Assert.IsType<OkResult>(await controller.CreateCost(new CreateCostRequest("Food", 40), meetup.Id));
        var cost = Assert.Single(meetup.Expenses);
        Assert.Equal("Food", cost.Title);
        Assert.Equal(40, cost.Amount);

        Assert.IsType<OkResult>(await controller.UpdateCost(new CostUpdateModel(55), meetup.Id, cost.Id));
        Assert.Equal(55, cost.Amount);

        Assert.IsType<OkResult>(await controller.DeleteCost(meetup.Id, cost.Id));
        Assert.Empty(meetup.Expenses);
    }

    [Fact]
    public async Task CollectionActions_OpenCollectionAndAddSupplementalDebt()
    {
        var meetup = AddEvent(started: true);
        TestData.SeedCost(meetup, 120);
        var organizerController = EventsControllerFor(organizer.Id);

        Assert.IsType<OkResult>(await organizerController.OpenCollection(meetup.Id));
        Assert.IsType<OkResult>(await organizerController.AddSupplementalDebts(meetup.Id));

        Assert.Equal(120, meetup.Budget);
        Assert.Equal(0, meetup.CollectedBudget);
        Assert.Equal(60, meetup.Debts[organizer.Id]);
        Assert.Equal(60, meetup.Debts[attendee.Id]);
    }

    [Fact]
    public async Task PayDebt_RecordsTheRequestedAmountForTheAuthenticatedParticipant()
    {
        var meetup = AddEvent();
        meetup.OpenCollection();
        var controller = EventsControllerFor(attendee.Id);

        var result = await controller.PayDebt(new PayDebtRequests(30), meetup.Id);

        Assert.IsType<OkResult>(result);
        Assert.Equal(30, meetup.CollectedBudget);
        Assert.Equal(20, meetup.Debts[attendee.Id]);
        Assert.Equal(50, meetup.Debts[organizer.Id]);
    }

    [Fact]
    public async Task BudgetActions_RejectInvalidUserId()
    {
        var eventId = Guid.NewGuid();
        var costId = Guid.NewGuid();
        var costsController = CostsControllerFor(null);
        var eventsController = EventsControllerFor(null);

        Assert.IsType<UnauthorizedResult>(await costsController.GetBudget(eventId));
        Assert.IsType<UnauthorizedResult>(await costsController.CreateCost(new CreateCostRequest("Food", 40), eventId));
        Assert.IsType<UnauthorizedResult>(await costsController.UpdateCost(new CostUpdateModel(55), eventId, costId));
        Assert.IsType<UnauthorizedResult>(await costsController.DeleteCost(eventId, costId));
        Assert.IsType<UnauthorizedResult>(await eventsController.OpenCollection(eventId));
        Assert.IsType<UnauthorizedResult>(await eventsController.PayDebt(new PayDebtRequests(30), eventId));
        Assert.IsType<UnauthorizedResult>(await eventsController.AddSupplementalDebts(eventId));
        Assert.Empty(events.EventChangeRequests);
    }

    private Event AddEvent(bool started = false)
    {
        var meetup = TestData.CreateEvent(organizer, DateTime.UtcNow.AddDays(started ? -1 : 1), 100, attendee);
        events.AddEvent(meetup);
        return meetup;
    }

    private CostsController CostsControllerFor(Guid? userId) => new(new CostService(events))
    {
        ControllerContext = ContextFor(userId)
    };

    private EventsController EventsControllerFor(Guid? userId) =>
        new(new EventService(new InMemoryUserRepository(organizer, attendee), events))
        {
            ControllerContext = ContextFor(userId)
        };

    private static ControllerContext ContextFor(Guid? userId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId?.ToString() ?? "invalid")], "test"))
        }
    };
}
