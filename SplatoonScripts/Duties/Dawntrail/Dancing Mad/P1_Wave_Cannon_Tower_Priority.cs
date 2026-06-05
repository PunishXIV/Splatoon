using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ECommons.PartyFunctions;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate;

public class P1_Wave_Cannon_Tower_Priority : SplatoonScript
{
    private const uint TerritoryDancingMadUltimate = 1363;
    private const uint GravenImage = 48370;
    private const uint FlagrantFireIII = 47778;
    private const uint DoubleTroubleTrapCast = 47782;
    private const uint DoubleTroubleTrapHit = 47783;
    private const uint PulseWave = 47785;
    private const uint WaveCannon = 47784;
    private const uint Explosion = 47786;
    private const uint UltimateEmbrace = 49740;

    private static readonly InternationalString MainSettingsHeaderText = new()
    {
        En = "Main settings",
        Jp = "主設定"
    };

    private static readonly InternationalString DisplayTextHeaderText = new()
    {
        En = "Display text",
        Jp = "表示テキスト"
    };

    private static readonly InternationalString MainDescriptionText = new()
    {
        En =
            "This P1 helper shows only your own Wave Cannon and tower instruction. After Flagrant Fire III, the Mystery Magic resolution, it shows the initial lineup, then collects Wave Cannon targets during Double-trouble Trap. Wave Cannon targets bait outward and do not soak towers; tower soakers are selected from the priority list and sent to support or DPS towers. The initial lineup can be adjusted with the center coordinate and spacing.",
        Jp =
            "P1の最初の波動砲と塔踏みを自分向けに表示します。なぞなぞマジック後に初期散開位置を出し、ずびずばトラップ詠唱中に波動砲対象を集めます。波動砲対象は塔を踏まず外へ誘導し、塔を踏む担当は下の優先順位に従ってTH塔またはDPS塔へ誘導します。初期位置は横一列の基準座標と間隔で調整できます。"
    };

    private static readonly InternationalString DisplayTextDescriptionText = new()
    {
        En =
            "Edit the instructions and marker labels shown on screen. The actual priority order is configured in PriorityData below.",
        Jp = "画面に表示する指示文とマーカー名を編集します。優先順位そのものは下のPriorityDataで設定します。"
    };

    private readonly HashSet<uint> _pulseWaveTargets = [];

    private readonly Dictionary<uint, WaveTarget> _waveTargets = [];
    private string _currentInstruction = "";
    private bool _firstImageStarted;
    private int _gravenImageCount;
    private bool _hasMyDestination;
    private Vector3 _myDestination = Vector3.Zero;
    private State _state = State.None;
    private int _towerResolveCount;
    private bool _waveCannonTargetCollectionStarted;

    public override HashSet<uint>? ValidTerritories { get; } = [TerritoryDancingMadUltimate];
    public override Metadata Metadata => new(2, "Garume, NightmareXIV");

    private Config C => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        Controller.RegisterElement("SelfInstruction", new Element(0)
        {
            Enabled = false,
            radius = 0.0f,
            thicc = 0.0f,
            overlayBGColor = 0xC8000000,
            overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 3.0f,
            overlayFScale = 3.0f,
            overlayText = ""
        });

        Controller.RegisterElement("Destination", new Element(0)
        {
            Enabled = false,
            radius = 1.8f,
            thicc = 6.0f,
            fillIntensity = 0.25f,
            color = 0xC8FF00FF,
            tether = true,
            overlayBGColor = 0xC8000000,
            overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 2.4f,
            overlayFScale = 1.6f,
            overlayText = ""
        });
    }

    public override void OnCombatStart()
    {
        ResetState();
    }

    public override void OnReset()
    {
        ResetState();
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence,
                DirectorUpdateCategory.Wipe))
            ResetState();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == UltimateEmbrace)
        {
            ClearActiveState();
            return;
        }

        if (castId != GravenImage) return;
        if (_firstImageStarted) return;

        _firstImageStarted = true;
        _gravenImageCount++;
        if (_gravenImageCount != 1) return;

        _state = State.CollectingFirstImageTethers;
        _waveTargets.Clear();
        _pulseWaveTargets.Clear();
        _hasMyDestination = false;
        _myDestination = Vector3.Zero;
        _towerResolveCount = 0;
        _waveCannonTargetCollectionStarted = false;
        _currentInstruction = "";
        ApplyDisplay();
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (_state is not (State.CollectingFirstImageTethers or State.InitialLineup)) return;
        if (!LooksLikeImageTether(data2, data3, data5)) return;
        if (target.GetObject() is not IPlayerCharacter player) return;

        AddWaveTarget(player.EntityId, player, source, "tether");
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        var actionId = set.Action?.RowId;

        if(actionId is 47774 or 47768 && _gravenImageCount == 1)
        {
            ShowInitialLineup($"cast {actionId}");
            return;
        }
        if (actionId == PulseWave)
        {
            foreach (var target in set.TargetEffects.Select(x => (uint)x.TargetID))
                if (target.GetObject() is IPlayerCharacter player)
                {
                    _pulseWaveTargets.Add(player.EntityId);
                    if (_waveTargets.Count < 4)
                        AddWaveTarget(player.EntityId, player, set.Source?.EntityId ?? 0, "pulse");
                }

            if (_state == State.InitialLineup)
                ApplyDisplay();
        }

        if (actionId == FlagrantFireIII)
            ShowInitialLineup($"action {actionId}");

        if (actionId is DoubleTroubleTrapCast or DoubleTroubleTrapHit)
            ShowInitialLineup($"action {actionId}");

        if (actionId == Explosion &&
            (_state is State.Solved or State.WaveCannonSeen || _waveCannonTargetCollectionStarted))
        {
            _towerResolveCount++;
            
            if (_towerResolveCount >= 4)
                ClearActiveState();
        }

        if (actionId == WaveCannon && _state != State.None)
        {
            if (!_waveCannonTargetCollectionStarted)
            {
                _waveCannonTargetCollectionStarted = true;
                _waveTargets.Clear();
                _pulseWaveTargets.Clear();
            }

            foreach (var target in set.TargetEffects.Select(x => (uint)x.TargetID))
                if (target.GetObject() is IPlayerCharacter player)
                    AddWaveTarget(player.EntityId, player, set.Source?.EntityId ?? 0, "wave");

            if (_waveTargets.Count >= 4 && _state != State.WaveCannonSeen)
            {
                Solve();
                _state = State.WaveCannonSeen;
            }
        }
    }

    public override void OnUpdate()
    {
        ApplyDisplay();
    }

    public override void OnSettingsDraw()
    {
        ImGui.TextWrapped(MainDescriptionText.Get(MainDescriptionText.En));
        ImGui.Separator();
        ImGui.TextUnformatted(MainSettingsHeaderText.Get(MainSettingsHeaderText.En));
        ImGui.Checkbox("Tower takers are non-targets", ref C.TowerTakersAreNonTargets);
        ImGui.Checkbox("Use fixed initial lineup", ref C.UseFixedInitialLineup);
        ImGui.InputFloat("Lineup center X", ref C.LineupCenterX);
        ImGui.InputFloat("Lineup Z", ref C.LineupZ);
        ImGui.InputFloat("Lineup spacing", ref C.LineupSpacing);
        ImGui.Separator();
        DrawDisplayTextSettings();
        ImGui.Separator();
        C.PriorityData.Draw();
    }

    private void DrawDisplayTextSettings()
    {
        if (!ImGui.CollapsingHeader(DisplayTextHeaderText.Get(DisplayTextHeaderText.En))) return;

        ImGui.Indent();
        ImGui.TextWrapped(DisplayTextDescriptionText.Get(DisplayTextDescriptionText.En));
        DrawInternationalString("Set priority list", C.SetPriorityListText);
        DrawInternationalString("Take support tower", C.TakeSupportTowerText);
        DrawInternationalString("Take DPS tower", C.TakeDpsTowerText);
        DrawInternationalString("Bait Wave Cannon", C.BaitWaveCannonText);
        DrawInternationalString("Wave target wait", C.WaveTargetWaitText);
        DrawInternationalString("No Wave tower", C.NoWaveTowerText);
        DrawInternationalString("Initial lineup", C.InitialLineupText);
        DrawInternationalString("Start overlay", C.StartOverlayText);
        DrawInternationalString("Tower overlay", C.TowerOverlayText);
        ImGui.Unindent();
    }

    private static void DrawInternationalString(string label, InternationalString text)
    {
        ImGui.PushID(label);
        ImGui.Text(label);
        ImGui.SameLine();
        var current = text.Get();
        text.ImGuiEdit(ref current);
        ImGui.PopID();
    }

    private bool LooksLikeImageTether(uint data2, uint data3, uint data5)
    {
        // Pull 26 observed the first-image tethers as replay ActorControl p2=45 and p4=15.
        // Splatoon's hook argument mapping is source/target/data2/data3/data5, so accept
        // either observed value in the hook payload until more live logs confirm the exact slot.
        return data2 is 45u or 15u || data3 is 45u or 15u || data5 is 45u or 15u;
    }

    private void AddWaveTarget(uint entityId, IPlayerCharacter player, uint source, string signal)
    {
        if (_waveTargets.ContainsKey(entityId)) return;
        if (_waveTargets.Count >= 4) return;

        _waveTargets[entityId] =
            new WaveTarget(player.Name.ToString(), entityId, source, player.GetRole(), player.Position);
    }

    private void Solve()
    {
        if (_waveTargets.Count == 0) return;

        var priority = C.PriorityData.GetPlayers(_ => true);
        if (priority is null || priority.Count == 0)
        {
            _currentInstruction = C.SetPriorityListText.Get();
            return;
        }

        var towerTargetIds = _waveTargets.Keys.ToHashSet();
        var supportPriority = priority.Where(x => x.IGameObject is IPlayerCharacter pc && IsSupport(pc)).ToList();
        var dpsPriority = priority.Where(x => x.IGameObject is IPlayerCharacter pc && pc.GetRole() == CombatRole.DPS)
            .ToList();
        var supportTowerCount = _waveTargets.Values.Count(x => IsSupportRole(x.Role));
        var dpsTowerCount = _waveTargets.Values.Count(x => x.Role == CombatRole.DPS);

        var supportTakers = SelectTakers(supportPriority, towerTargetIds, supportTowerCount);
        var dpsTakers = SelectTakers(dpsPriority, towerTargetIds, dpsTowerCount);
        var supportTargets = SelectTargets(supportPriority, towerTargetIds, supportTowerCount);
        var dpsTargets = SelectTargets(dpsPriority, towerTargetIds, dpsTowerCount);
        var destinationByTaker = BuildDestinationAssignments(supportTakers, supportTargets)
            .Concat(BuildDestinationAssignments(dpsTakers, dpsTargets))
            .ToDictionary(x => x.TakerId, x => x.TargetId);
        var takerIds = supportTakers.Concat(dpsTakers)
            .Select(x => x.IGameObject.EntityId)
            .ToHashSet();

        var me = BasePlayer;
        if (me == null)
        {
            _currentInstruction = "";
            return;
        }

        var iAmTarget = towerTargetIds.Contains(me.EntityId);
        var iTakeTower = takerIds.Contains(me.EntityId);
        _hasMyDestination = false;
        _myDestination = Vector3.Zero;

        if (iTakeTower)
        {
            if (destinationByTaker.TryGetValue(me.EntityId, out var destinationTargetId))
            {
                if (_waveTargets.TryGetValue(destinationTargetId, out var targetInfo))
                {
                    _myDestination = targetInfo.Position;
                    _hasMyDestination = true;
                }
                else if (TryGetFixedLineupPosition(priority, destinationTargetId, out var fixedPosition))
                {
                    _myDestination = fixedPosition;
                    _hasMyDestination = true;
                }
                else if (destinationTargetId.GetObject() is IPlayerCharacter targetPlayer)
                {
                    _myDestination = targetPlayer.Position;
                    _hasMyDestination = true;
                }
            }

            _currentInstruction = IsSupport(me)
                ? C.TakeSupportTowerText.Get()
                : C.TakeDpsTowerText.Get();
        }
        else if (iAmTarget)
        {
            _currentInstruction = C.TowerTakersAreNonTargets
                ? C.BaitWaveCannonText.Get()
                : C.WaveTargetWaitText.Get();
        }
        else
        {
            _currentInstruction = C.NoWaveTowerText.Get();
        }

        _state = State.Solved;
    }

    private void ShowInitialLineup(string signal)
    {
        if (!_firstImageStarted) return;
        if (_state is State.None or State.WaveCannonSeen) return;

        _state = State.InitialLineup;
        _currentInstruction = C.InitialLineupText.Get();

        var priority = C.PriorityData.GetPlayers(_ => true);
        if (priority is { Count: > 0 })
            SetInitialLineupDestination(priority);
        ApplyDisplay();

    }

    private void SetInitialLineupDestination(IReadOnlyList<UniversalPartyMember> priority)
    {
        var me = BasePlayer;
        if (!C.UseFixedInitialLineup || me == null) return;
        if (!TryGetFixedLineupPosition(priority, me.EntityId, out var position)) return;

        _myDestination = position;
        _hasMyDestination = true;

        if (_state == State.InitialLineup) _currentInstruction = C.InitialLineupText.Get();
    }

    private bool TryGetFixedLineupPosition(
        IReadOnlyList<UniversalPartyMember> priority,
        uint playerId,
        out Vector3 position)
    {
        position = Vector3.Zero;
        if (!C.UseFixedInitialLineup) return false;

        var index = priority.ToList().FindIndex(x => x.IGameObject.EntityId == playerId);
        if (index < 0) return false;

        var x = C.LineupCenterX + (index - 3.5f) * C.LineupSpacing;
        position = new Vector3(x, 0f, C.LineupZ);
        return true;
    }

    private IReadOnlyList<UniversalPartyMember> SelectTakers(
        IReadOnlyList<UniversalPartyMember> priority,
        HashSet<uint> towerTargetIds,
        int towerCount)
    {
        if (towerCount <= 0) return [];

        return priority
            .Where(x => C.TowerTakersAreNonTargets
                ? !towerTargetIds.Contains(x.IGameObject.EntityId)
                : towerTargetIds.Contains(x.IGameObject.EntityId))
            .Take(towerCount)
            .ToList();
    }

    private static IReadOnlyList<UniversalPartyMember> SelectTargets(
        IReadOnlyList<UniversalPartyMember> priority,
        HashSet<uint> towerTargetIds,
        int towerCount)
    {
        if (towerCount <= 0) return [];

        return priority
            .Where(x => towerTargetIds.Contains(x.IGameObject.EntityId))
            .Take(towerCount)
            .ToList();
    }

    private static IEnumerable<(uint TakerId, uint TargetId)> BuildDestinationAssignments(
        IReadOnlyList<UniversalPartyMember> takers,
        IReadOnlyList<UniversalPartyMember> targets)
    {
        var count = Math.Min(takers.Count, targets.Count);
        for (var i = 0; i < count; i++)
            yield return (takers[i].IGameObject.EntityId, targets[i].IGameObject.EntityId);
    }

    private static bool IsSupport(IPlayerCharacter player)
    {
        return IsSupportRole(player.GetRole());
    }

    private static bool IsSupportRole(CombatRole role)
    {
        return role is CombatRole.Tank or CombatRole.Healer;
    }

    private void ApplyDisplay()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

        var me = BasePlayer;
        if (me == null) return;

        if (Controller.TryGetElementByName("SelfInstruction", out var selfText))
        {
            selfText.Enabled = !string.IsNullOrWhiteSpace(_currentInstruction);
            selfText.SetRefPosition(me.Position);
            selfText.overlayText = _currentInstruction;
        }

        if (_hasMyDestination)
        {
            var color = Controller.AttentionColor;

            if (Controller.TryGetElementByName("Destination", out var destination))
            {
                destination.Enabled = true;
                destination.SetRefPosition(_myDestination);
                destination.color = color;
                destination.overlayText = _state == State.InitialLineup
                    ? C.StartOverlayText.Get()
                    : C.TowerOverlayText.Get();
            }
        }
    }

    private void ResetState()
    {
        _firstImageStarted = false;
        _gravenImageCount = 0;
        ClearActiveState();
    }

    private void ClearActiveState()
    {
        _state = State.None;
        _waveTargets.Clear();
        _pulseWaveTargets.Clear();
        _currentInstruction = "";
        _hasMyDestination = false;
        _myDestination = Vector3.Zero;
        _towerResolveCount = 0;
        _waveCannonTargetCollectionStarted = false;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    private enum State
    {
        None,
        CollectingFirstImageTethers,
        InitialLineup,
        Solved,
        WaveCannonSeen
    }

    private readonly record struct WaveTarget(string Name, uint Player, uint Source, CombatRole Role, Vector3 Position);

    public class Config : IEzConfig
    {
        public InternationalString BaitWaveCannonText = new()
        {
            En = "BAIT Wave Cannon, do not soak",
            Jp = "波動砲を外へ、塔は踏まない"
        };

        public InternationalString InitialLineupText = new()
        {
            En = "Wave Cannon: initial lineup",
            Jp = "波動砲: 初期位置へ"
        };

        public float LineupCenterX = 100.0f;
        public float LineupSpacing = 5.0f;
        public float LineupZ = 100.0f;

        public InternationalString NoWaveTowerText = new()
        {
            En = "No Wave tower: hold position",
            Jp = "波動砲塔なし: 待機"
        };

        public PriorityData PriorityData = new()
        {
            Name = "Wave Cannon tower priority",
            Description = "Default: H2 H1 ST MT | D1 D2 D3 D4",
            PriorityLists =
            [
                new PriorityList
                {
                    IsRole = true,
                    List =
                    [
                        new JobbedPlayer { Role = RolePosition.H2 },
                        new JobbedPlayer { Role = RolePosition.H1 },
                        new JobbedPlayer { Role = RolePosition.T2 },
                        new JobbedPlayer { Role = RolePosition.T1 },
                        new JobbedPlayer { Role = RolePosition.M1 },
                        new JobbedPlayer { Role = RolePosition.M2 },
                        new JobbedPlayer { Role = RolePosition.R1 },
                        new JobbedPlayer { Role = RolePosition.R2 }
                    ]
                }
            ]
        };

        public InternationalString SetPriorityListText = new()
        {
            En = "Set priority list",
            Jp = "優先リスト未設定"
        };

        public InternationalString StartOverlayText = new()
        {
            En = "Start",
            Jp = "初期"
        };

        public InternationalString TakeDpsTowerText = new()
        {
            En = "TAKE DPS tower",
            Jp = "DPS塔を踏む"
        };

        public InternationalString TakeSupportTowerText = new()
        {
            En = "TAKE support tower",
            Jp = "TH塔を踏む"
        };

        public InternationalString TowerOverlayText = new()
        {
            En = "Tower",
            Jp = "塔"
        };

        public bool TowerTakersAreNonTargets = true;
        public bool UseFixedInitialLineup = true;

        public InternationalString WaveTargetWaitText = new()
        {
            En = "Wave target, wait for priority",
            Jp = "波動砲対象、優先確認"
        };
    }
}
