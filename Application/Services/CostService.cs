using eventsynс.Application.Interfaces.Services;
using eventsynс.Application.Models;
using eventsynс.Application.Models.Cost;
using eventsynс.Domain.Entities;
using eventsynс.Domain.Repositories;

namespace eventsynс.Application.Services;

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
        if (!eventDetails.IsParticipant(currentUserId)) throw new UnauthorizedAccessException();
        return new BudgetData(
            eventDetails.Budget,
            eventDetails.CollectedBudget,
            eventDetails.SpentAmount(),
            eventDetails.Budget - eventDetails.SpentAmount(),
            eventDetails.Debts,
            eventDetails.Expenses.ToList()
        );
    }

    public async Task CreateCostAsync(Guid eventId, CostCreationModel data, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        if (!eventDetails.IsOrganizer(currentUserId)) throw new UnauthorizedAccessException();
        var cost = new Cost(
            Guid.Empty,
            data.Title,
            data.Amount);
        eventDetails.AddCost(cost);
        await eventRepository.SavedAsync();
    }

    public async Task UpdateCostAsync(Guid eventId,Guid costId, CostUpdateModel data, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        if (!eventDetails.IsOrganizer(currentUserId)) throw new UnauthorizedAccessException();
        var cost = eventDetails.GetRequiredCost(costId);
        cost.ChangeAmount(data.Amount);
        await eventRepository.SavedAsync();
    }

    public async Task DeleteCostAsync(Guid eventId, Guid costId, Guid currentUserId)
    {
        var eventDetails = await eventRepository.GetEventByIdAsync(eventId);
        if (!eventDetails.IsOrganizer(currentUserId)) throw new UnauthorizedAccessException();
        var cost = eventDetails.GetRequiredCost(costId);
        eventDetails.RemoveCost(costId);
        eventRepository.RemoveCost(cost);
        await eventRepository.SavedAsync();
    }
}