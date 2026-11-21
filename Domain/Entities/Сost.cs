namespace eventsynс.Domain.Entities;

public class Cost
{
    public Guid Id { get; }
    public string Title {get; }
    public int  Amount {get; }

    public Cost(Guid id, string title, int amount)
    {
        Id = id;
        Title = title;
        Amount = amount;
    }
    public void ChangeTitle(string title){}
    public void ChangeAmount(int amount){}
}