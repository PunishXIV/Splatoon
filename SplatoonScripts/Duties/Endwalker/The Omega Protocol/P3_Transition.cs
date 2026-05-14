using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;

public class P3_Transition : SplatoonScript
{
    #region Metadata
    public override Metadata? Metadata => new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryTop];
    #endregion

    #region Constant

    // TOP (The Omega Protocol) territory id.
    private const uint TerritoryTop = 1122;

    // Sniper debuffs: high-power cannon / wave cannon.
    private const uint StatusSniperCannon = 3426;
    private const uint StatusSniperWave = 3425;

    // Transition start: rapid burst; waves: incremental casts.
    private const uint StartTransitionCastId = 31567;
    private static readonly uint[] WaveIncrementCastIds = [31568, 31569, 31570];

    // Triangle marker arms (left/right).
    private const uint DataIdTriangleMarkerA = 0x3D66;
    private const uint DataIdTriangleMarkerB = 0x3D67;
    private const int MarkerCountForTriangleReverse = 3;
    private const int MarkerCountEndDetermination = 6;
    private const float MarkerAnchorX = 100f;
    private const float MarkerAnchorZ = 86f;
    private const float MarkerPositionEpsilon = 1.5f;

    private const string ElUnDetermined = "UnDetermined";
    private const string ElLeftOutside = "Left_Determined_Outside";
    private const string ElLeftInside = "Left_Determined_Inside";
    private const string ElLeftAvoid = "Left_Determined_Avoid";
    private const string ElRightOutside = "Right_Determined_Outside";
    private const string ElRightInside = "Right_Determined_Inside";
    private const string ElRightAvoid = "Right_Determined_Avoid";

    private static readonly string[] AllWaveOverlayElementNames =
    [
        ElUnDetermined,
        ElLeftOutside,
        ElLeftInside,
        ElLeftAvoid,
        ElRightOutside,
        ElRightInside,
        ElRightAvoid,
    ];

    private const int MaxWaveStage = 6;
    private const int WaveStageIdle = -1;
    private const int MinPartySize = 8;
    private const double RainbowHueCycleSeconds = 4d;

    #endregion

    #region Config
    private Config C => Controller.GetConfig<Config>();
    #endregion

    #region State

    private int _waveStage = WaveStageIdle;
    private GroupAssignment? _debugMyGroup;
    private int _debugPartyCountObjects;

    private TransitionPatternType _transitionPatternType = TransitionPatternType.Unknown;
    private bool _markerSnapshotCaptured;
    private bool _markerDeterminationEnded;
    private int _lastMarkerObjectCount;

    #endregion

    // True when scene id is P3 transition (3 or 4).
    private static bool IsP3TransitionScene(int scene) => scene is 3 or 4;

    #region LifeCycle

    public override void OnSetup()
    {
        RegisterWaveOverlayElements();
        DisableAllWaveOverlayElements();
    }

    // Registers wave overlay elements from preset JSON (values stay literal in JSON).
    private void RegisterWaveOverlayElements()
    {
        Controller.RegisterElementFromCode(ElUnDetermined,
            """{"Name":"UnDetermined","Enabled":false,"type":1,"offY":-17.3,"radius":2.17,"thicc":10.0,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true,"AdditionalRotation":0.0,"tether":true}""");
        Controller.RegisterElementFromCode(ElLeftOutside,
            """{"Name":"Left_Determined_Outside","Enabled":false,"type":1,"offX":3.5,"offY":-18.4,"radius":0.8,"thicc":10.0,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true,"AdditionalRotation":0.0,"tether":true}""");
        Controller.RegisterElementFromCode(ElLeftInside,
            """{"Name":"Left_Determined_Inside","Enabled":false,"type":1,"offX":3.0,"offY":-16.5,"radius":0.8,"thicc":10.0,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true,"AdditionalRotation":0.0,"tether":true}""");
        Controller.RegisterElementFromCode(ElLeftAvoid,
            """{"Name":"Left_Determined_Avoid","Enabled":false,"type":1,"offX":-4.5,"offY":-16.0,"radius":0.8,"thicc":10.0,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true,"AdditionalRotation":0.0,"tether":true}""");
        Controller.RegisterElementFromCode(ElRightOutside,
            """{"Name":"Right_Determined_Outside","Enabled":false,"type":1,"offX":-3.5,"offY":-18.4,"radius":0.8,"thicc":10.0,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true,"AdditionalRotation":0.0,"tether":true}""");
        Controller.RegisterElementFromCode(ElRightInside,
            """{"Name":"Right_Determined_Inside","Enabled":false,"type":1,"offX":-3.0,"offY":-16.5,"radius":0.8,"thicc":10.0,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true,"AdditionalRotation":0.0,"tether":true}""");
        Controller.RegisterElementFromCode(ElRightAvoid,
            """{"Name":"Right_Determined_Avoid","Enabled":false,"type":1,"offX":4.5,"offY":-16.0,"radius":0.8,"thicc":10.0,"fillIntensity":0.5,"refActorDataID":15717,"refActorComparisonType":3,"includeRotation":true,"AdditionalRotation":0.0,"tether":true}""");
    }

    public override void OnUpdate()
    {
        if(!IsP3TransitionScene(Controller.Scene))
        {
            if(_waveStage != WaveStageIdle) EndTransitionPhase();
            return;
        }

        UpdateDebugMyGroup();
        UpdateTransitionPatternMarkers();
        ApplyGuideVisibility();
    }

    public override void OnReset()
    {
        _waveStage = WaveStageIdle;
        _debugMyGroup = null;
        ResetTransitionPatternState();
        DisableAllWaveOverlayElements();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == StartTransitionCastId)
        {
            BeginTransitionFromCast();
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(_waveStage == WaveStageIdle) return;
        if(!IsP3TransitionScene(Controller.Scene)) return;
        if(set.Action == null || set.Source == null) return;
        if(set.Source is not IBattleNpc sourceBattleNpc) return;
        if(sourceBattleNpc.ObjectKind != ObjectKind.BattleNpc && sourceBattleNpc.ObjectKind != ObjectKind.EventNpc) return;

        var actionId = set.Action.Value.RowId;

        if(!WaveIncrementCastIds.Contains(actionId)) return;

        _waveStage = Math.Min(_waveStage + 1, MaxWaveStage);
        PluginLog.Information($"[P3 Transition] Wave via ActionEffect actionId={actionId}, waveStage={_waveStage}, sourceDataId={sourceBattleNpc.DataId}");

        if(_waveStage == MaxWaveStage) EndTransitionPhase();
    }

    // Clears overlays and idle wave state when the transition sequence ends.
    private void EndTransitionPhase()
    {
        DisableAllWaveOverlayElements();
        _waveStage = WaveStageIdle;
        ResetTransitionPatternState();
    }

    // Starts counting waves when the transition opener cast begins.
    private void BeginTransitionFromCast()
    {
        if(!IsP3TransitionScene(Controller.Scene)) return;
        if(_waveStage != WaveStageIdle) return;

        _waveStage = 0;
        ResetTransitionPatternState();
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Priority settings");
        C.PriorityData.Draw();

        ImGui.Text("\nGroup direction settings");
        DrawDirectionSelector("Stack1 (High 1 + None 1)", GroupAssignment.Stack1);
        DrawDirectionSelector("Stack2 (High 2 + None 2)", GroupAssignment.Stack2);
        DrawDirectionSelector("Spread1", GroupAssignment.Spread1);
        DrawDirectionSelector("Spread2", GroupAssignment.Spread2);
        DrawDirectionSelector("Spread3", GroupAssignment.Spread3);
        DrawDirectionSelector("Spread4", GroupAssignment.Spread4);

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text($"Wave stage ({WaveStageIdle} = idle / NotTransition): {_waveStage}");
            ImGui.Text($"Phase: {GetTransitionPhaseState()}");
            ImGui.Text($"BasePlayer: {BasePlayer?.Name.ToString() ?? "Unknown"}");
            ImGui.Text($"My Group: {_debugMyGroup?.ToString() ?? "Unknown"}");
            ImGui.Text($"Type: {_transitionPatternType}");
            ImGui.Text($"Controller.Scene (3 or 4): {Controller.Scene}");
        }
    }

    #endregion

    #region Private Method

    // ImGui combo to pick arena direction for one group assignment slot.
    private void DrawDirectionSelector(string label, GroupAssignment group)
    {
        var currentDirectionSpot = C.GroupDirection[group];
        if(ImGui.BeginCombo(label, currentDirectionSpot.ToString()))
        {
            foreach(var spot in Enum.GetValues<DirectionSpot>())
            {
                var selected = currentDirectionSpot == spot;
                if(ImGui.Selectable(spot.ToString(), selected))
                {
                    C.GroupDirection[group] = spot;
                }

                if(selected) ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }
    }

    // Maps wave stage and marker pattern to a coarse UI phase label.
    private TransitionPhaseState GetTransitionPhaseState()
    {
        if(_waveStage == WaveStageIdle) return TransitionPhaseState.NotTransition;

        if(_waveStage >= 1)
        {
            return _waveStage switch
            {
                1 => TransitionPhaseState.Wave1,
                2 => TransitionPhaseState.Wave2,
                3 => TransitionPhaseState.Wave3,
                4 => TransitionPhaseState.Wave4,
                5 => TransitionPhaseState.Wave5,
                6 => TransitionPhaseState.Wave6,
            };
        }

        if(_transitionPatternType != TransitionPatternType.Unknown) return TransitionPhaseState.DeterminedGuide;

        return TransitionPhaseState.UnDeterminedGuide;
    }

    // Tracks triangle markers on field to decide triangle vs reverse pattern.
    private void UpdateTransitionPatternMarkers()
    {
        if(_waveStage == WaveStageIdle || _markerDeterminationEnded) return;

        var markers = Svc.Objects
            .Where(x => x.DataId.EqualsAny<uint>(DataIdTriangleMarkerA, DataIdTriangleMarkerB))
            .Where(IsTransitionMarkerEligible)
            .ToList();

        _lastMarkerObjectCount = markers.Count;

        if(!_markerSnapshotCaptured && markers.Count >= MarkerCountForTriangleReverse)
        {
            _markerSnapshotCaptured = true;
            var hasAnchor = markers.Any(IsAtTriangleAnchorPosition);
            _transitionPatternType = hasAnchor ? TransitionPatternType.Triangle : TransitionPatternType.Reverse;
        }

        if(markers.Count >= MarkerCountEndDetermination)
        {
            _markerDeterminationEnded = true;
        }
    }

    // True when a marker sits on the fixed triangle anchor near north edge.
    private static bool IsAtTriangleAnchorPosition(IGameObject obj)
    {
        var position = obj.Position;
        return MathF.Abs(position.X - MarkerAnchorX) < MarkerPositionEpsilon && MathF.Abs(position.Z - MarkerAnchorZ) < MarkerPositionEpsilon;
    }

    // Filters objects that count as transition triangle markers.
    private static bool IsTransitionMarkerEligible(IGameObject obj)
    {
        if(obj.EntityId == 0) return false;

        return obj is ICharacter character && character.IsCharacterVisible();
    }

    // Clears marker snapshot flags when leaving transition logic.
    private void ResetTransitionPatternState()
    {
        _transitionPatternType = TransitionPatternType.Unknown;
        _markerSnapshotCaptured = false;
        _markerDeterminationEnded = false;
        _lastMarkerObjectCount = 0;
    }

    // Nearest living PCs to local player for role resolution (up to 8).
    private List<IPlayerCharacter> GetPartyMembers()
    {
        var partyMembersSortedByDistance = Svc.Objects
            .OfType<IPlayerCharacter>()
            .Where(x => !x.IsDead && x.CurrentHp > 0)
            .OrderBy(x => Vector3.Distance(x.Position, Player.Object?.Position ?? x.Position))
            .Take(MinPartySize)
            .ToList();
        _debugPartyCountObjects = partyMembersSortedByDistance.Count;
        return partyMembersSortedByDistance;
    }

    // Resolves which stack/spread slot the configured player occupies from debuffs.
    private bool TryResolveCurrentAssignment(out GroupAssignment? assignment)
    {
        assignment = null;
        var partyMembers = GetPartyMembers();
        if(partyMembers.Count < MinPartySize) return false;

        var targetPlayer = GetProcessingPlayer();
        if(targetPlayer == null) return false;

        assignment = ResolveGroupAssignment(targetPlayer.EntityId, partyMembers);
        return true;
    }

    // Maps local player entity id to group using sniper debuff ordering rules.
    private GroupAssignment? ResolveGroupAssignment(uint localPlayerEntityId, IReadOnlyList<IPlayerCharacter> partyMembers)
    {
        var sniperCannonOrdered = OrderByPriority(partyMembers.Where(x => HasStatus(x, StatusSniperCannon))).ToList();
        var sniperWaveOrdered = OrderByPriority(partyMembers.Where(x => HasStatus(x, StatusSniperWave))).ToList();
        var neitherDebuffOrdered = OrderByPriority(partyMembers.Where(x => !HasStatus(x, StatusSniperCannon) && !HasStatus(x, StatusSniperWave))).ToList();

        if(sniperCannonOrdered.Count >= 1 && sniperCannonOrdered[0].EntityId == localPlayerEntityId) return GroupAssignment.Stack1;
        if(sniperCannonOrdered.Count >= 2 && sniperCannonOrdered[1].EntityId == localPlayerEntityId) return GroupAssignment.Stack2;
        if(neitherDebuffOrdered.Count >= 1 && neitherDebuffOrdered[0].EntityId == localPlayerEntityId) return GroupAssignment.Stack1;
        if(neitherDebuffOrdered.Count >= 2 && neitherDebuffOrdered[1].EntityId == localPlayerEntityId) return GroupAssignment.Stack2;
        if(sniperWaveOrdered.Count >= 1 && sniperWaveOrdered[0].EntityId == localPlayerEntityId) return GroupAssignment.Spread1;
        if(sniperWaveOrdered.Count >= 2 && sniperWaveOrdered[1].EntityId == localPlayerEntityId) return GroupAssignment.Spread2;
        if(sniperWaveOrdered.Count >= 3 && sniperWaveOrdered[2].EntityId == localPlayerEntityId) return GroupAssignment.Spread3;
        if(sniperWaveOrdered.Count >= 4 && sniperWaveOrdered[3].EntityId == localPlayerEntityId) return GroupAssignment.Spread4;

        return null;
    }

    // Refreshes debug-only cached group for the operating player.
    private void UpdateDebugMyGroup()
    {
        _debugMyGroup = TryResolveCurrentAssignment(out var assignment) ? assignment : null;
    }

    // Player this script uses for assignment (BasePlayer / playback override).
    private IPlayerCharacter? GetProcessingPlayer() => BasePlayer;

    // Orders players by script priority list then entity id.
    private IEnumerable<IPlayerCharacter> OrderByPriority(IEnumerable<IPlayerCharacter> players)
        => players.OrderBy(GetPriorityIndex).ThenBy(x => x.EntityId);

    // Zero-based index in priority config, or max when unknown.
    private int GetPriorityIndex(IPlayerCharacter player)
    {
        var priorityList = C.PriorityData.GetPlayers(_ => true)?.ToList();
        if(priorityList == null) return int.MaxValue;

        var name = player.Name.ToString();
        for(var index = 0; index < priorityList.Count; index++)
        {
            if(priorityList[index].Name == name) return index;
        }

        return int.MaxValue;
    }

    // True if the player currently has the given status id.
    private static bool HasStatus(IPlayerCharacter player, uint statusId)
        => player.StatusList.Any(x => x.StatusId == statusId);

    // Each frame: refresh overlay when in waves, otherwise hide all.
    private void ApplyGuideVisibility()
    {
        if(_waveStage != WaveStageIdle) UpdateWaveOverlayVisibility();
        else DisableAllWaveOverlayElements();
    }

    // Disables every registered wave overlay element.
    private void DisableAllWaveOverlayElements()
    {
        foreach(var elementName in AllWaveOverlayElementNames)
        {
            if(Controller.TryGetElementByName(elementName, out var element))
            {
                element.Enabled = false;
                element.tether = false;
            }
        }
    }

    // Chooses which overlay to show from wave index and resolved group direction.
    private void UpdateWaveOverlayVisibility()
    {
        if(_waveStage == WaveStageIdle) return;

        _ = TryResolveCurrentAssignment(out var resolvedGroupAssignment);
        var directionSpot = resolvedGroupAssignment != null ? C.GroupDirection[resolvedGroupAssignment.Value] : DirectionSpot.NorthEast;
        var additionalRotationRadians = DirectionSpotToAdditionalRotation(directionSpot);

        DisableAllWaveOverlayElements();

        if(_waveStage >= MaxWaveStage)
        {
            return;
        }

        if(_waveStage >= 1)
        {
            switch(_waveStage)
            {
                case 1:
                case 3:
                case 4:
                    EnableDeterminedSideGuide(DeterminedSideGuideKind.Outside, additionalRotationRadians, directionSpot);
                    break;
                case 2:
                    EnableDeterminedSideGuide(DeterminedSideGuideKind.Inside, additionalRotationRadians, directionSpot);
                    break;
                case 5:
                    EnableDeterminedSideGuide(DeterminedSideGuideKind.Avoid, additionalRotationRadians, directionSpot);
                    break;
            }

            return;
        }

        switch(GetTransitionPhaseState())
        {
            case TransitionPhaseState.UnDeterminedGuide:
                SetNonWaveOverlayElement(ElUnDetermined, additionalRotationRadians);
                break;
            case TransitionPhaseState.DeterminedGuide:
                EnableDeterminedSideGuide(DeterminedSideGuideKind.Outside, additionalRotationRadians, directionSpot);
                break;
        }
    }

    // Turns on left/right outside/inside/avoid overlay for current pattern and facing.
    private void EnableDeterminedSideGuide(DeterminedSideGuideKind guideKind, float additionalRotationRadians, DirectionSpot directionSpot)
    {
        var displaySide = ResolveDisplaySide(directionSpot, _transitionPatternType);
        var elementName = displaySide == DisplaySide.Left
            ? guideKind switch
            {
                DeterminedSideGuideKind.Outside => ElLeftOutside,
                DeterminedSideGuideKind.Inside => ElLeftInside,
                DeterminedSideGuideKind.Avoid => ElLeftAvoid,
            }
            : guideKind switch
            {
                DeterminedSideGuideKind.Outside => ElRightOutside,
                DeterminedSideGuideKind.Inside => ElRightInside,
                DeterminedSideGuideKind.Avoid => ElRightAvoid,
            };
        SetNonWaveOverlayElement(elementName, additionalRotationRadians);
    }

    // Extra rotation (radians) applied to tether overlay for a compass slot.
    private static float DirectionSpotToAdditionalRotation(DirectionSpot spot)
        => spot switch
        {
            DirectionSpot.NorthEast => DegreesToRadians(30f),
            DirectionSpot.East => DegreesToRadians(90f),
            DirectionSpot.SouthEast => DegreesToRadians(150f),
            DirectionSpot.SouthWest => DegreesToRadians(210f),
            DirectionSpot.West => DegreesToRadians(270f),
            DirectionSpot.NorthWest => DegreesToRadians(330f),
            _ => 0f,
        };

    // Converts degrees to radians for element rotation fields.
    private static float DegreesToRadians(float deg) => deg * (MathF.PI / 180f);

    // Enables a named overlay element with tether, tint, and optional rotation.
    private void SetNonWaveOverlayElement(string elementName, float? additionalRotationRadians)
    {
        if(!Controller.TryGetElementByName(elementName, out var element)) return;
        element.Enabled = true;
        element.tether = true;
        element.color = GetRainbowColor(RainbowHueCycleSeconds).ToUint();
        if(additionalRotationRadians.HasValue)
        {
            element.AdditionalRotation = additionalRotationRadians.Value;
        }
    }

    // Full-saturation hue cycle for highlight tint on overlays.
    private Vector4 GetRainbowColor(double cycleSeconds)
    {
        if(cycleSeconds <= 0d)
        {
            cycleSeconds = 1d;
        }

        var tickMilliseconds = Environment.TickCount64;
        var normalizedTime = tickMilliseconds / 1000d / cycleSeconds;
        var hue = normalizedTime % 1f;
        return HsvToVector4(hue, 1f, 1f);
    }

    // HSV in 0–1 space to RGBA for ImGui tint conversion.
    private static Vector4 HsvToVector4(double h, double s, double v)
    {
        double r = 0f, g = 0f, b = 0f;
        var i = (int)(h * 6f);
        var f = h * 6f - i;
        var p = v * (1f - s);
        var q = v * (1f - f * s);
        var t = v * (1f - (1f - f) * s);

        switch(i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }

        return new Vector4((float)r, (float)g, (float)b, 1f);
    }

    // Maps compass slot and triangle/reverse pattern to left vs right overlay set.
    private static DisplaySide ResolveDisplaySide(DirectionSpot spot, TransitionPatternType patternType)
    {
        if(patternType == TransitionPatternType.Reverse)
        {
            return spot switch
            {
                DirectionSpot.NorthEast => DisplaySide.Left,
                DirectionSpot.East => DisplaySide.Right,
                DirectionSpot.SouthEast => DisplaySide.Left,
                DirectionSpot.SouthWest => DisplaySide.Right,
                DirectionSpot.West => DisplaySide.Left,
                DirectionSpot.NorthWest => DisplaySide.Right,
                _ => DisplaySide.Right,
            };
        }

        return spot switch
        {
            DirectionSpot.NorthEast => DisplaySide.Right,
            DirectionSpot.East => DisplaySide.Left,
            DirectionSpot.SouthEast => DisplaySide.Right,
            DirectionSpot.SouthWest => DisplaySide.Left,
            DirectionSpot.West => DisplaySide.Right,
            DirectionSpot.NorthWest => DisplaySide.Left,
            _ => DisplaySide.Right,
        };
    }

    #endregion

    #region Private Class

    private enum DeterminedSideGuideKind
    {
        Outside,
        Inside,
        Avoid,
    }

    private enum GroupAssignment
    {
        Stack1,
        Stack2,
        Spread1,
        Spread2,
        Spread3,
        Spread4,
    }

    private enum DirectionSpot
    {
        NorthEast,
        East,
        SouthEast,
        SouthWest,
        West,
        NorthWest,
    }

    private enum DisplaySide
    {
        Left,
        Right,
    }

    private enum TransitionPatternType
    {
        Unknown,
        Triangle,
        Reverse,
    }

    private enum TransitionPhaseState
    {
        NotTransition,
        UnDeterminedGuide,
        DeterminedGuide,
        Wave1,
        Wave2,
        Wave3,
        Wave4,
        Wave5,
        Wave6,
    }

    #endregion

    #region Config

    private class Config : IEzConfig
    {
        public PriorityData PriorityData = new();
        public Dictionary<GroupAssignment, DirectionSpot> GroupDirection = new()
        {
            [GroupAssignment.Stack1] = DirectionSpot.NorthWest,
            [GroupAssignment.Stack2] = DirectionSpot.NorthEast,
            [GroupAssignment.Spread1] = DirectionSpot.West,
            [GroupAssignment.Spread2] = DirectionSpot.SouthWest,
            [GroupAssignment.Spread3] = DirectionSpot.SouthEast,
            [GroupAssignment.Spread4] = DirectionSpot.East,
        };
    }

    #endregion
}
