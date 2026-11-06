namespace Jobsy.Web.Pwa;

/// <summary>Coordinates the global PWA install sheet (rendered on <c>app-shell</c>, not inside the header).</summary>
public sealed class PwaInstallSheetState
{
    public bool IsOpen { get; private set; }
    public PwaEntryPlatform Platform { get; private set; }

    public event Action? Changed;

    public void Open(PwaEntryPlatform platform)
    {
        Platform = platform;
        IsOpen = true;
        Changed?.Invoke();
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        Changed?.Invoke();
    }
}
