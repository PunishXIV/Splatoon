using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Splatoon;
using Splatoon.SplatoonScripting;
using Vector3 = System.Numerics.Vector3;

namespace SplatoonScriptsOfficial.Duties.Shadowbringers;

public class TEA_P4_Fate_Projection_α : SplatoonScript
{
    public enum FutureActionType : byte
    {
        None,
        FirstMotion,
        FirstStillness,
        SecondMotion,
        SecondStillness,
        Defamation,
        SharedSentence,
        Aggravated,
        Nothing,
        UnKnown
    }

    private readonly List<uint> _futurePlayers = new();

    private FutureActionType[] _futureActionTypes =
    [
        FutureActionType.None,
        FutureActionType.None,
        FutureActionType.None
    ];

    private bool _isStartFateProjectionCasting;
    private uint? _myFuturePlayer;

    private IBattleChara? SafeAlexander => Svc.Objects.OfType<IBattleNpc>()
        .FirstOrDefault(x => x is { NameId: 0x2352, IsCasting: true, CastActionId: 18858 });

    public override HashSet<uint>? ValidTerritories => new() { 887 };
    public override Metadata? Metadata => new(1, "Garume");


    private FutureActionType GetFutureAction(uint actionId)
    {
        switch (actionId)
        {
            case 19213:
                return FutureActionType.FirstMotion;
            case 19214:
                return FutureActionType.FirstStillness;
            case 18585:
                return FutureActionType.SecondMotion;
            case 18586:
                return FutureActionType.SecondStillness;
        }

        var reversed = _futurePlayers.ToArray().Reverse().ToArray();
        for (var i = 0; i < reversed.Length; i++)
            if (_myFuturePlayer == reversed[i])
                switch (i)
                {
                    case 0:
                        return FutureActionType.SharedSentence;
                    case 1:
                        return FutureActionType.Defamation;
                    case 2:
                    case 3:
                    case 4:
                        return FutureActionType.Aggravated;
                    case 5:
                    case 6:
                    case 7:
                        return FutureActionType.Nothing;
                }

        return FutureActionType.UnKnown;
    }

    private string GetFutureActionText(FutureActionType type)
    {
        return type switch
        {
            FutureActionType.FirstMotion => "最初は動け！",
            FutureActionType.FirstStillness => "最初は動くな",
            FutureActionType.SecondMotion => "最後は動け！",
            FutureActionType.SecondStillness => "最後は動くな！",
            FutureActionType.Defamation => "名誉罰: 上へ",
            FutureActionType.SharedSentence => "集団罰: 左下へ",
            FutureActionType.Aggravated => "加重罰: 右下へ",
            FutureActionType.Nothing => "無職: 左下へ",
            FutureActionType.UnKnown => "UnKnown",
            _ => "None"
        };
    }


    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 18555)
        {
            _isStartFateProjectionCasting = true;
            Controller.Schedule(() =>
            {
                if (Controller.TryGetElementByName("FirstText", out var firstTextElement))
                    firstTextElement.overlayTextColor = EColor.Red.ToUint();
            }, 34 * 1000);
            Controller.Schedule(() =>
            {
                if (Controller.TryGetElementByName("FirstText", out var firstTextElement))
                    firstTextElement.overlayTextColor = EColor.White.ToUint();

                if (Controller.TryGetElementByName("ThirdText", out var thirdTextElement))
                    thirdTextElement.overlayTextColor = EColor.Red.ToUint();
            }, 39 * 1000);
            Controller.Schedule(() =>
            {
                if (Controller.TryGetElementByName("ThirdText", out var thirdTextElement))
                    thirdTextElement.overlayTextColor = EColor.White.ToUint();
                _isStartFateProjectionCasting = false;
            }, 44 * 1000);
        }
    }

    public override void OnReset()
    {
        _isStartFateProjectionCasting = false;
        _futureActionTypes =
        [
            FutureActionType.None,
            FutureActionType.None,
            FutureActionType.None
        ];
        _futurePlayers.Clear();
        _myFuturePlayer = null;
    }

    public override void OnSetup()
    {
        var firstTextElement = new Element(0)
        {
            overlayText = "",
            overlayVOffset = 8f,
            overlayFScale = 5f,
            Filled = false,
            radius = 0f
        };
        firstTextElement.SetOffPosition(new Vector3(100f, 0, 100f));
        Controller.RegisterElement("FirstText", firstTextElement, true);

        var secondTextElement = new Element(0)
        {
            overlayText = "",
            overlayVOffset = 5f,
            overlayFScale = 5f,
            Filled = false,
            radius = 0f
        };
        secondTextElement.SetOffPosition(new Vector3(100f, 0, 100f));
        Controller.RegisterElement("SecondText", secondTextElement, true);

        var thirdTextElement = new Element(0)
        {
            overlayText = "",
            overlayVOffset = 2f,
            overlayFScale = 5f,
            Filled = false,
            radius = 0f
        };
        thirdTextElement.SetOffPosition(new Vector3(100f, 0, 100f));
        Controller.RegisterElement("ThirdText", thirdTextElement, true);

        Controller.RegisterElementFromCode("NorthBait",
            "{\"Name\":\"北側サークル\",\"type\":1,\"offY\":7.0,\"radius\":3.0,\"color\":3372169472,\"fillIntensity\":0.0,\"thicc\":5.0,\"refActorNPCNameID\":9042,\"refActorRequireCast\":true,\"refActorCastId\":[18858],\"refActorCastTimeMax\":30.0,\"refActorUseOvercast\":true,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"onlyVisible\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("SouthEastBait",
            "{\"Name\":\"南側サークル2\",\"type\":1,\"offX\":2.5,\"offY\":41.5,\"radius\":1.0,\"color\":3372169472,\"fillIntensity\":0.0,\"thicc\":5.0,\"refActorNPCNameID\":9042,\"refActorRequireCast\":true,\"refActorCastId\":[18858],\"refActorUseOvercast\":true,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"onlyVisible\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("SouthWestBait",
            "{\"Name\":\"南側サークル1\",\"type\":1,\"offX\":-2.5,\"offY\":41.5,\"radius\":1.0,\"color\":3372169472,\"fillIntensity\":0.0,\"thicc\":5.0,\"refActorNPCNameID\":9042,\"refActorRequireCast\":true,\"refActorCastId\":[18858],\"refActorUseOvercast\":true,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"onlyVisible\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnUpdate()
    {
        if (!_isStartFateProjectionCasting)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            return;
        }

        if (_futureActionTypes[0] != FutureActionType.None)
        {
            var text = GetFutureActionText(_futureActionTypes[0]);
            if (Controller.TryGetElementByName("FirstText", out var element))
            {
                element.overlayText = text;
                element.Enabled = true;
            }
        }

        if (_futureActionTypes[1] != FutureActionType.None)
        {
            var text = GetFutureActionText(_futureActionTypes[1]);
            if (Controller.TryGetElementByName("SecondText", out var element))
            {
                element.overlayText = text;
                element.Enabled = true;
            }
        }

        if (_futureActionTypes[2] != FutureActionType.None)
        {
            var text = GetFutureActionText(_futureActionTypes[2]);
            if (Controller.TryGetElementByName("ThirdText", out var element))
            {
                element.overlayText = text;
                element.Enabled = true;
            }
        }

        var safeAlexander = SafeAlexander;
        if (safeAlexander == null) return;
        if (_futureActionTypes[1] == FutureActionType.None) return;
        switch (_futureActionTypes[1])
        {
            case FutureActionType.Defamation:
                if (Controller.TryGetElementByName("NorthBait", out var northElement))
                {
                    northElement.Enabled = true;
                    northElement.tether = true;
                    northElement.color = GradientColor.Get(0xFFFF00FF.ToVector4(), 0xFFFFFF00.ToVector4()).ToUint();
                    northElement.thicc = 10f;
                }

                break;
            case FutureActionType.SharedSentence:
            case FutureActionType.Nothing:
                if (Controller.TryGetElementByName("SouthWestBait", out var southWestElement))
                {
                    southWestElement.Enabled = true;
                    southWestElement.tether = true;
                    southWestElement.color = GradientColor.Get(0xFFFF00FF.ToVector4(), 0xFFFFFF00.ToVector4()).ToUint();
                    southWestElement.thicc = 10f;
                }

                break;
            case FutureActionType.Aggravated:
                if (Controller.TryGetElementByName("SouthEastBait", out var southEastElement))
                {
                    southEastElement.Enabled = true;
                    southEastElement.tether = true;
                    southEastElement.color = GradientColor.Get(0xFFFF00FF.ToVector4(), 0xFFFFFF00.ToVector4()).ToUint();
                    southEastElement.thicc = 10f;
                }

                break;
            case FutureActionType.None:
            case FutureActionType.FirstMotion:
            case FutureActionType.FirstStillness:
            case FutureActionType.SecondMotion:
            case FutureActionType.SecondStillness:
            case FutureActionType.UnKnown:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (!_isStartFateProjectionCasting) return;
        _futurePlayers.Add(target);
        if (source == Player.Object.EntityId)
        {
            _myFuturePlayer = target;
            Controller.Schedule(() => _futureActionTypes[1] = GetFutureAction(0), 1000);
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!_isStartFateProjectionCasting) return; 
        if (set is { Action: not null, Source: not null, Target: not null } && set.Target.EntityId == _myFuturePlayer)
        {
            var futureAction = GetFutureAction(set.Action.RowId);
            if (_futureActionTypes[0] == FutureActionType.None) _futureActionTypes[0] = futureAction;
            else _futureActionTypes[2] = futureAction;
        }
    }
}