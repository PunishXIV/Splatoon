using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameFunctions.VirtualTableClassifier;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public unsafe class P3_Blackhole_Lite : SplatoonScript<P3_Blackhole_Lite.Config>
{
    public override Metadata Metadata { get; } = new(8, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1363];

    private ImGuiEx.RealtimeDragDrop<CardinalDirection>[] DragDrop = [new("CarDir0", x => x.ToString()), new("CarDir1", x => x.ToString()), new("CarDir2", x => x.ToString())];
    private ImGuiEx.RealtimeDragDrop<CardinalDirection>[] DragDrop2 = [new("CarDir3", x => x.ToString()), new("CarDir4", x => x.ToString()), new("CarDir5", x => x.ToString())];

    private uint Sequence = 0;
    private bool? IsAccretion = null;

    private IBattleNpc? Gigakefka => Svc.Objects.OfTypeIBattleNpc().FirstOrDefault(x => x.DataId == 19504 && x.HitboxRadius > 20f);

    public override void OnSetup()
    {
        Controller.RegisterElementsFromMultilineCode("""
            {"Name":"Tether0","type":2,"refX":107.77591,"refY":113.12219,"refZ":-1.9073486E-06,"offX":110.66847,"offY":99.43638,"offZ":1.9073486E-06,"radius":0.0,"color":3355508503,"Filled":false,"fillIntensity":0.5,"thicc":8.0,"refActorObjectID":1073765276,"refActorComparisonType":2,"tether":true}
            {"Name":"Tether1","type":2,"refX":107.77591,"refY":113.12219,"refZ":-1.9073486E-06,"offX":110.66847,"offY":99.43638,"offZ":1.9073486E-06,"radius":0.0,"color":3355508503,"Filled":false,"fillIntensity":0.5,"thicc":8.0,"refActorObjectID":1073765276,"refActorComparisonType":2,"tether":true}
            {"Name":"Tether2","type":2,"refX":107.77591,"refY":113.12219,"refZ":-1.9073486E-06,"offX":110.66847,"offY":99.43638,"offZ":1.9073486E-06,"radius":0.0,"color":3355508503,"Filled":false,"fillIntensity":0.5,"thicc":8.0,"refActorObjectID":1073765276,"refActorComparisonType":2,"tether":true}
            {"Name":"Idle","refX":100.0,"refY":100.0,"radius":4.0,"color":3355508490,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}
            {"Name":"HintTake","type":1,"radius":0.0,"color":3358457600,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":4278190335,"overlayVOffset":2.0,"overlayFScale":1.5,"thicc":0.0,"overlayText":"Sequence: # | Take tether","refActorType":1}
            {"Name":"HintIdle","type":1,"Enabled":false,"radius":0.0,"color":3358457600,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":4278386432,"overlayVOffset":2.0,"thicc":0.0,"overlayText":"Sequence: # | Idle","refActorType":1}
            {"Name":"HoleCW","type":1,"offX":-10.0,"offY":10.0,"radius":5.0,"color":3372220415,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorObjectID":0,"refActorComparisonType":2,"includeRotation":true,"tether":true,"LineEndA":1,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            {"Name":"HoleCCW","type":1,"offX":10.0,"offY":10.0,"radius":5.0,"color":3372220415,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorObjectID":0,"refActorComparisonType":2,"includeRotation":true,"tether":true,"LineEndA":1,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            {"Name":"HoleFixed","radius":5.0,"color":3372220415,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorObjectID":0,"refActorComparisonType":2,"includeRotation":true,"tether":true,"LineEndA":1,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            """);

        for(var i = 0; i < 20; i++)
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
            var i = 0;
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
        List<IBattleNpc> myHoles = [];
        var valid = true;
        if(accretions.Count() == 2)
        {
            IsAccretion = BasePlayer.HasStatus(1604);
        }
        var numElements = 0;
        if(C.Mode == Mode.Role)
        {
            ResolveRoleMode(ref numElements, ref valid, ref myHoles);
        }
        else if(C.Mode == Mode.Marker)
        {
            ResolveMarkerMode(ref numElements, ref valid, ref myHoles);
        }
        if(valid)
        {
            if(myHoles.Count == 1)
            {
                {
                    if(C.ShowTether == ShowTether.Clockwise && Controller.TryGetElementByName("HoleCW", out var e))
                    {
                        e.Enabled = true;
                        e.refActorObjectID = myHoles[0].ObjectId;
                    }
                }
                {
                    if(C.ShowTether == ShowTether.Counter_Clockwise && Controller.TryGetElementByName("HoleCCW", out var e))
                    {
                        e.Enabled = true;
                        e.refActorObjectID = myHoles[0].ObjectId;
                    }
                }
            }
            if(myHoles.Count == 2 && C.TwoTethersBetweenHoles)
            {
                if(Controller.TryGetElementByName("HoleFixed", out var e))
                {
                    e.Enabled = true;
                    e.RefPosition = (myHoles[0].Position + myHoles[1].Position) / 2;
                }
            }
        }
    }

    void ResolveRoleMode(ref int numElements, ref bool valid, ref List<IBattleNpc> myHoles)
    {
        if(C.UseKefkaRelative)
        {
            var tethers = GetTetheredOrderedBlackholes();
            if(tethers.Count > 0)
            {
                var seq = C.Takers.SafeSelect((int)Sequence);
                var isTaker = false;
                if(seq != null && GetMyRole() != Taker.None)
                {
                    for(var i = 0; i < tethers.Count; i++)
                    {
                        var current = seq[i];
                        if(current == GetMyRole())
                        {
                            var myHole = tethers.CircularSelect((int)C.ClockwiseNumber[i] - 1);
                            if(!DrawHole(ref numElements, myHole.Value.Object))
                            {
                                valid = false;
                            }

                            myHoles.Add(myHole.Value.Object);
                            isTaker = true;
                        }
                    }
                }
                if(!isTaker && tethers.Count > 0)
                {
                    DrawIdle();
                }
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
                    for(var i = 0; i < seq.Count; i++)
                    {
                        var current = seq[i];
                        foreach(var dir in C.TetherPriorities[i])
                        {
                            if(!takenDirection.Contains(dir) && tethers.TryGetValue(dir, out var blackhole))
                            {
                                takenDirection.Add(dir);
                                if(current == GetMyRole() && GetMyRole() != Taker.None)
                                {
                                    if(!DrawHole(ref numElements, blackhole))
                                    {
                                        valid = false;
                                    }

                                    myHoles.Add(blackhole);
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

    void ResolveMarkerMode(ref int numElements, ref bool valid, ref List<IBattleNpc> myHoles)
    {
        if(C.UseKefkaRelative)
        {
            var tethers = GetTetheredOrderedBlackholes();
            if(tethers.Count > 0)
            {
                var seq = C.MarkerTakers.SafeSelect((int)Sequence);
                var isTaker = false;
                if(seq != null && GetOwnMarker() != null)
                {
                    for(var i = 0; i < tethers.Count; i++)
                    {
                        var current = seq[i];
                        if(current == GetOwnMarker().Value.RowId)
                        {
                            var myHole = tethers.CircularSelect((int)C.ClockwiseNumber[i] - 1);
                            if(!DrawHole(ref numElements, myHole.Value.Object))
                            {
                                valid = false;
                            }

                            myHoles.Add(myHole.Value.Object);
                            isTaker = true;
                        }
                    }
                }
                if(!isTaker && tethers.Count > 0)
                {
                    DrawIdle();
                }
            }
        }
        else
        {
            var tethers = GetTetheredBlackholes();
            if(tethers.Count > 0)
            {
                var seq = C.MarkerTakers.SafeSelect((int)Sequence);
                if(seq != null)
                {
                    var isTaker = false;
                    HashSet<CardinalDirection> takenDirection = [];
                    for(var i = 0; i < seq.Count; i++)
                    {
                        var current = seq[i];
                        foreach(var dir in C.TetherPriorities[i])
                        {
                            if(!takenDirection.Contains(dir) && tethers.TryGetValue(dir, out var blackhole))
                            {
                                takenDirection.Add(dir);
                                if(GetOwnMarker() != null && current == GetOwnMarker()?.RowId)
                                {
                                    if(!DrawHole(ref numElements, blackhole))
                                    {
                                        valid = false;
                                    }

                                    myHoles.Add(blackhole);
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

    private bool DrawHole(ref int numElements, IBattleNpc blackhole)
    {
        var isMyTether = false;
        var name = $"Tether{numElements}";
        if(Controller.TryGetElementByName(name, out var e))
        {
            numElements++;
            isMyTether = blackhole.GetTethers().Any(x => x.PairId == BasePlayer.ObjectId);
            e.Enabled = true;
            e.RefPosition = blackhole.Position;
            e.OffPosition = blackhole.GetTethers().FirstOrDefault()?.Pair?.Position ?? default;
            e.color = isMyTether ? EColor.GreenBright.ToUint() : Controller.AttentionColor;
            e.thicc = Controller.OriginalElements[name].thicc / (isMyTether ? 2 : 1);
        }
        DrawTake();
        return isMyTether;
    }

    private List<List<CardinalDirection>> GetTetherPriorities()
    {
        return (!C.TetherPrio2Same && GetTetheredBlackholes().Count == 2) ? C.TetherPriorities2 : C.TetherPriorities;
    }

    public Taker GetMyRole()
    {
        if(IsAccretion == true)
        {
            if(GetOwnNumber() == 1)
            {
                return Taker.Accretion_1;
            }

            if(GetOwnNumber() == 2)
            {
                return Taker.Accretion_2;
            }
        }
        else
        {
            if(GetOwnNumber() == 1)
            {
                return Taker.Number_1;
            }

            if(GetOwnNumber() == 2)
            {
                return Taker.Number_2;
            }

            if(GetOwnNumber() == 3)
            {
                return Taker.Number_3;
            }
        }
        return Taker.None;
    }

    public int GetOwnNumber()
    {
        if(BasePlayer.HasStatus(3004))
        {
            return 1;
        }

        if(BasePlayer.HasStatus(3005))
        {
            return 2;
        }

        if(BasePlayer.HasStatus(3006))
        {
            return 3;
        }

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

    private void DrawIdle()
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

    private void DrawTake()
    {
        if(Controller.TryGetElementByName("HintTake", out var e))
        {
            e.Enabled = true;
            e.overlayText = Controller.OriginalElements["HintTake"].overlayText.Replace("#", $"{Sequence + 1}");
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("Mechanic Resolution Mode", ref C.Mode);
        if(C.Mode == Mode.Role)
        {
            ImGuiEx.TextWrapped($"Configure which tethers you will be taking during each wave according to your assigned number and accretion debuff.");
        }
        if(C.Mode == Mode.Marker)
        {
            ImGuiEx.TextWrapped($"Configre which marker takes which tether. If specified markers will not be present, the script will fail.");
        }
        ImGuiEx.TextV($"Mode: ");
        ImGui.SameLine();
        ImGuiEx.RadioButtonBool("Kefka relative", "True north", ref C.UseKefkaRelative, sameLine: true);
        if(ImGuiEx.BeginDefaultTable("table", ["Wave", "Tether 1", "Tether 2", "Tether 3"], 
            extraFlags: ImGuiTableFlags.SizingStretchSame, 
            nullifyFlags: ImGuiTableFlags.SizingFixedFit))
        {
            if(C.Mode == Mode.Role)
            {
                for(var i = 0; i < C.Takers.Count; i++)
                {
                    ImGui.PushID($"{i}");
                    var row = C.Takers[i];
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"Wave {i + 1}");
                    for(var k = 0; k < (i.EqualsAny(0, 9) ? 1 : i.EqualsAny(1, 8) ? 2 : row.Count); k++)
                    {
                        ImGui.PushID($"{k}");
                        ImGui.TableNextColumn();
                        ImGuiEx.SetNextItemFullWidth();
                        var x = row[k];
                        if(ImGuiEx.EnumCombo("##taker", ref x))
                        {
                            row[k] = x;
                        }

                        ImGui.PopID();
                    }
                    ImGui.PopID();
                }
            }
            else if(C.Mode == Mode.Marker)
            {
                for(var i = 0; i < C.MarkerTakers.Count; i++)
                {
                    ImGui.PushID($"{i}");
                    var row = C.MarkerTakers[i];
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"Wave {i + 1}");
                    for(var k = 0; k < (i.EqualsAny(0, 9) ? 1 : i.EqualsAny(1, 8) ? 2 : row.Count); k++)
                    {
                        ImGui.PushID($"{k}");
                        ImGui.TableNextColumn();
                        ImGuiEx.SetNextItemFullWidth();
                        var x = row[k];
                        var mk = Marker.Get(x);
                        if(DrawMkSel(mk, ref x))
                        {
                            row[k] = x;
                        }

                        ImGui.PopID();
                    }
                    ImGui.PopID();
                }
            }

            if(C.UseKefkaRelative)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Prio, CW from Kefka");
                for(var i = 0; i < 3; i++)
                {
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(100);
                    var x = C.ClockwiseNumber[i];
                    if(ImGui.InputUInt($"##prio{i}", ref x))
                    {
                        C.ClockwiseNumber[i] = x;
                    }
                }
            }
            else
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.Text($"Tether Priority\n(3 tethers)");
                for(var i = 0; i < 3; i++)
                {
                    ImGui.PushID($"d{i}");
                    ImGui.TableNextColumn();
                    var item = C.TetherPriorities[i];
                    DragDrop[i].Begin();
                    for(var k = 0; k < item.Count; k++)
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
                    for(var i = 0; i < 2; i++)
                    {
                        ImGui.PushID($"d2{i}");
                        ImGui.TableNextColumn();
                        var item = C.TetherPriorities2[i];
                        DragDrop2[i].Begin();
                        for(var k = 0; k < item.Count; k++)
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
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.EnumCombo("Show tether taking position", ref C.ShowTether);
        ImGui.Checkbox("Take two tethers between black holes", ref C.TwoTethersBetweenHoles);
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Checkbox("IsAccretion", ref IsAccretion);
            ImGui.InputUInt("Sequence", ref Sequence);
            ImGuiEx.Text($"My role: {GetMyRole()}");
            ImGuiEx.Text($"Using prio2: {!C.TetherPrio2Same && GetTetheredBlackholes().Count == 2}");
            ImGuiEx.Text("Marker:");
            if(GetOwnMarker() != null && ThreadLoadImageHandler.TryGetIconTextureWrap(GetOwnMarker().Value.Icon, false, out var t))
            {
                ImGui.Image(t.Handle, new(50));
            }
            foreach(var x in Controller.GetPartyMembers())
            {
                ImGui.PushID(x.GetNameWithWorld());
                if(ImGui.BeginCombo($"{x.GetNameWithWorld()}", $"{GetMarker(x.ObjectId)?.Name ?? "Not set"}", ImGuiComboFlags.HeightLarge))
                {
                    foreach(var v in Marker.Values)
                    {
                        if(ImGui.Selectable($"{v.Name}"))
                        {
                            for(int i = 0; i < MarkingController.Instance()->Markers.Length; i++)
                            {
                                if(MarkingController.Instance()->Markers[i].ObjectId == x.ObjectId)
                                {
                                    MarkingController.Instance()->Markers[i] = default;
                                }
                            }
                            if(v.RowId > 0) MarkingController.Instance()->Markers[(int)v.RowId - 1] = x.GameObjectId;
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.PopID();
            }
        }
    }

    private bool DrawMkSel(Marker mk, ref uint currentValue)
    {
        var ret = false;
        if(ThreadLoadImageHandler.TryGetIconTextureWrap(mk.Icon, false, out var tex))
        {
            if(ImGui.ImageButton(tex.Handle, new(40)))
            {
                ImGui.OpenPopup("MrkSlkt");
            }
            if(ImGui.BeginPopup("MrkSlkt"))
            {
                var cnt = 0;
                foreach(var v in Marker.Values)
                {
                    if(v.Icon != 0 && ThreadLoadImageHandler.TryGetIconTextureWrap(v.Icon, false, out var tex1))
                    {
                        if(ImGui.ImageButton(tex1.Handle, new(30)))
                        {
                            currentValue = v.RowId;
                            ret = true;
                            ImGui.CloseCurrentPopup();
                        }
                        if(++cnt % 5 != 0)
                        {
                            ImGui.SameLine();
                        }
                    }
                }
                ImGui.EndPopup();
            }
        }
        return ret;
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
        var bh = GetTetheredBlackholes();
        if(bh.Count == 0)
        {
            return [];
        }

        var ret = new OrderedDictionary<CardinalDirection, EnumerationResult<IBattleNpc>>();
        foreach(var x in MathHelper.EnumerateObjectsClockwiseEx(bh, x => x.Value.Position.ToVector2(), new(100, 100), -Gigakefka.Rotation.RadToDeg() - 5))
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
        public ShowTether ShowTether = ShowTether.Clockwise;
        public bool TwoTethersBetweenHoles = true;
        public Mode Mode = Mode.Role;
        public List<List<uint>> MarkerGroups = [[1, 2, 3], [6, 7, 8], [9, 10]];
        public List<List<uint>> MarkerTakers = [
            [1,0,0],
            [1,2,0],
            [1,2,3],
            [6,2,3],
            [6,7,3],
            [6,7,8],
            [9,7,8],
            [9,10,8],
            [9,10,0],
            [10,0,0],
            ];
    }

    public enum Mode
    {
        Role,
        Marker,
    }

    public enum ShowTether
    {
        Clockwise, Counter_Clockwise, Disabled
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

    public unsafe Marker? GetOwnMarker()
    {
        return GetMarker(BasePlayer.ObjectId);
    }

    public unsafe Marker? GetMarker(uint objectId)
    {
        var array = MarkingController.Instance()->Markers;
        for(var i = 0; i < array.Length; i++)
        {
            var x = array[i];
            if(x.ObjectId == objectId)
            {
                return Marker.Get((uint)(i + 1));
            }
        }
        return default;
    }
}
