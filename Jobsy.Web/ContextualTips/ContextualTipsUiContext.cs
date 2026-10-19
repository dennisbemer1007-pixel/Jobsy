using Jobsy.Core.Interfaces;

namespace Jobsy.Web.ContextualTips;

public sealed class ContextualTipsUiContext
{
    public required bool PlatformEnabled { get; init; }

    public ContextualTipsStateDto? State { get; init; }

    public Func<string, bool> IsTipVisible { get; init; } = _ => false;

    public Func<Task>? RefreshAsync { get; init; }

    public Func<string, Task>? HideTipAsync { get; init; }

    public Func<Task>? RestoreHiddenAsync { get; init; }

    public Func<UpdateContextualTipsSettingsRequest, Task>? UpdateSettingsAsync { get; init; }
}
