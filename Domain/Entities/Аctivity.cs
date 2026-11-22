namespace eventsynс.Domain.Entities;

public class Activity
{
    public Guid Id { get; }
    public string Title {get; }
    public string Description {get; }
    public Guid Creator{ get; }
    public DateTime АctivityStartTime {get; }
    public DateTime АctivityEndTime {get; }
    public int? MaxParticipants  {get; }
    public IReadOnlyList<User> Participants => participants;
    private readonly List<User> participants = new();

    public Activity(
        Guid id, 
        string title, 
        string description,
        Guid creator, 
        DateTime аctivityStartTime,
        DateTime аctivityEndTime, 
        int? maxParticipants)
    {
        Id = id;
        Title = title;
        Description = description;
        Creator = creator;
        АctivityStartTime = аctivityStartTime;
        АctivityEndTime = аctivityEndTime;
        if (maxParticipants == null)
        {
            MaxParticipants = int.MaxValue;
        }
        else
        {
            MaxParticipants = maxParticipants;
        }
        if (maxParticipants <= 0) throw new ArgumentOutOfRangeException();
    }
    
    public void ChangeTitle(string title){}
    public void ChangeDescription(string description){}
    public void ChangeTime(DateTime startTime, DateTime endTime){}
    public void ChangeMaxParticipants(int maxParticipants){}

    public void AddParticipant(User user)
    {
        if (!HasAvailableSeats()) throw DomainConflict.CreateException();
        participants.Add(user);
    }
    public void RemoveParticipant(Guid userId){}

    public bool IsParticipant(Guid userId)
    {
        return true;
    }

    public bool IsCreator(Guid userId)
    {
        return Creator==userId;;
    }

    public bool HasAvailableSeats()
    {
        return MaxParticipants > Participants.Count;
    }
}