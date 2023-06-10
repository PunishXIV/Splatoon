using ECommons;
using ECommons.MathHelpers;
using ECommons.DalamudServices;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using Splatoon;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Hooks;
using ECommons.Schedulers;
using Dalamud.Interface.Colors;
using ECommons.GameFunctions;
using ECommons.Configuration;
using ECommons.ImGuiMethods;
using ImGuiNET;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P12S_Classical_Concepts : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1154 };
        public override Metadata? Metadata => new(5, "tatad2");

        private string ElementNamePrefix = "P12SSC";

        private int cubeCount = 0;
        private int[,] cube = new int[4, 3];

        bool isBlinking = false;

        List<TickScheduler> Schedulers = new();

        private void Reset()
        {
            Hide();
            Schedulers.Each(x => x.Dispose());
            PluginLog.Debug("classical concepts RESET");
            cubeCount = 0;
        }

        void Hide()
        {
            Controller.ClearRegisteredElements();
            isBlinking = false;
        }

        public override void OnEnable()
        {
            Reset();
        }

        public override void OnUpdate()
        {
            if(Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 3588 && x.RemainingTime < 1f))
            {
                Hide();
            }
            if (isBlinking)
            {
                foreach(var e in Controller.GetRegisteredElements())
                {
                    if (e.Key.StartsWith("Red"))
                    {
                        e.Value.color = (GradientColor.Get(ImGuiColors.DalamudRed, ImGuiColors.DalamudRed / 2, 333) with { W = Conf.Trans }).ToUint();
                    }
                    if (e.Key.StartsWith("Yellow"))
                    {
                        e.Value.color = (GradientColor.Get(ImGuiColors.DalamudYellow, ImGuiColors.DalamudYellow / 2, 333) with { W = Conf.Trans }).ToUint();
                    }
                }
            }
        }

        public override void OnMessage(string Message)
        {
            if (Message.EqualsAny(">33574)"))
            {
                Reset();
            }
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if (category == DirectorUpdateCategory.Commence || category == DirectorUpdateCategory.Recommence || category == DirectorUpdateCategory.Wipe)
                Reset();
        }


        List<(int, int)> bias = new List<(int, int)> { (1, 0), (-1, 0), (0, 1), (0, -1) }; 
        private void DrawLines(bool swap)
        {
            PluginLog.Debug($"swap: {swap}"); 
            if (swap)
            {
                for(int x = 0; x < 4; x ++)
                {
                    (cube[x, 0], cube[3-x, 2]) = (cube[3-x, 2], cube[x, 0]);
                    if (x < 2)
                        (cube[x, 1], cube[3 - x, 1]) = (cube[3 - x, 1], cube[x, 1]); 
                }
            }

            for (int y = 0; y < 3; y++)
                PluginLog.Debug($"{cube[0, y]}, {cube[1, y]}, {cube[2, y]}, {cube[3, y]}"); 
            for (int x = 0; x < 4; x++) 
                for (int y = 0; y < 3; y++)
                {
                    if (cube[x, y] != 2) continue;
                    // blue 
                    List<(int, int)> Red = new List<(int, int)>();
                    List<(int, int)> Yellow = new List<(int, int)>(); 
                    
                    foreach((int, int) b in  bias)
                    {
                        int xx = x + b.Item1;
                        int yy = y + b.Item2;
                        if (xx < 0 || xx > 3 || yy < 0 || yy > 2) continue;
                        if (cube[xx, yy] == 1) Red.Add((xx, yy));
                        if (cube[xx, yy] == 3) Yellow.Add((xx, yy)); 
                    }

                    PluginLog.Debug($"(x, y): {x}, {y} redcount:{Red.Count}, yellowcount:{Yellow.Count}"); 

                    if (Red.Count == 1)
                        DrawLine(x, y, Red[0].Item1, Red[0].Item2, true);
                    else
                    {
                        int blueCount = 0;
                        foreach ((int, int) b in bias)
                        {
                            int xx = Red[0].Item1 + b.Item1;
                            int yy = Red[0].Item2 + b.Item2;
                            if (xx < 0 || xx > 3 || yy < 0 || yy > 2) continue;
                            if (cube[xx, yy] == 2) blueCount++;
                        }
                        int index = blueCount == 2 ? 1 : 0;
                        DrawLine(x, y, Red[index].Item1, Red[index].Item2, true);
                    }
                    if (Yellow.Count == 1)
                        DrawLine(x, y, Yellow[0].Item1, Yellow[0].Item2, false); 
                    else
                    {
                        int blueCount = 0;
                        foreach ((int, int) b in bias)
                        {
                            int xx = Yellow[0].Item1 + b.Item1;
                            int yy = Yellow[0].Item2 + b.Item2;
                            if (xx < 0 || xx > 3 || yy < 0 || yy > 2) continue;
                            if (cube[xx, yy] == 2) blueCount++;
                        }
                        int index = blueCount == 2 ? 1 : 0;
                        DrawLine(x, y, Yellow[index].Item1, Yellow[index].Item2, false);
                    }
                }
        }

        private void DrawLine(int x1, int y1, int x2, int y2, bool isRed)
        {
            //{"Name":"","type":2,"refX":88.0,"refY":92.0,"offX":96.0,"offY":92.0,"offZ":-3.8146973E-06,"radius":0.5,"refActorRequireCast":true,"refActorComparisonType":3,"includeRotation":true,"Filled":true}

            PluginLog.Debug($"draw: ({x1}, {y1}) => ({x2}, {y2})"); 
            x1 = x1 * 8 + 88;
            y1 = y1 * 8 + 84;
            x2 = x2 * 8 + 88;
            y2 = y2 * 8 + 84;

            Element e = new(2)
            {
                refX = x1,
                refY = y1,
                offX = x2,
                offY = y2,
                radius = Conf.LineWidth,
                color = ((isRed?ImGuiColors.DalamudRed:ImGuiColors.DalamudYellow) with { W = Conf.Trans }).ToUint(),
                thicc = Conf.LineThickness
            };

            string elementName = x1.ToString() + y1.ToString() + x2.ToString() + y2.ToString();
            Controller.RegisterElement((isRed?"Red":"Yellow") + elementName, e, true);

            Schedulers.Add(new TickScheduler(() =>
            {
                e.Enabled = false;
            }, 20000)); 
        }

        public override void OnObjectCreation(nint newObjectPtr)
        {
            Schedulers.Add(new TickScheduler(() =>
            {
                GameObject? obj = Svc.Objects.FirstOrDefault(x => x.Address == newObjectPtr);
                if (!(obj?.DataId == 0x3F37 || obj?.DataId == 0x3F38 || obj?.DataId == 0x3F39))
                    return;

                string color = obj.DataId == 0x3F37 ? "red" : obj.DataId == 0x3F38 ? "blue" : "yellow";
                Vector2 position = obj.Position.ToVector2();
                PluginLog.Debug($"cube color:{color}, position:{position.ToString()}");

                int xIndex = ((int)position.X - 88) / 8;
                int yIndex = ((int)position.Y - 84) / 8;
                cube[xIndex, yIndex] = (int)obj.DataId - 0x3F36;
                cubeCount++;
                if (cubeCount == 12)
                    DrawLines(false);
                if (cubeCount == 24)
                {
                    if (Conf.SwapSecond)
                    {
                        if (Conf.SwapSecondDelay == 0)
                        {
                            DrawLines(true);
                        }
                        else
                        {
                            DrawLines(false);
                            isBlinking = true;
                            Schedulers.Add(new TickScheduler(() => 
                            {
                                Hide();
                                DrawLines(true);
                            }, Conf.SwapSecondDelay * 1000));
                        }
                    }
                    else
                    {
                        DrawLines(false);
                    }
                }
            }, 100 )); 
        }

        public class Config : IEzConfig
        {
            public bool SwapSecond = true;
            public int SwapSecondDelay = 0;
            public float LineWidth = 0.5f;
            public float LineThickness = 2f;
            public float Trans = 0.3f;
        }

        Config Conf => Controller.GetConfig<Config>();

        public override void OnSettingsDraw()
        {
            ImGui.Checkbox($"Swap lines for second classical concepts", ref Conf.SwapSecond);
            ImGui.SetNextItemWidth(100);
            ImGui.InputInt("Swap delay, seconds", ref Conf.SwapSecondDelay.ValidateRange(0, 15));
            ImGui.Separator();
            ImGui.SetNextItemWidth(100);
            ImGui.DragFloat("Line width", ref Conf.LineWidth.ValidateRange(0f, 5f), 0.01f);
            ImGui.SetNextItemWidth(100);
            ImGui.DragFloat("Line thickness", ref Conf.LineThickness.ValidateRange(1, 20f), 0.01f);
            ImGui.SetNextItemWidth(100);
            ImGui.DragFloat("Transparency", ref Conf.Trans.ValidateRange(0.1f, 1f), 0.01f);
            if (ImGui.CollapsingHeader("Debug"))
            {
                ImGui.InputInt("num cubes", ref cubeCount);
            }
        }
    }
}
