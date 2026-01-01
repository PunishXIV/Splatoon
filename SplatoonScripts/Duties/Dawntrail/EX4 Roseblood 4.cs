using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.GameHelpers.LegacyPlayer;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Splatoon.Data.MapEffectNames;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraFX.Interop.Windows;
using static Splatoon.Data.MapEffectNames.MapEffects;
using Player = ECommons.GameHelpers.Player;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public unsafe class EX4_Roseblood_4 : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1271];

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("North_MeleeLeft", """{"Name":"","refX":102.0,"refY":95.0,"radius":1.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("North_MeleeRight", """{"Name":"","refX":98.0,"refY":95.0,"radius":1.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("North_RangedLeft", """{"Name":"","refX":104.0,"refY":91.0,"radius":1.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("North_RangedRight", """{"Name":"","refX":96.5,"refY":91.0,"radius":1.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("South_MeleeLeft", """{"Name":"","refX":98.0,"refY":105.0,"radius":1.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("South_MeleeRight", """{"Name":"","refX":102.0,"refY":105.0,"radius":1.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("South_RangedLeft", """{"Name":"","refX":96.5,"refY":109.0,"radius":1.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("South_RangedRight", """{"Name":"","refX":104.0,"refY":109.0,"radius":1.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");

        Controller.RegisterElementFromCode("West_UpperPart", """{"Name":"","refX":87.0,"refY":99.0,"radius":1.0,"color":3371826944,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("West_LowerPart", """{"Name":"","refX":87.0,"refY":101.0,"radius":1.0,"color":3371826944,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("East_UpperPart", """{"Name":"","refX":112.5,"refY":99.0,"radius":1.0,"color":3371826944,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("East_LowerPart", """{"Name":"","refX":112.0,"refY":101.0,"radius":1.0,"color":3371826944,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");

        Controller.RegisterElementFromCode("ChainWait_South", """{"Name":"","refX":100.0,"refY":105.5,"radius":1.0,"color":3355508731,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("ChainWait_North", """{"Name":"","refX":100.0,"refY":95.0,"radius":1.0,"color":3355508731,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");

        Controller.RegisterElementFromCode("SafeNorth_RangedRight", """{"Name":"","refX":111.0,"refY":108.0,"radius":1.0,"color":3355508620,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("SafeNorth_RangedLeft", """{"Name":"","refX":88.5,"refY":108.5,"radius":1.0,"color":3355508620,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("SafeNorth_MeleeRight", """{"Name":"","refX":101.0,"refY":107.0,"radius":1.0,"color":3355508620,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("SafeNorth_MeleeLeft", """{"Name":"","refX":95.5,"refY":108.5,"radius":1.0,"color":3355508620,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        
        //Controller.RegisterElementFromCode("SafeSouth_RangedRight", """""");
        //Controller.RegisterElementFromCode("SafeSouth_RangedLeft", """""");
        //Controller.RegisterElementFromCode("SafeSouth_MeleeRight", """""");
        //Controller.RegisterElementFromCode("SafeSouth_MeleeLeft", """""");
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        //> [01.01.2026 22:04:57 +03:00] Message: VFX vfx/lockon/eff/x6fd_monyou_lock1v.avfx spawned on DNC npc id=0, model id=0, name npc id=0, position=<95.8418, -7.081991E-12, 109.17822>, name=Dancer
        if(IsDropNorthPatternActive() || IsDropSouthPatternActive())
        {
            if(IsDropNorthPatternFilled() || IsDropSouthPatternFilled())
            {
                //> [01.01.2026 22:36:22 +03:00] Message: VFX vfx/lockon/eff/x6fd_share_4m_5s_c0v.avfx spawned on SCH npc id=0, model id=0, name npc id=0, position=<99.87024, -2.873164E-12, 105.6687>, name=Scholar
                if(Controller.GetPartyMembers().TryGetFirst(x => AttachedInfo.VFXInfos.TryGetValue(x.Address, out var vfx) && vfx.TryGetValue("vfx/lockon/eff/x6fd_share_4m_5s_c0v.avfx", out var eff) && eff.AgeF < 9f, out var member))
                {
                    var pos = IsDropNorthPatternActive() ? C.StackPositionWhenNorth : C.StackPositionWhenSouth;
                    string element = "";
                    if(member.GetJob().IsDps() == Player.Job.IsDps())
                    {
                        element = $"{pos}_{(IsDropNorthPatternActive() ? "Lower" : "Upper")}Part";
                        //player stays away from red
                    }
                    else
                    {
                        element = $"{pos}_{(IsDropNorthPatternActive() ? "Upper" : "Lower")}Part";
                        //player goes to red
                    }
                    if(Controller.TryGetElementByName(element, out var e))
                    {
                        e.Enabled = true;
                    }
                }
                else
                {
                    var pos = IsDropNorthPatternActive() ? C.ChainPosWhenNorth : C.ChainPosWhenSouth;
                    if(Controller.TryGetElementByName($"ChainWait_{pos}", out var e))
                    {
                        e.Enabled = true;
                    }
                }
            }
            else
            {
                var prefix = IsDropNorthPatternActive() ? "North" : "South";
                if(AttachedInfo.VFXInfos.TryGetValue(Player.Object.Address, out var vfx) && vfx.TryGetValue("vfx/lockon/eff/x6fd_monyou_lock1v.avfx", out var eff) && eff.AgeF < 6f)
                {
                    //player has rose marker
                    if(Controller.TryGetElementByName($"{prefix}_{C.DropPosition}", out var e))
                    {
                        e.Enabled = true;
                    }
                }
                else
                {
                    if(Controller.GetPartyMembers().Any(x => AttachedInfo.VFXInfos.TryGetValue(x.Address, out var vfx) && vfx.TryGetValue("vfx/lockon/eff/x6fd_monyou_lock1v.avfx", out var eff) && eff.AgeF < 9f))
                    {
                        //player has no rose marker but other players have
                    }
                }
            }
        }
        
    }

    public enum DropPosition { MeleeLeft, MeleeRight, RangedLeft, RangedRight }
    public enum StackPosition { West, East }
    public enum ChainPosition { North, South }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public DropPosition DropPosition = DropPosition.MeleeLeft;
        public StackPosition StackPositionWhenNorth = StackPosition.West;
        public StackPosition StackPositionWhenSouth = StackPosition.West;
        public ChainPosition ChainPosWhenNorth = ChainPosition.North;
        public ChainPosition ChainPosWhenSouth = ChainPosition.North;
    }

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.EnumCombo("Marker drop position, looking at the boss", ref C.DropPosition);
        ImGuiEx.Text($"When North is filled with red tiles:");
        ImGui.Indent();
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.EnumCombo("Initial chain poisition##1", ref C.ChainPosWhenNorth);
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.EnumCombo("Stack poisition##1", ref C.StackPositionWhenNorth);
        ImGui.Unindent();
        ImGuiEx.Text($"When South is filled with red tiles:");
        ImGui.Indent();
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.EnumCombo("Initial chain poisition##2", ref C.ChainPosWhenSouth);
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.EnumCombo("Stack poisition##2", ref C.StackPositionWhenSouth);
        ImGui.Unindent();
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"IsDropNorthPatternActive(): {IsDropNorthPatternActive()}");
            ImGuiEx.Text($"IsSouthPatternActive(): {this.IsDropSouthPatternActive()}");
            ImGuiEx.Text($"IsDropNorthPatternFilled(): {this.IsDropNorthPatternFilled()}");
            ImGuiEx.Text($"IsDropSouthPatternFilled(): {IsDropSouthPatternFilled()}");
            foreach(var x in Enum.GetValues<RecollectionEx>())
            {

                ImGuiEx.Text($"{x}: {Controller.GetMapEffect(x) == 64}");
            }
        }
    }

    bool IsDropNorthPatternActive()
    {
        return Controller.GetMapEffect(RecollectionEx.Inner_3) == 64
            && Controller.GetMapEffect(RecollectionEx.Inner_6) == 64
            && Controller.GetMapEffect(RecollectionEx.Outer_2) == 64
            && (Controller.GetMapEffect(RecollectionEx.Outer_4) == 64 || Controller.GetMapEffect(RecollectionEx.Outer_5) == 64)
            && Controller.GetMapEffect(RecollectionEx.Outer_7) == 64;
    }

    bool IsDropNorthPatternFilled()
    {
        return IsDropNorthPatternActive()
            && Controller.GetMapEffect(RecollectionEx.Inner_1) == 64
            && Controller.GetMapEffect(RecollectionEx.Inner_8) == 64
            && Controller.GetMapEffect(RecollectionEx.Outer_1) == 64
            && Controller.GetMapEffect(RecollectionEx.Outer_8) == 64;
    }

    bool IsDropSouthPatternActive()
    {
        return Controller.GetMapEffect(RecollectionEx.Inner_2) == 64
            && Controller.GetMapEffect(RecollectionEx.Inner_7) == 64
            && Controller.GetMapEffect(RecollectionEx.Outer_3) == 64
            && (Controller.GetMapEffect(RecollectionEx.Outer_1) == 64 || Controller.GetMapEffect(RecollectionEx.Outer_8) == 64)
            && Controller.GetMapEffect(RecollectionEx.Outer_6) == 64;
    }

    bool IsDropSouthPatternFilled()
    {
        return IsDropSouthPatternActive()
            && Controller.GetMapEffect(RecollectionEx.Inner_4) == 64
            && Controller.GetMapEffect(RecollectionEx.Inner_5) == 64
            && Controller.GetMapEffect(RecollectionEx.Outer_4) == 64
            && Controller.GetMapEffect(RecollectionEx.Outer_5) == 64;
    }
}
