using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.CircularBuffers;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using Splatoon;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public unsafe class P2_Forsaken_Fixed_Partner : SplatoonScript<P2_Forsaken_Fixed_Partner.Config>
{
    public override Metadata Metadata { get; } = new(2, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1363];
    public uint EffectSpread = 5085;
    public uint EffectStack = 5084;
    public uint EffectFan = 5086;

    public uint DebuffSpellsTrouble = 5083;

    public uint ActionTowerExplode = 47806;

    public uint CastFuturesEnd = 47826;
    public uint CastPastsEnd = 47827;
    public uint[] CastAllThingsEnding = [47836, 47837];


    uint TowerCount = 0;
    uint SequenceCount => TowerCount / 2 + 1;
    bool? FirstTaker = null;

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
        for(int i = 0; i < 2; i++)
        {
            Controller.RegisterElementFromCode($"Stack{i}", """
                {"Name":"Stack","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayTextColor":4278779648,"overlayVOffset":1.2,"thicc":0.0,"overlayText":">>> Stack <<<","refActorComparisonType":2}
                """);
            Controller.RegisterElementFromCode($"Spread{i}", """
                {"Name":"Spread","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayTextColor":4278190335,"overlayVOffset":1.2,"thicc":0.0,"overlayText":"<<< Spread >>>","refActorComparisonType":2}
                """);
            Controller.RegisterElementFromCode($"Fan{i}", """
                {"Name":"Cone","type":1,"radius":0.0,"color":3372220160,"Filled":false,"fillIntensity":0.5,"overlayTextColor":4294180608,"overlayVOffset":1.2,"thicc":0.0,"overlayText":"^^^ Cone ^^^","refActorComparisonType":2}
                """);
        }
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"1357_Left","Group":"Dmad forsaken partner script","ZoneLockH":[1363],"ElementsL":[{"Name":"Stack","refX":99.785255,"refY":106.868225,"refZ":3.8146973E-06,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"Fan","refX":99.88178,"refY":111.316605,"refZ":-3.8146973E-06,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"FanTaker","refX":99.77569,"refY":112.65779,"refZ":3.8146973E-06,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"StackTaker","refX":99.36622,"refY":103.380486,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true}]}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"1357_Right","Group":"Dmad forsaken partner script","ZoneLockH":[1363],"ElementsL":[{"Name":"Stack","refX":102.39029,"refY":105.67813,"refZ":-3.8146973E-06,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"Spread","refX":98.54814,"refY":111.194405,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"StackTaker","refX":102.592636,"refY":104.23891,"refZ":3.8146973E-06,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true}]}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"2468_Left","Group":"Dmad forsaken partner script","ZoneLockH":[1363],"ElementsL":[{"Name":"Fan","refX":98.0479,"refY":105.17803,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"Spread","refX":102.42997,"refY":110.723854,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"CloneBaiter","refX":92.03958,"refY":97.51317,"refZ":-3.8146973E-06,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"FanTaker","refX":95.72592,"refY":109.94875,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true}]}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"2468_Right","Group":"Dmad forsaken partner script","ZoneLockH":[1363],"ElementsL":[{"Name":"Fan","refX":101.771194,"refY":105.19224,"refZ":3.8146973E-06,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"Spread","refX":97.872345,"refY":110.857216,"refZ":-3.8146973E-06,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"CloneBaiter","refX":108.368996,"refY":97.684555,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"FanTaker","refX":104.3985,"refY":109.870316,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true}]}
            """);
        Controller.RegisterElementFromCode("""{"Name":"Bait","refX":102.39029,"refY":105.67813,"refZ":-3.8146973E-06,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true}""");
    }

    bool? StoredAoe = null;
    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action != null)
        {
            if(set.Action.Value.RowId == this.ActionTowerExplode)
            {
                this.TowerCount++;
            }
            if(set.Action.Value.RowId == this.CastFuturesEnd) StoredAoe = true;
            if(set.Action.Value.RowId == this.CastPastsEnd) StoredAoe = false;
        }
    }

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if(packet->ActionType == (int)ActionType.Action)
        {
            if(this.CastAllThingsEnding.Contains(packet->ActionID))
            {
                this.StoredAoe = null;
            }
        }
    }

    void ShowNextElement(uint id, string kind, bool applyText)
    {
        for(int i = 0; i < 8; i++)
        {
            var eName = $"{kind}{i}";
            if(Controller.TryGetElementByName(eName, out var e) && !e.Enabled)
            {
                if(id.TryGetPlayer(out var p) && (p.AddressEquals(BasePlayer) || (C.MyPartner.GetPlayer(x => true)?.IGameObject?.ObjectId == id)))
                {
                    //
                }
                else
                {
                    continue;
                }
                e.Enabled = true;
                e.refActorObjectID = id;
                return;
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

    public override void OnUpdate()
    {
        Controller.Hide();
        if(Controller.GetPartyMembers().Any(x => x.StatusList.Any(s => s.StatusId == DebuffSpellsTrouble)))
        {
            foreach(var x in Controller.GetPartyMembers())
            {
                if(IsFan(x)) ShowNextElement(x.ObjectId, "Fan", true);
                if(IsStack(x)) ShowNextElement(x.ObjectId, "Stack", true);
                if(IsSpread(x)) ShowNextElement(x.ObjectId, "Spread", true);
            }
            if(this.StoredAoe != null && this.ActiveMapEffects.Count() == 2)
            {
                var e = Controller.GetElementByName("Bait");
                if(!this.StoredAoe.Value)
                {
                    var pos = (this.MapEffect2TowerPos[this.ActiveMapEffects[0]] + this.MapEffect2TowerPos[this.ActiveMapEffects[1]]) / 2;
                    e.Enabled = true;
                    e.RefPosition = pos.ToVector3();
                }
                else
                {
                    var i1 = (((this.ActiveMapEffects[0] - 1) + 4) % 8) + 1;
                    var i2 = (((this.ActiveMapEffects[1] - 1) + 4) % 8) + 1;
                    var pos = (this.MapEffect2TowerPos[i1] + this.MapEffect2TowerPos[i2]) / 2;
                    e.Enabled = true;
                    e.RefPosition = pos.ToVector3();
                }
                return;
            }

            var partner = (IPlayerCharacter)C.MyPartner.GetPlayer(x => true, 1)!.IGameObject;
            if(IsStack(BasePlayer) || IsStack(partner))
            {
                this.FirstTaker ??= true;
            }
            else
            {
                if(HasMarker(BasePlayer) && HasMarker(partner))
                {
                    this.FirstTaker ??= false;
                }
            }
            if(!IsActive(BasePlayer))
            {
                if(SequenceCount.EqualsAny<uint>(1, 3, 5, 7))
                {
                    var isCone = C.IsLeftDefaultTower;
                    if(ShowRotatedLayout($"1357_{(isCone ? "Left" : "Right")}", isCone, out var l))
                    {
                        l.Enabled = true;
                        l.Hide();
                        var e = l.GetElement(C.IsStackTakerAsPassive || !isCone ? "StackTaker" : "FanTaker");
                        e.color = Controller.AttentionColor;
                        e.Enabled = true;
                    }
                }
                else
                {
                    if(ShowRotatedLayout($"2468_{(C.IsLeftDefaultTower?"Left":"Right")}", C.IsLeftDefaultTower, out var l))
                    {
                        l.Enabled = true;
                        l.Hide();
                        var e = l.GetElement(C.IsCloneBaitingAsPassive ? "CloneBaiter" : "FanTaker");
                        e.color = Controller.AttentionColor;
                        e.Enabled = true;
                    }
                }
            }
            else
            {
                var isCone = IsFan(BasePlayer) || (C.IsLeftDefaultTower && !IsSpread(BasePlayer));
                if(C.IsFlexerAsActive && MustAdjust(partner))
                {
                    isCone = !isCone;
                }
                if(SequenceCount.EqualsAny<uint>(1, 3, 5, 7))
                {
                    if(ShowRotatedLayout($"1357_{(isCone ? "Left" : "Right")}", isCone, out var l))
                    {
                        l.Enabled = true;
                        l.Hide();
                        var e = l.GetElement(IsFan(BasePlayer) ? "Fan" : IsStack(BasePlayer) ? "Stack" : "Spread");
                        e.color = Controller.AttentionColor;
                        e.Enabled = true;
                    }
                }
                else
                {
                    var isLeft = C.IsLeftDefaultTower;
                    if(C.IsFlexerAsActive && MustAdjust(partner))
                    {
                        isLeft = !isLeft;
                    }
                    if(this.ShowRotatedLayout($"2468_{(isLeft?"Left":"Right")}", isLeft, out var l))
                    {
                        l.Enabled = true;
                        l.Hide();
                        var e = l.GetElement(IsFan(BasePlayer) ? "Fan" : "Spread");
                        e.color = Controller.AttentionColor;
                        e.Enabled = true;
                    }
                }
            }
        }
    }

    public bool MustAdjust(IPlayerCharacter partner) => (IsStack(BasePlayer) && IsStack(partner)) || (IsFan(BasePlayer) && IsFan(partner)) || (IsSpread(BasePlayer) && IsSpread(partner));

    public bool HasMarker(IPlayerCharacter x) => IsFan(x) || IsSpread(x) || IsStack(x);
    public bool IsFan(IPlayerCharacter x) => x.StatusList.Any(s => s.StatusId == this.EffectFan);
    public bool IsStack(IPlayerCharacter x) => x.StatusList.Any(s => s.StatusId == this.EffectStack);
    public bool IsSpread(IPlayerCharacter x) => x.StatusList.Any(s => s.StatusId == this.EffectSpread);
    public bool IsActive(IPlayerCharacter x)
    {
        if(FirstTaker == null) return default;
        var bStep = C.Switchers.Contains(this.SequenceCount-1);
        return FirstTaker.Value ? !bStep : bStep;
    }

    public bool ShowRotatedLayout(string name, bool isLeft, out Layout l)
    {
        if(ActiveMapEffects.Count() < 2)
        {
            l = default;
            return false;
        }
        int myTower;
        int eff1 = (int)this.ActiveMapEffects.Order().ElementAt(0);
        int eff2 = (int)this.ActiveMapEffects.Order().ElementAt(1);
        if(Math.Abs(eff1-eff2) == 2)
        {
            myTower = (int)(isLeft ?eff2:eff1);
        }
        else
        {
            // if it's 8 and 2, or 7 and 1 basically, then lower will be left
            myTower = (int)(isLeft ?eff1:eff2);
        }
        //PluginLog.Information($"is left: {isLeft} {myTower} | {this.ActiveMapEffects.Order().Print()} | {Math.Abs(eff1 - eff2)}");
        if(Controller.TryGetLayoutByName(name, out l))
        {
            //PluginLog.Information($"Rotate: {((myTower - 1) * 45 - 180)}");
            var orig = Controller.OriginalLayouts[name];
            foreach(var element in l.ElementsL)
            {
                element.RefPosition = MathHelper.RotateWorldPoint(new(100, 0, 100), ((myTower - 1) * 45 + 180).DegreesToRadians(), orig.GetElement(element.Name).RefPosition);
            }
            return true;
        }
        return false;
    }

    public override void OnReset()
    {
        this.TowerCount = 0;
        this.FirstTaker = null;
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.TextWrapped($"""
            This script works for strategies that follow these rules:
            - Fixed by headmarker positions for odd towers
            - Swap to another tower as active group only when you and your partner have stacks
            - Always same bait patterns for 1357 and 2468 towers

            How to edit this script:
            0) RELOAD THE SCRIPT. YOU MUST RELOAD THE SCRIPT BEFORE YOU EDIT IT, IF MECHANIC HAS BEEN RUN, RELOAD SCRIPT BEFORE EDITING.
            1) Get into battle zone using Hyperborea or duty replay
            2) Open Debug section and spawn all 3 towers
            3) Edit layouts. Always work on layout as if middle tower is tower that is said in layout's name. So if you are editing 1357_Left, it means you must treat middle tower as left. If you're editing 1357_Right, you must treat middle tower as right. 
            4) If you need to make, for example, 1357's cones right and spread left, then edit left tower and drag it's elements to the right tower, and do the same with right tower. You will end up with elements in left and right debug towers, opposite of how they are named.  Do the same if you need to swap only stack, you can swap them around and make them display in reverse. 
            """);
        ImGui.Separator();
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
        ImGui.Separator();
        ImGuiEx.Text("Select your partner:");
        C.MyPartner.Draw();
        ImGui.Separator();
        ImGuiEx.Text("My tower, looking at boss:");
        ImGui.Indent();
        ImGuiEx.RadioButtonBool("Left", "Right", ref C.IsLeftDefaultTower);
        ImGui.Unindent();
        ImGui.Checkbox("I will flex if my partner and I both have stacks while in tower", ref C.IsFlexerAsActive);
        ImGuiEx.Text("When in passive group during even towers, I will:");
        ImGui.Indent();
        ImGuiEx.RadioButtonBool("Bait boss clone", "Bait active group's fan", ref C.IsCloneBaitingAsPassive);
        ImGui.Unindent();
        ImGuiEx.Text("(ignore for stack+spread tower) When helping active odd tower with fan, I will:");
        ImGui.Indent();
        ImGuiEx.RadioButtonBool("Stack", "Bait fan", ref C.IsStackTakerAsPassive);
        ImGui.Unindent();

        if(ImGui.CollapsingHeader("Debug"))
        {
            if(ImGui.Button("Spawn reference tower map effect (make all layouts on it)"))
            {
                MapEffect.Delegate((long)EventFramework.Instance()->GetInstanceContentDirector(), 5, 1, 2);
            }
            if(ImGui.Button("Spawn right tower map effect"))
            {
                MapEffect.Delegate((long)EventFramework.Instance()->GetInstanceContentDirector(), 3, 1, 2);
            }
            if(ImGui.Button("Spawn left tower map effect"))
            {
                MapEffect.Delegate((long)EventFramework.Instance()->GetInstanceContentDirector(), 7, 1, 2);
            }
            if(ImGui.Button("Copy map effects")) GenericHelpers.Copy(this.ActiveMapEffects.Print("|"));
            if(ImGui.Button("Paste map effects"))
            {
                GenericHelpers.Paste().Split("|").Select(x => uint.Parse(x)).Each(x => this.ActiveMapEffects.Push(x));
            }
            ImGui.InputUInt("Tower count", ref TowerCount);
            ImGuiEx.Checkbox("First taker", ref FirstTaker);
            ImGui.SameLine();
            if(ImGui.Button("Yes")) FirstTaker = true;
            ImGui.SameLine();
            if(ImGui.Button("No")) FirstTaker = false;
            ImGuiEx.Text($"IsActive: {IsActive(BasePlayer)}\nSequence:{SequenceCount}");
        }
    }

    public class Config
    {
        public HashSet<uint> Switchers = [3,4,5,6];
        public bool IsLeftDefaultTower = false;
        public bool IsFlexerAsActive = false;
        public bool IsStackTakerAsPassive = false;
        public bool IsCloneBaitingAsPassive = false;
        public Prio1 MyPartner = new();
    }

    public class Prio1 : PriorityData
    {
        public override int GetNumPlayers()
        {
            return 1;
        }
    }
}
