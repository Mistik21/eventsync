namespace eventsynс.Domain.Entities;

public class Location
{
    public string Address {get; }
    public string Coordinates {get; }

    public Location(string address, string coordinates)
    {
        Address = address;
        Coordinates = coordinates;
    }
}
