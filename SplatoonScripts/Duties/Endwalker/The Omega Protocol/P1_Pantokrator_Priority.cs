using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;

public class P1_Pantokrator_Priority : SplatoonScript
{
    #region Metadata

    public override Metadata Metadata => new(2, "mirage");
    public override HashSet<uint> ValidTerritories => [TerritoryTop];

    #endregion

    #region Constant

    private const uint TerritoryTop = 1122;
    private const int ScenePantokrator = 2;

    private const uint CastPantokrator = 31499;
    private const uint CastFireFirst = 31501;
    private const uint CastFireSecond = 32368;
    private const uint CastWaveCannon = 31505;

    private const uint StatusFirstTarget = 3004;
    private const uint StatusSecondTarget = 3005;
    private const uint StatusThirdTarget = 3006;
    private const uint StatusFourthTarget = 3451;

    private static readonly uint[] StatusKyriosMarkers =
    [
        StatusFirstTarget,
        StatusSecondTarget,
        StatusThirdTarget,
        StatusFourthTarget,
    ];

    private const float AngleSnapOffsetDegrees = -30f;
    private const float AngleStepDegrees = 30f;
    private const float FirePhaseBaitOffsetDegrees = 55f;

    // One fire wave = CastFireFirst AE + CastFireSecond AE (fireCount increments twice per wave).
    private const int ActionEffectsPerFire = 2;
    private const int LogicalFireMaxForAngle = 13;
    private const int LogicalFireWaveCannonSpread = 14;
    private const int RawFireMaxForAngle = LogicalFireMaxForAngle * ActionEffectsPerFire;
    private const int WaveCannonEffectCountToEnd = 6;

    private const int PartySize = 8;

    private static readonly Vector3 ArenaCenter = new(100f, 0f, 100f);
    private const uint BossDataId = 15708;
    private const float InitReferenceAngleOffsetDegrees = 5f;
    private const float InitReferenceDistance = 20f;
    private const float NaviRadius = 10f;
    private const float PreSpreadOffsetFromFirstFire = 120f;

    private const string ElementNavi = "PantokratorNavi";

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();

    public sealed class Config : IEzConfig
    {
        public PriorityData PriorityData = new();
        public WaveCannonSpreadDirection WaveCannonSpreadDirection = WaveCannonSpreadDirection.North;
        public bool WaveCannonSpreadIsTank = true;
    }

    #endregion

    #region State

    private bool _isPantokrator;
    private int _waveCannonEffectCount;
    private readonly PantokratorInfo _pantokratorInfo = new();
    private readonly List<PlayerInfo> _playerInfos = [];
    private PlayerInfo? _playerInfo;
    private bool _playerInfosLocked;

    #endregion

    #region PrivateClass

    private enum Rotate
    {
        None,
        Clockwise,
        CounterClockwise,
    }

    private enum Role
    {
        North,
        South,
    }

    public enum WaveCannonSpreadDirection
    {
        North,
        East,
        SouthEast,
        SouthRight,
        SouthLeft,
        SouthWest,
        West,
    }

    // ActionEffect counter (raw) and plan.md fireCount (logical = raw / ActionEffectsPerFire).
    private readonly struct FireCount
    {
        public int Raw { get; }

        public int Logical => Raw / ActionEffectsPerFire;

        public FireCount(int raw) => Raw = raw;

        public bool IsLogical(int logical) => Logical == logical;
    }

    private static readonly WaveCannonSpreadDirection[] WaveCannonSpreadDirectionsOther =
    [
        WaveCannonSpreadDirection.East,
        WaveCannonSpreadDirection.SouthEast,
        WaveCannonSpreadDirection.SouthRight,
        WaveCannonSpreadDirection.SouthLeft,
        WaveCannonSpreadDirection.SouthWest,
        WaveCannonSpreadDirection.West,
    ];

    private sealed class PlayerInfo
    {
        public string Name = string.Empty;
        public string PairName = string.Empty;
        public Role Role;
        public uint TargetStatusId;
        public byte TargetParam;
    }

    private sealed class PantokratorInfo
    {
        public readonly List<float> FirstFireAngles = [];
        public readonly List<float> SecondFireAngles = [];
        public float NorthInitAngle;
        public float SouthInitAngle;
        public Vector3 InitReferencePoint;
        public bool InitReferenceResolved;
        public Rotate Rotate = Rotate.None;
        public int FireCountRaw;
    }

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode(
            ElementNavi,
            """{"Name":"","type":1,"offY":12.0,"radius":0.5,"fillIntensity":0.5,"thicc":5.0,"refActorDataID":15708,"refActorComparisonType":3,"includeRotation":true,"AdditionalRotation":0,"Tether":true}""",
            overwrite: true);
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
        {
            ResetAll();
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null)
        {
            return;
        }

        var id = set.Action.Value.RowId;
        if(id is CastFireFirst or CastFireSecond)
        {
            _pantokratorInfo.FireCountRaw++;
        }

        if(id == CastWaveCannon && _isPantokrator)
        {
            _waveCannonEffectCount++;
            if(_waveCannonEffectCount >= WaveCannonEffectCountToEnd)
            {
                _isPantokrator = false;
            }
        }
    }

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        var castId = packet->ActionID;

        if(castId == CastPantokrator)
        {
            BeginPantokrator();
            return;
        }

        var angle = SnapCastAngle(packet);
        if(castId == CastFireFirst && _pantokratorInfo.FirstFireAngles.Count < 2)
        {
            _pantokratorInfo.FirstFireAngles.Add(angle);
            _pantokratorInfo.FirstFireAngles.Sort();
        }
        else if(castId == CastFireSecond && _pantokratorInfo.SecondFireAngles.Count < 2)
        {
            _pantokratorInfo.SecondFireAngles.Add(angle);
            _pantokratorInfo.SecondFireAngles.Sort();
            ComputeRotate();
        }
    }

    public override void OnUpdate()
    {
        DisableNaviElement();

        if(Controller.Scene != ScenePantokrator || !_isPantokrator)
        {
            return;
        }

        var fireCount = new FireCount(_pantokratorInfo.FireCountRaw);

        if(TryApplyWaveCannonSpreadNavi())
        {
            return;
        }

        if(TryApplyPreSpreadNavi(fireCount))
        {
            return;
        }

        if(fireCount.Raw > RawFireMaxForAngle)
        {
            return;
        }

        TryApplyFirePhaseNavi(fireCount);
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Pantokrator Priority Settings");
        C.PriorityData.Draw();
        ImGui.NewLine();
        DrawWaveCannonSpreadSettings();
        ImGui.NewLine();
    }

    #endregion

    #region PrivateMethod

    // Clears all script state on wipe or instance boundary.
    private void ResetAll()
    {
        _isPantokrator = false;
        ClearEncounterState();
    }

    // Starts Pantokrator tracking and resets fire/orientation state.
    private void BeginPantokrator()
    {
        _isPantokrator = true;
        ClearEncounterState();
    }

    // Shared reset for roster, angles, and fire counters (does not toggle _isPantokrator).
    private void ClearEncounterState()
    {
        _waveCannonEffectCount = 0;
        _playerInfos.Clear();
        _playerInfo = null;
        _playerInfosLocked = false;
        _pantokratorInfo.FirstFireAngles.Clear();
        _pantokratorInfo.SecondFireAngles.Clear();
        _pantokratorInfo.NorthInitAngle = 0f;
        _pantokratorInfo.SouthInitAngle = 0f;
        _pantokratorInfo.InitReferencePoint = default;
        _pantokratorInfo.InitReferenceResolved = false;
        _pantokratorInfo.Rotate = Rotate.None;
        _pantokratorInfo.FireCountRaw = 0;
    }

    // Snaps cast facing to the plan 30Â° grid (cast angle - 30).
    private unsafe float SnapCastAngle(PacketActorCast* packet)
    {
        return NormalizeAngle(SnapDegToStep(packet->RotationFromNorth.RadToDeg(), AngleSnapOffsetDegrees, AngleStepDegrees));
    }

    // Wave cannon spread navi at logical fire 14.
    private bool TryApplyWaveCannonSpreadNavi()
    {
        var fireCount = new FireCount(_pantokratorInfo.FireCountRaw);
        if(!fireCount.IsLogical(LogicalFireWaveCannonSpread))
        {
            return false;
        }

        var spreadDeg = GetSpreadDirectionDegrees(GetEffectiveWaveCannonSpreadDirection());
        ApplyNaviElementAt(spreadDeg);
        return true;
    }

    // Pre-spread navi at logical fire 0 when roster is ready.
    private bool TryApplyPreSpreadNavi(FireCount fireCount)
    {
        if(!fireCount.IsLogical(0))
        {
            return false;
        }

        BuildPlayerInfos();
        ResolveCurrentPlayer();
        InitAngle();

        if(_playerInfo == null)
        {
            return true;
        }

        var preDeg = GetPreSpreadAngle(_playerInfo.Role);
        ApplyNaviElementAt(preDeg);
        return true;
    }

    // Fire phase navi via getAngle (logical 1â€“13).
    private void TryApplyFirePhaseNavi(FireCount fireCount)
    {
        BuildPlayerInfos();
        ResolveCurrentPlayer();
        InitAngle();
        ComputeRotate();

        if(_playerInfo == null)
        {
            return;
        }

        var navDeg = GetAngle(fireCount);
        ApplyNaviElementAt(navDeg);
    }

    // When 8 players have Kyrios markers, build roster once and lock until reset.
    private void BuildPlayerInfos()
    {
        if(_playerInfosLocked)
        {
            return;
        }

        var playersWithMarker = new List<(IPlayerCharacter Player, uint StatusId, byte Param)>();
        foreach(var pc in Svc.Objects.OfType<IPlayerCharacter>())
        {
            if(pc.IsDead || pc.CurrentHp <= 0)
            {
                continue;
            }

            if(TryGetKyriosMarker(pc, out var statusId, out var param))
            {
                playersWithMarker.Add((pc, statusId, param));
            }
        }

        if(playersWithMarker.Count != PartySize)
        {
            return;
        }

        var byStatus = playersWithMarker.GroupBy(x => x.StatusId).ToList();
        if(byStatus.Count != 4 || byStatus.Any(g => g.Count() != 2))
        {
            return;
        }

        var built = new List<PlayerInfo>(PartySize);
        foreach(var group in byStatus)
        {
            var targetStatusId = group.Key;
            var members = group.ToArray();
            var playerA = members[0].Player;
            var playerB = members[1].Player;
            var ordered = OrderByPriority([playerA, playerB]).ToArray();
            var northPlayer = ordered[0];
            var southPlayer = ordered[1];
            var northParam = northPlayer == playerA ? members[0].Param : members[1].Param;
            var southParam = southPlayer == playerA ? members[0].Param : members[1].Param;

            built.Add(new PlayerInfo
            {
                Name = northPlayer.Name.ToString(),
                PairName = southPlayer.Name.ToString(),
                Role = Role.North,
                TargetStatusId = targetStatusId,
                TargetParam = northParam,
            });
            built.Add(new PlayerInfo
            {
                Name = southPlayer.Name.ToString(),
                PairName = northPlayer.Name.ToString(),
                Role = Role.South,
                TargetStatusId = targetStatusId,
                TargetParam = southParam,
            });
        }

        if(built.Count != PartySize)
        {
            return;
        }

        _playerInfos.Clear();
        _playerInfos.AddRange(built);
        _playerInfosLocked = true;
    }

    // Tank: North + Outside. Other: selectable direction (not North) + Inside.
    private WaveCannonSpreadDirection GetEffectiveWaveCannonSpreadDirection()
    {
        if(C.WaveCannonSpreadIsTank)
        {
            return WaveCannonSpreadDirection.North;
        }

        if(C.WaveCannonSpreadDirection == WaveCannonSpreadDirection.North)
        {
            return WaveCannonSpreadDirectionsOther[0];
        }

        return C.WaveCannonSpreadDirection;
    }

    private void DrawWaveCannonSpreadSettings()
    {
        ImGui.Text("Wavecannon spread direction (mechanic: Tank use invuln on north outside)");
        ImGuiEx.RadioButtonBool("Tank", "Other", ref C.WaveCannonSpreadIsTank, sameLine: true);

        ImGui.SameLine();
        if(C.WaveCannonSpreadIsTank)
        {
            return;
        }

        if(C.WaveCannonSpreadDirection == WaveCannonSpreadDirection.North)
        {
            C.WaveCannonSpreadDirection = WaveCannonSpreadDirectionsOther[0];
        }

        ImGui.SetNextItemWidth(150f);
        if(ImGui.BeginCombo("##waveCannonSpreadOther", C.WaveCannonSpreadDirection.ToString()))
        {
            foreach(var direction in WaveCannonSpreadDirectionsOther)
            {
                if(ImGui.Selectable(direction.ToString(), direction == C.WaveCannonSpreadDirection))
                {
                    C.WaveCannonSpreadDirection = direction;
                }
            }

            ImGui.EndCombo();
        }
    }

    //pre-spread angle: logical 0 North/South target param.
    private float GetPreSpreadAngle(Role role)
        => role == Role.North ? _pantokratorInfo.NorthInitAngle : _pantokratorInfo.SouthInitAngle;

    // Wave cannon spread compass degrees from config direction.
    private static float GetSpreadDirectionDegrees(WaveCannonSpreadDirection direction)
        => direction switch
        {
            WaveCannonSpreadDirection.North => 0f,
            WaveCannonSpreadDirection.East => 90f,
            WaveCannonSpreadDirection.SouthEast => 126f,
            WaveCannonSpreadDirection.SouthRight => 162f,
            WaveCannonSpreadDirection.SouthLeft => 198f,
            WaveCannonSpreadDirection.SouthWest => 234f,
            WaveCannonSpreadDirection.West => 270f,
            _ => 0f,
        };

    // isBait: logical fire bands 1â€“3 / 4â€“6 / 7â€“9 / 10â€“12 per target status.
    private static bool IsBait(FireCount fireCount, uint targetStatusId)
        => fireCount.Logical switch
        {
            >= 1 and <= 3 => targetStatusId == StatusFirstTarget,
            >= 4 and <= 6 => targetStatusId == StatusSecondTarget,
            >= 7 and <= 9 => targetStatusId == StatusThirdTarget,
            >= 10 and <= 12 => targetStatusId == StatusFourthTarget,
            _ => false,
        };

    // Resolves BasePlayer row from the built roster by name.
    private void ResolveCurrentPlayer()
    {
        _playerInfo = null;
        var self = BasePlayer;
        if(self == null || _playerInfos.Count == 0)
        {
            return;
        }

        var name = self.Name.ToString();
        _playerInfo = _playerInfos.FirstOrDefault(x => x.Name == name);
    }

    // initAngle: FirstFire + 120Â°; closer to boss reference is NorthInit, other is SouthInit.
    private void InitAngle()
    {
        if(_pantokratorInfo.FirstFireAngles.Count < 2)
        {
            return;
        }

        if(!_pantokratorInfo.InitReferenceResolved)
        {
            var boss = Svc.Objects.OfType<IBattleChara>().FirstOrDefault(x => x.DataId == BossDataId);
            if(boss == null)
            {
                return;
            }

            var referenceDeg = NormalizeAngle(boss.Rotation + InitReferenceAngleOffsetDegrees);
            _pantokratorInfo.InitReferencePoint = CalculatePointFromCenterByDegree(
                boss.Position, InitReferenceDistance, referenceDeg);
            _pantokratorInfo.InitReferenceResolved = true;
        }

        var reference = _pantokratorInfo.InitReferencePoint;
        var angleA = NormalizeAngle(_pantokratorInfo.FirstFireAngles[0] + PreSpreadOffsetFromFirstFire);
        var angleB = NormalizeAngle(_pantokratorInfo.FirstFireAngles[1] + PreSpreadOffsetFromFirstFire);
        var posA = CalculatePointFromCenterByDegree(ArenaCenter, NaviRadius, angleA);
        var posB = CalculatePointFromCenterByDegree(ArenaCenter, NaviRadius, angleB);

        if(DistanceXZ(posA, reference) <= DistanceXZ(posB, reference))
        {
            _pantokratorInfo.NorthInitAngle = angleA;
            _pantokratorInfo.SouthInitAngle = angleB;
        }
        else
        {
            _pantokratorInfo.NorthInitAngle = angleB;
            _pantokratorInfo.SouthInitAngle = angleA;
        }
    }

    // rotate: pairwise +30 / -30 between first and second fire angle lists.
    private void ComputeRotate()
    {
        if(_pantokratorInfo.FirstFireAngles.Count < 2)
        {
            return;
        }

        if(_pantokratorInfo.SecondFireAngles.Count < 2)
        {
            return;
        }

        if(_pantokratorInfo.Rotate != Rotate.None)
        {
            return;
        }

        foreach(var first in _pantokratorInfo.FirstFireAngles)
        {
            foreach(var second in _pantokratorInfo.SecondFireAngles)
            {
                if(NormalizeAngle(first + AngleStepDegrees) == NormalizeAngle(second))
                {
                    _pantokratorInfo.Rotate = Rotate.Clockwise;
                }

                if(NormalizeAngle(first - AngleStepDegrees) == NormalizeAngle(second))
                {
                    _pantokratorInfo.Rotate = Rotate.CounterClockwise;
                }
            }
        }
    }

    // plan.md getAngle with NormalizeAngle on the result.
    private float GetAngle(FireCount fireCount)
    {
        if(_playerInfo == null)
        {
            return 0f;
        }

        var baseAngle = _playerInfo.Role == Role.North
            ? _pantokratorInfo.NorthInitAngle
            : _pantokratorInfo.SouthInitAngle;
        var isBait = IsBait(fireCount, _playerInfo.TargetStatusId);
        var rotateSign = _pantokratorInfo.Rotate == Rotate.Clockwise ? 1f : -1f;
        var stepTerm = fireCount.Logical * AngleStepDegrees
            + (isBait ? FirePhaseBaitOffsetDegrees : -FirePhaseBaitOffsetDegrees);

        return NormalizeAngle(baseAngle + stepTerm * rotateSign);
    }

    // True when the player has any Kyrios circle-program marker.
    private static bool TryGetKyriosMarker(IPlayerCharacter player, out uint statusId, out byte param)
    {
        foreach(var status in player.StatusList)
        {
            if(StatusKyriosMarkers.Contains(status.StatusId))
            {
                statusId = status.StatusId;
                param = (byte)status.Param;
                return true;
            }
        }

        statusId = 0;
        param = 0;
        return false;
    }

    // Orders players by script priority list then entity id.
    private IEnumerable<IPlayerCharacter> OrderByPriority(IEnumerable<IPlayerCharacter> players)
        => players.OrderBy(GetPriorityIndex).ThenBy(x => x.EntityId);

    // Zero-based index in priority config, or max when unknown.
    private int GetPriorityIndex(IPlayerCharacter player)
    {
        var priorityList = C.PriorityData.GetPlayers(_ => true)?.ToList();
        if(priorityList == null)
        {
            return int.MaxValue;
        }

        var name = player.Name.ToString();
        for(var index = 0; index < priorityList.Count; index++)
        {
            if(priorityList[index].Name == name)
            {
                return index;
            }
        }

        return int.MaxValue;
    }

    // World XZ from arena center, radius, and compass angle in degrees.
    private static Vector3 CalculatePointFromCenterByDegree(Vector3 center, float radius, float degree)
    {
        var rad = degree.DegToRad();
        return new Vector3(
            center.X + MathF.Sin(rad) * radius,
            center.Y,
            center.Z - MathF.Cos(rad) * radius);
    }

    // Distance on arena XZ plane.
    private static float DistanceXZ(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    // Snaps degrees to nearest step after offset, then normalizes.
    private static float SnapDegToStep(float deg, float offset, float step)
    {
        var n = NormalizeAngle(deg + offset);
        var snapped = (float)Math.Round(n / step) * step;
        return NormalizeAngle(snapped);
    }

    // Normalizes degrees to [0, 360).
    private static float NormalizeAngle(float deg)
    {
        deg %= 360f;
        if(deg < 0)
        {
            deg += 360f;
        }

        return deg;
    }

    // Disables the navi element at the start of each tick.
    private void DisableNaviElement()
    {
        if(Controller.TryGetElementByName(ElementNavi, out var el))
        {
            el.Enabled = false;
        }
    }

    // Boss-relative navi rotation; color follows Splatoon Attention Color (rainbow when configured).
    private void ApplyNaviElementAt(float navDeg)
    {
        if(!Controller.TryGetElementByName(ElementNavi, out var el))
        {
            return;
        }

        el.AdditionalRotation = navDeg.DegToRad();
        el.color = Controller.AttentionColor;
        el.Enabled = true;
    }

    #endregion
}
