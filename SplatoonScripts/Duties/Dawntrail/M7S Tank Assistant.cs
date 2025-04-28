using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
internal unsafe partial class M7S_Tank_Assistant :SplatoonScript
{
    private const uint kSinisterSeed = 42349u;
    private const uint kBloomingAbominationNameId = 0x35BBu;
    private readonly Dictionary<string, Vector3> Phase1Pos = new Dictionary<string, Vector3>()
    {
        { "N", new Vector3(100f, 0f, 80f) },
        { "E", new Vector3(120f, 0f, 100f) },
        { "S", new Vector3(100f, 0f, 120f) },
        { "W", new Vector3(80f, 0f, 100f) },
    };
    private readonly Dictionary<string, Vector3> Phase2Pos = new Dictionary<string, Vector3>()
    {
        { "N", new Vector3(100f, -200f, -15f) },
        { "E", new Vector3(120f, -200f, 5f) },
        { "S", new Vector3(100f, -200f, 25f) },
        { "W", new Vector3(80f, -200f, 5f) },
    };

    public override HashSet<uint>? ValidTerritories { get; } = [1261];
#pragma warning disable VSSpell001 // Spell Check
    public override Metadata? Metadata => new(1, "Redmoon");
#pragma warning restore VSSpell001 // Spell Check

    private bool _gimmickActive = false;
    private bool _mobSpawned = false;
    private int _scatterSeedCounts = 0;
    private List<(string, uint)> _mobIdByPosList = new List<(string, uint)>();
    private IBattleChara?[] _bloomingAbominationEnemyList => GetBloomingAbominationList();
    private List<IBattleChara> _bloomingAbominationList =>
        Svc.Objects.OfType<IBattleChara>().Where(x => x.NameId == kBloomingAbominationNameId).ToList();

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == kSinisterSeed)
        {
            _scatterSeedCounts++;
            _gimmickActive = true;
        }

        if (!_gimmickActive) return;
    }

    public override void OnUpdate()
    {
        if (!_gimmickActive) return;
        if (_mobSpawned &&
            Svc.Objects.Where(x => x is IBattleChara npc && npc.NameId == kBloomingAbominationNameId).Count() == 0)
        {
            _gimmickActive = false;
            _mobSpawned = false;
            _mobIdByPosList.Clear();
        }
        if (_bloomingAbominationList.Count() > 0)
        {
            _mobSpawned = true;
        }

        if (!_mobSpawned) return;

        if (_config.UseTargetEnforcer)
        {
            UseTargetEnforcer();
        }
    }

    private void UseTargetEnforcer()
    {
        // Mapping of positions to directions
        if (_mobIdByPosList.Count == 0)
        {
            if (_scatterSeedCounts == 1)
            {
                foreach (var mob in _bloomingAbominationList)
                {
                    var pos = mob.Position;
                    var closestPos = Phase1Pos.OrderBy(x => Vector3.Distance(pos, x.Value)).First();
                    _mobIdByPosList.Add((closestPos.Key, mob.EntityId));
                }
            }
            else if (_scatterSeedCounts == 2)
            {
                foreach (var mob in _bloomingAbominationList)
                {
                    var pos = mob.Position;
                    var closestPos = Phase2Pos.OrderBy(x => Vector3.Distance(pos, x.Value)).First();
                    _mobIdByPosList.Add((closestPos.Key, mob.EntityId));
                }
            }
        }
        if (_mobIdByPosList.Count == 0) return;

        // Wait Targetable
        if (_bloomingAbominationList.All(x => x.IsTargetable == false)) return;

        // Targeting in order N, E, S, W
        foreach (var mob in _mobIdByPosList)
        {
            if (mob.Item1 == "N" && !_config.TargetNorth) continue;
            if (mob.Item1 == "E" && !_config.TargetEast) continue;
            if (mob.Item1 == "S" && !_config.TargetSouth) continue;
            if (mob.Item1 == "W" && !_config.TargetWest) continue;

            if (BasePlayer.TargetObjectId == mob.Item2)
            {
                if (mob.Item2.TryGetObject(out var mobObj))
                {
                    if (mobObj.TargetObjectId == BasePlayer.EntityId)
                    {
                        continue;
                    }
                    else
                    {
                        if (Svc.Targets.Target == null) return;
                        if (Svc.Targets.Target.EntityId == mob.Item2) return;
                        Svc.Targets.SetTarget(mobObj);
                        return;
                    }
                }
            }
        }
    }

    private void UseAutoInterject()
    {
        switch (_config.UseAutoInterjectType)
        {
            case Config.InterjectType.UseTooCloseMob:
                UseAutoInterjectTooCloseMob();
                break;
            case Config.InterjectType.FindAboveEnemyList:
                UseAutoInterjectFindAboveEnemyList();
                break;
            case Config.InterjectType.FindBelowEnemyList:
                UseAutoInterjectFindBelowEnemyList();
                break;
            case Config.InterjectType.WaitAtleast1Mob:
                UseAutoInterjectWaitAtleast1Mob();
                break;
            default:
                break;
        }
    }

    private void UseAutoInterjectTooCloseMob()
    {
        if (_bloomingAbominationList.Count() == 0) return;
        foreach (var mob in _bloomingAbominationEnemyList.OrderBy(x => Vector3.Distance(Player.Position, x.Position)))
        {
            if (mob == null) continue;

            if (mob.IsTargetable && Player.DistanceTo(mob) < mob.HitboxRadius)
            {
                Svc.Targets.SetTarget(mob);
                return;
            }
        }
    }

    private void UseAutoInterjectFindAboveEnemyList()
    {
        if (_bloomingAbominationList.Count() == 0) return;
        foreach (var mob in _bloomingAbominationList)
        {
            if (mob.IsTargetable && mob.Position.Z > Player.Position.Z)
            {
                Svc.Targets.SetTarget(mob);
                return;
            }
        }
    }

    private void UseAutoInterjectFindBelowEnemyList()
    {
        if (_bloomingAbominationList.Count() == 0) return;
        foreach (var mob in _bloomingAbominationList)
        {
            if (mob.IsTargetable && mob.Position.Z < Player.Position.Z)
            {
                Svc.Targets.SetTarget(mob);
                return;
            }
        }
    }

    private void UseAutoInterjectWaitAtleast1Mob()
    {
        if (_bloomingAbominationList.Count() == 0) return;
        foreach (var mob in _bloomingAbominationList)
        {
            if (mob.IsTargetable)
            {
                Svc.Targets.SetTarget(mob);
                return;
            }
        }
    }

    private IBattleChara?[] GetBloomingAbominationList()
    {
        var list = new IBattleChara?[8];
        var array = AtkStage.Instance()->AtkArrayDataHolder->NumberArrays[21];
        var characters =
            Svc.Objects.OfType<IBattleChara>().Where(x => x.NameId == kBloomingAbominationNameId).ToArray();
        for (int i = 0; i < 8; i++)
        {
            var id = *(uint*)&array->IntArray[8 + (i * 6)];
            if (id != 0xE0000000)
            {
                list[i] = characters.FirstOrDefault(x => x.EntityId == id);
            }
        }
        return list;
    }
}

// GUI
internal unsafe partial class M7S_Tank_Assistant
{
    private Config _config => Controller.GetConfig<Config>();
    private string _basePlayerOverride = "";
    private IPlayerCharacter BasePlayer
    {
        get
        {
            if (_basePlayerOverride == "")
                return Player.Object;
            return Svc.Objects.OfType<IPlayerCharacter>()
                .FirstOrDefault(x => x.Name.ToString().EqualsIgnoreCase(_basePlayerOverride)) ?? Player.Object;
        }
    }

    public class Config :IEzConfig
    {
        public bool UseTargetEnforcer = false;
        public bool TargetNorth = false;
        public bool TargetEast = false;
        public bool TargetSouth = false;
        public bool TargetWest = false;
        public bool UseAutoInterject = false;
        public enum InterjectType
        {
            UseTooCloseMob = 0,
            FindAboveEnemyList = 1,
            FindBelowEnemyList = 2,
            WaitAtleast1Mob = 3,
        }
        public InterjectType UseAutoInterjectType = InterjectType.UseTooCloseMob;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Use Target Enforcer", ref _config.UseTargetEnforcer);
        if (_config.UseTargetEnforcer)
        {
            bool prevTargetNorth = _config.TargetNorth;
            bool prevTargetEast = _config.TargetEast;
            bool prevTargetSouth = _config.TargetSouth;
            bool prevTargetWest = _config.TargetWest;
            ImGui.Checkbox("Target North", ref _config.TargetNorth);
            ImGui.Checkbox("Target East", ref _config.TargetEast);
            ImGui.Checkbox("Target South", ref _config.TargetSouth);
            ImGui.Checkbox("Target West", ref _config.TargetWest);
            int trueCount = 0;
            foreach (var targetConfig in new[] { _config.TargetNorth, _config.TargetEast, _config.TargetSouth, _config.TargetWest })
            {
                if (targetConfig) trueCount++;
            }
            if (trueCount > 2)
            {
                // Revert to previous state
                _config.TargetNorth = prevTargetNorth;
                _config.TargetEast = prevTargetEast;
                _config.TargetSouth = prevTargetSouth;
                _config.TargetWest = prevTargetWest;
            }
        }
        ImGui.Checkbox("Use Auto Interject", ref _config.UseAutoInterject);
        if (_config.UseAutoInterject)
        {
            ImGui.Text("Interject Type");
            ImGui.BeginCombo("Interject Type", _config.UseAutoInterjectType.ToString());
            foreach (var type in Enum.GetValues(typeof(Config.InterjectType)))
            {
                if (ImGui.Selectable(type.ToString()))
                {
                    _config.UseAutoInterjectType = (Config.InterjectType)type;
                }
                ImGui.EndCombo();
            }
        }
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.SetNextItemWidth(200);
            ImGui.InputText("Player override", ref _basePlayerOverride, 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.BeginCombo("Select..", "Select..."))
            {
                foreach (var x in Svc.Objects.OfType<IPlayerCharacter>())
                {
                    if (x.GetRole() != CombatRole.Tank) continue;
                    if (ImGui.Selectable(x.GetNameWithWorld()))
                    {
                        _basePlayerOverride = x.Name.ToString();
                    }
                }
                ImGui.EndCombo();
            }
        }
    }
}
