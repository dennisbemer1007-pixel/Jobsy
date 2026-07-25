namespace Jobsy.Core.Exceptions;

/// <summary>
/// Thrown from spend callbacks when vacancy state changed concurrently; triggers ledger rollback.
/// </summary>
public sealed class VacancyProductConflictException : InvalidOperationException
{
    public VacancyProductConflictException(string message) : base(message)
    {
    }
}
