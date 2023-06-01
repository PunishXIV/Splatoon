using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ECommons.MathHelpers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P12S_Wing_Cleaves : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1154 };
        public override Metadata? Metadata => new(2, "NightmareXIV");
        Queue<string> Cleaves = new();
        bool isSpin = false;
        Vector3 firstPos;
        int BaseRotation = 0;

        const uint AthenaNameId = 12377;
        readonly uint[] Casts = new uint[] { 33473, 33474, 33475, 33476, 33477, 33478, 33505, 33506, 33507, 33508, 33509, 33510, 33511, 33512, 33513, 33514, 33515, 33516 };

        BattleNpc? Athena => Svc.Objects.FirstOrDefault(x => x is BattleNpc b && b.NameId == AthenaNameId && b.IsTargetable()) as BattleNpc;
        Vector2 Center = new(100, 100);

        public override void OnSetup()
        {
            Controller.RegisterElementFromCode("Indicator", "{\"Name\":\"Indicator\",\"type\":5,\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"radius\":30.0,\"coneAngleMax\":180,\"refActorComparisonType\":3,\"includeRotation\":true,\"Filled\":true}");
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
                    BaseRotation = 360 - (int)Athena.Rotation.RadToDeg();
                    //DuoLog.Information($"Athena's rotation: {BaseRotation}");
                }
                var angle = (MathHelper.GetRelativeAngle(obj.Position, Athena.Position) + 360 - BaseRotation) % 360;
                //DuoLog.Information($"Angle: {angle}");
                if (angle.InRange(180, 360))
                {
                    if (Cleaves.Count == 1 && firstPos.Y < obj.Position.Y)
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
        }

        void Process(string dir)
        {
            if(Controller.TryGetElementByName("Indicator", out var e))
            {
                e.Enabled = true;
                if(dir == "Left")
                {
                    e.coneAngleMin = BaseRotation - 180;
                    e.coneAngleMax = BaseRotation;
                }
                else
                {
                    e.coneAngleMin = BaseRotation;
                    e.coneAngleMax = BaseRotation + 180;
                }
            }
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
            }
        }
    }
}
