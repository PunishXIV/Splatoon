using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Automation;
using ECommons.ChatMethods;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Status = Lumina.Excel.Sheets.Status;
using UIColor = ECommons.ChatMethods.UIColor;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public class P4_Debuff_Reminder : SplatoonScript<P4_Debuff_Reminder.Config>
{
    public override Metadata Metadata { get; } = new(11, "NightmareXIV, mirage");
    public override HashSet<uint>? ValidTerritories { get; } = [1363];

    private List<string> VfxLie = ["vfx/common/eff/z3oy_stlp6_c0c.avfx", "vfx/common/eff/z3oy_stlp4_c0c.avfx"];
    private List<string> VfxTruth = ["vfx/common/eff/z3oy_stlp7_c0c.avfx", "vfx/common/eff/z3oy_stlp5_c0c.avfx"];
    private record struct StatusInfo(uint objectId, uint statusId);
    private List<StatusInfo> FakeStatuses = [];

    public class Debuffs
    {
        /// <summary>
        /// becomes Move
        /// </summary>
        public static uint[] DebuffDontMove = [5546, 1072, 1384, 2657, 3793, 3802, 4144];
        /// <summary>
        /// becomes Look at person
        /// </summary>
        public static uint[] DebuffLookAway = [5543, 452];
        /// <summary>
        /// becomes Spread
        /// </summary>
        public static uint[] DebuffStack = [1023, 5545, 2142];
        /// <summary>
        /// becomes Stack
        /// </summary>
        public static uint[] DebuffSpread = [587, 3799, 5544];
        /// <summary>
        /// becomes Donut
        /// </summary>
        public static uint[] DebuffFireSpread = [1600, 5547];
        /// <summary>
        /// becomes Fire Spread
        /// </summary>
        public static uint[] DebuffDonut = [1601, 5548];
        /// <summary>
        /// must pass mechanics
        /// </summary>
        public static uint DebuffLive = 454;
        /// <summary>
        /// must fail mechanics
        /// </summary>
        public static uint[] DebuffDie = [1382, 5464];
        /// <summary>
        /// when with DebuffLive: must take black; with DebuffDie: white
        /// </summary>
        public static uint[] DebuffWhitewould = [4887, 5541];
        /// <summary>
        /// when with DebuffLive: must take white; withDebuffDie: black
        /// </summary>
        public static uint[] DebuffBlackwound = [4888, 5542];
    }

    private Dictionary<uint, bool> IsTruth = [];
    public List<uint> DebuffList
    {
        get
        {
            if(field == null)
            {
                field = [];
                foreach(var x in typeof(Debuffs).GetFields().Select(x => x.GetValue(null)!))
                {
                    if(x is uint u)
                    {
                        field.Add(u);
                    }
                    if(x is uint[] u2)
                    {
                        field.Add(u2);
                    }
                }
            }
            return field;
        }
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Black", """
            {"Name":"","type":3,"refY":40.0,"radius":12,"fillIntensity":0.6,"refActorNPCNameID":6055,"refActorRequireCast":true,"refActorCastId":[50069],"refActorComparisonType":6,"includeRotation":true}
            """);
        Controller.RegisterElementFromCode("White", """
            {"Name":"","type":3,"refY":40.0,"radius":12,"fillIntensity":0.6,"refActorNPCNameID":6055,"refActorRequireCast":true,"refActorCastId":[50068],"refActorComparisonType":6,"includeRotation":true}
            """);
        Controller.RegisterElementsFromMultilineCode("""
            {"Name":"LookAway","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":2550136832,"overlayTextColor":4278190335,"thicc":3.0,"overlayText":"LOOK AWAY","refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[5543],"refActorUseBuffTime":true,"refActorBuffTimeMax":15.0,"tether":true,"overlayVOffset":2.0}
            {"Name":"LookAt","type":1,"radius":0.0,"color":3355508521,"fillIntensity":0.5,"overlayBGColor":2550136832,"overlayTextColor":4278255376,"thicc":3.0,"overlayText":"LOOK AT","refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[5543],"refActorUseBuffTime":true,"refActorBuffTimeMax":15.0,"tether":true,"overlayVOffset":2.0}
            {"Name":"EyeScope","type":4,"radius":15.0,"coneAngleMin":-45,"coneAngleMax":45,"color":3355506687,"fillIntensity":0.125,"thicc":3.0,"refActorType":1,"includeRotation":true,"FillStep":99.0,"RenderEngineKind":2}
            {"Name":"Hint","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayTextColor":4292739327,"overlayVOffset":5.0,"thicc":0.0,"overlayText":"test","refActorType":1}
            {"Name":"StackSupport","refX":100.0,"refY":89.0,"radius":3.0,"Donut":0.5,"color":3355508521,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4280024832,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"Stack support","tether":true}
            {"Name":"StackDPS","refX":100.0,"refY":111.0,"radius":3.0,"Donut":0.5,"color":3355508521,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4280024832,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"stack dps","tether":true}
            {"Name":"SpreadSupport","refX":89.0,"refY":100.0,"radius":3.0,"Donut":0.5,"color":3355501823,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4278255605,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"Spread support","tether":true}
            {"Name":"SpreadDPS","refX":111.0,"refY":100.0,"radius":3.0,"Donut":0.5,"color":3355501823,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4278255605,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"Spread dps","tether":true}
            {"Name":"StackSupport_2","refX":100.0,"refY":89.0,"radius":3.0,"Donut":0.5,"color":3355508521,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4280024832,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"Stack support","tether":true}
            {"Name":"StackDPS_2","refX":100.0,"refY":111.0,"radius":3.0,"Donut":0.5,"color":3355508521,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4280024832,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"stack dps","tether":true}
            {"Name":"SpreadSupport_2","refX":89.0,"refY":100.0,"radius":3.0,"Donut":0.5,"color":3355501823,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4278255605,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"Spread support","tether":true}
            {"Name":"SpreadDPS_2","refX":111.0,"refY":100.0,"radius":3.0,"Donut":0.5,"color":3355501823,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4278255605,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"Spread dps","tether":true}
            {"Name":"MiddleGaze","refX":99.61274,"refY":99.88139,"refZ":-1.9073486E-06,"radius":4.0,"Donut":0.5,"fillIntensity":0.5,"overlayVOffset":1.2,"thicc":6.0,"tether":true}

            {"Name":"MiddleDrop","refX":99.61274,"refY":99.88139,"refZ":-1.9073486E-06,"radius":3.0,"Donut":0.5,"fillIntensity":0.5,"overlayTextColor":4278228223,"overlayVOffset":1.2,"thicc":6.0,"overlayText":"$ELEMENT","tether":true}
            
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"Language","ZoneLockH":[1363],"ElementsL":[{"Name":"LookAt","overlayText":"Look at in #","overlayTextIntl":{"Jp":"#秒後に視線"}},{"Name":"LookAway","overlayText":"Look AWAY in #","overlayTextIntl":{"Jp":"#秒後に視線外す"}},{"Name":"Spread","overlayText":"Spread in #","overlayTextIntl":{"Jp":"#秒後に散開"}},{"Name":"Stack","overlayText":"Stack in #","overlayTextIntl":{"Jp":"#秒後に頭割り"}},{"Name":"DontMove","overlayText":"Don't move in #","overlayTextIntl":{"Jp":"#秒後に動くな"}},{"Name":"Move","overlayText":"Move in #","overlayTextIntl":{"Jp":"#秒後に動け"}},{"Name":"DropDonut","overlayText":"Drop donut in #","overlayTextIntl":{"Jp":"#秒後にドーナツ"}},{"Name":"DropAOE","overlayText":"Drop AOE in #","overlayTextIntl":{"Jp":"#秒後に範囲"}}]}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"Move","ZoneLockH":[1363],"ElementsL":[{"Name":"","type":3,"refX":-0.5,"refY":-0.5,"offX":-2.0,"offY":-2.0,"radius":0.0,"color":3357671168,"fillIntensity":0.345,"thicc":4.0,"refActorType":1,"LineEndB":1},{"Name":"","type":3,"refX":-0.5,"refY":0.5,"offX":-2.0,"offY":2.0,"radius":0.0,"color":3357671168,"fillIntensity":0.345,"thicc":4.0,"refActorType":1,"LineEndB":1},{"Name":"","type":3,"refX":0.5,"refY":0.5,"offX":2.0,"offY":2.0,"radius":0.0,"color":3357671168,"fillIntensity":0.345,"thicc":4.0,"refActorType":1,"LineEndB":1},{"Name":"","type":3,"refX":0.5,"refY":-0.5,"offX":2.0,"offY":-2.0,"radius":0.0,"color":3357671168,"fillIntensity":0.345,"thicc":4.0,"refActorType":1,"LineEndB":1}]}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"DontMove","ZoneLockH":[1363],"ElementsL":[{"Name":"","type":3,"refX":-0.5,"refY":-0.5,"offX":-2.0,"offY":-2.0,"radius":0.0,"fillIntensity":0.345,"thicc":4.0,"refActorType":1,"LineEndA":1},{"Name":"","type":3,"refX":0.5,"refY":0.5,"offX":2.0,"offY":2.0,"radius":0.0,"fillIntensity":0.345,"thicc":4.0,"refActorType":1,"LineEndA":1},{"Name":"","type":3,"refX":0.5,"refY":-0.5,"offX":2.0,"offY":-2.0,"radius":0.0,"fillIntensity":0.345,"thicc":4.0,"refActorType":1,"LineEndA":1},{"Name":"","type":3,"refX":-0.5,"refY":0.5,"offX":-2.0,"offY":2.0,"radius":0.0,"fillIntensity":0.345,"thicc":5.0,"refActorType":1,"LineEndA":1}]}
            """);
    }

    private void ShowSpread(float timer)
    {
        if(Controller.TryGetElementByName($"Spread{(BasePlayer.Job.IsDps() ? "DPS" : "Support")}{(C.DifferentiateFirstSecondStackSpread && !IsFirstStackSpread() ? "_2" : "")}", out var e))
        {
            e.Enabled = true;
            e.color = Controller.AttentionColor;
            e.overlayText = Str_Spread($"{timer:F1}");
        }
    }

    private void ShowStack(float timer)
    {
        if(Controller.TryGetElementByName($"Stack{(BasePlayer.Job.IsDps() ? "DPS" : "Support")}{(C.DifferentiateFirstSecondStackSpread && !IsFirstStackSpread() ? "_2" : "")}", out var e))
        {
            e.Enabled = true;
            e.color = Controller.AttentionColor;
            e.overlayText = Str_Stack($"{timer:F1}");
        }
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        if(Controller.GetPartyMembers().Any(x => x.HasStatus([1602, 1603])))
        {
            return;
        }

        if(BasePlayer.HasStatus([.. Debuffs.DebuffWhitewould, .. Debuffs.DebuffBlackwound], out var status))
        {
            var showWhite = status[0].ID.EqualsAny(Debuffs.DebuffWhitewould);
            if(FakeStatuses.Contains(new(BasePlayer.ObjectId, status[0].ID)))
            {
                showWhite = !showWhite;
            }

            if(BasePlayer.HasStatus(Debuffs.DebuffDie) && !FakeStatuses.ContainsAny(Debuffs.DebuffDie.Select(x => new StatusInfo(BasePlayer.ObjectId, x))))
            {
                showWhite = !showWhite;
            }

            if(BasePlayer.HasStatus(Debuffs.DebuffLive) && FakeStatuses.Contains(new(BasePlayer.ObjectId, Debuffs.DebuffLive)))
            {
                showWhite = !showWhite;
            }

            Controller.GetElementByName(showWhite ? "White" : "Black")!.Enabled = true;
        }

        List<(string Text, float Time)> hints = [];

        foreach(var x in Controller.GetPartyMembers())
        {
            if(x.HasStatus(Debuffs.DebuffLookAway, out var time, lessThan: C.LookDontlookTH))
            {
                var f = FakeStatuses.ContainsAny(Debuffs.DebuffLookAway.Select(s => new StatusInfo(x.ObjectId, s)));
                var hint = (f ? Str_LookAt($"{time.SafeSelect(0).Time:F1}") : Str_LookAway($"{time.SafeSelect(0).Time:F1}"), time.SafeSelect(0).Time);
                var gaze = Controller.GetElementByName(f ? "LookAt" : "LookAway");
                gaze.Enabled = true;
                gaze.overlayText = hint.Item1;
                Controller.GetElementByName("EyeScope").Enabled = true;
                if(x.AddressEquals(BasePlayer))
                {
                    var ca = Controller.GetElementByName("MiddleGaze");
                    ca.Enabled = true;
                    ca.color = Controller.AttentionColor;
                    ca.tether = Vector2.Distance(BasePlayer.Position.ToVector2(), ca.RefPosition.ToVector2()) > ca.radius;
                }
            }
        }
        var spread = false;
        {
            if(BasePlayer.HasStatus(Debuffs.DebuffStack, out var time, lessThan: C.StackSpreadTH) && FakeStatuses.ContainsAny(Debuffs.DebuffStack.Select(s => new StatusInfo(BasePlayer.ObjectId, s))))
            {
                ShowSpread(time.SafeSelect(0).Time);
                spread = true;
            }
        }
        {
            if(BasePlayer.HasStatus(Debuffs.DebuffSpread, out var time, lessThan: C.StackSpreadTH) && !FakeStatuses.ContainsAny(Debuffs.DebuffSpread.Select(s => new StatusInfo(BasePlayer.ObjectId, s))))
            {
                ShowSpread(time.SafeSelect(0).Time);
                spread = true;
            }
        }
        if(!spread)
        {
            foreach(var x in Controller.GetPartyMembers())
            {
                {
                    if(x.HasStatus(Debuffs.DebuffStack, out var time, lessThan: C.StackSpreadTH) && !FakeStatuses.ContainsAny(Debuffs.DebuffStack.Select(s => new StatusInfo(x.ObjectId, s))))
                    {
                        ShowStack(time.SafeSelect(0).Time);
                        break;
                    }
                }
                {
                    if(x.HasStatus(Debuffs.DebuffSpread, out var time, lessThan: C.StackSpreadTH) && FakeStatuses.ContainsAny(Debuffs.DebuffSpread.Select(s => new StatusInfo(x.ObjectId, s))))
                    {
                        ShowStack(time.SafeSelect(0).Time);
                        break;
                    }
                }
            }
        }
        {
            if(BasePlayer.HasStatus(Debuffs.DebuffDontMove, out var time, lessThan: C.MoveDontmoveTH))
            {
                var isNoMove = !FakeStatuses.ContainsAny(Debuffs.DebuffDontMove.Select(s => new StatusInfo(BasePlayer.ObjectId, s)));
                hints.Add((isNoMove ? Str_DontMove($"{time.SafeSelect(0).Time:F1}") : Str_Move($"{time.SafeSelect(0).Time:F1}"), time.SafeSelect(0).Time));
                if(isNoMove)
                {
                    if(Controller.TryGetLayoutByName("DontMove", out var l))
                    {
                        l.Enabled = time.SafeSelect(0).Time > 3 ? Environment.TickCount64 % 500 > 250 : Environment.TickCount64 % 250 > 125;
                    }
                }
                else
                {
                    if(Controller.TryGetLayoutByName("Move", out var l))
                    {
                        l.Enabled = time.SafeSelect(0).Time > 3 ? Environment.TickCount64 % 500 > 250 : Environment.TickCount64 % 250 > 125;
                    }
                }
            }
        }
        {
            if(BasePlayer.HasStatus(Debuffs.DebuffDonut, out var time, lessThan: C.DonutAOETH))
            {
                var h = !FakeStatuses.ContainsAny(Debuffs.DebuffDonut.Select(s => new StatusInfo(BasePlayer.ObjectId, s))) ? Str_DropDonut($"{time.SafeSelect(0).Time:F1}") : Str_DropAOE($"{time.SafeSelect(0).Time:F1}");
                //hints.Add((h, time.SafeSelect(0).Time));
                DisplayMiddleDropIfNeeded(h);
            }
        }
        {
            if(BasePlayer.HasStatus(Debuffs.DebuffFireSpread, out var time, lessThan: C.DonutAOETH))
            {
                var h = !FakeStatuses.ContainsAny(Debuffs.DebuffFireSpread.Select(s => new StatusInfo(BasePlayer.ObjectId, s))) ? Str_DropAOE($"{time.SafeSelect(0).Time:F1}") : Str_DropDonut($"{time.SafeSelect(0).Time:F1}");
                //hints.Add((h, time.SafeSelect(0).Time));
                DisplayMiddleDropIfNeeded(h);
            }
        }
        if(Controller.TryGetElementByName("Hint", out var e))
        {
            e.Enabled = true;
            e.overlayText = hints.OrderByDescending(x => x.Time).ThenBy(x => x.Text).Select(x => x.Text).Print("\n");
        }
    }

    private void DisplayMiddleDropIfNeeded(string t)
    {
        if(Controller.GetPartyMembers().All(x => !x.HasStatus(Debuffs.DebuffLookAway, 6.5f)))
        {
            var el = Controller.GetElementByName("MiddleDrop");
            el.Enabled = true;
            el.color = Controller.AttentionColor;
            el.overlayText = t;
        }
    }

    private string StrGetAndReplace(string element, string s)
    {
        var e = Controller.GetRegisteredLayouts().SafeSelect("Language")?.GetElement(element);
        if(e == null)
        {
            return "Text could not be retrieved, reset script's settings";
        }

        return e.overlayTextIntl.Get(e.overlayText).Replace("#", s);
    }
    private string Str_LookAt(string s) => StrGetAndReplace("LookAt", s);
    private string Str_LookAway(string s) => StrGetAndReplace("LookAway", s);
    private string Str_Spread(string s) => StrGetAndReplace("Spread", s);
    private string Str_Stack(string s) => StrGetAndReplace("Stack", s);
    private string Str_DontMove(string s) => StrGetAndReplace("DontMove", s);
    private string Str_Move(string s) => StrGetAndReplace("Move", s);
    private string Str_DropDonut(string s) => StrGetAndReplace("DropDonut", s);
    private string Str_DropAOE(string s) => StrGetAndReplace("DropAOE", s);

    public override void OnReset()
    {
        IsTruth.Clear();
        FakeStatuses.Clear();
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(target.GetObject()?.DataId.EqualsAny<uint>(19510, 19507) == true)
        {
            if(VfxTruth.Contains(vfxPath))
            {
                IsTruth[target] = true;
            }
            else if(VfxLie.Contains(vfxPath))
            {
                IsTruth[target] = false;
            }
        }
    }

    public bool IsLie = false;

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action != null && set.Source?.ObjectId.EqualsAny(IsTruth.Keys) == true)
        {
            IsLie = !IsTruth[set.Source.ObjectId];
        }
    }

    public override void OnGainBuffEffect(uint sourceId, FFXIVClientStructs.FFXIV.Client.Game.Status Status)
    {
        if(DebuffList.Contains(Status.StatusId) && sourceId.TryGetPlayer(out var pc))
        {
            if(IsLie)
            {
                FakeStatuses.Add(new(sourceId, Status.StatusId));
            }
            if(pc.AddressEquals(BasePlayer))
            {
                if((Debuffs.DebuffSpread.Contains(Status.StatusId) && !IsLie) || (Debuffs.DebuffStack.Contains(Status.StatusId) && IsLie))
                {
                    if(Status.RemainingTime > 60f)
                    {
                        if(C.UseSelfmark && C.MarkingParamLongSpread != 0)
                        {
                            if(GenericHelpers.IsScreenReady() && EzThrottler.Throttle("Chat", 1000))
                            {
                                var cmd = $"/marking {TextCommandParam.Get(C.MarkingParamLongSpread).Param.GetText()} <me>";
                                UseCommand(cmd);
                            }
                        }
                        if(C.OutputInChat)
                        {
                            Print(UIColor.Orange, C.LongSpread.Get());
                        }
                    }
                    else
                    {
                        if(C.UseSelfmark && C.MarkingParamShortSpread != 0)
                        {
                            if(GenericHelpers.IsScreenReady() && EzThrottler.Throttle("Chat", 1000))
                            {
                                var cmd = $"/marking {TextCommandParam.Get(C.MarkingParamShortSpread).Param.GetText()} <me>";
                                UseCommand(cmd);
                            }
                        }
                        if(C.OutputInChat)
                        {
                            Print(UIColor.Orange, C.ShortSpread.Get());
                        }
                    }
                }

                if(Debuffs.DebuffLookAway.Contains(Status.StatusId))
                {
                    if(Status.RemainingTime > 65f)
                    {
                        if(C.OutputInChat)
                        {
                            Print(UIColor.Red, IsLie?C.LongGazeInv.Get():C.LongGaze.Get());
                        }
                    }
                    else
                    {
                        if(C.OutputInChat)
                        {
                            Print(UIColor.Red, IsLie?C.ShortGazeInv.Get():C.ShortGaze.Get());
                        }
                    }
                }

                if(Debuffs.DebuffDontMove.Contains(Status.StatusId))
                {
                    if(C.OutputInChat)
                    {
                        Print(UIColor.Yellow, IsLie ? C.AccelerationBombInv.Get() : C.AccelerationBomb.Get());
                    }
                }
            }
        }
    }

    private void Print(UIColor color, string msg)
    {
        var entry = new XivChatEntry()
        {
            Message = new SeStringBuilder().AddUiForeground(msg, (ushort)color).Build()
        };
        if(C.OverrideChatType != XivChatType.None)
        {
            entry.Type = C.OverrideChatType;
        }

        Svc.Chat.Print(entry);
    }

    private void UseCommand(string cmd)
    {
        Controller.Schedule(() =>
        {
            if(Svc.Condition[ConditionFlag.DutyRecorderPlayback])
            {
                DuoLog.Warning($"Would use command: {cmd}");
            }
            else
            {
                Chat.ExecuteCommand(cmd);
            }
        }, 2000 + Random.Shared.Next(2000));
    }

    public bool IsFirstStackSpread()
    {
        return Controller.GetPartyMembers().Count(x => x.HasStatus([.. Debuffs.DebuffSpread, .. Debuffs.DebuffStack])) > 4;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Different positions for first and second stack/spread", ref C.DifferentiateFirstSecondStackSpread);
        if(C.DifferentiateFirstSecondStackSpread)
        {
            ImGuiEx.TextWrapped(ImGuiColors.DalamudRed, "   Go to Registered elements and adjust positions of elements with \"_2\" prefix for second set of spreads/stacks!!!");
        }

        ImGui.Checkbox("Output your debuffs into local chat (for you only)", ref C.OutputInChat);
        if(C.OutputInChat)
        {
            ImGui.Indent();
            ImGui.SetNextItemWidth(200f);
            ImGuiEx.EnumCombo("Override chat channel (it will NOT send it to that channel, still local only, only affects visual)", ref C.OverrideChatType);
            ImGui.Unindent();
        }
        ImGuiEx.Checkbox("Self-mark spreads (dangerous)", ref C.UseSelfmark, enabled: C.UseSelfmark || ImGuiEx.Ctrl);
        ImGuiEx.Tooltip("Hold CTRL and click to enable");
        if(C.UseSelfmark)
        {
            DrawMarkingParam("Short spread", ref C.MarkingParamShortSpread);
            DrawMarkingParam("Long spread", ref C.MarkingParamLongSpread);
        }

        ImGui.SetNextItemWidth(150f);
        ImGuiEx.SliderFloat($"Display stack/spread in advance, seconds", ref C.StackSpreadTH, 3, 20);
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.SliderFloat($"Display move/don't move in advance, seconds", ref C.MoveDontmoveTH, 3, 20);
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.SliderFloat($"Display look/don't look in advance, seconds", ref C.LookDontlookTH, 3, 20);
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.SliderFloat($"Display donut/AOE placement in advance, seconds", ref C.LookDontlookTH, 3, 20);
        ImGui.Separator();
        ImGui.SetNextItemWidth(200f);
        C.AccelerationBomb.ImGuiEditNoDefault();
        ImGui.SameLine();
        ImGuiEx.Text("Acceleration bomb, normal");

        ImGui.SetNextItemWidth(200f);
        C.AccelerationBombInv.ImGuiEditNoDefault();
        ImGui.SameLine();
        ImGuiEx.Text("Acceleration bomb, inverted");

        ImGui.SetNextItemWidth(200f);
        C.LongGaze.ImGuiEditNoDefault();
        ImGui.SameLine();
        ImGuiEx.Text("Long gaze (away)");

        ImGui.SetNextItemWidth(200f);
        C.LongGazeInv.ImGuiEditNoDefault();
        ImGui.SameLine();
        ImGuiEx.Text("Long gaze (at)");

        ImGui.SetNextItemWidth(200f);
        C.ShortGaze.ImGuiEditNoDefault();
        ImGui.SameLine();
        ImGuiEx.Text("Short gaze (away)");

        ImGui.SetNextItemWidth(200f);
        C.ShortGazeInv.ImGuiEditNoDefault();
        ImGui.SameLine();
        ImGuiEx.Text("Short gaze (at)");

        ImGui.SetNextItemWidth(200f);
        C.LongSpread.ImGuiEditNoDefault();
        ImGui.SameLine();
        ImGuiEx.Text("Long spread");

        ImGui.SetNextItemWidth(200f);
        C.ShortSpread.ImGuiEditNoDefault();
        ImGui.SameLine();
        ImGuiEx.Text("Short spread");


        if(ImGui.CollapsingHeader("Debug"))
        {
            if(ImGui.Button("Export"))
            {
                GenericHelpers.Copy(JsonConvert.SerializeObject(FakeStatuses));
            }

            if(ImGui.Button("Import"))
            {
                FakeStatuses = JsonConvert.DeserializeObject<List<StatusInfo>>(GenericHelpers.Paste()) ?? throw new NullReferenceException();
            }

            ImGui.Checkbox(nameof(IsLie), ref IsLie);
            ImGuiEx.Text($"List: {DebuffList.Print()}");
            ImGuiEx.Text($"Casters: {IsTruth.Select(x => $"{x.Key}: {x.Value}").Print("\n")}");
            ImGuiEx.Text($"Fakes: \n{FakeStatuses.Select(x => $"{x.objectId.GetObject()} / {x.statusId} ({Svc.Data.GetExcelSheet<Status>().GetRowOrDefault(x.statusId)?.Name})").Print("\n")}");
        }
    }

    private void DrawMarkingParam(string name, ref uint param)
    {
        ImGui.PushID(name);
        ImGui.SetNextItemWidth(200f);
        if(ImGui.BeginCombo(name, param == 0 ? "Not set" : TextCommandParam.GetRef(param).ValueNullable?.Param.GetText(), ImGuiComboFlags.HeightLarge))
        {
            if(ImGui.Selectable("Not Set", param == 0))
            {
                param = 0;
            }

            foreach(var x in ValidTextParams)
            {
                if(ImGui.Selectable(TextCommandParam.Get(x).Param.GetText(), param == x))
                {
                    param = x;
                }
            }
            ImGui.EndCombo();
        }
        ImGui.PopID();
    }

    public class Config
    {
        public float StackSpreadTH = 8.5f;
        public float MoveDontmoveTH = 8f;
        public float LookDontlookTH = 10f;
        public float DonutAOETH = 10f;
        public bool DifferentiateFirstSecondStackSpread = false;
        public uint MarkingParamShortSpread;
        public uint MarkingParamLongSpread;
        public bool UseSelfmark = false;
        public bool OutputInChat = true;
        public XivChatType OverrideChatType = XivChatType.None;
        public InternationalString AccelerationBomb = new(en: "Acceleration bomb on YOU (DON'T MOVE)");
        public InternationalString AccelerationBombInv = new(en: "Inverted acceleration bomb on YOU (MOVE)");
        public InternationalString LongGaze = new(en: "LONG GAZE on YOU (Look Away)");
        public InternationalString LongGazeInv = new(en: "LONG GAZE on YOU (Look At)");
        public InternationalString ShortGaze = new(en: "SHORT GAZE on YOU (Look Away)");
        public InternationalString ShortGazeInv = new(en: "SHORT GAZE on YOU (Look At)");
        public InternationalString LongSpread = new(en:"LONG SPREAD on YOU");
        public InternationalString ShortSpread = new(en:"SHORT SPREAD on YOU");
    }

    private uint[] ValidTextParams = [80, 82, 84, 86, 88, 90, 92, 94, 96, 98, 100, 102, 104, 476, 478, 480,];
}
