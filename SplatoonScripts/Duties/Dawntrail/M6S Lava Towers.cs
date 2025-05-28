using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using ImGuiNET;
using Newtonsoft.Json;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe class M6S_Lava_Towers : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1259];
    public override Metadata? Metadata => new(6, "NightmareXIV");

    bool ReadyToSoak = false;
    bool IsSecondTowers = false;
    bool WingsActive => Svc.Objects.OfType<IPlayerCharacter>().Any(x => x.StatusList.Any(s => s.StatusId == 4450));

    Dictionary<int, Vector2> Towers = new()
    {
        //top left
        [69] = new(83, 91),
        [70] = new(93, 89),
        [71] = new(92,96),
        [72] = new(83,102),
        [73] = new(94,84),
        [74] = new(83,88),
        [75] = new(90,89),
        [76] = new(83,95),
        [77] = new(90,97.5f),
        [78] = new(83,104),
        //top right
        [79] = new(110,93),
        [80] = new(117,92),
        [81] = new(109,97),
        [82] = new(115,105),
        [83] = new(110,83),
        [84] = new(117,85),
        [85] = new(110,91),
        [86] = new(117,96),
        [87] = new(111,100),
        [88] = new(117,106),
        //bottom
        [89] = new(100,108),
        [90] = new(85,114),
        [91] = new(98,117),
        [92] = new(112,116),
        [93] = new(92,110),
        [94] = new(91,117),
        [95] = new(107,111),
        [96] = new(105,117),
    };

    HashSet<int> ActiveTowers = [];

    TowerPosition GetTowerPosition(Vector2 position)
    {
        if(Vector2.Distance(position, new(98, 123)) < 20f) return TowerPosition.Bottom;
        if(Vector2.Distance(position, new(81,88)) < 20f) return TowerPosition.Left;
        if(Vector2.Distance(position, new(121,91)) < 20f) return TowerPosition.Right;
        return TowerPosition.Undefined;
    }

    public enum TowerPosition { Undefined, Left, Right, Bottom }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("BaitLeft", "{\"Name\":\"\",\"refX\":86.55319,\"refY\":86.70691,\"radius\":4.0,\"color\":3355508503,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":4278190080,\"overlayTextColor\":4279172864,\"overlayFScale\":2.0,\"thicc\":8.0,\"overlayText\":\"Bait here\",\"tether\":true}");
        Controller.RegisterElementFromCode("BaitRight", "{\"Name\":\"\",\"refX\":114.13297,\"refY\":88.13472,\"refZ\":9.536743E-07,\"radius\":4.0,\"color\":3355508503,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":4278190080,\"overlayTextColor\":4279172864,\"overlayFScale\":2.0,\"thicc\":8.0,\"overlayText\":\"Bait here\",\"tether\":true}");
        Controller.RegisterElementFromCode("StayLeft", "{\"Name\":\"\",\"refX\":93.60492,\"refY\":96.64029,\"refZ\":1.9073486E-06,\"radius\":2.0,\"color\":3355508503,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":4278190080,\"overlayTextColor\":4279172864,\"overlayFScale\":2.0,\"thicc\":8.0,\"overlayText\":\"Stay Close\",\"tether\":true}");
        Controller.RegisterElementFromCode("StayRight", "{\"Name\":\"\",\"refX\":106.86854,\"refY\":98.85092,\"refZ\":-1.9073486E-06,\"radius\":2.0,\"color\":3355508503,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":4278190080,\"overlayTextColor\":4279172864,\"overlayFScale\":2.0,\"thicc\":8.0,\"overlayText\":\"Stay Close\",\"tether\":true}");
        Controller.RegisterElementFromCode("Prepare", "{\"Name\":\"\",\"refX\":84.394325,\"refY\":98.02954,\"refZ\":1.9073486E-06,\"radius\":3.0,\"fillIntensity\":0.8,\"overlayBGColor\":3355443200,\"overlayTextColor\":4278190335,\"overlayFScale\":2.0,\"thicc\":4.0,\"overlayText\":\"!!! PREPARE !!!\",\"tether\":true}");
        Controller.RegisterElementFromCode("Take", "{\"Name\":\"\",\"refX\":84.394325,\"refY\":98.02954,\"refZ\":1.9073486E-06,\"radius\":2.5,\"Donut\":0.5,\"color\":3356032768,\"fillIntensity\":0.8,\"overlayBGColor\":3355443200,\"overlayTextColor\":4279172864,\"overlayFScale\":2.0,\"thicc\":4.0,\"overlayText\":\"Take tower\",\"tether\":true}");
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

        if(Svc.Objects.OfType<IBattleNpc>().TryGetFirst(x => x.IsTargetable && x.DataId == 18335, out var result)
            && result.Struct()->GetCastInfo() != null && ((result.CastActionId == 42649 && result.CurrentCastTime <= 6.6f) || result.IsCasting(42679)))
        {
            this.ReadyToSoak = false;
            if(C.StartingPosition == TowerPosition.Left || C.StartingPosition == TowerPosition.Right)
            {
                Controller.GetElementByName($"{(C.BaitStack ? "Bait" : "Stay")}{C.StartingPosition}")!.Enabled = true;
            }
        }

        if(Controller.Scene == 5)
        {
            if(WingsActive)
            {
                IsSecondTowers = true;
            }
        }
        else
        {
            OnReset();
            return;
        }

        if(this.ActiveTowers.Count > 0)
        {
            if(WingsActive)
            {
                this.ReadyToSoak = false;
            }
            var myTowers = this.ActiveTowers.Select(x => Towers[x]).Where(x => GetTowerPosition(x) == C.StartingPosition).OrderBy(x => Vector2.Distance(x, new(100, 100))).ToList();
            if(!IsSecondTowers)
            {
                if(C.StartingPosition == TowerPosition.Left || C.StartingPosition == TowerPosition.Right)
                {
                    if(Controller.TryGetElementByName(ReadyToSoak ? "Take" : "Prepare", out var e))
                    {
                        e.SetRefPosition((C.TwoTowerCloserToMiddle ? myTowers[0] : myTowers[1]).ToVector3(0));
                        e.Enabled = true;
                    }
                }
                else
                {
                    var myTower = GetTowerPoint(myTowers, C.MeleeTower1);
                    if(Controller.TryGetElementByName(ReadyToSoak ? "Take" : "Prepare", out var e))
                    {
                        e.SetRefPosition(myTower.ToVector3(0));
                        e.Enabled = true;
                    }
                }
            }
            else
            {
                if(C.StartingPosition == TowerPosition.Bottom)
                {
                    var is8towers = this.ActiveTowers.Select(x => Towers[x]).Where(x => GetTowerPosition(x) == TowerPosition.Bottom).Count() == 8;
                    if(is8towers)
                    {
                        var adjTowers = this.ActiveTowers.Select(x => Towers[x]).ToArray();
                        if(Controller.TryGetElementByName(ReadyToSoak ? "Take" : "Prepare", out var e) && Towers.TryGetValue(C.Position8, out var pos))
                        {
                            e.Enabled = true;
                            e.SetRefPosition(pos.ToVector3(0));
                        }
                    }
                    else
                    {
                        var fourTowers = ActiveTowers.Select(x => Towers[x]).GroupBy(GetTowerPosition)
                            .Where(g => g.Key != TowerPosition.Undefined && g.Count() >= 4)
                            .Select(g => g.Take(4).ToList())
                            .First();
                        var tp = GetTowerPosition(fourTowers.First());
                        var inverted = false;
                        if(tp == TowerPosition.Left && C.InvertNW) inverted = true;
                        if(tp == TowerPosition.Right && C.InvertNE) inverted = true;
                        var myTower = GetTowerPoint(fourTowers, C.MeleeTower2, inverted);
                        if(Controller.TryGetElementByName(ReadyToSoak ? "Take" : "Prepare", out var e))
                        {
                            e.SetRefPosition(myTower.ToVector3(0));
                            e.Enabled = true;
                        }
                    }
                }
                else
                {
                    if(myTowers.Count == 4)
                    {
                        var adjTowers = this.ActiveTowers.Select(x => Towers[x]).Where(x => GetTowerPosition(x) == C.EscapeFrom4Towers).OrderBy(x => Vector2.Distance(x, new(100, 100))).ToArray().ToArray();
                        if(Controller.TryGetElementByName(ReadyToSoak ? "Take" : "Prepare", out var e))
                        {
                            e.Enabled = true;
                            e.SetRefPosition((C.TwoTowerCloserToMiddle ? adjTowers[0] : adjTowers[1]).ToVector3(0));
                        }
                    }
                    else
                    {
                        var is8towers = this.ActiveTowers.Select(x => Towers[x]).Where(x => GetTowerPosition(x) == TowerPosition.Bottom).Count() == 8;
                        if(is8towers)
                        {
                            var adjTowers = this.ActiveTowers.Select(x => Towers[x]).ToArray();
                            if(Controller.TryGetElementByName(ReadyToSoak ? "Take" : "Prepare", out var e) && Towers.TryGetValue(C.Position8, out var pos))
                            {
                                e.Enabled = true;
                                e.SetRefPosition(pos.ToVector3(0));
                            }
                        }
                        else
                        {
                            var destination = this.ActiveTowers.Select(x => Towers[x]).Where(x => GetTowerPosition(x) != C.StartingPosition).GroupBy(GetTowerPosition).First(x => x.Count() == 2);
                            var adjTowers = this.ActiveTowers.Select(x => Towers[x]).Where(x => GetTowerPosition(x) == GetTowerPosition(destination.First())).OrderBy(x => Vector2.Distance(x, new(100, 100))).ToArray();
                            if(Controller.TryGetElementByName(ReadyToSoak ? "Take" : "Prepare", out var e))
                            {
                                e.Enabled = true;
                                e.SetRefPosition((C.TwoTowerCloserToMiddle ? adjTowers[0] : adjTowers[1]).ToVector3(0));
                            }
                        }
                    }
                }
            }
        }
    }

    public override void OnReset()
    {
        this.IsSecondTowers = false;
        this.ReadyToSoak = false;
        this.ActiveTowers.Clear();
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action?.RowId == 42652)
        {
            this.ReadyToSoak = true;
        }
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(Towers.ContainsKey((int)position))
        {
            if(data1 == 1 && data2 == 2)
            {
                ActiveTowers.Add((int)position);
            }
            else if(data1 == 4 && data2 == 8)
            {
                this.ReadyToSoak = false;
                ActiveTowers.Clear();
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(100f);
        ImGuiEx.EnumCombo("Starting Position", ref C.StartingPosition);
        if(C.StartingPosition == TowerPosition.Left || C.StartingPosition == TowerPosition.Right)
        {
            ImGui.Checkbox("Bait stack", ref C.BaitStack);
            ImGuiEx.Text($"Tower to take:");
            ImGuiEx.RadioButtonBool("Closer to middle", "Further from middle", ref C.TwoTowerCloserToMiddle);
            ImGui.SetNextItemWidth(100f);
            ImGuiEx.EnumCombo("Flying direction if 4 towers spawned on your side", ref C.EscapeFrom4Towers);
        }
        else if(C.StartingPosition == TowerPosition.Bottom)
        {
            ImGui.SetNextItemWidth(100f);
            ImGuiEx.EnumCombo("First 4 towers position", ref C.MeleeTower1);
            ImGui.SetNextItemWidth(100f);
            ImGuiEx.EnumCombo("Second 4 towers position", ref C.MeleeTower2);
            ImGuiEx.HelpMarker("Relative if you're looking at the middle of the arena.");
            ImGui.Indent();
            ImGui.Checkbox("Invert position if second 4 towers spawn NorthWest ←", ref C.InvertNW);
            ImGuiEx.HelpMarker("Inverts position so that left becomes right, second from left becomes second from right, etc. if second towers spawns NorthWest.");
            ImGui.Checkbox("Invert position second 4 towers spawn NorthEast →", ref C.InvertNE);
            ImGuiEx.HelpMarker("Inverts position so that left becomes right, second from left becomes second from right, etc. if second towers spawns NorthEast.");
            ImGui.Unindent();
        }
        ImGui.SetNextItemWidth(100f);
        ImGuiEx.Combo("8 towers position", ref C.Position8, Towers.Keys.Where(x => x >= 89));
        if(ThreadLoadImageHandler.TryGetTextureWrap("https://github.com/PunishXIV/Splatoon/blob/main/Presets/Files/Dawntrail/image_230.png?raw=true", out var w))
        {
            ImGui.Image(w.ImGuiHandle, w.Size);
        }
        if(ImGui.CollapsingHeader("Debug"))
        {
            if(ImGui.Button("Store active towers"))
            {
                GenericHelpers.Copy(JsonConvert.SerializeObject(this.ActiveTowers));
            }
            if(ImGui.Button("Restore active towers"))
            {
                try
                {
                    ActiveTowers = JsonConvert.DeserializeObject<HashSet<int>>(GenericHelpers.Paste()!) ?? [];
                }
                catch(Exception e)
                {
                    e.LogDuo();
                }
            }
            ImGui.Checkbox("ReadyToSoak", ref ReadyToSoak);
            ImGui.Checkbox("IsSecondTowers", ref IsSecondTowers);
        }
    }
    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public TowerPosition StartingPosition = TowerPosition.Undefined;
        public bool BaitStack = false;
        public bool TwoTowerCloserToMiddle = true;
        public TowerPosition EscapeFrom4Towers = TowerPosition.Undefined;
        public int Position8 = 90;
        public MeleeTower1 MeleeTower1 = MeleeTower1.Upper_Left;
        public MeleeTower2 MeleeTower2 = MeleeTower2.Leftmost;
        public bool InvertNW = false;
        public bool InvertNE = false;
    }

    public static Vector2 GetTowerPoint(List<Vector2> points, MeleeTower1 corner)
    {
        if(points == null || points.Count != 4)
            throw new ArgumentException("You must provide exactly 4 points.");

        // Invert Y logic: highest Y becomes 'lowest visually'
        List<Vector2> sorted = [.. points];
        sorted.Sort((a, b) => a.Y.CompareTo(b.Y)); // Ascending Y: 0 = top visually, 3 = bottom visually

        Vector2 topLeft, topRight, bottomLeft, bottomRight;

        if(sorted[0].X < sorted[1].X)
        {
            topLeft = sorted[0];
            topRight = sorted[1];
        }
        else
        {
            topLeft = sorted[1];
            topRight = sorted[0];
        }

        if(sorted[2].X < sorted[3].X)
        {
            bottomLeft = sorted[2];
            bottomRight = sorted[3];
        }
        else
        {
            bottomLeft = sorted[3];
            bottomRight = sorted[2];
        }

        return corner switch
        {
            MeleeTower1.Upper_Left => topLeft,
            MeleeTower1.Upper_Right => topRight,
            MeleeTower1.Lower_Left => bottomLeft,
            MeleeTower1.Lower_Right => bottomRight,
            _ => throw new ArgumentOutOfRangeException(nameof(corner), "Unknown corner value."),
        };
    }

    public static Vector2 GetTowerPoint(List<Vector2> points, MeleeTower2 position, bool inverted)
    {
        if(points == null || points.Count != 4)
            throw new ArgumentException("You must provide exactly 4 points.");

        Vector2 center = new Vector2(100, 100);

        var withAngles = points.Select(p =>
        {
            var dx = p.X - center.X;
            var dy = -(p.Y - center.Y);
            var angle = Math.Atan2(dx, -dy); 
            if(angle < 0) angle += 2 * Math.PI;
            return (Point: p, Angle: angle);
        }).ToList();

        withAngles.Sort((a, b) => a.Angle.CompareTo(b.Angle));

        if(!inverted)
        {
            return position switch
            {
                MeleeTower2.Rightmost => withAngles[3].Point,
                MeleeTower2.Second_from_right => withAngles[2].Point,
                MeleeTower2.Second_from_left => withAngles[1].Point,
                MeleeTower2.Leftmost => withAngles[0].Point,
                _ => throw new ArgumentOutOfRangeException(nameof(position), "Unknown position enum value."),
            };
        }
        else
        {
            return position switch
            {
                MeleeTower2.Rightmost => withAngles[0].Point,
                MeleeTower2.Second_from_right => withAngles[1].Point,
                MeleeTower2.Second_from_left => withAngles[2].Point,
                MeleeTower2.Leftmost => withAngles[3].Point,
                _ => throw new ArgumentOutOfRangeException(nameof(position), "Unknown position enum value."),
            };
        }
    }

    public enum Wall
    {
        Left,
        Right,
        Top,
        Bottom
    }

    public enum MeleeTower1
    {
        Upper_Left,
        Upper_Right,
        Lower_Left,
        Lower_Right,
    }

    public enum MeleeTower2
    {
        Leftmost,
        Second_from_left,
        Second_from_right,
        Rightmost,
    }


}
