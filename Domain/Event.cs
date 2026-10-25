namespace eventsynс.Domain;

public class Event
{
    public Guid Id { get; }
    public string Title {get; }
    public string Description {get;}
    public string MainImageUrl { get;  }
    public DateTime EventStartTime {get; }
    public Location EventLocation {get;}
    public CollectionStatus CollectionStatus { get; }
    public int CollectedBudget { get; }
    public int Budget {get; }
    public IReadOnlyDictionary<Guid, int> Debts {get; }
    public IReadOnlyList<User> Participants {get; }
    public User Organizer {get; }
    public IReadOnlyList<Item> Items {get; }
    public IReadOnlyList<Аctivity> Аctivities {get; }
    public IReadOnlyList<Car> Cars {get; }
    public string[] ImageEventUrl { get;  }
    public IReadOnlyList<Cost> Expenses {get; }
    
    public void ChangeTitle(string title){}
    public void ChangeDescription(string description){}
    public void ChangeStartTime(DateTime startTime){}
    public void ChangeLocation(Location location){}
    public void ChangeBudget(int budget){}
    public void ChangeMainImage(string imageUrl){}
    public void OpenCollection(){}
    public void AddImage(string imageUrl){}
    public void RemoveImage(string imageUrl){}
    public void AddParticipant(User user){}
    public void PayDebt(Guid userId){}
    public void RemoveParticipant(Guid userId){}

    public bool IsParticipant(Guid userId)
    {
        return true;
    }
    public void AddItem(Item item){}
    public void RemoveItem(Guid itemId){}
    public void AddActivity(Аctivity activity){}
    public void RemoveActivity(Guid activityId){}
    public void AddCar(Car car){}
    public void RemoveCar(Guid carId){}
    public void AddDebt(Guid userId, int amount){}
    public void ReduceDebt(Guid userId, int amount){}

    public int GetDebt(Guid userId)
    {
        return 1;
    }

    public bool IsOrganizer(Guid userId)
    {
        return true;
    }
    public void AddDebtToAll(int amount){}
    public void AddCost(Cost cost){}
    public void RemoveCost(Guid costId){}
}