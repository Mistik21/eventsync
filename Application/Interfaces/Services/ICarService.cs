using eventsynс.Application.Models;
using eventsynс.Application.Models.Car;
using eventsynс.Domain;

namespace eventsynс.Application.Interfaces.Services;

public interface ICarService
{
    Task<List<Car>> GetCarsAsync(Guid eventId, Guid currentUserId);
    Task<Car> GetCarAsync(Guid carId, Guid currentUserId);

    Task<Guid> CreateCarAsync(Guid eventId, CarCreationModel data, Guid currentUserId);

    Task UpdateCarAsync(Guid carId, CarUpdateModel data, Guid currentUserId);

    Task DeleteCarAsync(Guid carId, Guid currentUserId);

    Task JoinCarAsync(Guid carId, Guid currentUserId);
    Task LeaveCarAsync(Guid carId, Guid currentUserId);
}