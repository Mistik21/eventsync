namespace eventsynс.Domain.Entities;

public class Item
{
    public Guid Id { get; }
    public string Title {get;private set; }
    public string Description {get;private set; }
    public int Count {get;private set; }
    public Guid? ResponsibleID {get; private set; }
    public string? UrlShop {get;private set; }

    public Item(Guid id, string title, string description, int count,string? urlShop)
    {
        Id = id;
        Title = title;
        Description = description;
        Count = count;
        UrlShop = urlShop;
    }

    public void ChangeTitle(string title)
    {
        Title = title;
    }

    public void ChangeDescription(string description)
    {
        Description = description;
    }

    public void ChangeCount(int count)
    {
        if (count < 0)
        {
            throw new Exception();
        }
        Count = count;
    }

    public void ChangeUrlShop(string? url)
    {
        UrlShop = url;
    }

    public void AssignTo(Guid userId)
    {
        ResponsibleID = userId;
    }

    public void UnassignTo()
    {
        ResponsibleID = null;
    }

    public bool IsResponsible(Guid userId)
    {
        return userId == ResponsibleID;
    }
}