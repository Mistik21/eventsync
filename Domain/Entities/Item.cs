namespace eventsynс.Domain.Entities;

public class Item
{
    public Guid Id { get; }
    public string Title {get; }
    public string Description {get; }
    public int Count {get; }
    public Guid? ResponsibleID {get; }
    public string? UrlShop {get; }

    public Item(Guid id, string title, string description, int count,string? urlShop)
    {
        Id = id;
        Title = title;
        Description = description;
        Count = count;
        UrlShop = urlShop;
    }
    
    public void ChangeTitle(string title){}
    public void ChangeDescription(string description){}
    public void ChangeCount(int count){}
    public void ChangeUrlShop(string? url){}
    public void AssignTo(Guid userId){}

    public bool IsResponsible(Guid userId)
    {
        return userId == ResponsibleID;
    }
}