using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.Events;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol
{
    public class Pantokrator : SplatoonScript
    {
        public override Metadata? Metadata => new(2, "NightmareXIV");
        public override HashSet<uint> ValidTerritories => new() { 1122 };
        BattleChara? Omega => Svc.Objects.FirstOrDefault(x => x is BattleChara o && o.NameId == 7695 && o.IsTargetable()) as BattleChara;

        //  Condensed Wave Cannon Kyrios (3508), Remains = 9.6, Param = 0, Count = 0
        //  Guided Missile Kyrios Incoming (3497), Remains = 21.6, Param = 0, Count = 0
        const uint FirstInLine = 3004;

        GameObject[] Lasers => Svc.Objects.Where(x => x is PlayerCharacter pc && pc.StatusList.Any(z => z.StatusId.EqualsAny<uint>(3507, 3508, 3509, 3510) && z.RemainingTime <= 6f)).ToArray();
        GameObject[] Rockets => Svc.Objects.Where(x => x is PlayerCharacter pc && pc.StatusList.Any(z => z.StatusId.EqualsAny<uint>(3424, 3495, 3496, 3497) && (z.RemainingTime <= 6f || pc.StatusList.Any(c => c.StatusId == FirstInLine)))).ToArray();

        public override void OnSetup()
        {
            Controller.RegisterElement("Laser1", new(2) { Enabled = false, radius = 4f, refX = 100f, refY = 100f });
            Controller.RegisterElement("Laser2", new(2) { Enabled = false, radius = 4f, refX = 100f, refY = 100f });
            Controller.RegisterElement("Rocket1", new(0) { Enabled = false, radius = 5f, Filled = true });
            Controller.RegisterElement("Rocket2", new(0) { Enabled = false, radius = 5f, Filled = true });
            Controller.TryRegisterLayoutFromCode("CW", "~Lv2~{\"Enabled\":false,\"Name\":\"Clockwise\",\"Group\":\"\",\"ZoneLockH\":[1122],\"ElementsL\":[{\"Name\":\"1\",\"type\":2,\"refX\":98.0,\"refY\":98.0,\"offX\":98.0,\"offY\":102.0,\"radius\":0.0,\"color\":3355508735,\"thicc\":5.0},{\"Name\":\"1\",\"type\":2,\"refX\":98.5,\"refY\":100.0,\"offX\":98.0,\"offY\":98.0,\"radius\":0.0,\"color\":3355508735,\"thicc\":5.0},{\"Name\":\"1\",\"type\":2,\"refX\":97.5,\"refY\":100.0,\"offX\":98.0,\"offY\":98.0,\"radius\":0.0,\"color\":3355508735,\"thicc\":5.0},{\"Name\":\"2\",\"type\":2,\"refX\":102.0,\"refY\":102.0,\"offX\":98.0,\"offY\":102.0,\"radius\":0.0,\"color\":3355508735,\"thicc\":5.0},{\"Name\":\"3\",\"type\":2,\"refX\":102.0,\"refY\":102.0,\"offX\":102.0,\"offY\":98.0,\"radius\":0.0,\"color\":3355508735,\"thicc\":5.0},{\"Name\":\"4\",\"type\":2,\"refX\":102.0,\"refY\":98.0,\"offX\":98.0,\"offY\":98.0,\"radius\":0.0,\"color\":3355508735,\"thicc\":5.0},{\"Name\":\"2\",\"type\":2,\"refX\":98.0,\"refY\":102.0,\"offX\":100.0,\"offY\":102.5,\"radius\":0.0,\"color\":3355508735,\"thicc\":5.0},{\"Name\":\"2\",\"type\":2,\"refX\":98.0,\"refY\":102.0,\"offX\":100.0,\"offY\":101.5,\"radius\":0.0,\"color\":3355508735,\"thicc\":5.0},{\"Name\":\"3\",\"type\":2,\"refX\":102.5,\"refY\":100.0,\"offX\":102.0,\"offY\":102.0,\"radius\":0.0,\"color\":3355508735,\"thicc\":5.0},{\"Name\":\"3\",\"type\":2,\"refX\":101.5,\"refY\":100.0,\"offX\":102.0,\"offY\":102.0,\"radius\":0.0,\"color\":3355508735,\"thicc\":5.0},{\"Name\":\"4\",\"type\":2,\"refX\":100.0,\"refY\":98.5,\"offX\":102.0,\"offY\":98.0,\"radius\":0.0,\"color\":3355508735,\"thicc\":5.0},{\"Name\":\"4\",\"type\":2,\"refX\":100.0,\"refY\":97.5,\"offX\":102.0,\"offY\":98.0,\"radius\":0.0,\"color\":3355508735,\"thicc\":5.0},{\"Name\":\"\",\"refX\":100.0,\"refY\":100.0,\"radius\":0.0,\"color\":3355508735,\"overlayTextColor\":4278255615,\"overlayFScale\":1.5,\"thicc\":0.0,\"overlayText\":\"Clockwise\"}]}", out _);
            Controller.TryRegisterLayoutFromCode("CCW", "~Lv2~{\"Enabled\":false,\"Name\":\"CounterClockwise\",\"Group\":\"\",\"ZoneLockH\":[1122],\"ElementsL\":[{\"Name\":\"1\",\"type\":2,\"refX\":98.0,\"refY\":98.0,\"offX\":98.0,\"offY\":102.0,\"radius\":0.0,\"color\":3372155135,\"thicc\":5.0},{\"Name\":\"1\",\"type\":2,\"refX\":98.5,\"refY\":100.0,\"offX\":98.0,\"offY\":102.0,\"radius\":0.0,\"color\":3372155135,\"thicc\":5.0},{\"Name\":\"1\",\"type\":2,\"refX\":97.5,\"refY\":100.0,\"offX\":98.0,\"offY\":102.0,\"radius\":0.0,\"color\":3372155135,\"thicc\":5.0},{\"Name\":\"2\",\"type\":2,\"refX\":102.0,\"refY\":102.0,\"offX\":98.0,\"offY\":102.0,\"radius\":0.0,\"color\":3372155135,\"thicc\":5.0},{\"Name\":\"3\",\"type\":2,\"refX\":102.0,\"refY\":102.0,\"offX\":102.0,\"offY\":98.0,\"radius\":0.0,\"color\":3372155135,\"thicc\":5.0},{\"Name\":\"4\",\"type\":2,\"refX\":102.0,\"refY\":98.0,\"offX\":98.0,\"offY\":98.0,\"radius\":0.0,\"color\":3372155135,\"thicc\":5.0},{\"Name\":\"2\",\"type\":2,\"refX\":102.0,\"refY\":102.0,\"offX\":100.0,\"offY\":102.5,\"radius\":0.0,\"color\":3372155135,\"thicc\":5.0},{\"Name\":\"2\",\"type\":2,\"refX\":102.0,\"refY\":102.0,\"offX\":100.0,\"offY\":101.5,\"radius\":0.0,\"color\":3372155135,\"thicc\":5.0},{\"Name\":\"3\",\"type\":2,\"refX\":102.5,\"refY\":100.0,\"offX\":102.0,\"offY\":98.0,\"radius\":0.0,\"color\":3372155135,\"thicc\":5.0},{\"Name\":\"3\",\"type\":2,\"refX\":101.5,\"refY\":100.0,\"offX\":102.0,\"offY\":98.0,\"radius\":0.0,\"color\":3372155135,\"thicc\":5.0},{\"Name\":\"4\",\"type\":2,\"refX\":100.0,\"refY\":98.5,\"offX\":98.0,\"offY\":98.0,\"radius\":0.0,\"color\":3372155135,\"thicc\":5.0},{\"Name\":\"4\",\"type\":2,\"refX\":100.0,\"refY\":97.5,\"offX\":98.0,\"offY\":98.0,\"radius\":0.0,\"color\":3372155135,\"thicc\":5.0},{\"Name\":\"\",\"refX\":100.0,\"refY\":100.0,\"radius\":0.0,\"overlayTextColor\":4294902015,\"overlayFScale\":1.5,\"thicc\":0.0,\"overlayText\":\"Counter-Clockwise\"}]}", out _);
        }

        public override void OnUpdate()
        {
            if (!Omega || !ProperOnLogin.PlayerPresent) return;
            
            if(Lasers.Length == 2)
            {
                void EnableLaser(int which)
                {
                    var e = Controller.GetElementByName($"Laser{which+1}");
                    var angle = GetRelativeAngleRad(new(100f, 100f), Lasers[which].Position.ToVector2());
                    var point = RotatePoint(100f, 100f, angle, new(100f, 130f, 0f));
                    e.Enabled = true;
                    if (Lasers[which].Address == Svc.ClientState.LocalPlayer.Address)
                    {
                        e.color = Controller.GetConfig<Config>().LaserColSelf.ToUint();
                    }
                    else
                    {
                        e.color = Controller.GetConfig<Config>().LaserCol.ToUint();
                    }
                    e.offX = point.X;
                    e.offY = point.Y;
                }
                EnableLaser(0);
                EnableLaser(1);
            }
            else
            {
                Controller.GetElementByName("Laser1").Enabled = false;
                Controller.GetElementByName("Laser2").Enabled = false;
            }

            if (Rockets.Length == 2)
            {
                void EnableRocket(int which)
                {
                    var e = Controller.GetElementByName($"Rocket{which + 1}");
                    e.Enabled = true;
                    if (Rockets[which].Address == Svc.ClientState.LocalPlayer.Address)
                    {
                        if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId.EqualsAny<uint>(3424, 3495, 3496, 3497) && x.RemainingTime < 3f))
                        {
                            e.color = GradientColor.Get(Controller.GetConfig<Config>().RocketColSelf, Controller.GetConfig<Config>().RocketColSelf2, 250).ToUint();
                        }
                        else
                        {
                            e.color = Controller.GetConfig<Config>().RocketColSelf.ToUint();
                        }
                    }
                    else
                    {
                        e.color = Controller.GetConfig<Config>().RocketCol.ToUint();
                    }
                    e.SetRefPosition(Rockets[which].Position);
                }
                EnableRocket(0);
                EnableRocket(1);
            }
            else
            {
                Controller.GetElementByName("Rocket1").Enabled = false;
                Controller.GetElementByName("Rocket2").Enabled = false;
            }

            var secondcasters = Svc.Objects.Where(x => x is BattleChara c && c.CastActionId == 32368).Cast<BattleChara>();
            if(Controller.GetConfig<Config>().DisplayDirection && secondcasters.Count() >= 2)
            {
                var firstcasters = Svc.Objects.Where(x => x is BattleChara c && c.CastActionId == 31501).Cast<BattleChara>();
                if (firstcasters.Count() >= 2)
                {
                    foreach(var x in firstcasters)
                    {
                        //Dequeued message: [Splatoon] first: 119.568756, 299.56604 second: 269.5674, 89.56467 
                        var angle = x.Rotation.RadToDeg();
                        foreach(var z in secondcasters)
                        {
                            var angle2 = z.Rotation.RadToDeg();
                            if(Math.Abs(angle - angle2) < 40)
                            {
                                if(angle > angle2)
                                {
                                    Controller.GetRegisteredLayouts()["CW"].Enabled = true;
                                    Controller.GetRegisteredLayouts()["CCW"].Enabled = false;
                                }
                                else
                                {
                                    Controller.GetRegisteredLayouts()["CW"].Enabled = false;
                                    Controller.GetRegisteredLayouts()["CCW"].Enabled = true;
                                }
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                Controller.GetRegisteredLayouts()["CW"].Enabled = false;
                Controller.GetRegisteredLayouts()["CCW"].Enabled = false;
            }
        }

        public static Vector3 RotatePoint(float cx, float cy, float angle, Vector3 p)
        {
            if (angle == 0f) return p;
            var s = (float)Math.Sin(angle);
            var c = (float)Math.Cos(angle);

            // translate point back to origin:
            p.X -= cx;
            p.Y -= cy;

            // rotate point
            float xnew = p.X * c - p.Y * s;
            float ynew = p.X * s + p.Y * c;

            // translate point back:
            p.X = xnew + cx;
            p.Y = ynew + cy;
            return p;
        }

        float GetRelativeAngleRad(Vector2 origin, Vector2 target)
        {
            var vector2 = target - origin;
            var vector1 = new Vector2(0, 1);
            return ((MathF.Atan2(vector2.Y, vector2.X) - MathF.Atan2(vector1.Y, vector1.X)));
        }

        public override void OnSettingsDraw()
        {
            ImGui.Checkbox("Enable direction indicator", ref Controller.GetConfig<Config>().DisplayDirection);
            ImGui.ColorEdit4("Self laser color", ref Controller.GetConfig<Config>().LaserColSelf);
            ImGui.ColorEdit4("Others laser color", ref Controller.GetConfig<Config>().LaserCol);
            ImGui.ColorEdit4("Self rocket color", ref Controller.GetConfig<Config>().RocketColSelf);
            ImGui.ColorEdit4("Self rocket color blink - last puddle", ref Controller.GetConfig<Config>().RocketColSelf2);
            ImGui.ColorEdit4("Others rocket color", ref Controller.GetConfig<Config>().RocketCol);
            if(ImGui.Button("Apply settings"))
            {
                Controller.Clear();
                this.OnSetup();
            }
            if (ImGui.CollapsingHeader("Debug"))
            {
                foreach(var x in Svc.Objects)
                {
                    if(x is BattleChara b && !b.IsTargetable() && b.IsCasting)
                    {
                        ImGuiEx.TextCopy($"{b} {b.ObjectId} casting {b.CastActionId} -> {b.CastTargetObjectId} {b.CurrentCastTime}/{b.TotalCastTime} heading {MathHelper.GetRelativeAngle(new(100, 100), b.Position.ToVector2()).RadToDeg()}");
                    }
                }
                ImGuiEx.Text($"Lasers: ");
                Lasers.Each(x => ImGuiEx.Text($"{x}"));
                ImGuiEx.Text($"Rockets: ");
                Rockets.Each(x => ImGuiEx.Text($"{x}"));
            }
        }

        public class Config : IEzConfig
        {
            public Vector4 LaserColSelf = 0x500000FFu.ToVector4();
            public Vector4 LaserCol = 0x50FFFF00u.ToVector4();
            public Vector4 RocketColSelf = 0x500000FFu.ToVector4();
            public Vector4 RocketColSelf2 = 0x5000FFFFu.ToVector4();
            public Vector4 RocketCol = 0x50FFFF00u.ToVector4();
            public bool DisplayDirection = true;
        }
    }
}
