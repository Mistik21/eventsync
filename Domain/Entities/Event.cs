namespace eventsynс.Domain.Entities;

public class Event
{
    public Guid Id { get; }
    public string Title { get; set; }
    public string Description { get; private set; }
    public string MainImageUrl { get; private set; }
    public DateTime EventStartTime { get; private set; }
    public Location EventLocation { get; private set; } = null!;
    public CollectionStatus CollectionStatus { get; private set; }
    public int CollectedBudget { get; private set; }
    public int Budget { get; private set; }
    public IReadOnlyDictionary<Guid, int> Debts { get; private set; } = new Dictionary<Guid, int>();
    public IReadOnlyList<User> Participants { get; private set; } = new List<User>();
    public Guid Organizer { get; }
    public IReadOnlyList<Item> Items { get; private set; } = new List<Item>();
    public IReadOnlyList<Activity> Activities => activities;
    public IReadOnlyList<Car> Cars { get; }
    public string[] ImageEventUrl { get; }
    public IReadOnlyList<Cost> Expenses => expenses;
    
    private readonly List<Cost> expenses = new();
    private readonly List<Activity> activities = new();

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
        ArgumentOutOfRangeException.ThrowIfNegative(budget);
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
        if (IsOpenCollection()) throw DomainConflict.CreateException();
        Title = title;
    }

    public int SpentAmount()
    {
        return Expenses.Sum(e => e.Amount);
    }

    public void ChangeDescription(string description)
    {
        if (IsOpenCollection()) throw DomainConflict.CreateException();
        Description = description;
    }

    public void ChangeStartTime(DateTime startTime)
    {
        if (IsOpenCollection()) throw DomainConflict.CreateException();
        var shift=startTime-EventStartTime;
        foreach (var activity in activities)
        {
            activity.ChangeTime(activity.АctivityStartTime+shift,activity.АctivityEndTime+shift);
        }
        EventStartTime = startTime;
    }

    public void ChangeLocation(Location location)
    {
        if (IsOpenCollection()) throw DomainConflict.CreateException();
        EventLocation = location;
    }

    public void ChangeBudget(int budget)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(budget);
        if (IsOpenCollection()) throw DomainConflict.CreateException();
        Budget = budget;
    }

    public void ChangeMainImage(string imageUrl)
    {
        if (IsOpenCollection()) throw DomainConflict.CreateException();
        MainImageUrl = imageUrl;
    }

    public void OpenCollection()
    {
        if (IsOpenCollection()) throw DomainConflict.CreateException();
        CollectionStatus = CollectionStatus.Open;
        var result = new Dictionary<Guid, int>();
        var shift = 0;
        foreach (var user in Participants)
        {
            if((Budget-shift) % Participants.Count==0)
                result.Add(user.Id, Budget / Participants.Count);
            else
            {
                result.Add(user.Id, Budget / Participants.Count+1);
                shift++;
            }
                
        }

        Debts = result;
    }

    public bool IsOpenCollection()
    {
        return CollectionStatus == CollectionStatus.Open;
    }

    public void AddImage(string imageUrl)
    {
    }

    public void RemoveImage(string imageUrl)
    {
    }

    public void AddParticipant(User user)
    {
        if (Participants.Contains(user) || IsOpenCollection()) throw DomainConflict.CreateException();
        Participants = Participants.Append(user).ToList();
    }

    public void PayDebt(Guid userId)
    {
        if (!IsOpenCollection()) throw DomainConflict.CreateException();
        CollectedBudget += Debts[userId];
        Debts = new Dictionary<Guid, int>(Debts) { [userId] = 0 };
    }

    public void RemoveParticipant(Guid userId)
    {
    }

    public bool IsParticipant(Guid userId)
    {
        return Participants.Any(x => x.Id == userId);
    }

    public void AddItem(Item item)
    {
        Items = Items.Append(item).ToList();
    }

    public void RemoveItem(Guid itemId)
    {
        Items = Items.Where(item => item.Id != itemId).ToList();
    }

    public void AddActivity(Activity activity)
    {
        activities.Add(activity);
    }

    public void RemoveActivity(Guid activityId)
    {
        activities.RemoveAll(activity => activity.Id == activityId);
    }

    public void AddCar(Car car)
    {
    }

    public void RemoveCar(Guid carId)
    {
    }

    public bool IsOrganizer(Guid userId)
    {
        return userId == Organizer;
    }

    public void AddCost(Cost cost)
    {
        if (!IsOpenCollection()) throw DomainConflict.CreateException();
        expenses.Add(cost);
    }

    public void RemoveCost(Guid costId)
    {
        expenses.RemoveAll(cost => cost.Id == costId);
    }

    public Item GetRequiredItem(Guid itemId)
    {
        return Items.SingleOrDefault(item => item.Id == itemId) ?? throw new KeyNotFoundException();
    }
    public Cost GetRequiredCost(Guid costId)
    {
        return Expenses.SingleOrDefault(cost => cost.Id == costId) ?? throw new KeyNotFoundException();
    }
    public Activity GetRequiredActivity(Guid activityId)
    {
        return Activities.SingleOrDefault(activity => activity.Id == activityId) ?? throw new KeyNotFoundException();
    }
}
