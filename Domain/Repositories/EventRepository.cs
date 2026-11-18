using eventsynс.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eventsynс.Domain.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext db;
    
}