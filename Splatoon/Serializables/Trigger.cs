using System.ComponentModel;

namespace Splatoon;

[Serializable]
public class Trigger
{
    [NonSerialized] public static string[] Types = { "Show at time in combat", "Hide at time in combat", "Show at log message", "Hide at log message" };
    [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
    /// <summary>
    /// 0: Show at time |
    /// 1: Hide at time |
    /// 2: Show at text |
    /// 3: Hide at text
    /// </summary>
    [DefaultValue(0)] public int Type = 0;
    [DefaultValue(0f)] public float TimeBegin = 0;
    [DefaultValue(0f)] public float Duration = 0;
    [DefaultValue("")] public string Match = "";
    public InternationalString MatchIntl = new();
    [DefaultValue(0f)] public float MatchDelay = 0;
    [DefaultValue(true)] public bool ResetOnCombatExit = true;
    [DefaultValue(true)] public bool ResetOnTChange = true;
    [DefaultValue(false)] public bool FireOnce = false;
    /// <summary>
    /// 0: not fired |
    /// 1: fired but not ended |
    /// 2: fired and ended
    /// </summary>
    [NonSerialized] public int FiredState = 0;
    [NonSerialized] public List<long> EnableAt = new();
    [NonSerialized] public List<long> DisableAt = new();
    [NonSerialized] internal bool Disabled = false;
    [DefaultValue(false)] public bool IsRegex = false;

    public bool ShouldSerializeMatchIntl()
    {
        return !MatchIntl.IsEmpty();
    }
}
