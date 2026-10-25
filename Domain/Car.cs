namespace Domain;

public class Car
{
    public Guid Id { get; }
    public User Driver { get; }
    public int AvailableSeats { get; } 
    public int MaxNumberSeats { get; }
    public string Description {get; }
    public Location LocationEnd {get;}
    public IReadOnlyList<User> Passengers { get; }
    
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