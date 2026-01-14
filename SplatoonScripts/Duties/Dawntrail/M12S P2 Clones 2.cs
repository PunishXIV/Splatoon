using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Newtonsoft.Json;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Splatoon.Splatoon;
using Element = Splatoon.Element;
#pragma warning disable
namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public unsafe class M12S_P2_Clones_2 : SplatoonScript
{
    public override Metadata Metadata { get; } = new(5, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1327];
    int IsHovering = -1;

    public enum Direction { N, NE, E, SE, S, SW, W, NW }
    public enum TetherKind
    {
        Nothing = 0,
        Boss = 374,
        Stack = 369,
        Fan = 367,
        Defamation = 368,
    }
    public enum ObjectDataId : uint
    {
        PlayerClone = 19210,
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Beacon", """
            {"Name":"","type":2,"refX":80.0,"refY":100.0,"offX":80.0,"offY":100.0,"offZ":30.0,"radius":0.0,"color":3355506687,"fillIntensity":0.345,"thicc":40.0}
            """);
        Controller.RegisterElementFromCode("TetherWanted", """
            {"Name":"","type":2,"refX":92.782196,"refY":113.39666,"refZ":-3.8146973E-06,"offX":83.49066,"offY":106.54342,"radius":0.0,"fillIntensity":0.345,"thicc":10}
            """);
        Controller.RegisterElementFromCode("TetherWantedPartners", """
            {"Name":"","type":2,"refX":92.782196,"refY":113.39666,"refZ":-3.8146973E-06,"offX":83.49066,"offY":106.54342,"radius":0.0,"fillIntensity":0.345,"thicc":2}
            """);
        Controller.RegisterElementFromCode("TetherValid", """
            {"Name":"","type":2,"refX":92.782196,"refY":113.39666,"refZ":-3.8146973E-06,"offX":83.49066,"offY":106.54342,"radius":0.0,"color":3355639552,"fillIntensity":0.345,"thicc":8.0}
            """);
        Controller.RegisterElementFromCode("TetherValidPartners", """
            {"Name":"","type":2,"refX":92.782196,"refY":113.39666,"refZ":-3.8146973E-06,"offX":83.49066,"offY":106.54342,"radius":0.0,"color":3355639552,"fillIntensity":0.345,"thicc":2}
            """);
        Controller.RegisterElementFromCode("GoTo", """
            {"Name":"","refX":102.4791,"refY":97.90576,"radius":0.75,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"tether":true}
            """);
        for(int i = 0; i < 100; i++)
        {
            Controller.RegisterElementFromCode($"Debug{i}", """{"Name":"","radius":0.5,"color":3372220415,"fillIntensity":0.5,"thicc":4.0,"overlayText":"Dbg"}""");
        }
    }

    Direction? PlayerDirection = null;
    int Phase = 0;

    public override void OnReset()
    {
        PlayerDirection = null;
        Phase = 0;
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        for(int i = 0; i < DebugCnt; i++)
        {
            if(Controller.TryGetElementByName($"Debug{i}", out var e)) e.Enabled = true;
        }
        DebugCnt = 0;
        IsHovering = -1;
        var go = Controller.GetElementByName("GoTo");
        if(PlayerDirection != null)
        {
            go.color = GetRainbowColor(1).ToUint();
        }
        if(Phase == 0)
        {
            foreach(var x in Svc.Objects.OfType<IBattleNpc>())
            {
                if(x.DataId == (uint)ObjectDataId.PlayerClone && x.Struct()->Vfx.Tethers.ToArray().Any(t => t.TargetId == BasePlayer.ObjectId))
                {
                    PlayerDirection = GetDirection(x.Position);
                }
            }

            if(PlayerDirection != null)
            {

                var relAngle = 45 * (int)BaseDirection;
                var point = MathHelper.RotateWorldPoint(new(100, 0, 100), relAngle.DegreesToRadians(), new(100, 0, 80));
                var e = Controller.GetElementByName("Beacon");
                e.SetRefPosition(point);
                e.SetOffPosition(point with { Y = 30});
                e.Enabled = true;
                if(Svc.Objects.OfType<IBattleNpc>().Count(x => x.Struct()->Vfx.Tethers.ToArray().Any(t => t.Id != 0 && Enum.GetValues<TetherKind>().Contains((TetherKind)t.Id))) == 7)
                {
                    IBattleNpc wantTetherFrom = null;
                    if(GetDesiredTether() == TetherKind.Boss)
                    {
                        wantTetherFrom = Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.NameId == 14379 && x.IsTargetable);
                    }
                    else if(GetDesiredTether() == TetherKind.Nothing)
                    {
                        //
                    }
                    else if(C.LP1.Contains(PlayerDirection.Value))
                    {
                        wantTetherFrom = TetherCandidates[0];
                    }
                    else if(C.LP2.Contains(PlayerDirection.Value))
                    {
                        wantTetherFrom = TetherCandidates.Reverse().First();
                    }
                    if(wantTetherFrom != null)
                    {
                        var haveTetherFromWanted = wantTetherFrom.Struct()->Vfx.Tethers.ToArray().Any(x => x.Id == (uint)GetDesiredTether() && x.TargetId == BasePlayer.ObjectId);
                        if(haveTetherFromWanted && Controller.TryGetElementByName("TetherValid", out var v))
                        {
                            v.Enabled = true;
                            v.SetRefPosition(BasePlayer.Position);
                            v.SetOffPosition(wantTetherFrom.Position);

                            VfxContainer.Tether otherTetherStruct = default;
                            var otherTether = Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.Struct()->Vfx.Tethers.ToArray().TryGetFirst(x => x.Id == (uint)GetDesiredTether() && x.TargetId != BasePlayer.ObjectId, out otherTetherStruct));
                            if(otherTether != null && otherTetherStruct.TargetId.ObjectId.TryGetPlayer(out var otherPlayer) && Controller.TryGetElementByName("TetherValidPartners", out var others))
                            {
                                others.Enabled = true;
                                others.SetRefPosition(otherPlayer.Position);
                                others.SetOffPosition(otherTether.Position);
                            }
                        }
                        if(!haveTetherFromWanted && Controller.TryGetElementByName("TetherWanted", out var w))
                        {
                            w.Enabled = true;
                            w.SetRefPosition(wantTetherFrom.Position);
                            IPlayerCharacter pc = null;
                            w.SetOffPosition(wantTetherFrom.Struct()->Vfx.Tethers.ToArray().FirstOrDefault(x => x.Id == (uint)GetDesiredTether()).TargetId.ObjectId.TryGetPlayer(out pc) ? pc.Position : default);
                            w.color = GetRainbowColor(3).ToUint();

                            VfxContainer.Tether otherTetherStruct = default;
                            var otherTether = Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.Struct()->Vfx.Tethers.ToArray().TryGetFirst(x => x.Id == (uint)GetDesiredTether() && x.TargetId != pc?.ObjectId, out otherTetherStruct));
                            if(otherTether != null && otherTetherStruct.TargetId.ObjectId.TryGetPlayer(out var otherPlayer) && Controller.TryGetElementByName("TetherWantedPartners", out var others))
                            {
                                others.Enabled = true;
                                others.SetRefPosition(otherPlayer.Position);
                                others.SetOffPosition(otherTether.Position);
                                others.color = w.color;
                            }
                        }
                    }
                }
            }
        }

        if(Phase == 1 && PlayerDirection != null)
        {
            var lpNumber = C.LP1.Contains(PlayerDirection.Value) ? 0 : 1;
            go.Enabled = true;
            go.SetRefPosition(C.Phase1Positions[lpNumber][GetDesiredTether()].ToVector3());
        }

        if(Phase == 2 && PlayerDirection != null)
        {
            var lpNumber = C.LP1.Contains(PlayerDirection.Value) ? 0 : 1;
            go.Enabled = true;
            go.SetRefPosition(C.Phase2Positions[lpNumber][GetDesiredTether()].ToVector3());
        }

        if(Phase == 3 && PlayerDirection != null)
        {
            var lpNumber = C.LP1.Contains(PlayerDirection.Value) ? 0 : 1;
            go.Enabled = true;
            go.SetRefPosition(C.Phase3Positions[lpNumber][GetDesiredTether()].ToVector3());
        }

        if(Phase == 4 && PlayerDirection != null)
        {
            var lpNumber = C.LP1.Contains(PlayerDirection.Value) ? 0 : 1;
            go.Enabled = true;
            go.SetRefPosition(C.Phase4Positions[lpNumber][GetDesiredTether()].ToVector3());
        }

        if(Phase == 5 && PlayerDirection != null)
        {
            var lpNumber = C.LP1.Contains(PlayerDirection.Value) ? 0 : 1;
            go.Enabled = true;
            go.SetRefPosition(C.Phase5Positions[lpNumber][GetDesiredTether()].ToVector3());
        }

        if(Phase > 5)
        {
            this.Controller.Reset();
            Phase = -1;
        }
    }

    int DebugCnt = 0;
    Element GetDebugElement(Vector2 position, string str, Vector4 color, bool current = false)
    {
        var ret = Controller.GetElementByName($"Debug{DebugCnt}");
        ret.SetRefPosition(position.ToVector3(0));
        ret.overlayText = str;
        ret.overlayVOffset = 2;
        ret.color = color.ToUint();
        if(current)
        {
            ret.color = GradientColor.Get(EColor.RedBright, color, 500).ToUint();
            IsHovering = DebugCnt;
        }
        ret.overlayTextColor = ret.color;
        DebugCnt++;
        return ret;
    }

    IBattleNpc[] TetherCandidates => Svc.Objects.OfType<IBattleNpc>().Where(x => x.Struct()->Vfx.Tethers.ToArray().Any(t => t.Id == (uint)GetDesiredTether())).OrderBy(x =>
    {
        var relAngle = 45 * (int)BaseDirection;
        var a = (MathHelper.GetRelativeAngle(new(100, 0, 100), x.Position) + relAngle + 180 - 5) % 360;
        PluginLog.Information($"Tether at {a} {x.Position}");
        return a;
    }).ToArray();

    TetherKind GetDesiredTether()
    {
        if(PlayerDirection != null)
        {
            if(C.LP1.Contains(PlayerDirection.Value)) return C.LP1Tethers[C.LP1.IndexOf(x => x == PlayerDirection.Value)];
            if(C.LP2.Contains(PlayerDirection.Value)) return C.LP2Tethers[C.LP2.IndexOf(x => x == PlayerDirection.Value)];
        }
        return default;
    }

    Direction GetDirection(Vector3 pos)
    {
        var a = MathHelper.GetRelativeAngle(new(100, 0, 100), pos);
        if(a.ApproximatelyEquals(360, 5f)) return Direction.N;
        for(int i = 0; i < Enum.GetValues<Direction>().Length; i++)
        {
            if(a.ApproximatelyEquals(i * 45, 5f))
            {
                return (Direction)i;
            }
        }
        return 0;
    }

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if(Phase == 0 && packet->ActionID == 46307)
        {
            Phase = 1;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(PlayerDirection != null)
        {
            if(Phase == 1 && set.Action?.RowId == 46311)
            {
                Phase = 2;
            }
            if(Phase == 2 && set.Action?.RowId == 46312)
            {
                Phase = 3;
            }
            if(Phase == 3 && set.Action?.RowId == 46384)
            {
                Phase = 4;
            }
            if(Phase >= 4 && set.Action?.RowId == 48733)
            {
                Phase++;
            }
        }
    }

    uint? W2S = null;
    public override void OnSettingsDraw()
    {
        if(!AgreedToConfigure)
        {
            ImGuiEx.HelpMarker("", color: EColor.RedBright, symbolOverride: FontAwesomeIcon.ExclamationTriangle.ToIconString(), sameLine: false);
            ImGui.SameLine();
            ImGuiEx.TextWrapped(EColor.RedBright, $"""
            Warning! Configuring this script requires you to fully understand mechanic and either have a recording of the battle or being in the zone.
            """);
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Play, "Proceed"))
            {
                AgreedToConfigure = true;
            }
        }
        if(AgreedToConfigure)
        {
            ImGuiEx.TextWrapped(EColor.OrangeBright, "There was an issue that light parties 1 and 2 were flipped. It is now fixed, you should not need any reconfiguration, fix is purely visual. Report any inconsistencies, thank you. ");
            ImGuiEx.TextV("Relative North on position");
            ImGui.SameLine();
            if(ImGui.RadioButton("1", C.BaseNum == 0)) C.BaseNum = 0;
            ImGui.SameLine();
            if(ImGui.RadioButton("2", C.BaseNum == 1)) C.BaseNum = 1;
            ImGui.SameLine();
            if(ImGui.RadioButton("3", C.BaseNum == 2)) C.BaseNum = 2;
            ImGui.SameLine();
            if(ImGui.RadioButton("4", C.BaseNum == 3)) C.BaseNum = 3;
            ImGui.SameLine();
            ImGuiEx.TextV("of");
            ImGui.SameLine();
            if(ImGui.RadioButton("Light Party 1", !C.BaseLP1)) C.BaseLP1 = false;
            ImGui.SameLine();
            if(ImGui.RadioButton("Light Party 2", C.BaseLP1)) C.BaseLP1 = true;
            ImGui.SameLine();
            ImGuiEx.Text($"Currently: {BaseDirection}");
            ImGuiEx.TreeNodeCollapsingHeader("Light Party 1 positions", () =>
            {
                for(int i = 0; i < C.LP2.Length; i++)
                {
                    ImGui.SetNextItemWidth(150f);
                    ImGuiEx.EnumCombo($"Position {i + 1} counter-clockwise from rel.north, excl. north", ref C.LP2[i]);
                }
            });
            ImGuiEx.TreeNodeCollapsingHeader("Light Party 2 positions", () =>
            {
                for(int i = 0; i < C.LP1.Length; i++)
                {
                    ImGui.SetNextItemWidth(150f);
                    ImGuiEx.EnumCombo($"Position {i + 1} clockwise from rel. north, incl. north", ref C.LP1[i]);
                }
            });
            ImGuiEx.TreeNodeCollapsingHeader("Light Party 1 Tethers", () =>
            {
                for(int i = 0; i < C.LP2Tethers.Length; i++)
                {
                    ImGui.SetNextItemWidth(150f);
                    ImGuiEx.EnumCombo($"Tether for {C.LP2[i]} ({i + 1} CCW from rel. north excl. north)", ref C.LP2Tethers[i]);
                }
            });

            ImGuiEx.TreeNodeCollapsingHeader("Light Party 2 Tethers", () =>
            {
                for(int i = 0; i < C.LP1Tethers.Length; i++)
                {
                    ImGui.SetNextItemWidth(150f);
                    ImGuiEx.EnumCombo($"Tether for {C.LP1[i]} ({i + 1} CW from rel. north incl. north)", ref C.LP1Tethers[i]);
                }
            });

            ImGuiEx.TreeNodeCollapsingHeader("Phase 1 Positions", () =>
            {
                ImGuiEx.TextWrapped($"Phase 1 is considered when players have their tethers permanently bound, and spread on their positions for the first time.");
                LpPositionsEdit(1, C.Phase1Positions);
            });

            ImGuiEx.TreeNodeCollapsingHeader("Phase 2 Positions", () =>
            {
                ImGuiEx.TextWrapped($"Phase 2 is considered when players took their first proteans and now need to stack.");
                LpPositionsEdit(2, C.Phase2Positions);
            });

            ImGuiEx.TreeNodeCollapsingHeader("Phase 3 Positions", () =>
            {
                ImGuiEx.TextWrapped($"Phase 3 is considered when time rewind begins.");
                LpPositionsEdit(3, C.Phase3Positions);
            });

            ImGuiEx.TreeNodeCollapsingHeader("Phase 4 Positions", () =>
            {
                ImGuiEx.TextWrapped($"Phase 4 is considered when Netherwrath has been resolved and people go to bait proteans and take first stack.");
                LpPositionsEdit(4, C.Phase4Positions);
            });

            ImGuiEx.TreeNodeCollapsingHeader("Phase 5 Positions", () =>
            {
                ImGuiEx.TextWrapped($"Phase 5 is considered when first stack has been taken and players need to take second stack.");
                LpPositionsEdit(5, C.Phase5Positions);
            });
        }
        ImGui.Separator();
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.EnumCombo("Direction", ref PlayerDirection);
            if(PlayerDirection != null)
            {
                if(C.LP1.Contains(PlayerDirection.Value))
                {
                    ImGuiEx.Text("LP1");
                }
                if(C.LP2.Contains(PlayerDirection.Value))
                {
                    ImGuiEx.Text("LP2");
                }
                ImGuiEx.Text($"Desired tether: {GetDesiredTether()}");
                ImGuiEx.Text($"Candidates: {TetherCandidates.Print()}");
                ImGui.InputInt("Phase", ref Phase);
            }
        }
    }

    Direction BaseDirection => (C.BaseLP1 ? C.LP1 : C.LP2).SafeSelect(C.BaseNum);

    void LpPositionsEdit(int num, List<Dictionary<TetherKind, Vector2>> positions)
    {
        ImGui.PushID(num.ToString());
        ImGuiEx.Text("Light Party 1 Positions:");
        ImGui.Indent();
        this.PhaseEdit(num, "LP1", positions[1]);
        ImGui.Unindent();
        ImGuiEx.Text("Light Party 2 Positions:");
        ImGui.Indent();
        this.PhaseEdit(num, "LP2", positions[0]);
        ImGui.Unindent();
        ImGui.PopID();
    }

    void PhaseEdit(int num, string id, Dictionary<TetherKind, Vector2> dict)
    {
        ImGui.PushID(id);
        foreach(var e in Enum.GetValues<TetherKind>())
        {
            ImGui.PushID(e.ToString());
            if(dict.TryGetValue(e, out var value))
            {
                var has = true;
                if(ImGui.Checkbox("##enable", ref has))
                {
                    new TickScheduler(() => dict.Remove(e));
                }
                ImGui.SameLine(0, 2);
                var x = dict[e];
                ImGui.SetNextItemWidth(200f);
                var active = false;
                ImGui.DragFloat2("##position", ref x, vSpeed: 0.01f, vMin: 80f, vMax: 120f);
                if(ImGui.IsItemHovered() || ImGui.IsItemActive()) active = true;
                ImGui.SameLine(0, 2);
                if(ImGuiEx.IconButton(FontAwesomeIcon.Copy)) GenericHelpers.Copy(JsonConvert.SerializeObject(x));
                if(ImGui.IsItemHovered() || ImGui.IsItemActive()) active = true;
                ImGui.SameLine(0, 2);
                if(ImGuiEx.IconButton(FontAwesomeIcon.Paste))
                {
                    try
                    {
                        x = JsonConvert.DeserializeObject<Vector2>(GenericHelpers.Paste());
                    }
                    catch(Exception ex)
                    {
                        ex.Log();
                    }
                }
                if(ImGui.IsItemHovered() || ImGui.IsItemActive()) active = true;
                ImGui.SameLine(0, 2);
                ImGuiEx.IconButton(FontAwesomeIcon.MousePointer);
                if(ImGui.IsItemHovered() || ImGui.IsItemActive()) active = true;
                if(this.IsEnabled) GetDebugElement(x, $"Phase{num} {id} {e}", id == "LP2" ? EColor.GreenBright : EColor.YellowBright, active);
                if(ImGui.BeginDragDropSource())
                {
                    if(Svc.GameGui.ScreenToWorld(ImGui.GetIO().MousePos, out var pos))
                    {
                        if(GenericHelpers.IsKeyPressed(ECommons.Interop.LimitedKeys.LeftShiftKey))
                        {
                            pos = new((float)(Math.Round(pos.X * 2d) / 2d), (float)(Math.Round(pos.Y * 2d) / 2d), (float)(Math.Round(pos.Z * 2d) / 2d));
                        }
                        x = pos.ToVector2();
                    }
                    ImGui.EndDragDropSource();
                }
                ImGuiEx.Tooltip("Drag this element to assign coords via dragdrop.");
                dict[e] = x;
            }
            else
            {
                var has = false;
                if(ImGui.Checkbox("##enable", ref has))
                {
                    new TickScheduler(() => dict[e] = default);
                }
            }
            ImGui.SameLine();
            ImGuiEx.Text($"{e}");
            ImGui.PopID();
        }
        ImGui.PopID();
    }

    bool AgreedToConfigure = false;
    public Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public bool BaseLP1 = true;
        public int BaseNum = 0;
        public Direction[] LP1 = [Direction.W, Direction.NW, Direction.N, Direction.NE];
        public Direction[] LP2 = [Direction.SW, Direction.S, Direction.SE, Direction.E];
        public TetherKind[] LP1Tethers = [TetherKind.Boss, TetherKind.Stack, TetherKind.Fan, TetherKind.Defamation];
        public TetherKind[] LP2Tethers = [TetherKind.Stack, TetherKind.Fan, TetherKind.Defamation, TetherKind.Nothing];
        public List<Dictionary<TetherKind, Vector2>> Phase1Positions = new()
    {
        new()
        {
            [TetherKind.Stack] = new(81, 96),
            [TetherKind.Fan] = new(82.5f, 92),
            [TetherKind.Defamation] = new(100,80.5f),
            [TetherKind.Boss] = new(89, 100)
        },
        new()
        {
            [TetherKind.Stack] = new(81, 102.5f),
            [TetherKind.Fan] = new(82, 107),
            [TetherKind.Defamation] = new(100, 119.5f),
            [TetherKind.Nothing] = new(119.5f, 100)
        }
    };

        public List<Dictionary<TetherKind, Vector2>> Phase2Positions = new()
    {

        new()
        {
            [TetherKind.Stack] = new(94, 94.5f),
            [TetherKind.Fan] = new(91.5f, 97.5f),
            [TetherKind.Defamation] = new(94, 94.5f),
            [TetherKind.Boss] = new(94, 104.5f)
        },
        new()
        {
            [TetherKind.Stack] = new(94, 104.5f),
            [TetherKind.Fan] = new(91.5f, 102.5f),
            [TetherKind.Defamation] = new(94, 104.5f),
            [TetherKind.Nothing] = new(94, 94.5f)
        },
    };

        public List<Dictionary<TetherKind, Vector2>> Phase3Positions = new() //rewind 1
    {

        new()
        {
            [TetherKind.Stack] = new(89, 96.5f),
            [TetherKind.Fan] = new(89, 91),
            [TetherKind.Defamation] = new(82.5f, 100.5f),
            [TetherKind.Boss] = new(82.5f, 100.5f)
        },
        new()
        {
            [TetherKind.Stack] = new(89, 103.5f),
            [TetherKind.Fan] = new(89f, 109),
            [TetherKind.Defamation] = new(82.5f, 100.5f),
            [TetherKind.Nothing] = new(82.5f, 100.5f)
        },
    };

        public List<Dictionary<TetherKind, Vector2>> Phase4Positions = new() // 1st stack
    {
        new()
        {
            [TetherKind.Stack] = new(81, 96),
            [TetherKind.Fan] = new(82.5f, 92),
            [TetherKind.Defamation] = new(90,110),
            [TetherKind.Boss] = new(90,110)
        },
        new()
        {
            [TetherKind.Stack] = new(81, 102.5f),
            [TetherKind.Fan] = new(82, 107),
            [TetherKind.Defamation] = new(90,110),
            [TetherKind.Nothing] = new(90,110)
        },
    };

        public List<Dictionary<TetherKind, Vector2>> Phase5Positions = new() // 2nd stack
    {
        new()
        {
            [TetherKind.Stack] = new(81, 96),
            [TetherKind.Fan] = new(82.5f, 92),
            [TetherKind.Defamation] = new(90,90),
            [TetherKind.Boss] = new(90,90)
        },
        new()
        {
            [TetherKind.Stack] = new(81, 102.5f),
            [TetherKind.Fan] = new(82, 107),
            [TetherKind.Defamation] = new(90,90),
            [TetherKind.Nothing] = new(90,90)
        },
    };
    }

    public static Vector4 GetRainbowColor(double cycleSeconds)
    {
        if(cycleSeconds <= 0d)
        {
            cycleSeconds = 1d;
        }

        var ms = Environment.TickCount64;
        var t = (ms / 1000d) / cycleSeconds;
        var hue = t % 1f;
        return HsvToVector4(hue, 1f, 1f);
    }

    public static Vector4 HsvToVector4(double h, double s, double v)
    {
        double r = 0f, g = 0f, b = 0f;
        var i = (int)(h * 6f);
        var f = h * 6f - i;
        var p = v * (1f - s);
        var q = v * (1f - f * s);
        var t = v * (1f - (1f - f) * s);

        switch(i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }

        return new Vector4((float)r, (float)g, (float)b, 1f);
    }
}
