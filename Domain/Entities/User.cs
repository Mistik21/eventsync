namespace eventsynс.Domain.Entities;

public class User
{
    public Guid Id { get; }
    public long TelegramID { get; }
    public string Name { get; private set; } = null!;

    private User()
    {
    }
    public User(Guid id,long telegramId, string name)
    {
        Id = id;
        TelegramID = telegramId;
        Name = name;
    }
    
}
