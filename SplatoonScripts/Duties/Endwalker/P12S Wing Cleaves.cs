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
        Queue<string> Cleaves = new();

        const uint AthenaNameId = 12377;
        readonly uint[] Casts = new uint[] { 33473, 33474, 33475, 33476, 33477, 33478, 33505, 33506, 33507, 33508, 33509, 33510, 33511, 33512, 33513, 33514, 33515, 33516 };

        BattleNpc? Athena => Svc.Objects.FirstOrDefault(x => x is BattleNpc b && b.NameId == AthenaNameId && b.IsTargetable()) as BattleNpc;
        Vector2 Center = new(100, 100);

        public override void OnSetup()
        {
            Controller.RegisterElementFromCode("West", "{\"Name\":\"West safe\",\"type\":2,\"Enabled\":false,\"refX\":110.0,\"refY\":80.0,\"offX\":110.0,\"offY\":120.0,\"offZ\":9.536743E-07,\"radius\":10.0,\"color\":1677721855}");
            Controller.RegisterElementFromCode("East", "{\"Name\":\"East safe\",\"type\":2,\"Enabled\":false,\"refX\":90.0,\"refY\":80.0,\"offX\":90.0,\"offY\":120.0,\"offZ\":9.536743E-07,\"radius\":10.0,\"color\":1677721855}");
        }

        public override void OnEnable()
        {
            ActionEffect.ActionEffectEvent += ActionEffect_ActionEffectEvent;
        }

        private void ActionEffect_ActionEffectEvent(ActionEffectSet set)
        {
            if (Casts.Contains(set.Action.RowId))
            {
                //DuoLog.Information($"Cast");
                GenericHelpers.Safe(() =>
                {
                    Cleaves.Dequeue();
                    Hide();
                    if (Cleaves.Count > 0)
                    {
                        Controller.GetElementByName(Cleaves.Peek()).Enabled = true;
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
                var angle = (MathHelper.GetRelativeAngle(obj.Position.ToVector2(), Center) + 360) % 360;
                //DuoLog.Information($"Angle: {angle}");
                if (angle.InRange(180, 360))
                {
                    if (Cleaves.Count == 1)
                    {
                        Cleaves.Enqueue("East");
                    }
                    else
                    {
                        Cleaves.Enqueue("West");
                    }
                }
                else
                {
                    if (Cleaves.Count == 1)
                    {
                        Cleaves.Enqueue("West");
                    }
                    else
                    {
                        Cleaves.Enqueue("East");
                    }
                }
                if(Cleaves.Count == 1)
                {
                    //DuoLog.Information($"{Cleaves.Peek()}");
                    Hide();
                    Controller.GetElementByName(Cleaves.Peek()).Enabled = true;
                }
            }
        }

        void Hide()
        {
            Controller.GetElementByName("West").Enabled = false;
            Controller.GetElementByName("East").Enabled = false;
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
