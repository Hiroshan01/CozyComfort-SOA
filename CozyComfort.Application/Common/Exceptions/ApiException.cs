namespace CozyComfort.Application.Common.Exceptions;

public sealed class ApiException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
