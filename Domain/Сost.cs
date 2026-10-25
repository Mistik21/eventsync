namespace Domain;

public class Cost
{
    public Guid Id { get; }
    public string Title {get; }
    public int  Amount {get; }

    public void ChangeTitle(string title){}
    public void ChangeAmount(int amount){}
}