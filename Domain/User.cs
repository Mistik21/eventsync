

namespace eventsynс.Domain;

public class User
{
    public Guid id { get;}
    public long TelegramID;
    public string Name {get; }
    public int AttendedEventCount {get; }
    
    public void ChangeName(string name){}
    public void IncrementAttendedEventCount(){}

    public bool IsTelegramUser(long telegramId)
    {
        return true;
    }
}