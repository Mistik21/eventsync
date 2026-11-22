namespace eventsynс.Domain;

internal static class DomainConflict
{
    private const string Marker = "EventSync.DomainConflict";

    public static InvalidOperationException CreateException()
    {
        var exception = new InvalidOperationException();
        exception.Data[Marker] = true;
        return exception;
    }

    public static bool IsConflict(InvalidOperationException exception)
    {
        return exception.Data[Marker] is true;
    }
}
