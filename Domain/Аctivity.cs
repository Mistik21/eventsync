namespace eventsynс.Domain;

public class Аctivity
{
    public Guid Id { get; }
    public string Title {get; }
    public string Description {get; }
    public User Creator{ get; }
    public DateTime АctivityStartTime {get; }
    public DateTime АctivityEndTime {get; }
    public int MaxParticipants  {get; }
    public IReadOnlyList<User> Participants {get; }
    
    public void ChangeTitle(string title){}
    public void ChangeDescription(string description){}
    public void ChangeTime(DateTime startTime, DateTime endTime){}
    public void ChangeMaxParticipants(int maxParticipants){}
    public void AddParticipant(User user){}
    public void RemoveParticipant(Guid userId){}

    public bool IsParticipant(Guid userId)
    {
        return true;
    }

    public bool IsCreator(Guid userId)
    {
        return true;
    }

    public bool HasAvailableSeats()
    {
        return true;
    }
}