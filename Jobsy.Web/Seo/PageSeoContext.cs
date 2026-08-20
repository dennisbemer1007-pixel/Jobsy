namespace Jobsy.Web.Seo;

/// <summary>Circuit-scoped overlay so a page can replace catalog metadata without duplicate tags.</summary>
public sealed class PageSeoContext
{
    private int _version;

    public PageSeoOverride? Current { get; private set; }

    public event Action? Changed;

    public int Set(PageSeoOverride overlay)
    {
        Current = overlay;
        var version = Interlocked.Increment(ref _version);
        Changed?.Invoke();
        return version;
    }

    public void Clear(int version)
    {
        if (version != Volatile.Read(ref _version))
        {
            return;
        }

        Current = null;
        Changed?.Invoke();
    }
}
