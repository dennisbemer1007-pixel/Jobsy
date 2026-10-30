using Jobsy.Web.Components.Layout;

namespace Jobsy.Web.Scholen;

/// <summary>Per-circuit shell choice for <c>/leerling/reis</c> (classic vs Zee-expeditie mockup).</summary>
public sealed class LeerlingReisShellState
{
    private LeerlingReisShellMode _mode = LeerlingReisShellMode.Classic;

    public event Action? Changed;

    public LeerlingReisShellMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value)
            {
                return;
            }

            _mode = value;
            Changed?.Invoke();
        }
    }
}
