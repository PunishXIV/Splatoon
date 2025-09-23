using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.SimpleGui;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;
public sealed class P3_Dive_from_Grace_Neo : SplatoonScript
{
    public override Metadata Metadata { get; } = new(3, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [Raids.Dragonsongs_Reprise_Ultimate];

    IPlayerCharacter BasePlayer
    {
        get
        {
            if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.DutyRecorderPlayback] && C.BPO != "" && Players.TryGetFirst(x => x.GetNameWithWorld() == C.BPO, out var p))
            {
                return p;
            }
            return Player.Object;
        }
    }

    IEnumerable<IPlayerCharacter> Players => Controller.GetPartyMembers();


    const uint Pos1 = 3004;
    const uint Pos2 = 3005;
    const uint Pos3 = 3006;
    const uint SpotForward = 2756;
    const uint SpotBackwards = 2757;
    const uint SpotOnPlayer = 2755;
    public MechanicStage Stage;
    P3_Dive_from_Grace_Neo.AssignmentWindow Window;

    public override void OnSetup()
    {
       /* Controller.RegisterElementFromCode("NorthIn", """{"Name":"","refX":100.0,"refY":93.0,"radius":1.0,"Donut":0.2,"color":3355508484,"fillIntensity":0.494,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("NorthOut", """{"Name":"","refX":100.0,"refY":90.5,"radius":1.0,"Donut":0.2,"color":3355508484,"fillIntensity":0.494,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("SouthOut", """{"Name":"","refX":100.0,"refY":109.5,"radius":1.0,"Donut":0.2,"color":3355508484,"fillIntensity":0.494,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("SouthIn", """{"Name":"","refX":100.0,"refY":107.0,"radius":1.0,"Donut":0.2,"color":3355508484,"fillIntensity":0.494,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("WestIn", """{"Name":"","refX":93.0,"refY":100.0,"radius":1.0,"Donut":0.2,"color":3355508484,"fillIntensity":0.494,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("WestOut", """{"Name":"","refX":90.5,"refY":100.0,"radius":1.0,"Donut":0.2,"color":3355508484,"fillIntensity":0.494,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("EastIn", """{"Name":"","refX":107.0,"refY":100.0,"radius":1.0,"Donut":0.2,"color":3355508484,"fillIntensity":0.494,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("EastOut", """{"Name":"","refX":109.5,"refY":100.0,"radius":1.0,"Donut":0.2,"color":3355508484,"fillIntensity":0.494,"thicc":4.0,"tether":true}""");*/
        Controller.RegisterElementFromCode("NorthWest", """{"Name":"","refX":92,"refY":90.0,"radius":1.0,"Donut":0.2,"color":3355508484,"fillIntensity":0.494,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("NorthEast", """{"Name":"","refX":108,"refY":90.0,"radius":1.0,"Donut":0.2,"color":3355508484,"fillIntensity":0.494,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("FaceEast", """{"Name":"","type":3,"refX":5.0,"radius":0.0,"color":3372220160,"fillIntensity":0.345,"thicc":8.0,"refActorObjectID":0,"refActorComparisonType":2,"LineEndA":1}""");
        Controller.RegisterElementFromCode("FaceWest", """{"Name":"","type":3,"refX":-5.0,"radius":0.0,"color":3372220160,"fillIntensity":0.345,"thicc":8.0,"refActorObjectID":0,"refActorComparisonType":2,"LineEndA":1}""");
        Controller.RegisterElementFromCode("FaceLine", """{"Name":"","type":1,"radius":0.0,"color":3372220160,"fillIntensity":0.345,"overlayBGColor":2617245696,"overlayTextColor":4294963968,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":8.0,"overlayText":"Face Line!","refActorObjectID":0,"refActorComparisonType":2,"LineEndA":1}""");
        Controller.RegisterElementFromCode("West", """{"Name":"","refX":92,"refY":100.0,"radius":1.0,"Donut":0.2,"color":3355508484,"fillIntensity":0.494,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("East", """{"Name":"","refX":108,"refY":100.0,"radius":1.0,"Donut":0.2,"color":3355508484,"fillIntensity":0.494,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("North", """{"Name":"","refX":100.0,"refY":92,"radius":1.0,"Donut":0.2,"color":3355508484,"fillIntensity":0.494,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("South", """{"Name":"","refX":100.0,"refY":108,"radius":1.0,"Donut":0.2,"color":3355508484,"fillIntensity":0.494,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("Bait", """{"Name":"","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":2852126720,"overlayTextColor":4278190335,"overlayVOffset":3.0,"thicc":10.0,tether:true,"overlayText":"Bait Outside","refActorComparisonType":2,"onlyUnTargetable":true,"onlyVisible":true,"DistanceMax":5.0,"UseDistanceSourcePlaceholder":true}""");
        Controller.RegisterElementFromCode("Tether1", """{"Name":"","type":1,"radius":0.0,"color":3372155119,"Filled":false,"fillIntensity":0.494,"thicc":4.0,"refActorObjectID":3758096384,"refActorComparisonType":2,"tether":true}""");
        Controller.RegisterElementFromCode("Tether2", """{"Name":"","type":1,"radius":0.0,"color":3372155119,"Filled":false,"fillIntensity":0.494,"thicc":4.0,"refActorObjectID":3758096384,"refActorComparisonType":2,"tether":true}""");
    }

    public bool HaveStatus(IPlayerCharacter p, uint id) => p.StatusList.Any(x => x.StatusId == id);
    public bool HaveStatus(IPlayerCharacter p, IEnumerable<uint> id) => p.StatusList.Any(x => x.StatusId.EqualsAny(id));

    public bool IsCastingDive => Svc.Objects.OfType<IBattleNpc>().Any(x => x.IsCasting(26381));
    List<uint>[] Numbers = [[], [], []];

    public override void OnEnable()
    {
        Window = new(this);
    }

    public override void OnDisable()
    {
        Window.Dispose();
    }

    int MyNumber
    {
        get
        {
            for(var i = 0; i < Numbers.Length; i++)
            {
                var x = Numbers[i];
                if(x.Any(s => s == BasePlayer.EntityId)) return i;
            }
            PluginLog.Warning("Can not detemine own number");
            return default;
        }
    }

    enum Position { West, South, East }
    (Position Number, float Confidence) MyPosition;


    public enum MechanicStage
    {
        Initial,        
        AllAssigned,    //1: drop e/w/s             2: stack north      3: stack north
        Tower1Dropped,  //1: stack north            2: drop ne/nw       3: take e/w/s
        Tower1Taken,    //1: stack north            2: drop ne/nw       3: bait e/w/s
        Tower1Baited,   //1: stack north            2: drop ne/nw       3: drop e/w/s
        Tower2Dropped,  //1: take ne/nw(1/3)        2: stack north      3: drop e/w/s
                        //   stack north(2)
        Tower2Taken,    //1: bait ne/nw or north    2: stack north      3: drop e/w/s 
        Tower2Baited,   //1: stack north            2: stack north      3: drop e/w/s
        Tower3Dropped,  //1: idle (1/3)             2: take e/w         3: idle
                        //   take south (2)
        Tower3Taken,    //1: idle/bait south        2: bait e/w         3: idle
        Tower3Baited,   //end
    }

    public override void OnUpdate()
    {
        if(EzThrottler.Check("DontCloseWindowDFG")) Window.IsOpen = false;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(Controller.Scene != 6)
        {
            MyPosition = default;
            Stage = MechanicStage.Initial;
            return;
        }
        if(Players.All(x => HaveStatus(x, [Pos1, Pos2, Pos3])))
        {
            Window.IsOpen = true;
            EzThrottler.Throttle("DontCloseWindowDFG", 500, true);
            if(IsCastingDive)
            {
                Stage = MechanicStage.Initial;
            }
            if(Stage == MechanicStage.Initial) 
            { 
                Numbers = [[.. Players.Where(x => HaveStatus(x, Pos1)).Select(x => x.EntityId)], [.. Players.Where(x => HaveStatus(x, Pos2)).Select(x => x.EntityId)], [.. Players.Where(x => HaveStatus(x, Pos3)).Select(x => x.EntityId)]];
                var myPartners = Numbers[MyNumber];
                if(myPartners.Any(x => HaveStatus((IPlayerCharacter)x.GetObject(), SpotForward)))
                {
                    //assuming forward goes right, change later in config
                    if(!C.Invert)
                    {
                        if(HaveStatus(BasePlayer, SpotForward)) MyPosition = (Position.East, 999999);
                        if(HaveStatus(BasePlayer, SpotBackwards)) MyPosition = (Position.West, 999999);
                    }
                    else
                    {
                        if(HaveStatus(BasePlayer, SpotForward)) MyPosition = (Position.West, 777777);
                        if(HaveStatus(BasePlayer, SpotBackwards)) MyPosition = (Position.East, 777777);
                    }
                    if(HaveStatus(BasePlayer, SpotOnPlayer)) MyPosition = (Position.South, 999999);
                }
                else
                {
                    //attempt to figure it out
                    var suggested = GetIndexAndConfidence(myPartners);
                    if(suggested.Confidence > MyPosition.Confidence) MyPosition = ((Position)suggested.myIndex, suggested.Confidence);
                }
                int num = 0;
                foreach(var x in myPartners)
                {
                    if(x == BasePlayer.EntityId) continue;
                    if(Controller.TryGetElementByName($"Tether{++num}", out var e))
                    {
                        e.Enabled = true;
                        e.refActorObjectID = x;
                    }
                }
                if(Players.All(x => HaveStatus(x, [SpotForward, SpotBackwards, SpotOnPlayer])))
                {
                    Stage = MechanicStage.AllAssigned;
                }
            }
        }
        if(Stage == MechanicStage.AllAssigned)
        {
            if(MyNumber == 0)
            {
                ShowArrowIfNeeded();
                ShowMyPosition();
            }
            else if(MyNumber == 1 || MyNumber == 2)
            {
                ShowNorth();
            }
        }
        else if(Stage == MechanicStage.Tower1Dropped)
        {
            if(MyNumber == 0)
            {
                ShowNorth();
            }
            else if(MyNumber == 1)
            {
                ShowMy2Position();
                ShowArrowIfNeeded();
            }
            else if(MyNumber == 2)
            {
                ShowMyPosition();
            }
        }
        else if(Stage == MechanicStage.Tower1Taken)
        {
            if(MyNumber == 0)
            {
                ShowNorth();
            }
            else if(MyNumber == 1)
            {
                ShowMy2Position();
                ShowArrowIfNeeded();
            }
            else if(MyNumber == 2)
            {
                ShowBait();
            }
        }
        else if(Stage == MechanicStage.Tower1Baited)
        {
            if(MyNumber == 0)
            {
                ShowNorth();
            }
            else if(MyNumber == 1)
            {
                ShowMy2Position();
                ShowArrowIfNeeded();
            }
            else if(MyNumber == 2)
            {
                ShowMyPosition();
                ShowArrowIfNeeded();
            }
        }
        else if(Stage == MechanicStage.Tower2Dropped)
        {
            if(MyNumber == 0)
            {
                if(MyPosition.Number == Position.South)
                {
                    ShowNorth();
                }
                else
                {
                    ShowMy2Position();
                }
            }
            else if(MyNumber == 1)
            {
                ShowNorth();
            }
            else if(MyNumber == 2)
            {
                ShowMyPosition();
                ShowArrowIfNeeded();
            }
        }
        else if(Stage == MechanicStage.Tower2Taken)
        {
            if(MyNumber == 0)
            {
                if(MyPosition.Number == Position.South)
                {
                    ShowNorth();
                }
                else
                {
                    ShowBait();
                }
            }
            else if(MyNumber == 1)
            {
                ShowNorth();
            }
            else if(MyNumber == 2)
            {
                ShowMyPosition();
                ShowArrowIfNeeded();
            }
        }
        else if(Stage == MechanicStage.Tower2Baited)
        {
            if(MyNumber == 0)
            {
                ShowNorth();
            }
            else if(MyNumber == 1)
            {
                ShowNorth();
            }
            else if(MyNumber == 2)
            {
                ShowMyPosition();
                ShowArrowIfNeeded();
            }
        }
        else if(Stage == MechanicStage.Tower3Dropped)
        {
            if(MyNumber == 0)
            {
                if(MyPosition.Number == Position.South)
                {
                    ShowMyPosition();
                }
            }
            else if(MyNumber == 1)
            {
                ShowMyPosition();
            }
        }
        else if(Stage == MechanicStage.Tower3Taken)
        {
            if(MyNumber == 0)
            {
                if(MyPosition.Number == Position.South)
                {
                    ShowBait();
                }
            }
            else if(MyNumber == 1)
            {
                ShowBait();
            }
        }
    }

    void ShowBait()
    {
        var e = Controller.GetElementByName("Bait");
        var entity = Svc.Objects.OfType<IBattleNpc>().Where(x => x.NameId == 3458 && !x.IsTargetable && x.IsCharacterVisible()).OrderBy(Player.DistanceTo);
        e.refActorObjectID = entity.FirstOrDefault()?.EntityId ?? 0;
        e.Enabled = true;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action != null && set.Source is not IPlayerCharacter)
        {
            //PluginLog.Information(ExcelActionHelper.GetActionName(set.Action.Value.RowId, true));
            if(set.Action.Value.RowId == 26385) //tower taken
            {
                if(Stage == MechanicStage.Tower1Dropped)
                {
                    Stage = MechanicStage.Tower1Taken;
                }
                else if(Stage == MechanicStage.Tower2Dropped)
                {
                    Stage = MechanicStage.Tower2Taken;
                }
                if(Stage == MechanicStage.Tower3Dropped)
                {
                    Stage = MechanicStage.Tower3Taken;
                }
            }
            else if(set.Action.Value.RowId == 26382) //tower dropped
            {
                if(Stage == MechanicStage.AllAssigned)
                {
                    Stage = MechanicStage.Tower1Dropped;
                }
                else if(Stage == MechanicStage.Tower1Baited)
                {
                    Stage = MechanicStage.Tower2Dropped;
                }
                else if(Stage == MechanicStage.Tower2Baited)
                {
                    Stage = MechanicStage.Tower3Dropped;
                }
            }
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 26378) //tower baited
        {
            if(Stage == MechanicStage.Tower1Taken)
            {
                Stage = MechanicStage.Tower1Baited;
            }
            else if(Stage == MechanicStage.Tower2Taken)
            {
                Stage = MechanicStage.Tower2Baited;
            }
            if(Stage == MechanicStage.Tower3Taken)
            {
                Stage = MechanicStage.Tower3Baited;
            }
        }
    }

    void ShowMyPosition()
    {
        if(MyPosition.Number == Position.West) ShowWest();
        if(MyPosition.Number == Position.East) ShowEast();
        if(MyPosition.Number == Position.South) ShowSouth();
    }

    void ShowMy2Position()
    {
        if(MyPosition.Number == Position.West) Controller.GetElementByName("NorthWest").Enabled = true;
        if(MyPosition.Number == Position.East) Controller.GetElementByName("NorthEast").Enabled = true;
    }

    void ShowNorth() => Controller.GetElementByName("North").Enabled = true;
    void ShowSouth() => Controller.GetElementByName("South").Enabled = true;
    void ShowEast() => Controller.GetElementByName("East").Enabled = true;
    void ShowWest() => Controller.GetElementByName("West").Enabled = true;

    void ShowArrowIfNeeded()
    {
        if(HaveStatus(BasePlayer, SpotForward))
        {
            if(MyPosition.Number == Position.West)
            {
                Controller.GetElementByName("FaceEast").Enabled = true;
                Controller.GetElementByName("FaceEast").refActorObjectID = BasePlayer.EntityId;
                Controller.GetElementByName("FaceLine").Enabled = true;
                Controller.GetElementByName("FaceLine").refActorObjectID = BasePlayer.EntityId;
            }
            if(MyPosition.Number == Position.East)
            {
                Controller.GetElementByName("FaceWest").Enabled = true; 
                Controller.GetElementByName("FaceWest").refActorObjectID = BasePlayer.EntityId;
                Controller.GetElementByName("FaceLine").Enabled = true;
                Controller.GetElementByName("FaceLine").refActorObjectID = BasePlayer.EntityId;
            }
        }
        if(HaveStatus(BasePlayer, SpotBackwards))
        {
            if(MyPosition.Number == Position.East)
            {
                Controller.GetElementByName("FaceEast").Enabled = true;
                Controller.GetElementByName("FaceEast").refActorObjectID = BasePlayer.EntityId;
                Controller.GetElementByName("FaceLine").Enabled = true;
                Controller.GetElementByName("FaceLine").refActorObjectID = BasePlayer.EntityId;
            }
            if(MyPosition.Number == Position.West)
            {
                Controller.GetElementByName("FaceWest").Enabled = true;
                Controller.GetElementByName("FaceWest").refActorObjectID = BasePlayer.EntityId;
                Controller.GetElementByName("FaceLine").Enabled = true;
                Controller.GetElementByName("FaceLine").refActorObjectID = BasePlayer.EntityId;
            }
        }
    }

    public (int myIndex, float Confidence) GetIndexAndConfidence(List<uint> ids)
    {
        var basePlayerId = BasePlayer.EntityId;
        Vector2 getPosition(uint id) => id.GetObject().Position.ToVector2();
        if(ids == null || ids.Count < 2 || ids.Count > 3)
            throw new ArgumentException("IDs must contain exactly 2 or 3 elements.");

        var positions = ids.Select(id => (id, pos: getPosition(id))).ToList();
        var basePos = getPosition(BasePlayer.EntityId);

        if(ids.Count == 2)
        {
            var ordered = positions.OrderBy(p => p.pos.X).ToList();
            int index = ordered.FindIndex(p => p.id == basePlayerId) * 2;
            float confidence = Vector2.Distance(ordered[0].pos, ordered[1].pos);
            return (index, confidence);
        }
        else // 3 IDs
        {
            var ordered = positions.OrderBy(p => p.pos.X).ToList();
            int baseIndex = ordered.FindIndex(p => p.id == basePlayerId);

            var special = ordered
                .Where(p =>
                { 
                    var dx = MathF.Abs(p.pos.X - basePos.X);
                    var dy = MathF.Abs(p.pos.Y - basePos.Y);
                    return dy > dx && p.pos.Y > basePos.Y;
                })
                .FirstOrDefault();

            if(!special.Equals(default))
            {
                // Special one is index 1
                var finalOrder = new (uint id, Vector2 pos)[3];
                finalOrder[1] = special;

                var others = ordered.Where(p => p.id != special.id).ToList();
                finalOrder[0] = others[0];
                finalOrder[2] = others[1];

                int index = Array.FindIndex(finalOrder, p => p.id == basePlayerId);

                float confidence = 0f;
                for(int i = 0; i < 3; i++)
                {
                    if(finalOrder[i].id != basePlayerId)
                    {
                        confidence += Vector2.Distance(finalOrder[i].pos, basePos);
                    }
                }

                return (index, confidence);
            }
            else
            {
                // All in line, just keep X order
                int index = baseIndex;
                float confidence = 0f;
                foreach(var p in ordered)
                {
                    if(p.id != basePlayerId)
                        confidence += Vector2.Distance(p.pos, basePos);
                }
                return (index, confidence);
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.RadioButtonBool("Easthogg (beta)", "Westhogg", ref C.Invert);
        ImGui.DragFloat2("Window offset", ref C.Offset);
        if(ImGui.IsItemHovered())
        {
            if(this.IsEnabled)
            {
                this.Window.IsOpen = true;
                EzThrottler.Throttle("DontCloseWindowDFG", 500, true);
            }
        }
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"Stage: {Stage}");
            ImGuiEx.Text($"MyPositoon: {MyPosition}");
            ImGui.InputText("BPO", ref C.BPO);
            if(ImGui.BeginCombo("##sel", "Select base player"))
            {
                foreach(var x in Players)
                {
                    if(ImGuiEx.Selectable($"{x.GetNameWithWorld()}"))
                    {
                        C.BPO = x.GetNameWithWorld();
                        MyPosition = default;
                    }
                }
                ImGui.EndCombo();
            }
            if(ImGui.Button("Reset position"))
            {
                MyPosition = default;
            }
            ImGuiEx.Text($"My number: {MyNumber}");
            ImGuiEx.Text(Numbers.Select(x => x.Select(o => o.GetObject()).Print("\n")).Print("\n\n"));
            ImGui.Separator();
            ImGuiEx.Text($"Partners: {Numbers[MyNumber].Select(x => x.GetObject()).Print()}");
        }
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public string BPO = "";
        public Vector2 Offset = Vector2.Zero;
        public bool Invert = false;
    }

    class AssignmentWindow : Window, IDisposable
    {
        P3_Dive_from_Grace_Neo Script;
        public AssignmentWindow(P3_Dive_from_Grace_Neo s) : base("Dive from Grace Neo", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoFocusOnAppearing, true)
        {
            Script = s;
            EzConfigGui.WindowSystem.AddWindow(this);
            this.IsOpen = false;
            this.ShowCloseButton = false;
            this.AllowClickthrough = false;
            this.AllowPinning = false;
            this.RespectCloseHotkey = false;
        }

        public void Dispose()
        {
            EzConfigGui.WindowSystem.RemoveWindow(this);
        }

        public override void Draw()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));
            ImGuiEx.Text($"Your position: {Script.MyPosition.Number}, confidence={Script.MyPosition.Confidence}");
            var pos = Script.MyPosition.Number;
            if(pos == P3_Dive_from_Grace_Neo.Position.West) ImGui.PushStyleColor(ImGuiCol.Text, EColor.RedBright);
            if(ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ArrowLeft, "West", size: new(80f.Scale(), 50f.Scale()))) { Script.MyPosition = (P3_Dive_from_Grace_Neo.Position.West, 555555); }
            if(pos == P3_Dive_from_Grace_Neo.Position.West) ImGui.PopStyleColor();
            var dis = Script.Numbers[Script.MyNumber].Count == 2;
            if(dis) ImGui.BeginDisabled();
            ImGui.SameLine();
            if(pos == P3_Dive_from_Grace_Neo.Position.South) ImGui.PushStyleColor(ImGuiCol.Text, EColor.RedBright);
            if(ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ArrowDown, "South", size: new(80f.Scale(), 50f.Scale()))) { Script.MyPosition = (P3_Dive_from_Grace_Neo.Position.South, 555555); }
            if(pos == P3_Dive_from_Grace_Neo.Position.South) ImGui.PopStyleColor();
            if(dis) ImGui.EndDisabled();
            ImGui.SameLine();
            if(pos == P3_Dive_from_Grace_Neo.Position.East) ImGui.PushStyleColor(ImGuiCol.Text, EColor.RedBright);
            if(ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ArrowRight, "East", size:new(80f.Scale(), 50f.Scale()))) { Script.MyPosition = (P3_Dive_from_Grace_Neo.Position.East, 555555); }
            if(pos == P3_Dive_from_Grace_Neo.Position.East) ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            this.Position = new Vector2(ImGuiHelpers.MainViewport.Size.X / 2 - ImGui.GetWindowSize().X / 2, 0) + Script.C.Offset;
        }
    }
}