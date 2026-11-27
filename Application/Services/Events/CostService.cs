using eventsynс.Application.Access;
using eventsynс.Application.Interfaces.Events;
using eventsynс.Application.Models;
using eventsynс.Application.Models.Cost;
using eventsynс.Domain;
using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories.Interfaces;

namespace eventsynс.Application.Services.Events;

public class CostService : ICostService
{
    private readonly IEventRepository eventRepository;

    public CostService(IEventRepository eventRepository)
    {
        this.eventRepository = eventRepository;
    }

    public async Task<BudgetData> GetBudgetAsync(Guid eventId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        EventAccess.RequireParticipant(eventDetails, currentUserId);
        return new BudgetData(
            eventDetails.Budget,
            eventDetails.CollectedBudget,
            eventDetails.SpentAmount(),
            eventDetails.Budget - eventDetails.SpentAmount(),
            eventDetails.Debts,
            eventDetails.Expenses.ToList()
        );
    }

    public Task CreateCostAsync(Guid eventId, CostCreationModel data, Guid currentUserId)
    {
        return eventRepository.SaveEventChangesAsync(eventId, eventDetails =>
        {
            EventAccess.RequireOrganizer(eventDetails, currentUserId);
            var cost = new Cost(
                Guid.Empty,
                data.Title,
                data.Amount);
            eventDetails.AddCost(cost);
            return Task.CompletedTask;
        });
    }

    public Task UpdateCostAsync(Guid eventId,Guid costId, CostUpdateModel data, Guid currentUserId)
    {
        return eventRepository.SaveEventChangesAsync(eventId, eventDetails =>
        {
            EventAccess.RequireOrganizer(eventDetails, currentUserId);
            if (eventDetails.EventStartTime <= DateTime.UtcNow) throw new InvalidOperationException();
            var cost = eventDetails.GetRequiredCost(costId);
            cost.ChangeAmount(data.Amount);
            return Task.CompletedTask;
        });
    }

    public Task DeleteCostAsync(Guid eventId, Guid costId, Guid currentUserId)
    {
        return eventRepository.SaveEventChangesAsync(eventId, eventDetails =>
        {
            EventAccess.RequireOrganizer(eventDetails, currentUserId);
            var cost = eventDetails.GetRequiredCost(costId);
            eventDetails.RemoveCost(costId);
            eventRepository.RemoveCost(cost);
            return Task.CompletedTask;
        });
    }
}
