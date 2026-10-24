namespace Api;

public sealed class ArenaException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
