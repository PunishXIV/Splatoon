using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public unsafe class M12S_P2_Clones_1_Navigation : SplatoonScript
{
    public enum Corner
    {
        NE,
        SE,
        SW,
        NW
    }

    public enum RoleSlot
    {
        MeleeLeft,
        MeleeRight,
        RangeLeft,
        RangeRight
    }

    private const uint DebuffId = 3323;
    private const float PosDetectRadius = 3.5f;
    private const uint DarknessCast = 46303;
    private const int DebugElementCount = 8;
    
    private static readonly Vector3 PosNE = new(108f, 0f, 92f);
    private static readonly Vector3 PosSE = new(108f, 0f, 108f);
    private static readonly Vector3 PosSW = new(92f, 0f, 108f);
    private static readonly Vector3 PosNW = new(92f, 0f, 92f);

    private static readonly Vector3 NavMeleeLeft = new(102.5f, 0f, 93.2f);
    private static readonly Vector3 NavMeleeRight = new(93.5f, 0f, 101.8f);
    private static readonly Vector3 NavRangeLeft = new(113.5f, 0f, 99f);
    private static readonly Vector3 NavRangeRight = new(99f, 0f, 114f);
    private static readonly Vector3 NavMeleeDebuff = new(101f, 0f, 101.5f);
    private static readonly Vector3 NavRangeDebuff = new(86.5f, 0f, 100.5f);
    private static readonly RoleSlot[] DebugRoleOrder =
    [
        RoleSlot.MeleeLeft,
        RoleSlot.MeleeRight,
        RoleSlot.RangeLeft,
        RoleSlot.RangeRight
    ];

    private Phase CurrentPhase = Phase.Idle;
    private List<uint> DarknessClones = [];
    private Corner? DetectedNorth;
    private uint MasterDarknessClone;
    public override Metadata Metadata { get; } = new(2, "Garume");
    public override HashSet<uint>? ValidTerritories { get; } = [1327];

    private Config C => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Nav",
            """{"Name":"","refX":100.0,"refY":100.0,"radius":0.7,"color":3357671168,"Filled":false,"fillIntensity":0.5,"thicc":9.0,"tether":true}""");
        for (var i = 0; i < DebugElementCount; i++)
        {
            Controller.RegisterElementFromCode($"Debug{i}",
                """{"Name":"","radius":0.7,"Filled":false,"fillIntensity":0.5,"thicc":9.0}""");
            if (Controller.TryGetElementByName($"Debug{i}", out var e))
                e.color = EColor.RedBright.ToUint();
        }
    }

    public override void OnReset()
    {
        CurrentPhase = Phase.Idle;
        DetectedNorth = null;
        MasterDarknessClone = 0;
        DarknessClones.Clear();
    }

    public override void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if (packet->ActionID == 46296 && CurrentPhase == Phase.Idle) CurrentPhase = Phase.WaitForDebuff;

        if (packet->ActionID == 46368) CurrentPhase = Phase.End;
    }

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.EnumCombo("Wait position", ref C.WaitSpot);
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.EnumCombo("Role position", ref C.RoleSlot);

        ImGui.Text("Bait Color:");
        ImGuiComponents.HelpMarker(
            "Change the color of the bait and the text that will be displayed on your bait.\nSetting different values makes it rainbow.");
        ImGui.Indent();
        ImGui.ColorEdit4("Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.SameLine();
        ImGui.ColorEdit4("Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
        ImGui.Unindent();
        ImGui.Checkbox("Show Debug", ref C.ShowDebug);

        if (ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text($"Current Phase: {CurrentPhase}");
            ImGui.Text($"Detected North: {DetectedNorth}");
            ImGui.Text($"Darkness Clones Count: {DarknessClones.Count}");
            ImGui.Text($"Master Darkness Clone ID: {MasterDarknessClone}");
        }
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        UpdateDarkClones();
        UpdateNavigation();
        UpdateDebug();
    }

    private void UpdateDarkClones()
    {
        foreach (var x in Svc.Objects.OfType<IBattleNpc>())
            if (DarknessClones.Count != 2 && x.IsCasting && x.CastActionId == DarknessCast)
                MasterDarknessClone = x.EntityId;

        if (DarknessClones.Count < 2 && MasterDarknessClone != 0 &&
            MasterDarknessClone.TryGetBattleNpc(out var darkness))
            DarknessClones = Svc.Objects.OfType<IBattleNpc>()
                .Where(x => x.DataId == 19204 &&
                            Vector3.Distance(darkness.Position, x.Position).ApproximatelyEquals(5f, 0.1f))
                .Select(x => x.EntityId)
                .ToList();
    }

    private void UpdateNavigation()
    {
        var e = Controller.GetElementByName("Nav");
        e.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
        switch (CurrentPhase)
        {
            case Phase.WaitForDebuff:
                if (TryGetDarkDebuffHolder(out _))
                {
                    CurrentPhase = Phase.DetectNorth;
                }
                else
                {
                    e.Enabled = true;
                    var pos = C.WaitSpot switch
                    {
                        Corner.NE => PosNE,
                        Corner.SE => PosSE,
                        Corner.SW => PosSW,
                        Corner.NW => PosNW,
                        _ => PosNE
                    };
                    e.SetRefPosition(pos);
                }

                break;
            case Phase.DetectNorth:
                if (!TryGetDarkDebuffHolder(out var reminingTime))
                {
                    CurrentPhase = Phase.Idle;
                    DetectedNorth = null;
                    break;
                }

                if (reminingTime > 8f) break;
                Corner north = default;
                var bestDistance = float.MaxValue;
                foreach (var cloneId in DarknessClones)
                {
                    if (!cloneId.TryGetBattleNpc(out var clone)) continue;
                    var dist = Vector3.Distance(clone.Position, PosNE);
                    if (dist <= PosDetectRadius && dist < bestDistance)
                    {
                        bestDistance = dist;
                        north = Corner.NE;
                    }

                    dist = Vector3.Distance(clone.Position, PosSE);
                    if (dist <= PosDetectRadius && dist < bestDistance)
                    {
                        bestDistance = dist;
                        north = Corner.SE;
                    }

                    dist = Vector3.Distance(clone.Position, PosSW);
                    if (dist <= PosDetectRadius && dist < bestDistance)
                    {
                        bestDistance = dist;
                        north = Corner.SW;
                    }

                    dist = Vector3.Distance(clone.Position, PosNW);
                    if (dist <= PosDetectRadius && dist < bestDistance)
                    {
                        bestDistance = dist;
                        north = Corner.NW;
                    }
                }

                if (bestDistance != float.MaxValue)
                {
                    DetectedNorth = north;
                    CurrentPhase = Phase.Navigate;
                }

                break;
            case Phase.Navigate:
                if (!TryGetDarkDebuffHolder(out _))
                {
                    CurrentPhase = Phase.End;
                    DetectedNorth = null;
                    break;
                }

                if (DetectedNorth == null) break;
                var hasDebuff = BasePlayer.StatusList.Any(s => s.StatusId == DebuffId);
                var basePos = GetBasePosition(C.RoleSlot, hasDebuff);
                var rotation = GetRotation(DetectedNorth.Value);
                var rotated = MathHelper.RotateWorldPoint(new Vector3(100,0,100), rotation.DegToRad(), basePos);
                e.Enabled = true;
                e.SetRefPosition(rotated);
                break;
        }
    }

    private void UpdateDebug()
    {
        if (!C.ShowDebug) return;

        var north = DetectedNorth ?? Corner.SE;
        var rotation = GetRotation(north);
        var index = 0;
        foreach (var role in DebugRoleOrder)
        {
            SetDebugElement(index++, GetBasePosition(role, false), rotation);
        }

        foreach (var role in DebugRoleOrder)
        {
            SetDebugElement(index++, GetBasePosition(role, true), rotation);
        }
    }

    private void SetDebugElement(int index, Vector3 basePos, float rotation)
    {
        if (!Controller.TryGetElementByName($"Debug{index}", out var e)) return;
        e.Enabled = true;
        var rotated = MathHelper.RotateWorldPoint(new Vector3(100,0,100),rotation.DegToRad(), basePos);
        e.SetRefPosition(rotated);
    }

    private static Vector3 GetBasePosition(RoleSlot roleSlot, bool hasDebuff)
    {
        if (hasDebuff)
        {
            return roleSlot is RoleSlot.MeleeLeft or RoleSlot.MeleeRight ? NavMeleeDebuff : NavRangeDebuff;
        }

        return roleSlot switch
        {
            RoleSlot.MeleeLeft => NavMeleeLeft,
            RoleSlot.MeleeRight => NavMeleeRight,
            RoleSlot.RangeLeft => NavRangeLeft,
            RoleSlot.RangeRight => NavRangeRight,
            _ => NavMeleeLeft
        };
    }

    private static float GetRotation(Corner north)
    {
        return north switch
        {
            Corner.SE => 0f,
            Corner.SW => 90f,
            Corner.NW => 180f,
            Corner.NE => 270f,
            _ => 0f
        };
    }

    private bool TryGetDarkDebuffHolder(out float remaining)
    {
        foreach (var player in Svc.Objects.OfType<IPlayerCharacter>())
        {
            var status = player.StatusList.FirstOrDefault(s => s.StatusId == DebuffId);
            if (status == null) continue;
            remaining = status.RemainingTime;
            return true;
        }

        remaining = 0f;
        return false;
    }

    private enum Phase
    {
        Idle,
        WaitForDebuff,
        DetectNorth,
        Navigate,
        End
    }

    public class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public RoleSlot RoleSlot = RoleSlot.MeleeLeft;
        public Corner WaitSpot = Corner.NE;
        public bool ShowDebug = false;
    }
}
