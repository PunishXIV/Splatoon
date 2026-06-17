using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameFunctions.VirtualTableClassifier;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public class P3_Blackhole_Lite : SplatoonScript<P3_Blackhole_Lite.Config>
{
    public override Metadata Metadata { get; } = new(5, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1363];

    ImGuiEx.RealtimeDragDrop<CardinalDirection>[] DragDrop = [new("CarDir0", x => x.ToString()), new("CarDir1", x => x.ToString()), new("CarDir2", x => x.ToString())];
    ImGuiEx.RealtimeDragDrop<CardinalDirection>[] DragDrop2 = [new("CarDir3", x => x.ToString()), new("CarDir4", x => x.ToString()), new("CarDir5", x => x.ToString())];

    uint Sequence = 0;
    bool? IsAccretion = null;

    IBattleNpc? Gigakefka => Svc.Objects.OfTypeIBattleNpc().FirstOrDefault(x => x.DataId == 19504 && x.HitboxRadius > 20f);

    public override void OnSetup()
    {
        Controller.RegisterElementsFromMultilineCode("""
            {"Name":"Tether0","type":2,"refX":107.77591,"refY":113.12219,"refZ":-1.9073486E-06,"offX":110.66847,"offY":99.43638,"offZ":1.9073486E-06,"radius":0.0,"color":3355508503,"Filled":false,"fillIntensity":0.5,"thicc":8.0,"refActorObjectID":1073765276,"refActorComparisonType":2,"tether":true}
            {"Name":"Tether1","type":2,"refX":107.77591,"refY":113.12219,"refZ":-1.9073486E-06,"offX":110.66847,"offY":99.43638,"offZ":1.9073486E-06,"radius":0.0,"color":3355508503,"Filled":false,"fillIntensity":0.5,"thicc":8.0,"refActorObjectID":1073765276,"refActorComparisonType":2,"tether":true}
            {"Name":"Tether2","type":2,"refX":107.77591,"refY":113.12219,"refZ":-1.9073486E-06,"offX":110.66847,"offY":99.43638,"offZ":1.9073486E-06,"radius":0.0,"color":3355508503,"Filled":false,"fillIntensity":0.5,"thicc":8.0,"refActorObjectID":1073765276,"refActorComparisonType":2,"tether":true}
            {"Name":"Idle","refX":100.0,"refY":100.0,"radius":4.0,"color":3355508490,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}
            {"Name":"HintTake","type":1,"radius":0.0,"color":3358457600,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":4278190335,"overlayVOffset":2.0,"overlayFScale":1.5,"thicc":0.0,"overlayText":"Sequence: # | Take tether","refActorType":1}
            {"Name":"HintIdle","type":1,"Enabled":false,"radius":0.0,"color":3358457600,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":4278386432,"overlayVOffset":2.0,"thicc":0.0,"overlayText":"Sequence: # | Idle","refActorType":1}
            """);

        for(int i = 0; i < 20; i++)
        {
            Controller.RegisterElementFromCode($$$"""{"Name":"Indicator{{{i}}}"}""");
        }
    }

    public override void OnReset()
    {
        Sequence = 0;
        IsAccretion = null;
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        if(Gigakefka != null)
        {
            int i = 0;
            foreach(var x in GetTetheredOrderedBlackholes())
            {
                i++;
                if(Controller.TryGetElementByName($"Indicator{i}", out var e))
                {
                    e.Enabled = true;
                    e.RefPosition = x.Value.Object.Position;
                    e.overlayText = $"{i}/{x.Key}/{x.Value.AngleDegrees:F1}";
                }
            }
        }
        var accretions = Controller.GetPartyMembers().Where(x => x.HasStatus(1604));
        if(accretions.Count() == 2)
        {
            IsAccretion = BasePlayer.HasStatus(1604);
        }
        int numElements = 0;
        if(C.UseKefkaRelative)
        {
            var tethers = GetTetheredOrderedBlackholes();
            var seq = C.Takers.SafeSelect((int)Sequence);
            var isTaker = false;
            if(seq != null && GetMyRole() != Taker.None)
            {
                for(int i = 0; i < tethers.Count; i++)
                {
                    var current = seq[i];
                    if(current == GetMyRole())
                    {
                        var myHole = tethers.CircularSelect((int)(C.ClockwiseNumber[i]) - 1);
                        DrawHole(ref numElements, myHole.Value.Object);
                        isTaker = true;
                    }
                }
            }
            if(!isTaker && tethers.Count > 0)
            {
                DrawIdle();
            }
        }
        else
        {
            var tethers = GetTetheredBlackholes();
            if(tethers.Count > 0)
            {
                var seq = C.Takers.SafeSelect((int)Sequence);
                if(seq != null)
                {
                    var isTaker = false;
                    HashSet<CardinalDirection> takenDirection = [];
                    for(int i = 0; i < seq.Count; i++)
                    {
                        var current = seq[i];
                        foreach(var dir in C.TetherPriorities[i])
                        {
                            if(!takenDirection.Contains(dir) && tethers.TryGetValue(dir, out var blackhole))
                            {
                                takenDirection.Add(dir);
                                if(current == GetMyRole() && GetMyRole() != Taker.None)
                                {
                                    DrawHole(ref numElements, blackhole);
                                    isTaker = true;
                                }
                                break;
                            }
                        }
                    }
                    if(!isTaker && tethers.Count > 0)
                    {
                        DrawIdle();
                    }
                }
            }
        }
    }

    void DrawHole(ref int numElements, IBattleNpc blackhole)
    {
        var name = $"Tether{numElements}";
        if(Controller.TryGetElementByName(name, out var e))
        {
            numElements++;
            var isMyTether = blackhole.GetTethers().Any(x => x.PairId == BasePlayer.ObjectId);
            e.Enabled = true;
            e.RefPosition = blackhole.Position;
            e.OffPosition = blackhole.GetTethers().FirstOrDefault()?.Pair?.Position ?? default;
            e.color = isMyTether ? EColor.GreenBright.ToUint() : Controller.AttentionColor;
            e.thicc = Controller.OriginalElements[name].thicc / (isMyTether ? 2 : 1);
        }
        DrawTake();
    }

    List<List<CardinalDirection>> GetTetherPriorities()
    {
        return (!C.TetherPrio2Same && GetTetheredBlackholes().Count == 2) ? C.TetherPriorities2 : C.TetherPriorities;
    }

    public Taker GetMyRole()
    {
        if(IsAccretion == true)
        {
            if(GetOwnNumber() == 1) return Taker.Accretion_1;
            if(GetOwnNumber() == 2) return Taker.Accretion_2;
        }
        else
        {
            if(GetOwnNumber() == 1) return Taker.Number_1;
            if(GetOwnNumber() == 2) return Taker.Number_2;
            if(GetOwnNumber() == 3) return Taker.Number_3;
        }
        return Taker.None;
    }

    public int GetOwnNumber()
    {
        if(BasePlayer.HasStatus(3004)) return 1;
        if(BasePlayer.HasStatus(3005)) return 2;
        if(BasePlayer.HasStatus(3006)) return 3;
        return 0;
    }

    //> > Action #47868 Nothingness cast on 19512/Black Hole effect on VPR npc id=8343, model id=1967, transform=0 data=19512 name=Black Hole ActionEffect|47868|19512|VPR|8343|1967|0|19512
    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(EzThrottler.Check("Nothingness"))
        {
            if(set.Action?.RowId == 47868)
            {
                EzThrottler.Throttle("Nothingness", 500, true);
                Sequence++;
            }
        }
    }

    void DrawIdle()
    {
        {
            if(Controller.TryGetElementByName("HintIdle", out var e))
            {
                e.Enabled = true;
                e.overlayText = Controller.OriginalElements["HintIdle"].overlayText.Replace("#", $"{Sequence + 1}");
            }
        }
        {
            if(Controller.TryGetElementByName("Idle", out var e))
            {
                e.Enabled = true;
                e.color = Controller.AttentionColor;
            }
        }
    }

    void DrawTake()
    {
        if(Controller.TryGetElementByName("HintTake", out var e))
        {
            e.Enabled = true;
            e.overlayText = Controller.OriginalElements["HintTake"].overlayText.Replace("#", $"{Sequence + 1}");
        }
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.TextWrapped($"Configure which tethers you will be taking during each wave according to your assigned number and accretion debuff.");
        ImGuiEx.TextV($"Mode: ");
        ImGui.SameLine();
        ImGuiEx.RadioButtonBool("Kefka relative", "True north", ref C.UseKefkaRelative, sameLine:true);
        if(ImGuiEx.BeginDefaultTable("table", ["Wave", "Tether 1", "Tether 2", "Tether 3"], extraFlags: ImGuiTableFlags.SizingStretchSame, nullifyFlags:ImGuiTableFlags.SizingFixedFit))
        {
            for(int i = 0; i < C.Takers.Count; i++)
            {
                ImGui.PushID($"{i}");
                var row = C.Takers[i];
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Wave {i + 1}");
                for(int k = 0; k < (i.EqualsAny(0, 9)?1:i.EqualsAny(1,8)?2:row.Count); k++)
                {
                    ImGui.PushID($"{k}");
                    ImGui.TableNextColumn();
                    ImGuiEx.SetNextItemFullWidth();
                    var x = row[k];
                    if(ImGuiEx.EnumCombo("##taker", ref x)) row[k] = x;
                    ImGui.PopID();
                }
                ImGui.PopID();
            }

            if(C.UseKefkaRelative)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Prio, CW from Kefka");
                for(int i = 0; i < 3; i++)
                {
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(100);
                    var x = C.ClockwiseNumber[i];
                    if(ImGui.InputUInt($"##prio{i}", ref x)) C.ClockwiseNumber[i] = x;
                }
            }
            else
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.Text($"Tether Priority\n(3 tethers)");
                for(int i = 0; i < 3; i++)
                {
                    ImGui.PushID($"d{i}");
                    ImGui.TableNextColumn();
                    var item = C.TetherPriorities[i];
                    DragDrop[i].Begin();
                    for(int k = 0; k < item.Count; k++)
                    {
                        DragDrop[i].NextRow();
                        DragDrop[i].DrawButtonDummy(item[k].ToString(), item, k);
                        ImGui.SameLine();
                        ImGuiEx.TextV($"{item[k]}");
                    }
                    DragDrop[i].End();
                    ImGui.PopID();
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.Text($"Tether Priority\n(2 tethers)");
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.SameLine();
                ImGuiEx.ButtonCheckbox(FontAwesomeIcon.Equals.ToIconString(), ref C.TetherPrio2Same, smallButton: true);
                ImGui.PopFont();
                ImGuiEx.Tooltip("Use same priority as 3 tethers");
                if(!C.TetherPrio2Same)
                {
                    for(int i = 0; i < 2; i++)
                    {
                        ImGui.PushID($"d2{i}");
                        ImGui.TableNextColumn();
                        var item = C.TetherPriorities2[i];
                        DragDrop2[i].Begin();
                        for(int k = 0; k < item.Count; k++)
                        {
                            DragDrop2[i].NextRow();
                            DragDrop2[i].DrawButtonDummy(item[k].ToString(), item, k);
                            ImGui.SameLine();
                            ImGuiEx.TextV($"{item[k]}");
                        }
                        DragDrop2[i].End();
                        ImGui.PopID();
                    }
                }
            }
            ImGui.EndTable();
        }
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Checkbox("IsAccretion", ref IsAccretion);
            ImGui.InputUInt("Sequence", ref Sequence);
            ImGuiEx.Text($"My role: {GetMyRole()}");
            ImGuiEx.Text($"Using prio2: {!C.TetherPrio2Same && GetTetheredBlackholes().Count == 2}");
        }
    }

    public Dictionary<CardinalDirection, IBattleNpc> GetTetheredBlackholes()
    {
        var ret = new Dictionary<CardinalDirection, IBattleNpc>();
        foreach(var x in Svc.Objects.OfTypeIBattleNpc().Where(x => x.DataId == 19512))
        {
            if(x.GetTethers().Count != 0)
            {
                ret[MathHelper.GetCardinalDirection(new(100, 0, 100), x.Position)] = x;
            }
        }
        return ret;
    }

    public OrderedDictionary<CardinalDirection, EnumerationResult<IBattleNpc>> GetTetheredOrderedBlackholes()
    {
        var ret = new OrderedDictionary<CardinalDirection, EnumerationResult<IBattleNpc>>();
        foreach(var x in MathHelper.EnumerateObjectsClockwiseEx(GetTetheredBlackholes(), x => x.Value.Position.ToVector2(), new(100, 100), -Gigakefka.Rotation.RadToDeg() - 5))
        {
            ret.Add(x.Object.Key, new(x.Object.Value, x.AngleDegrees));
        }
        return ret;
    }

    public class Config
    {
        public List<List<Taker>> Takers = [
            [Taker.Number_1, default, default],
            [default, default, default],
            [Taker.Number_1, default, Taker.Accretion_1],
            [Taker.Number_1, default, Taker.Accretion_1],
            [Taker.Number_2, default, Taker.Accretion_1],
            [Taker.Number_2, default, Taker.Accretion_2],
            [Taker.Number_2, default, Taker.Accretion_2],
            [Taker.Number_3, default, Taker.Accretion_2],
            [Taker.Number_3, Taker.Number_3, default],
            [default, default, default],
            ];
        public List<List<CardinalDirection>> TetherPriorities = [
            [CardinalDirection.East, CardinalDirection.West, CardinalDirection.South, CardinalDirection.North],
            [CardinalDirection.East, CardinalDirection.West, CardinalDirection.South, CardinalDirection.North],
            [CardinalDirection.East, CardinalDirection.West, CardinalDirection.South, CardinalDirection.North],
            ];
        public List<List<CardinalDirection>> TetherPriorities2 = [
            [CardinalDirection.East, CardinalDirection.West, CardinalDirection.South, CardinalDirection.North],
            [CardinalDirection.East, CardinalDirection.West, CardinalDirection.South, CardinalDirection.North],
            [CardinalDirection.East, CardinalDirection.West, CardinalDirection.South, CardinalDirection.North],
            ];
        public List<uint> ClockwiseNumber = [1, 2, 3];
        public bool UseKefkaRelative = false;
        public bool TetherPrio2Same = true;
    }

    public enum Taker
    {
        None,
        Number_1,
        Number_2,
        Number_3,
        Accretion_1,
        Accretion_2,
    }
}
