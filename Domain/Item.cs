namespace eventsynс.Domain;

public class Item
{
    public Guid Id { get; }
    public string Title {get; }
    public string Description {get; }
    public int Count {get; }
    public Guid? ResponsibleID {get; }
    public string? UrlShop {get; }
    
    public void ChangeTitle(string title){}
    public void ChangeDescription(string description){}
    public void ChangeCount(int count){}
    public void ChangeUrlShop(string? url){}
    public void AssignTo(Guid userId){}

    public bool IsResponsible(Guid userId)
    {
        return true;
    }
}