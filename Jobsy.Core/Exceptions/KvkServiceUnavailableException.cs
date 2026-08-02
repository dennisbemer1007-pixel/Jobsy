namespace Jobsy.Core.Exceptions;

/// <summary>
/// Raised when the external KVK API is unreachable, times out, or returns a transient error.
/// Callers should offer a deferred-verification (pending) registration path.
/// </summary>
public sealed class KvkServiceUnavailableException : Exception
{
    public KvkServiceUnavailableException()
        : base("KVK-dienst is tijdelijk niet beschikbaar.")
    {
    }

    public KvkServiceUnavailableException(string message)
        : base(message)
    {
    }

    public KvkServiceUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
