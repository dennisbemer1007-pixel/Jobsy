namespace Jobsy.Core.Options;

/// <summary>Lobsy Golf 2 pilot (Westland). All toggles default off until appsettings enables them.</summary>
public sealed class Golf2WestlandOptions
{
    public const string SectionName = "Golf2Westland";

    public bool Enabled { get; set; }

    public bool ConversationSheet { get; set; }

    public bool OutsideWork { get; set; }

    public bool FourTestsFeedback { get; set; }

    public bool TaskPicker { get; set; }

    public bool PilotReporting { get; set; }
}
