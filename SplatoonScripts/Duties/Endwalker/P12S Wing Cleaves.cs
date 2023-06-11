using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P12S_Wing_Cleaves : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1153, 1154 };
        public override Metadata? Metadata => new(5, "NightmareXIV");
        Queue<string> Cleaves = new();
        bool isSpin = false;
        Vector3 firstPos;
        float BaseRotation = 0;

        const uint AthenaNameId = 12377;
        readonly uint[] Casts = new uint[] { 33473, 33474, 33475, 33476, 33477, 33478, 33505, 33506, 33507, 33508, 33509, 33510, 33511, 33512, 33513, 33514, 33515, 33516 };

        BattleNpc? Athena => Svc.Objects.FirstOrDefault(x => x is BattleNpc b && b.NameId == AthenaNameId && b.IsTargetable()) as BattleNpc;
        Vector2 Center = new(100, 100);
        bool IsSavage => Svc.ClientState.TerritoryType == 1154;

        public override void OnSetup()
        {
            Controller.RegisterElementFromCode("Indicator", "{\"Name\":\"Indicator\",\"type\":5,\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"radius\":30.0,\"coneAngleMax\":180,\"refActorComparisonType\":3,\"includeRotation\":true,\"Filled\":true}");
            Controller.RegisterElementFromCode("Line", "{\"Name\":\"\",\"type\":2,\"radius\":0.0,\"thicc\":6.0}");
        }

        public override void OnEnable()
        {
            ActionEffect.ActionEffectEvent += ActionEffect_ActionEffectEvent;
        }

        private void ActionEffect_ActionEffectEvent(ActionEffectSet set)
        {
            if (set.Action == null) return;
            if (Casts.Contains(set.Action.RowId))
            {
                ////DuoLog.Information($"Cast");
                GenericHelpers.Safe(() =>
                {
                    Cleaves.Dequeue();
                    Hide();
                    if (Cleaves.Count > 0)
                    {
                        Process(Cleaves.Peek());
                        //DuoLog.Information($"-> {Cleaves.Peek()}");
                    }
                });
            }
        }

        public override void OnDisable() 
        {
            ActionEffect.ActionEffectEvent -= ActionEffect_ActionEffectEvent;
        }

        public override void OnVFXSpawn(uint target, string vfxPath)
        {
            var obj = target.GetObject();
            if(obj?.DataId == 16229 && vfxPath.Contains("vfx/lockon/eff/m0829"))
            {
                if (Cleaves.Count == 0)
                {
                    firstPos = obj.Position;
                    BaseRotation = 360 - Athena.Rotation.RadToDeg();
                    //DuoLog.Information($"Athena's rotation: {BaseRotation}");
                }
                var angle = (MathHelper.GetRelativeAngle(obj.Position, Athena.Position) + 360f - BaseRotation) % 360f;
                //DuoLog.Information($"Angle: {angle}");
                if (angle.InRange(180, 360))
                {
                    if (Cleaves.Count == 1 && firstPos.Y < obj.Position.Y && IsSavage)
                    {
                        Cleaves.Enqueue("Right");
                    }
                    else
                    {
                        Cleaves.Enqueue("Left");
                    }
                }
                else
                {
                    if (Cleaves.Count == 1 && firstPos.Y < obj.Position.Y)
                    {
                        Cleaves.Enqueue("Left");
                    }
                    else
                    {
                        Cleaves.Enqueue("Right");
                    }
                }
                if(Cleaves.Count == 1)
                {
                    //DuoLog.Information($"{Cleaves.Peek()}");
                    Hide();
                    Process(Cleaves.Peek());
                }
            }
        }

        void Hide()
        {
            Controller.GetElementByName("Indicator").Enabled = false;
            Controller.GetElementByName("Line").Enabled = false;
        }

        void Process(string dir)
        {
            if(Controller.TryGetElementByName("Indicator", out var e) && Controller.TryGetElementByName("Line", out var l))
            {
                var apos = Athena.Position;
                var arot = Athena.Rotation;
                e.SetRefPosition(apos);
                e.Enabled = true;
                if(dir == "Left")
                {
                    e.coneAngleMin = (int)(BaseRotation - 180f) + 1;
                    e.coneAngleMax = (int)BaseRotation - 1;
                }
                else
                {
                    e.coneAngleMin = (int)BaseRotation + 1;
                    e.coneAngleMax = (int)(BaseRotation + 180) - 1;
                }
                l.Enabled = true;
                var fpos = Static.GetPositionXZY(Athena);
                SetPos(l, Static.RotatePoint(fpos.X, fpos.Y, -arot, fpos with { Y = fpos.Y + 30 }), Static.RotatePoint(fpos.X, fpos.Y, -arot + 180f.DegreesToRadians(), fpos with { Y = fpos.Y + 30 }));
            }
        }

        void SetPos(Element e, Vector3 RefPosition, Vector3 OffPosition)
        {
            e.refX = RefPosition.X;
            e.refY = RefPosition.Y;
            e.refZ = RefPosition.Z;
            e.offX = OffPosition.X;
            e.offY = OffPosition.Y;
            e.offZ = OffPosition.Z;
        }

        public override void OnUpdate()
        {
            if (Cleaves.Count > 0)
            {
                var wings = Svc.Objects.Where(x => x.DataId == 16229);
                if (!wings.Any())
                {
                    Cleaves.Clear();
                    Hide();
                }
                else
                {
                    if(Controller.TryGetElementByName("Line", out var e) && Controller.TryGetElementByName("Indicator", out var i))
                    {
                        e.color = GradientColor.Get(C.Col1, C.Col2).ToUint();
                        i.color = e.color;
                    }
                }
            }
        }

        Config C => Controller.GetConfig<Config>();
        public class Config: IEzConfig
        {
            public Vector4 Col1 = Vector4FromRGBA(0xFF0000C8);
            public Vector4 Col2 = Vector4FromRGBA(0xFF7500C8);
        }

        public override void OnSettingsDraw()
        {
            ImGui.ColorEdit4("Color 1", ref C.Col1, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Color 2", ref C.Col2, ImGuiColorEditFlags.NoInputs);
        }

        public unsafe static Vector4 Vector4FromRGBA(uint col)
        {
            byte* bytes = (byte*)&col;
            return new Vector4((float)bytes[3] / 255f, (float)bytes[2] / 255f, (float)bytes[1] / 255f, (float)bytes[0] / 255f);
        }
    }
}
