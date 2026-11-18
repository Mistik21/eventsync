namespace eventsynс.Domain.Entities;

public class User
{
    public Guid Id { get; }
    public long TelegramID { get; }
    public string Name { get; private set; }
    public int AttendedEventCount { get; }

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

    public void ChangeName(string name)
    {
        Name = name;
    }

    public void IncrementAttendedEventCount()
    {
    }

    public bool IsTelegramUser(long telegramId)
    {
        return true;
    }
}