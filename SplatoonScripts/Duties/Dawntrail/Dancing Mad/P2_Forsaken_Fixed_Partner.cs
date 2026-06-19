using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.CircularBuffers;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameFunctions.VirtualTableClassifier;
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
namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public unsafe class P2_Forsaken_Fixed_Partner : SplatoonScript<P2_Forsaken_Fixed_Partner.Config>
{
    public override Metadata Metadata { get; } = new(13, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1363];
    public uint EffectSpread = 5085;
    public uint EffectStack = 5084;
    public uint EffectFan = 5086;

    public uint DebuffSpellsTrouble = 5083;

    public uint ActionTowerExplode = 47806;

    public uint CastFuturesEnd = 47826;
    public uint CastPastsEnd = 47827;
    public uint[] CastAllThingsEnding = [47836, 47837];
    public List<uint> AoeMapEffectsBlock = [];

    private bool? StoredAoe = null;
    private uint? TemporaryPartnerOverride = null;
    private bool? TemporaryAdjustOverride = null;
    private bool? TemporaryIsLeftTower = null;

    private uint TowerCount = 0;
    private uint SequenceCount => (TowerCount / 2) + 1;
    private bool? FirstTaker = null;

    private Dictionary<uint, Vector2> MapEffect2TowerPos
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
        for(var i = 0; i < 2; i++)
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
            ~Lv2~{"Enabled":false,"Name":"Grid","ZoneLockH":[1363],"ElementsL":[{"Name":"","type":2,"refX":100.0,"refY":104.0,"offX":100.23069,"offY":112.065765,"radius":4.0,"color":1895825663,"fillIntensity":1.0,"thicc":0.5,"FillStep":1.3333334,"RenderEngineKind":2},{"Name":"","type":2,"refX":104.0,"refY":108.0,"offX":96.0,"offY":108.0,"radius":0.0},{"Name":"","type":2,"refX":97.0,"refY":105.0,"offX":103.0,"offY":111.0,"radius":0.0},{"Name":"","type":2,"refX":103.0,"refY":105.0,"offX":97.0,"offY":111.0,"radius":0.0},{"Name":"","type":2,"refX":100.0,"refY":104.0,"offX":100.0,"offY":112.0,"radius":0.0}]}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"1357_Left","Group":"Dmad forsaken partner script","ZoneLockH":[1363],"ElementsL":[{"Name":"Stack","refX":99.5,"refY":107.0,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"Fan","refX":100.0,"refY":111.0,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"FanTaker","refX":100.0,"refY":112.5,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"StackTaker","refX":99.5,"refY":103.5,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true}]}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"1357_Right","Group":"Dmad forsaken partner script","ZoneLockH":[1363],"ElementsL":[{"Name":"Stack","refX":102.0,"refY":105.0,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"Spread","refX":97.5,"refY":110.5,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"StackTaker","refX":101.0,"refY":103.5,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true}]}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"2468_Left","Group":"Dmad forsaken partner script","ZoneLockH":[1363],"ElementsL":[{"Name":"Fan","refX":100.0,"refY":104.5,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"Spread","refX":101.99653,"refY":110.96512,"refZ":-1.9073486E-06,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"CloneBaiter","refX":94.0,"refY":99.0,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"FanTaker","refX":95.5,"refY":108.0,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true}]}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"2468_Right","Group":"Dmad forsaken partner script","ZoneLockH":[1363],"ElementsL":[{"Name":"Fan","refX":100.0,"refY":104.5,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"Spread","refX":98.41889,"refY":111.2488,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"CloneBaiter","refX":106.0,"refY":99.0,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true},{"Name":"FanTaker","refX":104.5,"refY":108.0,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true}]}
            """);
        Controller.RegisterElementFromCode("""{"Name":"Bait","refX":102.39029,"refY":105.67813,"refZ":-3.8146973E-06,"radius":0.5,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"overlayText":"$ELEMENT","tether":true}""");
    }

    private int GetTowerByPosition(Vector2 pos)
    {
        for(var i = 1; i <= 8; i++)
        {
            if(Vector2.Distance(MapEffect2TowerPos[(uint)i], pos) <= 4)
            {
                return i;
            }
        }
        return 0;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action != null)
        {
            if(set.Action.Value.RowId == ActionTowerExplode)
            {
                TowerCount++;
                //PluginLog.Information("1");
                try
                {
                    foreach(var obj in set.TargetEffects.Select(x => ((uint)x.TargetID).GetObject()).Where(x => x != null))
                    {
                        if(obj is IPlayerCharacter)
                        {
                            //PluginLog.Information("2");
                            HitPlayers.Add(obj.ObjectId);
                            if(HitPlayers.Count >= 4)
                            {
                                if(HitPlayers.TakeLast(4).Contains(BasePlayer.ObjectId))
                                {
                                    Dictionary<int, List<IPlayerCharacter>> groups = [];
                                    foreach(var x in HitPlayers.TakeLast(4))
                                    {
                                        if(x.TryGetPlayer(out var pc))
                                        {
                                            groups.GetOrCreate(GetTowerByPosition(pc.Position.ToVector2()), () => []).Add(pc);
                                        }
                                    }
                                    var myPartner = groups.FirstOrDefault(x => x.Value.Any(g => g.AddressEquals(BasePlayer))).Value?.FirstOrDefault(x => !x.AddressEquals(BasePlayer));
                                    if(myPartner != null)
                                    {
                                        TemporaryPartnerOverride = myPartner.ObjectId;
                                        TemporaryAdjustOverride = Vector2.Distance(BasePlayer.Position.ToVector2(), new(100, 100)) > Vector2.Distance(myPartner.Position.ToVector2(), new(100, 100));
                                    }
                                    PluginLog.Information($"""
                                Groups:
                                {groups.Select(x => $"{x.Key}: {x.Value.Print()}").Print("\n")}
                                """);
                                    var t = groups.Keys.Order().ToList();
                                    if(t.Count == 2)
                                    {
                                        var myTower = groups.FirstOrDefault(x => x.Value.Any(s => s.AddressEquals(BasePlayer))).Key;
                                        if(Math.Abs(t[0] - t[1]) == 2)
                                        {
                                            TemporaryIsLeftTower = myTower == t[1];
                                        }
                                        else
                                        {
                                            TemporaryIsLeftTower = myTower == t[0];
                                        }
                                    }
                                }
                                HitPlayers.Clear();
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    e.Log();
                }
            }
            if(set.Action.Value.RowId == CastFuturesEnd)
            {
                StoredAoe = true;
                AoeMapEffectsBlock = ActiveMapEffects.ToList();
            }
            if(set.Action.Value.RowId == CastPastsEnd)
            {
                StoredAoe = false;
                AoeMapEffectsBlock = ActiveMapEffects.ToList();
            }
        }
    }

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if(packet->ActionType == (int)ActionType.Action)
        {
            if(CastAllThingsEnding.Contains(packet->ActionID))
            {
                StoredAoe = null;
            }
        }
    }

    private void ShowNextElement(uint id, string kind, bool applyText)
    {
        for(var i = 0; i < 8; i++)
        {
            var eName = $"{kind}{i}";
            if(Controller.TryGetElementByName(eName, out var e) && !e.Enabled)
            {
                if(id.TryGetPlayer(out var p))
                {
                    bool shouldShow;
                    if(C.SouthAdjusts && TemporaryPartnerOverride != null)
                    {
                        shouldShow = id == TemporaryPartnerOverride.Value;
                    }
                    else
                    {
                        shouldShow = C.MyPartner.GetPlayer(x => true)?.IGameObject?.ObjectId == id;
                    }
                    if((p.AddressEquals(BasePlayer) || (shouldShow)))
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
    }

    private CircularArray<uint> ActiveMapEffects = new(2);
    private List<PairInfo> TowerPairs = [];
    private List<uint> HitPlayers = [];

    public readonly record struct PairInfo(bool IsLeft, uint Player1, uint Player2);

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(MapEffect2TowerPos.ContainsKey(position) && data1 == 1)
        {
            ActiveMapEffects.Push(position);
        }
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        if(Controller.GetPartyMembers().Any(x => x.StatusList.Any(s => s.StatusId == DebuffSpellsTrouble)) || (StoredAoe != null))
        {
            foreach(var x in Controller.GetPartyMembers())
            {
                if(IsFan(x))
                {
                    ShowNextElement(x.ObjectId, "Fan", true);
                }

                if(IsStack(x))
                {
                    ShowNextElement(x.ObjectId, "Stack", true);
                }

                if(IsSpread(x))
                {
                    ShowNextElement(x.ObjectId, "Spread", true);
                }
            }
            if(StoredAoe != null && ActiveMapEffects.Count() == 2)
            {
                var e = Controller.GetElementByName("Bait");
                if(!StoredAoe.Value || (C.LastBaitAlwaysBetweenTowers && SequenceCount >= 8))
                {
                    var pos = (MapEffect2TowerPos[ActiveMapEffects[0]] + MapEffect2TowerPos[ActiveMapEffects[1]]) / 2;
                    e.Enabled = true;
                    e.RefPosition = Extend(new(100, 100), pos, 2).ToVector3();
                    e.color = Controller.AttentionColor;
                    e.overlayVOffset = C.VOffset;
                }
                else
                {
                    var i1 = ((ActiveMapEffects[0] - 1 + 4) % 8) + 1;
                    var i2 = ((ActiveMapEffects[1] - 1 + 4) % 8) + 1;
                    var pos = (MapEffect2TowerPos[i1] + MapEffect2TowerPos[i2]) / 2;
                    e.Enabled = true;
                    e.color = Controller.AttentionColor;
                    e.RefPosition = Extend(new(100, 100), pos, 2).ToVector3();
                    e.overlayVOffset = C.VOffset;
                }
                return;
            }

            var partner = (IPlayerCharacter)C.MyPartner.GetPlayer(x => true, 1)!.IGameObject;
            if(C.SouthAdjusts && TemporaryPartnerOverride?.TryGetPlayer(out var pc) == true)
            {
                partner = pc;
            }
            if(IsStack(BasePlayer) || IsStack(partner))
            {
                FirstTaker ??= true;
            }
            else
            {
                if(HasMarker(BasePlayer) && HasMarker(partner))
                {
                    FirstTaker ??= false;
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
                    if(ShowRotatedLayout($"2468_{(C.IsLeftDefaultTower ? "Left" : "Right")}", C.IsLeftDefaultTower, out var l))
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
                var doAdjust = C.IsFlexerAsActive && MustAdjust(partner);

                if(doAdjust)
                {
                    isCone = !isCone;
                }
                if(SequenceCount == 1 && IsStack(BasePlayer) && C.FirstTowerFollowsPartner)
                {
                    isCone = IsFan(partner);
                }
                if(SequenceCount.EqualsAny<uint>(1, 3, 5, 7))
                {
                    if(C.SouthAdjusts && SequenceCount > 1 && TemporaryAdjustOverride != null && TemporaryPartnerOverride?.TryGetPlayer(out var tempPartner) == true && TemporaryIsLeftTower != null)
                    {
                        isCone = TemporaryIsLeftTower.Value;
                        doAdjust = TemporaryAdjustOverride.Value && MustAdjust(partner);
                        if(doAdjust) isCone = !isCone;
                    }
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
                    bool isLeft;
                    if(C.SouthAdjusts && TemporaryAdjustOverride != null && TemporaryPartnerOverride?.TryGetPlayer(out var tempPartner) == true && TemporaryIsLeftTower != null)
                    {
                        isLeft = TemporaryIsLeftTower.Value;
                        doAdjust = TemporaryAdjustOverride.Value && MustAdjust(partner);
                        if(doAdjust)
                        {
                            isLeft = !isLeft;
                        }
                    }
                    else
                    {
                        isLeft = C.IsLeftDefaultTower;
                        if(C.IsFlexerAsActive && MustAdjust(partner))
                        {
                            isLeft = !isLeft;
                        }
                    }

                    if(ShowRotatedLayout($"2468_{(isLeft ? "Left" : "Right")}", isLeft, out var l))
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

    public static Vector2 Extend(Vector2 center, Vector2 point, float distance)
    {
        Vector2 direction = Vector2.Normalize(point - center);
        return point + (direction * distance);
    }

    public bool MustAdjust(IPlayerCharacter partner) => (IsStack(BasePlayer) && IsStack(partner)) || (IsFan(BasePlayer) && IsFan(partner)) || (IsSpread(BasePlayer) && IsSpread(partner));

    public bool HasMarker(IPlayerCharacter x) => IsFan(x) || IsSpread(x) || IsStack(x);
    public bool IsFan(IPlayerCharacter x) => x.StatusList.Any(s => s.StatusId == EffectFan);
    public bool IsStack(IPlayerCharacter x) => x.StatusList.Any(s => s.StatusId == EffectStack);
    public bool IsSpread(IPlayerCharacter x) => x.StatusList.Any(s => s.StatusId == EffectSpread);
    public bool IsActive(IPlayerCharacter x)
    {
        if(FirstTaker == null)
        {
            return default;
        }

        var bStep = C.Switchers.Contains(SequenceCount - 1);
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
        var eff1 = (int)ActiveMapEffects.Order().ElementAt(0);
        var eff2 = (int)ActiveMapEffects.Order().ElementAt(1);
        if(Math.Abs(eff1 - eff2) == 2)
        {
            myTower = (int)(isLeft ? eff2 : eff1);
        }
        else
        {
            // if it's 8 and 2, or 7 and 1 basically, then lower will be left
            myTower = (int)(isLeft ? eff1 : eff2);
        }
        //PluginLog.Information($"is left: {isLeft} {myTower} | {this.ActiveMapEffects.Order().Print()} | {Math.Abs(eff1 - eff2)}");
        if(Controller.TryGetLayoutByName(name, out l))
        {
            //PluginLog.Information($"Rotate: {((myTower - 1) * 45 - 180)}");
            var orig = Controller.OriginalLayouts[name];
            foreach(var element in l.ElementsL)
            {
                element.RefPosition = MathHelper.RotateWorldPoint(new(100, 0, 100), (((myTower - 1) * 45) + 180).DegreesToRadians(), orig.GetElement(element.Name).RefPosition);
                element.overlayVOffset = C.VOffset;
            }
            return true;
        }
        return false;
    }

    public override void OnReset()
    {
        TowerCount = 0;
        FirstTaker = null;
        ActiveMapEffects = new(2);
        TowerPairs.Clear();
        HitPlayers.Clear();
        StoredAoe = null;
        TemporaryPartnerOverride = null;
        TemporaryAdjustOverride = null;
        TemporaryIsLeftTower = null;
        AoeMapEffectsBlock.Clear();
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
        ImGui.SetNextItemWidth(150f);
        ImGui.SliderFloat("Overlay text vertical offset", ref C.VOffset, 0, 5);

        ImGui.Separator();
        ImGuiEx.Text("Tower taking order, where group A is the group that takes first tower:");
        for(uint i = 0; i < 8; i++)
        {
            if(i == 0)
            {
                ImGui.BeginDisabled();
            }

            ImGui.PushID(i);
            ImGuiEx.TextV($"{i + 1}:");
            ImGui.SameLine();
            if(ImGui.RadioButton("A", !C.Switchers.Contains(i)))
            {
                C.Switchers.Remove(i);
            }

            ImGui.SameLine();
            if(ImGui.RadioButton("B", C.Switchers.Contains(i)))
            {
                C.Switchers.Add(i);
            }

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
        ImGui.Checkbox("I will flex if my partner and I both have stacks while in tower", ref C.IsFlexerAsActive);
        ImGui.Checkbox("South adjust mode (beta)", ref C.SouthAdjusts);
        ImGuiEx.HelpMarker("For even towers, if two players have same debuff after tower got resolved, southmost player will adjust");
        ImGui.Checkbox("Follow partner for first tower if I have stack", ref C.FirstTowerFollowsPartner);
        ImGui.Checkbox("Last future/past bait is always between towers", ref C.LastBaitAlwaysBetweenTowers);
        ImGui.Unindent();
        ImGuiEx.Text("When in passive group during even towers, I will:");
        ImGui.Indent();
        ImGuiEx.RadioButtonBool("Bait boss clone (melee)", "Bait active group's fan (ranged)", ref C.IsCloneBaitingAsPassive);
        ImGui.Unindent();
        ImGuiEx.Text("(ignore for stack+spread tower) When helping active odd tower with fan, I will:");
        ImGui.Indent();
        ImGuiEx.RadioButtonBool("Stack (tank)", "Bait fan (healer)", ref C.IsStackTakerAsPassive);
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
            if(ImGui.Button("Copy map effects"))
            {
                GenericHelpers.Copy(ActiveMapEffects.Print("|"));
            }

            if(ImGui.Button("Paste map effects"))
            {
                GenericHelpers.Paste().Split("|").Select(x => uint.Parse(x)).Each(x => ActiveMapEffects.Push(x));
            }
            ImGuiEx.Text($"TemporaryIsLeftTower: {TemporaryIsLeftTower}");
            ImGuiEx.Text($"TemporaryPartnerOverride: {TemporaryPartnerOverride?.GetObject()}");
            ImGui.InputUInt("Tower count", ref TowerCount);
            ImGuiEx.Checkbox("First taker", ref FirstTaker);
            ImGui.SameLine();
            if(ImGui.Button("Yes"))
            {
                FirstTaker = true;
            }

            ImGui.SameLine();
            if(ImGui.Button("No"))
            {
                FirstTaker = false;
            }

            ImGuiEx.Text($"IsActive: {IsActive(BasePlayer)}\nSequence:{SequenceCount}");
        }
    }

    public class Config
    {
        public HashSet<uint> Switchers = [3, 4, 5, 6];
        public bool IsLeftDefaultTower = false;
        public bool IsFlexerAsActive = false;
        public bool IsStackTakerAsPassive = false;
        public bool IsCloneBaitingAsPassive = false;
        public bool FirstTowerFollowsPartner = false;
        public Prio1 MyPartner = new();
        public float VOffset = 1f;
        public bool SouthAdjusts = false;
        public bool LastBaitAlwaysBetweenTowers = false;
    }

    public class Prio1 : PriorityData
    {
        public override int GetNumPlayers()
        {
            return 1;
        }
    }
}
