using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Hooks;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol
{
    public class Beyond_Defense : SplatoonScript
    {
        public override Metadata? Metadata => new(1, "NightmareXIV");
        public override HashSet<uint> ValidTerritories => new() { 1122 };

        bool isRunning = false;

        List<uint> ProximityMap = new();
        HashSet<uint> ExclMap = new();

        public override void OnSetup()
        {
            Controller.RegisterElementFromCode("AOE", "{\"Enabled\":false,\"Name\":\"\",\"type\":1,\"radius\":5.0,\"color\":1677721855,\"refActorComparisonType\":2,\"Filled\":true}");
        }

        public override void OnVFXSpawn(uint target, string vfxPath)
        {
            //PluginLog.Verbose($"VFX {vfxPath}");
            if(vfxPath == "vfx/lockon/eff/all_at8s_0v.avfx")
            {
                //DuoLog.Information($"Excluded: {target.GetObject()}");
                isRunning = false;
                ProximityMap.RemoveAll(x => x == target);
                ExclMap.Add(target);
            }
        }

        public override void OnUpdate()
        {
            if(Svc.Objects.Any(x => x is BattleChara c && c.CastActionId == 31527))
            {
                if (!isRunning)
                {
                    var omegaM = (BattleChara)Svc.Objects.Where(x => x is BattleChara c && c.CastActionId == 31527).First();
                    ProximityMap = Svc.Objects.Where(x => x is PlayerCharacter pc && !pc.IsDead).OrderBy(z => Vector3.Distance(omegaM.Position, z.Position)).Select(x => x.ObjectId).ToList();
                    isRunning = true;
                    ProximityMap.RemoveAll(ExclMap.Contains);
                    Controller.GetElementByName("AOE").refActorObjectID = ProximityMap.FirstOrDefault();
                    Controller.GetElementByName("AOE").Enabled = true;
                    //DuoLog.Information($"Excl map: {ExclMap.Select(x => x.GetObject()).Print()}, proximity map: {ProximityMap.Select(x => x.GetObject()).Print()}");
                }
            }
            else
            {
                if (isRunning)
                {
                    Reset();
                }
            }
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if(category.EqualsAny(DirectorUpdateCategory.Wipe, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Commence))
            {
                Reset();
            }
        }

        void Reset()
        {
            ProximityMap.Clear();
            ExclMap.Clear();
            isRunning = false;
            Controller.GetElementByName("AOE").Enabled = false;
        }
    }
}
