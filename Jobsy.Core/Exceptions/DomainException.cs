namespace Jobsy.Core.Exceptions;

/// <summary>Business-rule rejection safe to surface (sanitized) to API clients.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
