using Dalamud.Bindings.ImGui;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using Lumina.Excel.Sheets;
using Lumina.Excel.Sheets.Experimental;
using Newtonsoft.Json;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using TerraFX.Interop.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using Status = Lumina.Excel.Sheets.Status;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public class P4_Debuff_Reminder : SplatoonScript
{
    public override Metadata Metadata { get; } = new(5, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1363];

    private Config C => Controller.GetConfig<Config>();

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

    private sealed class Config : IEzConfig
    {
        public InternationalString LookAtHintText = new()
        {
            En = "Look at in {0}",
            Jp = "{0}秒後に視線"
        };

        public InternationalString LookAwayHintText = new()
        {
            En = "Look AWAY in {0}",
            Jp = "{0}秒後に視線外す"
        };

        public InternationalString SpreadHintText = new()
        {
            En = "Spread in {0}",
            Jp = "{0}秒後に散開"
        };

        public InternationalString StackHintText = new()
        {
            En = "Stack in {0}",
            Jp = "{0}秒後に頭割り"
        };

        public InternationalString DontMoveHintText = new()
        {
            En = "Don't move in {0}",
            Jp = "{0}秒後に動くな"
        };

        public InternationalString MoveHintText = new()
        {
            En = "Move in {0}",
            Jp = "{0}秒後に動け"
        };

        public InternationalString DropDonutHintText = new()
        {
            En = "Drop donut in {0}",
            Jp = "{0}秒後にドーナツ"
        };

        public InternationalString DropAoeHintText = new()
        {
            En = "Drop AOE in {0}",
            Jp = "{0}秒後に範囲"
        };

        public void EnsureDefaults()
        {
            LookAtHintText ??= new InternationalString { En = LookAtHintText.En, Jp = LookAtHintText.Jp };
            LookAwayHintText ??= new InternationalString { En = LookAwayHintText.En, Jp = LookAwayHintText.Jp };
            SpreadHintText ??= new InternationalString { En = SpreadHintText.En, Jp = SpreadHintText.Jp };
            StackHintText ??= new InternationalString { En = StackHintText.En, Jp = StackHintText.Jp };
            DontMoveHintText ??= new InternationalString { En = DontMoveHintText.En, Jp = DontMoveHintText.Jp };
            MoveHintText ??= new InternationalString { En = MoveHintText.En, Jp = MoveHintText.Jp };
            DropDonutHintText ??= new InternationalString { En = DropDonutHintText.En, Jp = DropDonutHintText.Jp };
            DropAoeHintText ??= new InternationalString { En = DropAoeHintText.En, Jp = DropAoeHintText.Jp };
        }
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
        C.EnsureDefaults();

        Controller.RegisterElementFromCode("Black", """
            {"Name":"","type":3,"refY":40.0,"radius":12,"fillIntensity":0.6,"refActorNPCNameID":6055,"refActorRequireCast":true,"refActorCastId":[50069],"refActorComparisonType":6,"includeRotation":true}
            """);
        Controller.RegisterElementFromCode("White", """
            {"Name":"","type":3,"refY":40.0,"radius":12,"fillIntensity":0.6,"refActorNPCNameID":6055,"refActorRequireCast":true,"refActorCastId":[50068],"refActorComparisonType":6,"includeRotation":true}
            """);
        Controller.RegisterElementsFromMultilineCode("""
            {"Name":"LookAway","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":2550136832,"overlayTextColor":4278190335,"thicc":3.0,"overlayText":"LOOK AWAY","refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[5543],"refActorUseBuffTime":true,"refActorBuffTimeMax":15.0,"tether":true}
            {"Name":"LookAt","type":1,"radius":0.0,"color":3355508521,"fillIntensity":0.5,"overlayBGColor":2550136832,"overlayTextColor":4278255376,"thicc":3.0,"overlayText":"LOOK AT","refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[5543],"refActorUseBuffTime":true,"refActorBuffTimeMax":15.0,"tether":true}
            {"Name":"EyeScope","type":4,"radius":15.0,"coneAngleMin":-45,"coneAngleMax":45,"color":3355506687,"fillIntensity":0.125,"thicc":3.0,"refActorType":1,"includeRotation":true,"FillStep":99.0,"RenderEngineKind":2}
            {"Name":"Hint","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayTextColor":4292739327,"overlayVOffset":5.0,"thicc":0.0,"overlayText":"test","refActorType":1}
            """);
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        if(BasePlayer.HasStatus([..Debuffs.DebuffWhitewould, ..Debuffs.DebuffBlackwound], out var status))
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
            if(x.HasStatus(Debuffs.DebuffLookAway, out var time, lessThan: 10))
            {
                var f = this.FakeStatuses.ContainsAny(Debuffs.DebuffLookAway.Select(s => new StatusInfo(x.ObjectId, s)));
                hints.Add((f ? FormatText(C.LookAtHintText, $"{time.SafeSelect(0).Time:F1}") : FormatText(C.LookAwayHintText, $"{time.SafeSelect(0).Time:F1}"), time.SafeSelect(0).Time));
                Controller.GetElementByName(f ? "LookAt" : "LookAway").Enabled = true;
                Controller.GetElementByName("EyeScope").Enabled = true;
                break;
            }
        }
        bool spread = false;
        {
            if(BasePlayer.HasStatus(Debuffs.DebuffStack, out var time, lessThan: 10f) && this.FakeStatuses.ContainsAny(Debuffs.DebuffStack.Select(s => new StatusInfo(BasePlayer.ObjectId, s))))
            {
                hints.Add((FormatText(C.SpreadHintText, $"{time.SafeSelect(0).Time:F1}"), time.SafeSelect(0).Time));
                spread = true;
            }
        }
        {
            if(BasePlayer.HasStatus(Debuffs.DebuffSpread, out var time, lessThan: 10f) && !this.FakeStatuses.ContainsAny(Debuffs.DebuffSpread.Select(s => new StatusInfo(BasePlayer.ObjectId, s))))
            {
                hints.Add((FormatText(C.SpreadHintText, $"{time.SafeSelect(0).Time:F1}"), time.SafeSelect(0).Time));
                spread = true;
            }
        }
        if(!spread)
        {
            foreach(var x in Controller.GetPartyMembers())
            {
                {
                    if(x.HasStatus(Debuffs.DebuffStack, out var time, lessThan: 10f) && !this.FakeStatuses.ContainsAny(Debuffs.DebuffStack.Select(s => new StatusInfo(x.ObjectId, s))))
                    {
                        hints.Add((FormatText(C.StackHintText, $"{time.SafeSelect(0).Time:F1}"), time.SafeSelect(0).Time));
                        break;
                    }
                }
                {
                    if(x.HasStatus(Debuffs.DebuffSpread, out var time, lessThan: 10f) && this.FakeStatuses.ContainsAny(Debuffs.DebuffSpread.Select(s => new StatusInfo(x.ObjectId, s))))
                    {
                        hints.Add((FormatText(C.StackHintText, $"{time.SafeSelect(0).Time:F1}"), time.SafeSelect(0).Time));
                        break;
                    }
                }
            }
        }
        {
            if(BasePlayer.HasStatus(Debuffs.DebuffDontMove, out var time, lessThan: 10f))
            {
                hints.Add((!this.FakeStatuses.ContainsAny(Debuffs.DebuffDontMove.Select(s => new StatusInfo(BasePlayer.ObjectId, s))) ? FormatText(C.DontMoveHintText, $"{time.SafeSelect(0).Time:F1}") : FormatText(C.MoveHintText, $"{time.SafeSelect(0).Time:F1}"), time.SafeSelect(0).Time));
            }
        }
        {
            if(BasePlayer.HasStatus(Debuffs.DebuffDonut, out var time, lessThan: 10f))
            {
                hints.Add((!this.FakeStatuses.ContainsAny(Debuffs.DebuffDonut.Select(s => new StatusInfo(BasePlayer.ObjectId, s))) ? FormatText(C.DropDonutHintText, $"{time.SafeSelect(0).Time:F1}") : FormatText(C.DropAoeHintText, $"{time.SafeSelect(0).Time:F1}"), time.SafeSelect(0).Time));
            }
        }
        {
            if(BasePlayer.HasStatus(Debuffs.DebuffFireSpread, out var time, lessThan: 10f))
            {
                hints.Add((!this.FakeStatuses.ContainsAny(Debuffs.DebuffFireSpread.Select(s => new StatusInfo(BasePlayer.ObjectId, s))) ? FormatText(C.DropAoeHintText, $"{time.SafeSelect(0).Time:F1}") : FormatText(C.DropDonutHintText, $"{time.SafeSelect(0).Time:F1}"), time.SafeSelect(0).Time));
            }
        }
        if(Controller.TryGetElementByName("Hint", out var e))
        {
            e.Enabled = true;
            e.overlayText = hints.OrderByDescending(x => x.Time).ThenBy(x => x.Text).Select(x => x.Text).Print("\n");
        }
    }

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
        }
    }

    // Format InternationalString placeholders.
    private static string FormatText(InternationalString text, params object[] args)
    {
        var format = text.Get() ?? "";
        try
        {
            return string.Format(format, args);
        }
        catch(FormatException)
        {
            return format;
        }
    }

    // Draw one editable InternationalString row in settings.
    private static void DrawInternationalString(string label, InternationalString text)
    {
        ImGui.PushID(label);
        var current = text.Get();
        text.ImGuiEdit(ref current);
        ImGui.SameLine();
        ImGui.Text(label);
        ImGui.PopID();
    }

    public override void OnSettingsDraw()
    {
        if(ImGui.CollapsingHeader("Hints", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawInternationalString("Look at", C.LookAtHintText);
            DrawInternationalString("Look away", C.LookAwayHintText);
            DrawInternationalString("Spread", C.SpreadHintText);
            DrawInternationalString("Stack", C.StackHintText);
            DrawInternationalString("Don't move", C.DontMoveHintText);
            DrawInternationalString("Move", C.MoveHintText);
            DrawInternationalString("Drop donut", C.DropDonutHintText);
            DrawInternationalString("Drop AOE", C.DropAoeHintText);
        }

        if(ImGui.CollapsingHeader("Debug"))
        {
            if(ImGui.Button("Export")) GenericHelpers.Copy(JsonConvert.SerializeObject(FakeStatuses));
            if(ImGui.Button("Import")) FakeStatuses = JsonConvert.DeserializeObject<List<StatusInfo>>(GenericHelpers.Paste()) ?? throw new NullReferenceException();
            ImGui.Checkbox(nameof(IsLie), ref IsLie);
            ImGuiEx.Text($"List: {DebuffList.Print()}");
            ImGuiEx.Text($"Casters: {IsTruth.Select(x => $"{x.Key}: {x.Value}").Print("\n")}");
            ImGuiEx.Text($"Fakes: \n{FakeStatuses.Select(x => $"{x.objectId.GetObject()} / {x.statusId} ({Svc.Data.GetExcelSheet<Status>().GetRowOrDefault(x.statusId)?.Name})").Print("\n")}");
        }
    }
}
