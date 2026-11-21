namespace eventsynс.Domain.Entities;

public class Event
{
    private readonly List<Cost> expenses = new();

    public Guid Id { get; }
    public string Title { get; set; }
    public string Description {get;}
    public string MainImageUrl { get;  }
    public DateTime EventStartTime {get; }
    public Location EventLocation { get; private set; } = null!;
    public CollectionStatus CollectionStatus { get; private set; }
    public int CollectedBudget { get; private set; }
    public int Budget {get; }
    public IReadOnlyDictionary<Guid, int> Debts { get; private set; } = new Dictionary<Guid, int>();
    public IReadOnlyList<User> Participants { get; private set; } = new List<User>();
    public Guid Organizer {get; }
    public IReadOnlyList<Item> Items {get; }
    public IReadOnlyList<Activity> Activities {get; }
    public IReadOnlyList<Car> Cars {get; }
    public string[] ImageEventUrl { get;  }
    public IReadOnlyList<Cost> Expenses => expenses;

    public Event(
        Guid id,
        string title,
        string description,
        string? mainImageUrl,
        DateTime eventStartTime,
        Location eventLocation,
        int budget,
        Guid organizer)
        : this(id, title, description, mainImageUrl, eventStartTime, budget, organizer)
    {
        EventLocation = eventLocation;
    }

    private Event(
        Guid id,
        string title,
        string description,
        string? mainImageUrl,
        DateTime eventStartTime,
        int budget,
        Guid organizer)
    {
        Id = id;
        Title = title;
        Description = description;
        MainImageUrl = mainImageUrl;
        EventStartTime = eventStartTime;
        Budget = budget;
        Organizer = organizer;
    }
    public void ChangeTitle(string title)
    {
        Title = title; 
    }

    public int SpentAmount()
    {
        return Expenses.Sum(e => e.Amount);
    }
    public void ChangeDescription(string description){}
    public void ChangeStartTime(DateTime startTime){}
    public void ChangeLocation(Location location){}
    public void ChangeBudget(int budget){}
    public void ChangeMainImage(string imageUrl){}

    public void OpenCollection()
    {
        CollectionStatus = CollectionStatus.Open;
        var result = new Dictionary<Guid, int>();
        foreach (var user in Participants)
        {
            result.Add(user.Id, Budget/Participants.Count);
        }

        Debts = result;
    }

    public bool IsOpenCollection()
    {
        return CollectionStatus == CollectionStatus.Open;
    }
    public void AddImage(string imageUrl){}
    public void RemoveImage(string imageUrl){}

    public void AddParticipant(User user)
    {
        Participants = Participants.Append(user).ToList();
    }
    public void PayDebt(Guid userId)
    {
        CollectedBudget+=Debts[userId];
        Debts = new Dictionary<Guid, int>(Debts) { [userId] = 0 };
    }
    public void RemoveParticipant(Guid userId){}

    public bool IsParticipant(Guid userId)
    {
        return Participants.Any(x => x.Id == userId);
    }
    public void AddItem(Item item){}
    public void RemoveItem(Guid itemId){}
    public void AddActivity(Activity activity){}
    public void RemoveActivity(Guid activityId){}
    public void AddCar(Car car){}
    public void RemoveCar(Guid carId){}
    public void ReduceDebt(Guid userId, int amount){}

    public bool IsOrganizer(Guid userId)
    {
        return userId==Organizer;
    }
    public void AddCost(Cost cost)
    {
        expenses.Add(cost);
    }
    public void RemoveCost(Guid costId){}
}
