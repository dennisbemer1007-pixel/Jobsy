namespace Jobsy.Web.Services;

/// <summary>
/// Result of a candidate "me/*" GET. Transient API failures become
/// <see cref="TemporarilyUnavailable"/> instead of throwing into the circuit.
/// </summary>
public readonly record struct MeGetResult<T>(T? Value, bool TemporarilyUnavailable)
{
    public static MeGetResult<T> Ok(T? value) => new(value, false);

    public static MeGetResult<T> Unavailable() => new(default, true);

    public bool HasValue => Value is not null && !TemporarilyUnavailable;
}
