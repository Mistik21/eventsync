namespace eventsynс.Domain.Entities;

public class Activity
{
    public Guid Id { get; }
    public string Title {get; private set;}
    public string Description {get; private set;}
    public Guid Creator{ get; }
    public DateTime АctivityStartTime {get; private set;}
    public DateTime АctivityEndTime {get; private set;}
    public int? MaxParticipants  {get; private set;}
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

    public void ChangeTitle(string title)
    {
        Title = title;
    }

    public void ChangeDescription(string description)
    {
        Description = description;
    }

    public void ChangeTime(DateTime startTime, DateTime endTime)
    {
        АctivityStartTime=startTime;
        АctivityEndTime=endTime;
    }

    public void ChangeMaxParticipants(int? maxParticipants)
    {
        if (maxParticipants == null)
        {
            MaxParticipants = int.MaxValue;
        }
        else
        {
            MaxParticipants = maxParticipants;
        }
        if(maxParticipants <= 0 || maxParticipants<Participants.Count) throw new ArgumentOutOfRangeException();
    }

    public void AddParticipant(User user)
    {
        if (!HasAvailableSeats() || IsParticipant(user.Id)) throw DomainConflict.CreateException();
        participants.Add(user);
    }
    public void RemoveParticipant(Guid userId){}

    public bool IsParticipant(Guid userId)
    {
        return participants.Any(user => user.Id == userId);
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