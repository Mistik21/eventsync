namespace eventsynс.Domain.Entities;

public class Car
{
    public Guid Id { get; }
    public Guid Driver { get; }
    public int MaxNumberSeats { get; }
    public string Description {get; }
    public Location LocationEnd {get;}
    public IReadOnlyList<User> Passengers { get; }
    
    
    public int OccupiedSeats => Passengers.Count;

    public int AvailableSeats => MaxNumberSeats - OccupiedSeats;
    
    public void ChangeDescription(string description){}
    public void ChangeLocationEnd(Location location){}
    public void ChangeMaxNumberSeats(int maxSeats){}
    public void AddPassenger(User user){}
    public void RemovePassenger(Guid userId){}

    public bool IsPassenger(Guid userId)
    {
        return true;
    }

    public bool IsDriver(Guid userId)
    {
        return true;
    }
}