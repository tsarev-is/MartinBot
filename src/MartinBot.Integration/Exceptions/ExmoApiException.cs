namespace MartinBot.Integration.Exceptions;

public sealed class ExmoApiException : Exception
{
    public ExmoApiException(string message) : base(message) { }
}
