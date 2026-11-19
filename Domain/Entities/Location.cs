namespace eventsynс.Domain.Entities;

public class Location
{
    public string Address {get; }
    public string Coordinates {get; }
    
    private Location() { }

    public Location(string address, string coordinates)
    {
        Address = address;
        Coordinates = coordinates;
    }
}