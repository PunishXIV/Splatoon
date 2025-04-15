using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ImGuiNET;
using NightmareUI.PrimaryUI;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public unsafe class P3_Apocalypse : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(11, "Errer, NightmareXIV");
    public long StartTime = 0;
    private bool IsAdjust = false;
    private bool IsClockwise = true;

    private long Phase => Environment.TickCount64 - StartTime;

    public override Dictionary<int, string> Changelog => new()
    {
        [9] = """
            - Added an option to make safe spots different when rotating cw and ccw
            """,
        [10] = "Added second stack display hint",
        [11] = "Fixed issues regarding to second stack",
    };

    public int NumDebuffs => Svc.Objects.OfType<IPlayerCharacter>().Count(x => x.StatusList.Any(s => s.StatusId == 2461));

    private List<Vector2> Spreads = [new(106, 81.5f), new(100, 90.5f), new(96, 81), new(93, 93.5f)];
    private List<Vector2> SpreadsInverted = [new(100 - 6, 81.5f), new(100, 90.5f), new(100 + 4, 81), new(100 + 7, 93.5f)];
    private Dictionary<int, Vector2> Positions = new()
    {
        [0] = new(100, 100),
        [1] = new(100, 86),
        [2] = new(109.9f, 90.1f),
        [3] = new(114, 100),
        [4] = new(109.9f, 109.9f),
        [-4] = new(90.1f, 90.1f),
        [-3] = new(86, 100),
        [-2] = new(90.1f, 109.9f),
        [-1] = new(100, 114),
    };

    public override void OnSetup()
    {
        for(var i = 0; i < 6; i++)
        {
            Controller.RegisterElementFromCode($"Circle{i}", "{\"Name\":\"Circle\",\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"radius\":9.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
            Controller.RegisterElementFromCode($"EarlyCircle{i}", "{\"Name\":\"Circle\",\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"radius\":9.0,\"color\":3355508223,\"fillIntensity\":0.25,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        }
        Controller.RegisterElementFromCode("TankLine", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"radius\":0.0,\"color\":3372209152,\"fillIntensity\":0.345,\"thicc\":4.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Line2", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":4.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("LineRot1", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"radius\":0.0,\"color\":3355508484,\"fillIntensity\":0.345,\"thicc\":4.0,\"LineEndA\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("LineRot2", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"radius\":0.0,\"color\":3355508484,\"fillIntensity\":0.345,\"thicc\":4.0,\"LineEndA\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        Controller.RegisterElementFromCode("Adjust", "{\"Name\":\"\",\"Enabled\":false,\"refX\":88.87717,\"refY\":108.2411,\"radius\":4.0,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":2936012800,\"overlayTextColor\":4278190335,\"overlayFScale\":2.0,\"thicc\":4.0,\"overlayText\":\"ADJUST\",\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        Controller.RegisterElementFromCode("Safe", "{\"Name\":\"\",\"Enabled\":false,\"refX\":99.93774,\"refY\":87.4826,\"radius\":2.0,\"color\":3355508521,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":10.0,\"overlayText\":\"<<< Safe spot >>>\",\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        Controller.RegisterElementFromCode("KB", "{\"Name\":\"\",\"type\":1,\"Enabled\":false,\"radius\":0,\"fillIntensity\":0.5,\"thicc\":4.0,\"refActorNPCNameID\":9832,\"refActorComparisonType\":6,\"onlyTargetable\":true,\"tether\":true,\"ExtraTetherLength\":22.0,\"LineEndB\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        Controller.RegisterElementFromCode("Stack", "{\"Name\":\"\",\"Enabled\":false,\"refX\":89.721855,\"refY\":98.5322,\"refZ\":-1.9073486E-06,\"radius\":1.0,\"color\":3355476735,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":4278190080,\"thicc\":4.0,\"overlayText\":\">>> Stack <<<\",\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        for(var i = 0; i < 8; i++)
        {
            Controller.RegisterElementFromCode($"Debug{i}", "{\"Name\":\"\",\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        }

        for(var i = 0; i < 4; i++)
        {
            Controller.RegisterElementFromCode($"Spreads{i}", "{\"Name\":\"\",\"Enabled\":false,\"refX\":93.0,\"refY\":93.5,\"refZ\":9.536743E-07,\"radius\":0.5,\"Donut\":0.2,\"color\":3357671168,\"fillIntensity\":1.0,\"thicc\":1.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        }

        Controller.RegisterElementFromCode($"SecondStack", "{\"Name\":\"\",\"radius\":0.75,\"color\":4278251775,\"Filled\":false,\"fillIntensity\":0.5,\"overlayTextColor\":4278234623,\"thicc\":5.0,\"overlayText\":\">>> Stack <<<\",\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    private int[][] Clockwise = [[0, 1, -1], [0, 1, -1, 2, -2], [1, 2, 3, -1, -2, -3], [2, 3, 4, -2, -3, -4], [1, 3, 4, -1, -3, -4], [1, 2, 4, -1, -2, -4]];
    private int[][] CounterClockwise = [[0, 1, -1], [0, 1, -1, 4, -4], [1, 3, 4, -1, -3, -4], [2, 3, 4, -2, -3, -4], [1, 2, 3, -1, -2, -3], [1, 2, 4, -1, -2, -4]];

    private void Draw(int[] values, int rotation, bool early = false)
    {
        for(var i = 0; i < values.Length; i++)
        {
            if(Controller.TryGetElementByName((early ? "Early" : "") + $"Circle{i}", out var e))
            {
                e.Enabled = true;
                var pos = Positions[values[i]].ToVector3(0);
                var rotated = MathHelper.RotateWorldPoint(new(100, 0, 100), rotation.DegreesToRadians(), pos);
                e.SetRefPosition(rotated);
            }
        }
    }

    private int InitialDelay = 14000 + 4000;
    private int Delay = 2000;

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 40296) StartTime = Environment.TickCount64;
    }

    public override void OnReset()
    {
        IsAdjust = false;
        IsClockwise = true;
    }

    private List<int> GetValidPositions(bool respectAdjust)
    {
        List<int> positions;
        if(!C.IsDifferentSafeSpots)
        {
            positions = C.SelectedPositions;
        }
        else
        {
            positions = IsClockwise ? C.SelectedPositions : C.SelectedPositionsAlt;
        }
        if(respectAdjust && IsAdjust)
        {
            return Positions.Keys.Where(x => !C.SelectedPositions.Contains(x) && x != 0).ToList();
        }
        else
        {
            return C.SelectedPositions;
        }
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

        if(C.SelectedPositions.Count == 4)
        {
            if(NumDebuffs == 6 && Svc.Objects.OfType<IPlayerCharacter>().Any(s => GetDebuffTime(s) > 31))
            {
                var selfDebuffTime = GetDebuffTime(Player.Object);
                if(C.Priority.GetOwnIndex(x => Math.Abs(GetDebuffTime((IPlayerCharacter)x.IGameObject) - selfDebuffTime) < 1, true) == 1)
                {
                    IsAdjust = true;
                    var positionToAdjust = Positions[GetValidPositions(true)[2]];
                    if(Controller.TryGetElementByName("Adjust", out var e))
                    {
                        e.Enabled = Player.DistanceTo(positionToAdjust) > 5;
                        e.SetRefPosition(positionToAdjust.ToVector3(0));
                    }
                }
            }
        }

        if(Phase < 60000 && C.SelectedPositions.Count == 4 && NumDebuffs <= 2)
        {
            var players = Svc.Objects.OfType<IPlayerCharacter>().Where(x => GetDebuffTime(x) > 0).ToArray();
            if(players.Length > 0)
            {
                var time = GetDebuffTime(players[0]);
                if(time < 6)
                {
                    var gaia = Svc.Objects.FirstOrDefault(x => x is IBattleNpc n && n.NameId == 9832 && n.IsTargetable);
                    if(gaia != null)
                    {
                        var gaiaAngle = (180 + 360 - MathHelper.GetRelativeAngle(new(100, 0, 100), gaia.Position)) % 360;
                        var a = -1;
                        if(C.IsLeftStack) a *= -1;
                        if(IsAdjust) a *= -1;
                        var adjustedAngle = ((gaiaAngle + a * 20 + 360) % 360);
                        var adjustedPoint = MathHelper.GetPointFromAngleAndDistance(new(100, 100), adjustedAngle.DegreesToRadians(), 8f).ToVector3(0);
                        var stk = Controller.GetElementByName("Stack")!;
                        stk.SetRefPosition(adjustedPoint);
                        stk.Enabled = true;
                    }
                    if(time > 3)
                    {
                        //show kb helper
                        var elem = Controller.GetElementByName("KB")!;
                        elem.Enabled = true;
                    }
                }
            }
        }

        var obj = Svc.Objects.Where(x => x.DataId == 2011391);
        var close = obj.FirstOrDefault(x => Vector3.Distance(x.Position, Positions[0].ToVector3(0)) < 1f);
        var far = obj.FirstOrDefault(x => Vector3.Distance(x.Position, Positions[1].ToVector3(0)) < 1f);
        if(close != null && far != null)
        {
            var rot = close.Rotation.RadToDeg();
            var angle = 0;
            if(rot.InRange(22, 22 + 45) || rot.InRange(180 + 22, 180 + 22 + 45)) angle = 45 * 3;
            if(rot.InRange(22 + 45, 22 + 45 * 2) || rot.InRange(180 + 22 + 45, 180 + 22 + 45 * 2)) angle = 45 * 2;
            if(rot.InRange(22 + 45 * 2, 22 + 45 * 3) || rot.InRange(180 + 22 + 45 * 2, 180 + 22 + 45 * 3)) angle = 45;
            IsClockwise = far.Rotation.RadToDeg().InRange(45, 45 + 90);
            var set = IsClockwise ? Clockwise : CounterClockwise;
            if(C.ShowInitialApocMove && Phase < 15000)
            {
                if(Controller.TryGetElementByName("Line2", out var line))
                {
                    line.Enabled = true;
                    var linePos1 = Positions[1].ToVector3(0);
                    var linePos2 = Positions[-1].ToVector3(0);
                    line.SetRefPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos1));
                    line.SetOffPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos2));
                }
            }

            if(C.SelectedPositions.Count == 4 && Phase < 22000 && NumDebuffs == 4)
            {
                Vector3[] candidates = [MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), Positions[IsClockwise ? -4 : 2].ToVector3(0)), MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), Positions[IsClockwise ? 4 : -2].ToVector3(0))];
                var i = 0;
                foreach(var x in GetValidPositions(!C.OriginalSpreads))
                {
                    if(ShowDebug && Controller.TryGetElementByName($"Debug{i++}", out var d))
                    {
                        d.Enabled = true;
                        d.SetRefPosition(Positions[x].ToVector3(0));
                        d.overlayText = $"Safe {x}";
                    }
                    foreach(var pos in candidates)
                    {
                        if(Vector2.Distance(Positions[x], pos.ToVector2()) < 2f)
                        {
                            var element = Controller.GetElementByName("Safe")!;
                            element.Enabled = Phase < C.TankDelayMS;
                            element.SetRefPosition(pos);
                            var adjustAngle = MathHelper.GetRelativeAngle(new Vector2(100f, 100f), pos.ToVector2());
                            for(var s = 0; s < Spreads.Count; s++)
                            {
                                if(Controller.TryGetElementByName($"Spreads{s}", out var e))
                                {
                                    e.Enabled = true;
                                    var adjPos = MathHelper.RotateWorldPoint(new(100, 0, 100), adjustAngle.DegreesToRadians(), (IsClockwise ? Spreads : SpreadsInverted)[s].ToVector3(0));
                                    e.SetRefPosition(adjPos);
                                }
                            }
                        }

                    }
                }
            }

            if(C.SelectedPositions.Count == 4 && Phase.InRange(17000, 26500) && !Svc.Objects.OfType<IBattleNpc>().Any(x => x.Struct()->GetCastInfo() != null && x.CastActionId == 40273 && x.CurrentCastTime < 4.7f))
            {
                Vector3[] candidates = [MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), Positions[IsClockwise ? -4 : 2].ToVector3(0)), MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), Positions[IsClockwise ? 4 : -2].ToVector3(0))];
                foreach(var x in GetValidPositions(true))
                {
                    foreach(var pos in candidates)
                    {
                        if(Vector2.Distance(Positions[x], pos.ToVector2()) < 2f)
                        {
                            var element = Controller.GetElementByName("SecondStack")!;
                            element.Enabled = true;
                            var adjustAngle = MathHelper.GetRelativeAngle(new Vector2(100f, 100f), pos.ToVector2());
                            var rotatedPoint = MathHelper.RotateWorldPoint(new(100, 0, 100), adjustAngle.DegreesToRadians(), new(100, 0, 96));
                            element.SetRefPosition(rotatedPoint);
                        }

                    }
                }
            }

            if(C.ShowMoveGuide && Phase < C.TankDelayMS)
            {
                {
                    if(Controller.TryGetElementByName("LineRot1", out var line))
                    {
                        line.Enabled = true;
                        var linePos1 = Positions[IsClockwise ? -4 : 2].ToVector3(0);
                        var linePos2 = Positions[1].ToVector3(0);
                        line.SetRefPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos1));
                        line.SetOffPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos2));
                    }
                }
                {
                    if(Controller.TryGetElementByName("LineRot2", out var line))
                    {
                        line.Enabled = true;
                        var linePos1 = Positions[IsClockwise ? 4 : -2].ToVector3(0);
                        var linePos2 = Positions[-1].ToVector3(0);
                        line.SetRefPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos1));
                        line.SetOffPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos2));
                    }
                }
            }
            if(Phase > C.DelayMS)
            {
                for(var i = 0; i < 6; i++)
                {
                    if(Phase < InitialDelay + Delay * i)
                    {
                        Draw(set[i], angle);
                        if(i < 5)
                        {
                            Draw(set[i + 1].Where(x => !set[i].Contains(x)).ToArray(), angle, true);
                        }
                        break;
                    }
                }
            }
            if(C.ShowTankGuide && Phase > C.TankDelayMS)
            {
                if(Controller.TryGetElementByName("TankLine", out var line))
                {
                    line.Enabled = true;
                    var linePos1 = Positions[3].ToVector3(0);
                    var linePos2 = Positions[-3].ToVector3(0);
                    line.SetRefPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos1));
                    line.SetOffPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos2));
                }
            }
        }
    }

    private void ResolvePrio()
    {
        if(C.SelectedPositions.Count != 4) return;
    }

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.SliderIntAsFloat("Delay before displaying AOE", ref C.DelayMS, 0, 12000);
        ImGui.Checkbox("Show initial movement", ref C.ShowInitialApocMove);
        ImGui.Checkbox("Show move guide for party (arrows to safe spot from initial movement)", ref C.ShowMoveGuide);
        ImGui.Checkbox("Show tank bait guide (beta)", ref C.ShowTankGuide);
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.SliderIntAsFloat("Hide move guide and switch to tank bait guide at", ref C.TankDelayMS, 0, 30000);
        new NuiBuilder().Section("Safe spots and adjustments").Widget(() =>
        {
            ImGuiEx.TextWrapped(EColor.RedBright, $"If you want to resolve adjusts, safe spot and stack position, fill the priority list.");
            ImGui.Checkbox("Original groups for Dark Eruption", ref C.OriginalSpreads);
            ImGuiEx.Text($"Select 4 safe spot positions for your default group");
            ImGui.Checkbox("Different safe spot positions for clockwise/counter-clockwise", ref C.IsDifferentSafeSpots);
            //-4  1  2
            //-3  0  3
            //-2 -1  4
            int[][] collection = [[-4, 1, 2], [-3, 0, 3], [-2, -1, 4]];
            void drawSelector(ICollection<int> selectedPositions)
            {
                foreach(var a in collection)
                {
                    foreach(var b in a)
                    {
                        var dis = b == 0 || (selectedPositions.Count >= 4 && !selectedPositions.Contains(b));
                        if(dis) ImGui.BeginDisabled();
                        ImGuiEx.CollectionCheckbox($"##{b}", b, selectedPositions);
                        if(dis) ImGui.EndDisabled();
                        ImGui.SameLine();
                    }
                    ImGui.NewLine();
                }
                if(selectedPositions.Count == 4)
                {
                    ImGuiEx.Text(EColor.GreenBright, "Configuration is valid");
                }
                else
                {
                    ImGuiEx.Text(EColor.RedBright, "Configuration is not valid. 4 positions must be selected.");
                }
            }
            ImGui.Indent();
            if(!C.IsDifferentSafeSpots)
            {
                drawSelector(C.SelectedPositions);
            }
            else
            {
                ImGuiEx.Text("Clockwise pattern:");
                ImGui.Indent();
                drawSelector(C.SelectedPositions);
                ImGui.Unindent();
                ImGuiEx.Text("Counter-clockwise pattern:");
                ImGui.Indent();
                ImGui.PushID("CCW");
                drawSelector(C.SelectedPositionsAlt);
                ImGui.PopID();
                ImGui.Unindent();
            }
            ImGui.Unindent();
            ImGuiEx.Text("Your default stack (when looking at Gaia):");
            ImGuiEx.RadioButtonBool("Left", "Right", ref C.IsLeftStack, true);
            C.Priority.Draw();
        }).Draw();
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"""
                NumDebuffs: {NumDebuffs}
                """);
            foreach(var x in Svc.Objects.OfType<IPlayerCharacter>())
            {
                ImGuiEx.Text($"{x.Name}: {GetDebuffTime(x)}");
            }
            ImGui.Checkbox("Adjust", ref IsAdjust);
            ImGui.Checkbox("ShowDebug", ref ShowDebug);
            ImGuiEx.Text($"unadjusted: {C.SelectedPositions.Print()}");
            ImGuiEx.Text($"Safe: {GetValidPositions(true).Print()}");
            var i = 0;
            foreach(var x in GetValidPositions(true))
            {
                if(Controller.TryGetElementByName($"Debug{i++}", out var d))
                {
                    d.Enabled = true;
                    d.SetRefPosition(Positions[x].ToVector3(0));
                    d.overlayText = $"Safe {x}";
                }
            }
        }
    }

    private bool ShowDebug = false;

    private Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public int DelayMS = 8000;
        public bool ShowInitialApocMove = true;
        public bool ShowMoveGuide = false;
        public bool ShowTankGuide = false;
        public int TankDelayMS = 18000;
        public Priority4 Priority = new();
        public List<int> SelectedPositions = [];
        public bool IsLeftStack = false;
        public bool OriginalSpreads = false;
        public bool IsDifferentSafeSpots = false;
        public List<int> SelectedPositionsAlt = [];
    }

    public class Priority4 : PriorityData
    {
        public override int GetNumPlayers()
        {
            return 4;
        }
    }

    private float GetDebuffTime(IPlayerCharacter pc)
    {
        return pc.StatusList.FirstOrDefault(x => x.StatusId == 2461)?.RemainingTime ?? 0f;
    }
}
