using ECommons.LanguageHelpers;
using Splatoon.RenderEngines;
using Splatoon.Serializables;
using Splatoon.Utility;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Splatoon;

[Serializable]
public class Element
{
    [NonSerialized]
    public static string[] ElementTypes = Array.Empty<string>();
    [NonSerialized] public static string[] ActorTypes = Array.Empty<string>();
    [NonSerialized] public static string[] ComparisonTypes = Array.Empty<string>();
    public static void Init()
    {
        ElementTypes = new string[]{
            "Circle at fixed coordinates".Loc(),
            "Circle relative to object position".Loc(),
            "Line between two fixed coordinates".Loc(),
            "Line relative to object position".Loc(),
            "Cone relative to object position".Loc(),
            "Cone at fixed coordinates".Loc()
        };
        ActorTypes = new string[] {
            "Game object with specific data".Loc(),
            "Self".Loc(),
            "Targeted enemy".Loc()
        };
        ComparisonTypes = new string[]{
            "Name (case-insensitive, partial)".Loc(),
            "Model ID".Loc(),
            "Object ID".Loc(),
            "Data ID".Loc(),
            "NPC ID".Loc(),
            "Placeholder".Loc(),
            "NPC Name ID".Loc(),
            "VFX Path".Loc(),
            "Object Effect".Loc(),
            "Icon ID".Loc()
        };
    }


    public string Name = "";
    [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
    [NonSerialized] internal bool Delete = false;
    /// <summary>
    /// 0: Object at fixed coordinates |
    /// 1: Object relative to actor position | 
    /// 2: Line between two fixed coordinates | 
    /// 3: Line relative to object pos | 
    /// 4: Cone relative to object position |
    /// 5: Cone at fixed coordinates
    /// </summary>
    public int type;
    /// <summary>
    /// 0: Object at fixed coordinates |
    /// 1: Object relative to actor position | 
    /// 2: Line between two fixed coordinates | 
    /// 3: Line relative to object pos | 
    /// 4: Cone relative to object position |
    /// 5: Cone at fixed coordinates
    /// </summary>
    public Element(int t)
    {
        type = t;
    }
    [DefaultValue(true)] public bool Enabled = true;
    [DefaultValue(0f)] public float refX = 0f;
    [DefaultValue(0f)] public float refY = 0f;
    [DefaultValue(0f)] public float refZ = 0f;
    [DefaultValue(0f)] public float offX = 0f;
    [DefaultValue(0f)] public float offY = 0f;
    [DefaultValue(0f)] public float offZ = 0f;
    [DefaultValue(0.35f)] public float radius = 0.35f; // if it's 0, draw it as point, otherwise as circle
    [DefaultValue(0)] public float Donut = 0f;
    [DefaultValue(0)] public int coneAngleMin = 0;
    [DefaultValue(0)] public int coneAngleMax = 0;
    [DefaultValue(0xc80000ff)] public uint color = 0xc80000ff;
    [DefaultValue(true)] public bool Filled = true;
    [DefaultValue(null)] public float? fillIntensity = null;
    [DefaultValue(false)] public bool overrideFillColor = false;
    [DefaultValue(null)] public uint? originFillColor = null;
    [DefaultValue(null)] public uint? endFillColor = null;
    [DefaultValue(0x70000000)] public uint overlayBGColor = 0x70000000;
    [DefaultValue(0xC8FFFFFF)] public uint overlayTextColor = 0xC8FFFFFF;
    [DefaultValue(0f)] public float overlayVOffset = 0f;
    [DefaultValue(1f)] public float overlayFScale = 1f;
    [DefaultValue(false)] public bool overlayPlaceholders = false;
    [DefaultValue(2f)] public float thicc = 2f;
    [DefaultValue("")] public string overlayText = "";
    [DefaultValue("")] public string refActorName = "";
    public InternationalString refActorNameIntl = new();
    [DefaultValue(0)] public uint refActorModelID = 0;
    [DefaultValue(0)] public uint refActorObjectID = 0;
    [DefaultValue(0)] public uint refActorDataID = 0;
    [DefaultValue(0)] public uint refActorNPCID = 0;
    [DefaultValue(0)] public uint refActorTargetingYou = 0;
    [DefaultValue("")] public List<string> refActorPlaceholder = new();
    [DefaultValue(0)] public uint refActorNPCNameID = 0;
    [DefaultValue(0)] public uint refActorNamePlateIconID = 0;
    [DefaultValue(false)] public bool refActorComparisonAnd = false;
    [DefaultValue(false)] public bool refActorRequireCast = false;
    [DefaultValue(false)] public bool refActorCastReverse = false;
    public List<uint> refActorCastId = new();
    [DefaultValue(false)] public bool refActorUseCastTime = false;
    [DefaultValue(0f)] public float refActorCastTimeMin = 0f;
    [DefaultValue(0f)] public float refActorCastTimeMax = 0f;
    [DefaultValue(false)] public bool refActorUseOvercast = false;
    [DefaultValue(false)] public bool refTargetYou = false;
    [DefaultValue(false)] public bool refActorRequireBuff = false;
    public List<uint> refActorBuffId = new();
    [DefaultValue(false)] public bool refActorRequireAllBuffs = false;
    [DefaultValue(false)] public bool refActorRequireBuffsInvert = false;
    [DefaultValue(false)] public bool refActorUseBuffTime = false;
    [DefaultValue(false)] public bool refActorUseBuffParam = false;
    [DefaultValue(0)] public int refActorBuffParam = 0;
    [DefaultValue(0f)] public float refActorBuffTimeMin = 0f;
    [DefaultValue(0f)] public float refActorBuffTimeMax = 0f;
    [DefaultValue(false)] public bool refActorObjectLife = false;
    [DefaultValue(0)] public float refActorLifetimeMin = 0;
    [DefaultValue(0)] public float refActorLifetimeMax = 0;
    /// <summary>
    /// 0: Name |
    /// 1: Model ID |
    /// 2: Object ID |
    /// 3: Data ID | 
    /// 4: NPC ID |
    /// 5: Placeholder |
    /// 6: Name ID | 
    /// 7: VFX Path |
    /// 8: Object Effect
    /// 9: Icon ID
    /// </summary>
    [DefaultValue(0)] public int refActorComparisonType = 0;
    /// <summary>
    /// 0: Game object with specific name |
    /// 1: Self |
    /// 2: Targeted enemy
    /// </summary>
    [DefaultValue(0)] public int refActorType = 0;
    [DefaultValue(false)] public bool includeHitbox = false;
    [DefaultValue(false)] public bool includeOwnHitbox = false;
    [DefaultValue(false)] public bool includeRotation = false;
    [DefaultValue(false)] public bool onlyTargetable = false;
    [DefaultValue(false)] public bool onlyUnTargetable = false;
    [DefaultValue(false)] public bool onlyVisible = false;
    [DefaultValue(false)] public bool tether = false;
    [DefaultValue(0f)] public float ExtraTetherLength = 0f;
    [DefaultValue(LineEnd.None)] public LineEnd LineEndA = LineEnd.None;
    [DefaultValue(LineEnd.None)] public LineEnd LineEndB = LineEnd.None;
    [DefaultValue(0f)] public float AdditionalRotation = 0f;
    [DefaultValue(false)] public bool LineAddHitboxLengthX = false;
    [DefaultValue(false)] public bool LineAddHitboxLengthY = false;
    [DefaultValue(false)] public bool LineAddHitboxLengthZ = false;
    [DefaultValue(false)] public bool LineAddHitboxLengthXA = false;
    [DefaultValue(false)] public bool LineAddHitboxLengthYA = false;
    [DefaultValue(false)] public bool LineAddHitboxLengthZA = false;
    [DefaultValue(false)] public bool LineAddPlayerHitboxLengthX = false;
    [DefaultValue(false)] public bool LineAddPlayerHitboxLengthY = false;
    [DefaultValue(false)] public bool LineAddPlayerHitboxLengthZ = false;
    [DefaultValue(false)] public bool LineAddPlayerHitboxLengthXA = false;
    [DefaultValue(false)] public bool LineAddPlayerHitboxLengthYA = false;
    [DefaultValue(false)] public bool LineAddPlayerHitboxLengthZA = false;
    [DefaultValue(false)] public bool FaceMe = false;
    [DefaultValue(false)] public bool LimitDistance = false;
    [DefaultValue(false)] public bool LimitDistanceInvert = false;
    [DefaultValue(0f)] public float DistanceSourceX = 0f;
    [DefaultValue(0f)] public float DistanceSourceY = 0f;
    [DefaultValue(0f)] public float DistanceSourceZ = 0f;
    [DefaultValue(0f)] public float DistanceMin = 0f;
    [DefaultValue(0f)] public float DistanceMax = 0f;
    [DefaultValue("")] public string refActorVFXPath = "";
    [DefaultValue(0)] public int refActorVFXMin = 0;
    [DefaultValue(0)] public int refActorVFXMax = 0;
    [DefaultValue(false)] public bool LimitRotation = false;
    [DefaultValue(0)] public float RotationMax = 0;
    [DefaultValue(0)] public float RotationMin = 0;
    [DefaultValue(0)] public uint refActorObjectEffectData1 = 0;
    [DefaultValue(0)] public uint refActorObjectEffectData2 = 0;
    [DefaultValue(0)] public int refActorObjectEffectMin = 0;
    [DefaultValue(0)] public int refActorObjectEffectMax = 0;
    [DefaultValue(false)] public bool refActorTether = false;
    [DefaultValue(0)] public float refActorTetherTimeMin = 0;
    [DefaultValue(0)] public float refActorTetherTimeMax = 0;
    [DefaultValue(null)] public int? refActorTetherParam1 = null;
    [DefaultValue(null)] public int? refActorTetherParam2 = null;
    [DefaultValue(null)] public int? refActorTetherParam3 = null;
    [DefaultValue(null)] public bool? refActorIsTetherSource = null;
    [DefaultValue(false)] public bool refActorIsTetherInvert = false;
    [DefaultValue(false)] public bool refActorObjectEffectLastOnly = false;
    [DefaultValue(false)] public bool refActorUseTransformation = false;
    public List<string> refActorTetherConnectedWithPlayer = [];
    [DefaultValue(0)] public int refActorTransformationID = 0;
    [DefaultValue(MechanicType.Unspecified)] public MechanicType mechanicType = MechanicType.Unspecified;
    [DefaultValue(false)] public bool refMark = false;
    [DefaultValue(0)] public int refMarkID = 0;
    [DefaultValue("<1>")] public string faceplayer = "<1>";
    [DefaultValue(0.5f)] public float FillStep = 0.5f;
    [DefaultValue(false)] public bool LegacyFill = false;
    [DefaultValue(RenderEngineKind.Unspecified)] public RenderEngineKind RenderEngineKind = RenderEngineKind.Unspecified;

    public bool ShouldSerializerefActorTransformationID()
    {
        return refActorUseTransformation;
    }

    public bool ShouldSerializerefActorObjectEffectLastOnly()
    {
        return refActorComparisonType == 8 || refActorComparisonAnd;
    }
    public bool ShouldSerializerefActorObjectEffectMax()
    {
        return refActorComparisonType == 8 || refActorComparisonAnd;
    }
    public bool ShouldSerializerefActorObjectEffectMin()
    {
        return refActorComparisonType == 8 || refActorComparisonAnd;
    }
    public bool ShouldSerializerefActorObjectEffectData2()
    {
        return refActorComparisonType == 8 || refActorComparisonAnd;
    }

    public bool ShouldSerializerefActorObjectEffectData1()
    {
        return refActorComparisonType == 8 || refActorComparisonAnd;
    }

    public bool ShouldSerializerefActorNamePlateIconId()
    {
        return refActorComparisonType == 9 || refActorComparisonAnd;
    }

    public bool ShouldSerializeRotationMax()
    {
        return this.ShouldSerializeRotationMin();
    }

    public bool ShouldSerializeRotationMin()
    {
        return this.LimitRotation;
    }

    public bool ShouldSerializerefActorVFXPath()
    {
        return refActorComparisonType == 7 || refActorComparisonAnd;
    }

    public bool ShouldSerializerefActorVFXMin()
    {
        return refActorComparisonType == 7 || refActorComparisonAnd;
    }

    public bool ShouldSerializerefActorVFXMax()
    {
        return refActorComparisonType == 7 || refActorComparisonAnd;
    }

    public bool ShouldSerializerefActorNameIntl()
    {
        return ShouldSerializerefActorName() && !refActorNameIntl.IsEmpty();
    }

    public bool ShouldSerializeconeAngleMax()
    {
        return ShouldSerializeconeAngleMin();
    }

    public bool ShouldSerializeconeAngleMin()
    {
        return type == 4 || type == 5;
    }

    public bool ShouldSerializerefActorLifetimeMax()
    {
        return ShouldSerializerefActorLifetimeMin();
    }

    public bool ShouldSerializerefActorLifetimeMin()
    {
        return refActorObjectLife;
    }

    public bool ShouldSerializerefActorCastId()
    {
        return refActorRequireCast && refActorCastId.Count > 0;
    }

    public bool ShouldSerializerefActorBuffId()
    {
        return refActorRequireBuff && refActorBuffId.Count > 0;
    }

    public bool ShouldSerializerefActorBuffParam()
    {
        return refActorRequireBuff && refActorUseBuffParam;
    }

    public bool ShouldSerializerefActorName()
    {
        return refActorComparisonType == 0 || refActorComparisonAnd;
    }

    public bool ShouldSerializerefActorModelID()
    {
        return refActorComparisonType == 1 || refActorComparisonAnd;
    }

    public bool ShouldSerializerefActorObjectID()
    {
        return refActorComparisonType == 2 || refActorComparisonAnd;
    }

    public bool ShouldSerializerefActorDataID()
    {
        return refActorComparisonType == 3 || refActorComparisonAnd;
    }

    public bool ShouldSerializerefActorNPCID()
    {
        return refActorComparisonType == 4 || refActorComparisonAnd;
    }

    public bool ShouldSerializerefActorPlaceholder()
    {
        return refActorComparisonType == 5 || refActorComparisonAnd;
    }

    public bool ShouldSerializerefActorNPCNameID()
    {
        return refActorComparisonType == 6 || refActorComparisonAnd;
    }

    public bool ShouldSerializerefX()
    {
        return type != 1;
    }
    public bool ShouldSerializerefY() { return ShouldSerializerefX(); }
    public bool ShouldSerializerefZ() { return ShouldSerializerefX(); }

    public bool ShouldSerializeDonut()
    {
        return type.EqualsAny(0, 1, 2, 3) && Donut > 0;
    }

    public bool ShouldSerializerefActorTetherConnectedWithPlayer() => refActorTether;
}
