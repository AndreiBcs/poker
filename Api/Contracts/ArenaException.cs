namespace Api.Contracts;

public sealed class ArenaException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
