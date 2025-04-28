using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M8S_Ultraviolent_Ray_4th :SplatoonScript
{
    /*
     * Constants and Types
     */
    #region Constants
    private enum State
    {
        Inactive = 0,
        Active,
        Wait,
        PrePosition,
        Show,
    }

    private enum LandPosition
    {
        None = 0,
        UpLeft,
        UpRight,
        Left,
        Right,
        Down,
    }

    private enum NearFar
    {
        None = 0,
        Near,
        Far,
    }

    private class PartyData
    {
        public uint EntityId = 0u;
        public NearFar NearFar = NearFar.None;
        public Job Job = 0;
        public CombatRole Role = CombatRole.NonCombat;
        public CombatRole PairJob = CombatRole.NonCombat;
        public LandPosition LandPosition = LandPosition.None;
        public bool IsTargeted = false;
        public LandPosition GotoPosition = LandPosition.None;
    }

    private const string MarkerVfxPath = "vfx/lockon/eff/m0005sp_19o0t.avfx";
    private const uint kUltraviolentRayCastId = 42076;
    private const uint kLoneWolfsLamentCastId = 42115;
    private const uint kHerosBlowCastId = 42080;

    private Dictionary<LandPosition, Vector3> LandPositionData = new()
    {
        { LandPosition.UpLeft, new Vector3(89.71376f, -150f, 85.8422f) },
        { LandPosition.UpRight, new Vector3(110.2862f, -150f, 85.8422f) },
        { LandPosition.Left, new Vector3(83.35651f, -150f, 105.4078f) },
        { LandPosition.Right, new Vector3(116.6435f, -150f, 105.4078f) },
        { LandPosition.Down, new Vector3(100f, -150f, 117.5f) }
    };
    #endregion

    /*
     * Public values
     */
    #region public values
    public override HashSet<uint>? ValidTerritories => [1263];
    public override Metadata? Metadata => new(1, "Redmoon");
    #endregion

    /*
     * Private values
     */
    #region private values
    private Config C => Controller.GetConfig<Config>();
    private List<PartyData> _partyDatas = new();
    private string _basePlayerOverride = "";
    private State _state = State.Inactive;
    #endregion

    /*
     * Public values
     */
    #region public values
    public override void OnSetup()
    {
        var element = new Element(0)
        {
            radius = 5f,
            thicc = 15f,
            tether = true,
            Donut = 0.3f
        };
        Controller.RegisterElement("Bait", element);
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == kLoneWolfsLamentCastId)
        {
            foreach (var pc in FakeParty.Get())
            {
                var partyData = new PartyData
                {
                    EntityId = pc.EntityId,
                    Role = pc.GetRole(),
                    Job = pc.GetJob(),
                };
                _partyDatas.Add(partyData);
            }
            _state = State.Active;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!set.Action.HasValue) return;

        if (set.Action.Value.RowId == kHerosBlowCastId)
        {
            _state = State.PrePosition;
            GotoPrePosition();
        }

        if (set.Action.Value.RowId == kUltraviolentRayCastId)
        {
            this.OnReset();
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (_state == State.Inactive) return;
        // Near
        if (data2 == 0 && data3 == 317 && data5 == 15)
        {
            var pcSourceData = _partyDatas.FirstOrDefault(x => x.EntityId == source);
            var pcTargetData = _partyDatas.FirstOrDefault(x => x.EntityId == target);
            if (!source.TryGetObject(out var pcSource) ||
                !target.TryGetObject(out var pcTarget) ||
                !(pcSource is IPlayerCharacter pcSourceObj) ||
                !(pcTarget is IPlayerCharacter pcTargetObj)) return;

            if (pcSourceData != null && pcTargetData != null)
            {
                pcSourceData.NearFar = NearFar.Near;
                pcTargetData.NearFar = NearFar.Near;
                pcSourceData.PairJob = pcTargetObj.GetRole();
                pcTargetData.PairJob = pcSourceObj.GetRole();
            }
        }
        // Far
        else if (data2 == 0 && data3 == 318 && data5 == 15)
        {
            var pcSourceData = _partyDatas.FirstOrDefault(x => x.EntityId == source);
            var pcTargetData = _partyDatas.FirstOrDefault(x => x.EntityId == target);
            if (!source.TryGetObject(out var pcSource) ||
                !target.TryGetObject(out var pcTarget) ||
                !(pcSource is IPlayerCharacter pcSourceObj) ||
                !(pcTarget is IPlayerCharacter pcTargetObj)) return;
            if (pcSourceData != null && pcTargetData != null)
            {
                pcSourceData.NearFar = NearFar.Far;
                pcTargetData.NearFar = NearFar.Far;
                pcSourceData.PairJob = pcTargetObj.GetRole();
                pcTargetData.PairJob = pcSourceObj.GetRole();
            }
        }

        if (_partyDatas.All(x => x.NearFar != NearFar.None))
        {
            var ret = CheckLoneWolfGimmickPosition();
            if (ret)
            {
                _state = State.Wait;
            }
            else
            {
                this.OnReset();
                PluginLog.Debug("Lone Wolf Gimmick failed");
                return;
            }
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (_state == State.Inactive) return;
        if (vfxPath == MarkerVfxPath)
        {
            var pcData = _partyDatas.FirstOrDefault(x => x.EntityId == target);
            if (pcData == null)
            {
                PluginLog.Debug($"VFX spawn: {target} not found");
                this.OnReset();
                return;
            }
            else
            {
                pcData.IsTargeted = true;
            }

            if (_partyDatas.Where(x => x.IsTargeted).Count() >= 5)
            {
                CheckGotoPosition();
                ShowGotoPosition();
                _state = State.Show;
            }
        }
    }

    public override void OnUpdate()
    {
        if (_state == State.Inactive) return;
        Controller.GetRegisteredElements().Where(x => x.Value.Enabled)
                .Each(x => x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
    }

    public override void OnReset()
    {
        _state = State.Inactive;
        _partyDatas.Clear();
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("If true, pure healer will move to the right");
        ImGui.Checkbox("Move pure healer", ref C.movePureHealer);
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            foreach (var pc in _partyDatas)
            {
                ImGui.Text($"EntityId: {pc.EntityId}");
                ImGui.Text($"NearFar: {pc.NearFar}");
                ImGui.Text($"Job: {pc.Job}");
                ImGui.Text($"Role: {pc.Role}");
                ImGui.Text($"PairJob: {pc.PairJob}");
                ImGui.Text($"LandPosition: {pc.LandPosition}");
                ImGui.Text($"IsTargeted: {pc.IsTargeted}");
                ImGui.Text($"GotoPosition: {pc.GotoPosition}");
                ImGui.Text($"==================================");
            }

            ImGui.SetNextItemWidth(200);
            ImGui.InputText("Player override", ref _basePlayerOverride, 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.BeginCombo("Select..", "Select..."))
            {
                foreach (var x in Svc.Objects.OfType<IPlayerCharacter>())
                    if (ImGui.Selectable(x.GetNameWithWorld()))
                        _basePlayerOverride = x.Name.ToString();
                ImGui.EndCombo();
            }
        }
    }
    #endregion

    /*
     * Private methods
     */
    #region private methods
    private bool CheckLoneWolfGimmickPosition()
    {
        foreach (var pc in _partyDatas)
        {
            if (pc.Role == CombatRole.Tank)
            {
                if (pc.NearFar == NearFar.Near)
                {
                    pc.LandPosition = LandPosition.UpLeft;
                }
                else
                {
                    pc.LandPosition = LandPosition.Left;
                }
            }
            else if (pc.Role == CombatRole.Healer)
            {
                pc.LandPosition = LandPosition.Down;
            }
            else if (pc.Role == CombatRole.DPS)
            {
                if (pc.NearFar == NearFar.Near &&
                    pc.PairJob == CombatRole.Tank)
                {
                    pc.LandPosition = LandPosition.UpRight;
                }
                else if (pc.NearFar == NearFar.Near &&
                         pc.PairJob == CombatRole.Healer)
                {
                    pc.LandPosition = LandPosition.Right;
                }
                else if (pc.NearFar == NearFar.Far &&
                         pc.PairJob == CombatRole.Tank)
                {
                    pc.LandPosition = LandPosition.Right;
                }
                else // (pc.NearFar == NearFar.Far &&
                     //  pc.PairJob == CombatRole.Healer)
                {
                    pc.LandPosition = LandPosition.UpRight;
                }
            }
        }

        if (_partyDatas.All(x => x.LandPosition != LandPosition.None)) return true;

        return false;
    }

    private void GotoPrePosition()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        var pc = _partyDatas.FirstOrDefault(x => x.EntityId == BasePlayer.EntityId);
        if (pc == null)
        {
            PluginLog.Debug($"GotoPrePosition: {BasePlayer.EntityId} not found");
            this.OnReset();
            return;
        }
        else
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            if (Controller.TryGetElementByName("Bait", out var element))
            {
                element.SetRefPosition(LandPositionData[pc.LandPosition]);
                element.Enabled = true;
            }
        }
    }

    private void CheckGotoPosition()
    {
        // DPS 4
        if (_partyDatas.Where(x => x.IsTargeted && x.Role == CombatRole.DPS).Count() == 4)
        {
            // Tank/Healer
            var pc = _partyDatas.FirstOrDefault(x => x.IsTargeted && x.Role != CombatRole.DPS);
            if (pc == null) return;
            pc.GotoPosition = LandPosition.Left;

            // DPS
            // UpRight
            var dps = _partyDatas.Where(x => x.IsTargeted && x.Role == CombatRole.DPS && x.LandPosition == LandPosition.UpRight);
            if (dps.Count() != 2) return;
            var dps1 = dps.First();
            var dps2 = dps.Last();
            if (dps1 == null || dps2 == null) return;

            var moveDps = AdjustJob(dps1.Job, dps2.Job);
            if (moveDps == dps1.Job)
            {
                dps1.GotoPosition = LandPosition.UpLeft;
                dps2.GotoPosition = LandPosition.UpRight;
            }
            else
            {
                dps1.GotoPosition = LandPosition.UpRight;
                dps2.GotoPosition = LandPosition.UpLeft;
            }

            // Right
            dps = _partyDatas.Where(x => x.IsTargeted && x.Role == CombatRole.DPS && x.LandPosition == LandPosition.Right);
            if (dps.Count() != 2) return;
            dps1 = dps.First();
            dps2 = dps.Last();
            if (dps1 == null || dps2 == null) return;

            moveDps = AdjustJob(dps1.Job, dps2.Job);
            if (moveDps == dps1.Job)
            {
                dps1.GotoPosition = LandPosition.Down;
                dps2.GotoPosition = LandPosition.Right;
            }
            else
            {
                dps1.GotoPosition = LandPosition.Right;
                dps2.GotoPosition = LandPosition.Down;
            }
        }
        // Healer/Tank 4
        else if (_partyDatas.Where(x => x.IsTargeted &&
                (x.Role is CombatRole.Healer or CombatRole.Tank)).Count() == 4)
        {
            // Tank
            var pc = _partyDatas.FirstOrDefault(x => x.LandPosition == LandPosition.UpLeft);
            if (pc == null) return;
            pc.GotoPosition = LandPosition.UpLeft;

            pc = _partyDatas.FirstOrDefault(x => x.LandPosition == LandPosition.Left);
            if (pc == null) return;
            pc.GotoPosition = LandPosition.Left;

            // Healer
            var healers = _partyDatas.Where(x => x.LandPosition == LandPosition.Down);
            if (healers.Count() != 2) return;
            var h1 = healers.First();
            var h2 = healers.Last();
            if (h1 == null || h2 == null) return;

            if (C.movePureHealer)
            {
                if (h1.Job is Job.SCH or Job.SGE)
                {
                    h1.GotoPosition = LandPosition.Down;
                    h2.GotoPosition = LandPosition.Right;
                }
                else
                {
                    h1.GotoPosition = LandPosition.Right;
                    h2.GotoPosition = LandPosition.Down;
                }
            }
            else
            {
                if (h1.Job is Job.SCH or Job.SGE)
                {
                    h1.GotoPosition = LandPosition.Right;
                    h2.GotoPosition = LandPosition.Down;
                }
                else
                {
                    h1.GotoPosition = LandPosition.Down;
                    h2.GotoPosition = LandPosition.Right;
                }
            }

            // DPS
            var dps = _partyDatas.Find(x => x.IsTargeted && x.Role == CombatRole.DPS);
            if (dps == null) return;
            dps.GotoPosition = LandPosition.UpRight;
        }
        // ERROR
        else
        {
            PluginLog.Debug("CheckGotoPosition: No DPS or Healer/Tank found");
            this.OnReset();
            return;
        }

        // No IsTargeted
        foreach (var pc in _partyDatas)
        {
            if (!pc.IsTargeted)
            {
                pc.GotoPosition = pc.LandPosition;
            }
        }
    }

    private void ShowGotoPosition()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        var pc = _partyDatas.FirstOrDefault(x => x.EntityId == BasePlayer.EntityId);
        if (pc == null) return;

        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if (Controller.TryGetElementByName("Bait", out var element))
        {
            element.SetRefPosition(LandPositionData[pc.GotoPosition]);
            element.Enabled = true;
        }
    }

    private Job AdjustJob(Job job1, Job job2)
    {
        // Melee DPS is High Priority
        if (job1 is Job.MNK or Job.NIN or Job.DRG or Job.RPR or Job.SAM or Job.VPR)
        {
            return job1;
        }
        if (job2 is Job.MNK or Job.NIN or Job.DRG or Job.RPR or Job.SAM or Job.VPR)
        {
            return job2;
        }
        // Ranged Physical DPS is Next Highest
        if (job1 is Job.BRD or Job.DNC or Job.MCH)
        {
            return job1;
        }
        else
        {
            return job2;
        }
    }

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
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public PriorityData PriorityData = new();
        public bool movePureHealer = false;
    }
    #endregion 
}