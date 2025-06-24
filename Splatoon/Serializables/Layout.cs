using ECommons.ExcelServices;
using Splatoon.Serializables;
using Splatoon.Structures;
using System.ComponentModel;

namespace Splatoon;

[Serializable]
public class Layout
{
    [NonSerialized] public static string[] DisplayConditions = { };
    [NonSerialized] public uint LastDisplayFrame = 0;
    [NonSerialized] public bool? ConditionalStatus = null;
    [DefaultValue(true)] public bool Enabled = true;
    [DefaultValue("")] public string Name = "";
    public InternationalString InternationalName = new();
    [DefaultValue("")] public string Description = "";
    public InternationalString InternationalDescription = new();
    [DefaultValue("")] public string Group = "";
    [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
    public HashSet<ushort> ZoneLockH = [];
    public HashSet<int> Scenes = [];
    [DefaultValue(false)] public bool IsZoneBlacklist = false;
    [DefaultValue(false)] public bool ConditionalAnd = false;
    public List<LayoutSubconfiguration> Subconfigurations = [];
    public Guid SelectedSubconfigurationID = Guid.Empty;

    /// <summary>
    /// 0: Always shown <br />
    /// 1: Only in combat <br />
    /// 2: Only in instance <br />
    /// 3: Only in combat AND instance <br />
    /// 4: Only in combat OR instance <br />
    /// 5: Never <br />
    /// 6: Outside of combat <br />
    /// 7: Outside of instance <br />
    /// 8: Outside of combat AND instance <br />
    /// 9: Outside of combat OR instance
    /// </summary>
    [DefaultValue(0)] public int DCond = 0;
    [DefaultValue(false)] public bool DisableDisabling = false;
    [Obsolete("Use JobLockH")][DefaultValue(0)] public ulong JobLock = 0;
    public HashSet<Job> JobLockH = [];
    [DefaultValue(false)] public bool DisableInDuty = false;
    [DefaultValue(false)] public bool UseTriggers = false;
    /// <summary>
    /// 0: Unchanged |
    /// -1: Hidden |
    /// 1: Shown
    /// </summary>
    [NonSerialized] public int TriggerCondition = 0;
    [DefaultValue(0f)] public float MinDistance = 0f;
    [DefaultValue(0f)] public float MaxDistance = 0f;
    [DefaultValue(false)] public bool UseDistanceLimit = false;
    [DefaultValue(false)] public bool DistanceLimitMyHitbox = false;
    [DefaultValue(false)] public bool DistanceLimitTargetHitbox = false;
    /// <summary>
    /// 0: To target | 1: To object
    /// </summary>
    [DefaultValue(0)] public int DistanceLimitType = 0;
    [DefaultValue(0)] public int Phase = 0;
    [DefaultValue(false)] public bool Freezing = false;
    [DefaultValue(0.1f)] public float FreezeFor = 0.1f;
    [DefaultValue(10f)] public float IntervalBetweenFreezes = 10f;
    [DefaultValue(true)] public bool FreezeResetCombat = true;
    [DefaultValue(true)] public bool FreezeResetTerr = true;
    [DefaultValue(0f)] public float FreezeDisplayDelay = 0f;
    [Obsolete] public Dictionary<string, Element> Elements = []; //never delete
    public List<Trigger> Triggers = [];
    public List<Element> ElementsL = [];
    [NonSerialized] internal FreezeInfo FreezeInfo = new();

    public bool IsVisible() => LastDisplayFrame == P.FrameCounter;

    public List<Element> GetElementsWithSubconfiguration()
    {
        if(Subconfigurations.Count == 0 || SelectedSubconfigurationID == Guid.Empty) return ElementsL;
        for(var i = 0; i < Subconfigurations.Count; i++)
        {
            if(Subconfigurations[i].Guid == SelectedSubconfigurationID)
            {
                return Subconfigurations[i].Elements;
            }
        }
        return ElementsL;
    }

    public bool ShouldSerializeJobLockH() => JobLockH.Count > 0;
    public bool ShouldSerializeInternationalDescription() => !InternationalDescription.IsEmpty();
    public bool ShouldSerializeInternationalName() => !InternationalName.IsEmpty();
    public bool ShouldSerializeScenes() => Scenes.Count > 0;
    public bool ShouldSerializeIntervalBetweenFreezes() => Freezing;
    public bool ShouldSerializeFreezeResetCombat() => Freezing;
    public bool ShouldSerializeFreezeResetTerr() => Freezing;
    public bool ShouldSerializeFreezeFor() => Freezing;
    public bool ShouldSerializeMinDistance() => UseDistanceLimit;
    public bool ShouldSerializeMaxDistance() => UseDistanceLimit;
    public bool ShouldSerializeDistanceLimitMyHitbox() => UseDistanceLimit;
    public bool ShouldSerializeDistanceLimitTargetHitbox() => UseDistanceLimit;
    public bool ShouldSerializeDistanceLimitType() => UseDistanceLimit;
    public bool ShouldSerializeZoneLockH() => ZoneLockH.Count > 0;
    public bool ShouldSerializeTriggers() => UseTriggers && Triggers.Count > 0;
    public bool ShouldSerializeElements() => false;
    public bool ShouldSerializeSubconfigurations() => Subconfigurations.Count != 0;
    public bool ShouldSerializeSelectedSubconfigurationID() => ShouldSerializeSubconfigurations();
}
