using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.CircularBuffers;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public unsafe class P2_Forsaken : SplatoonScript<P2_Forsaken.Config>
{
    public override Metadata Metadata { get; } = new(7, "NightmareXIV, Poneglyph");
    public override HashSet<uint>? ValidTerritories { get; } = [1363];

    public uint EffectSpread = 5085;
    public uint EffectStack = 5084;
    public uint EffectFan = 5086;

    public uint DebuffSpellsTrouble = 5083;

    public uint ActionTowerExplode = 47806;
    List<uint> FirstTakers = [];
    uint TowerCount = 0;

    Dictionary<uint, Vector2> MapEffect2TowerPos
    {
        get
        {
            if(field == null)
            {
                field = [];
                for(uint i = 1; i <= 8; i++)
                {
                    field[i] = MathHelper.RotateWorldPoint(new(100, 0, 100), (45f * (i - 1)).DegreesToRadians(), new(100, 0, 92)).ToVector2();
                }
                for(uint i = 9; i <= 16; i++)
                {
                    field[i] = MathHelper.RotateWorldPoint(new(100, 0, 100), (45f * (i - 1)).DegreesToRadians(), new(100, 0, 88)).ToVector2();
                }
            }
            return field;
        }
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode($"Stack", """
                {"Name":"Stack","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayTextColor":4278779648,"overlayVOffset":1.2,"thicc":0.0,"overlayText":">>> Stack <<<","refActorComparisonType":2}
                """);
        Controller.RegisterElementFromCode($"Spread", """
                {"Name":"Spread","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayTextColor":4278190335,"overlayVOffset":1.2,"thicc":0.0,"overlayText":"<<< Spread >>>","refActorComparisonType":2}
                """);
        Controller.RegisterElementFromCode($"Fan", """
                {"Name":"Cone","type":1,"radius":0.0,"color":3372220160,"Filled":false,"fillIntensity":0.5,"overlayTextColor":4294180608,"overlayVOffset":1.2,"thicc":0.0,"overlayText":"^^^ Cone ^^^","refActorComparisonType":2}
                """);

        Controller.RegisterElementFromCode($"VStack", """
                {"Name":"Stack","type":1,"radius":5.0,"Donut":0.5,"color":3357277952,"fillIntensity":0.5,"overlayTextColor":4278779648,"overlayVOffset":1.2,"overlayText":"","refActorComparisonType":2}
                """);
        Controller.RegisterElementFromCode($"VSpread", """
                {"Name":"Spread","type":1,"radius":5.0,"fillIntensity":0.5,"Donut":0.5,"overlayTextColor":4278190335,"overlayVOffset":1.2,"overlayText":"","refActorComparisonType":2}
                """);
        Controller.RegisterElementFromCode($"VFan", """
                {"Name":"Cone","type":4,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.3,"overlayTextColor":4294180608,"overlayVOffset":1.2,"overlayText":"","thicc":8.0,"includeRotation":true,"FaceMe":true,"refActorComparisonType":2}
                """);

        for(int i = 0; i < 2; i++)
        {
            Controller.RegisterElementFromCode($"TowerSplit2-{i}", """
                {"Name":"","type":3,"refX":4.0,"offX":-4.0,"radius":0.0,"refActorNameIntl":{"En":"Kefka"},"refActorDataID":9020,"refActorPlaceholder":[],"refActorNPCNameID":7131,"refActorComparisonAnd":true,"refActorComparisonType":3,"includeRotation":true,"onlyUnTargetable":true,"LimitDistance":true,"DistanceSourceX":107.919945,"DistanceSourceY":100.056015,"DistanceSourceZ":3.8146973E-06,"DistanceMax":0.1,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
                """);
            Controller.RegisterElementFromCode($"TowerSplit1-{i}", """
                {"Name":"","type":3,"refY":4.0,"offY":-4.0,"radius":0.0,"refActorDataID":9020,"refActorPlaceholder":[],"refActorNPCNameID":7131,"refActorComparisonAnd":true,"refActorComparisonType":3,"includeRotation":true,"onlyUnTargetable":true,"LimitDistance":true,"DistanceSourceX":107.919945,"DistanceSourceY":100.056015,"DistanceSourceZ":3.8146973E-06,"DistanceMax":0.1,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
                """);
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action != null && set.Action.Value.RowId == this.ActionTowerExplode)
        {
            this.TowerCount++;
            if(this.FirstTakers.Count < 4)
            {
                foreach(var x in set.TargetEffects)
                {
                    if(((uint)x.TargetID).TryGetPlayer(out var p))
                    {
                        FirstTakers.Add(p.ObjectId);
                    }
                }
            }
        }
    }

    public override void OnReset()
    {
        FirstTakers.Clear();
        this.TowerCount = 0;
    }

    Element GetOrCreateElementAtIndex(string name, int index)
    {
        if(Controller.TryGetElementByName($"{name}{index}", out var e))
        {
            return e;
        }
        else
        {
            Controller.RegisterElement($"{name}{index}", Controller.GetElementByName(name).JSONClone());
            return Controller.GetElementByName($"{name}{index}");
        }
    }

    void ShowNextElement(uint id, string kind, bool applyText, bool force = false)
    {
        for(int i = 0; i < 8; i++)
        {
            var e = GetOrCreateElementAtIndex(kind, i);
            if(!e.Enabled)
            {
                if(!C.ShowAll && !force)
                {
                    if(id.TryGetPlayer(out var p) && (p.AddressEquals(BasePlayer) || (C.ShowOnlyPartner && C.Partner.GetPlayer(x => true)?.IGameObject?.ObjectId == id)))
                    {
                        //
                    }
                    else
                    {
                        continue;
                    }
                }
                e.Enabled = true;
                e.refActorObjectID = id;
                if(applyText)
                {
                    if(FirstTakers.Count > 0 && C.ShowInOut)
                    {
                        var isTaking = FirstTakers.Contains(id);
                        if((this.TowerCount / 2).EqualsAny<uint>(C.Switchers)) isTaking = !isTaking;
                        if(!isTaking)
                        {
                            e.overlayText = Controller.OriginalElements[kind].overlayText + "| -- OUT --";
                        }
                        else
                        {
                            e.overlayText = Controller.OriginalElements[kind].overlayText + "| ++ IN ++";
                        }
                    }
                    else
                    {
                        e.overlayText = Controller.OriginalElements[kind].overlayText;
                    }
                }
                return;
            }
        }
    }

    private const float TowerCoordinateRadius = 4f;
    private bool IsPlayerInActiveTower(IPlayerCharacter player)
    {
        if(player.IsDead)
        {
            return false;
        }

        if(ActiveMapEffects.Count() == 0)
        {
            return false;
        }

        var playerPos = new Vector2(player.Position.X, player.Position.Z);
        var threshold = TowerCoordinateRadius * TowerCoordinateRadius;

        foreach(var effectPosition in ActiveMapEffects)
        {
            if(!MapEffect2TowerPos.TryGetValue(effectPosition, out var towerPos))
            {
                continue;
            }

            if(Vector2.DistanceSquared(playerPos, towerPos) <= threshold)
            {
                return true;
            }
        }

        return false;
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        if(Controller.GetPartyMembers().Any(x => x.StatusList.Any(s => s.StatusId == DebuffSpellsTrouble)))
        {
            var pcs = Svc.Objects.OfType<IPlayerCharacter>().ToList();
            int i = 0;
            foreach(var x in ActiveMapEffects)
            {
                var n1 = $"TowerSplit1-{i}";
                var n2 = $"TowerSplit2-{i}";
                if(Controller.TryGetElementByName(n1, out var e1)&& Controller.TryGetElementByName(n2, out var e2))
                {
                    e1.Enabled = true;
                    e2.Enabled = true;
                    e1.DistanceSourceX = MapEffect2TowerPos[x].X;
                    e1.DistanceSourceY = MapEffect2TowerPos[x].Y;
                    e2.DistanceSourceX = MapEffect2TowerPos[x].X;
                    e2.DistanceSourceY = MapEffect2TowerPos[x].Y;
                }
                i++;
            }
            foreach(var x in Controller.GetPartyMembers())
            {
                if(x.StatusList.Any(s => s.StatusId == this.EffectFan)) ShowNextElement(x.ObjectId, "Fan", true);
                if(x.StatusList.Any(s => s.StatusId == this.EffectStack)) ShowNextElement(x.ObjectId, "Stack", true);
                if(x.StatusList.Any(s => s.StatusId == this.EffectSpread)) ShowNextElement(x.ObjectId, "Spread", true);
            }

            if(C.Visualize)
            {
                for(int j = 0; j < pcs.Count && j < 8; j++)
                {
                    var source = pcs[j];

                    if(!IsPlayerInActiveTower(source))
                    {
                        continue;
                    }

                    if(source.StatusList.Any(s => s.StatusId == this.EffectFan))
                    {
                        var nearest = pcs
                            .Where(x => x.EntityId != source.EntityId)
                            .OrderBy(x => Vector3.DistanceSquared(x.Position, source.Position))
                            .FirstOrDefault();

                        var e = GetOrCreateElementAtIndex("VFan", j);
                        if(nearest != null)
                        {
                            e.refActorComparisonType = 2;
                            e.refActorObjectID = source.EntityId;
                            e.faceplayer = GetPlayerOrder(nearest);
                            e.Enabled = true;
                        }
                    }
                }

                foreach(var x in Controller.GetPartyMembers().Where(IsPlayerInActiveTower))
                {
                    if(x.StatusList.Any(s => s.StatusId == this.EffectStack)) ShowNextElement(x.ObjectId, "VStack", false, true);
                    if(x.StatusList.Any(s => s.StatusId == this.EffectSpread)) ShowNextElement(x.ObjectId, "VSpread", false, true);
                }
            }
        }
    }

    CircularArray<uint> ActiveMapEffects = new(2);

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(this.MapEffect2TowerPos.ContainsKey(position) && data1 == 1)
        {
            ActiveMapEffects.Push(position);
        }
    }
    private string GetPlayerOrder(IPlayerCharacter c)
    {
        for(var i = 1; i <= 8; i++)
        {
            if((nint)FakePronoun.Resolve($"<{i}>") == c.Address)
                return $"<{i}>";
        }
        throw new Exception("Could not determine player order");
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Show all players (otherwise yourself only)", ref C.ShowAll);
        if(!C.ShowAll)
        {
            ImGui.Indent();
            ImGui.Checkbox("Show your partner", ref C.ShowOnlyPartner);
            if(C.ShowOnlyPartner)
            {
                C.Partner.Draw();
            }
            ImGui.Unindent();
        }
        ImGui.Checkbox("Visualize attacks from towers", ref C.Visualize);
        ImGui.Checkbox("Show in/out", ref C.ShowInOut);
        if(C.ShowInOut)
        {
            ImGui.Indent();
            ImGuiEx.Text("Tower taking order, where group A is the group that takes first tower:");
            for(uint i = 0; i < 8; i++)
            {
                if(i == 0) ImGui.BeginDisabled();
                ImGui.PushID(i);
                ImGuiEx.TextV($"{i + 1}:");
                ImGui.SameLine();
                if(ImGui.RadioButton("A", !C.Switchers.Contains(i))) C.Switchers.Remove(i);
                ImGui.SameLine();
                if(ImGui.RadioButton("B", C.Switchers.Contains(i))) C.Switchers.Add(i);
                ImGui.PopID();
                if(i == 0)
                {
                    ImGui.EndDisabled();
                    C.Switchers.Remove(0);
                }
            }
            ImGui.Unindent();
        }
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.InputUInt("Tower count", ref this.TowerCount);
            ImGuiEx.Text($"First takers: \n{FirstTakers.Select(x => x.TryGetPlayer(out var p) ? p.ToString() : "").Print("\n")}");
            foreach(var x in MapEffect2TowerPos)
            {
                ImGuiEx.Text(ActiveMapEffects.Contains(x.Key) ?EColor.GreenBright:null, $"{x.Key}: {x.Value}");
            }
        }
    }

    public class Config
    {
        public bool ShowAll = true;
        public bool Visualize = false;
        public bool ShowOnlyPartner = false;
        public bool ShowInOut = true;
        public HashSet<uint> Switchers = [1, 2, 5, 6];
        public Prio1 Partner = new();
    }

    public class Prio1 : PriorityData
    {
        public override int GetNumPlayers()
        {
            return 1;
        }
    }
}
