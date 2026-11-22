namespace eventsynс.Domain.Entities;

public class User
{
    public Guid Id { get; }
    public long TelegramID { get; }
    public string Name { get; private set; }
    public int AttendedEventCount { get; private set; }

    private User()
    {
    }
    public User(Guid id,long telegramId, string name, int attendedEventCount)
    {
        Id = id;
        TelegramID = telegramId;
        Name = name;
        AttendedEventCount = attendedEventCount;
    }

    public void IncrementAttendedEventCount()
    {
        AttendedEventCount++;
    }

    public void DecrementAttendedEventCount()
    {
        AttendedEventCount = Math.Max(0, AttendedEventCount - 1);
    }
    
}
