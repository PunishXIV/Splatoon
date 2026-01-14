using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.MathHelpers;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public unsafe class M12S_P2_Clones_1 : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1327];

    public override void OnSetup()
    {
        for(int i = 0; i < 4; i++)
        {
            Controller.RegisterElementFromCode($"Dark{i}", """{"Name":"CloneDark","type":3,"refZ":5.0,"radius":0.0,"color":3372155094,"Filled":false,"fillIntensity":0.345,"thicc":20.0,"refActorNPCNameID":14380,"refActorComparisonType":2,"includeRotation":true}""");
            Controller.RegisterElementFromCode($"Fire{i}", """{"Name":"CloneFire","type":3,"refZ":5.0,"radius":0.0,"color":3355492351,"Filled":false,"fillIntensity":0.345,"thicc":20.0,"refActorNPCNameID":14380,"refActorComparisonType":2,"includeRotation":true}""");
        }
    }

    int RentedDark = 0;
    int RentedFire = 0;

    uint DarknessCast = 46303;
    uint FireCast = 46301;

    uint MasterFireClone = 0;
    uint MasterDarknessClone = 0;

    List<uint> FireClones = [];
    List<uint> DarknessClones = [];

    public override void OnReset()
    {
        MasterFireClone = 0;
        MasterDarknessClone = 0;
        FireClones.Clear();
        DarknessClones.Clear();
    }

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if(packet->ActionID == 46368)
        {
            this.Controller.Reset();
        }
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        if(DarknessClones.Count != 2)
        {
            var e = Controller.GetElementByName($"Dark0");
            e.Enabled = true;
            e.refActorObjectID = MasterDarknessClone;

        }
        if(FireClones.Count != 2)
        {
            var e = Controller.GetElementByName($"Fire0");
            e.Enabled = true;
            e.refActorObjectID = MasterFireClone;
        }
        foreach(var x in Svc.Objects.OfType<IBattleNpc>())
        {
            if(DarknessClones.Count != 2 && x.IsCasting(DarknessCast))
            {
                MasterDarknessClone = x.ObjectId;
                var e = Controller.GetElementByName($"Dark0");
                e.Enabled = true;
                e.refActorObjectID = MasterDarknessClone;

            }
            if(FireClones.Count != 2 && x.IsCasting(FireCast))
            {
                MasterFireClone = x.ObjectId;
                var e = Controller.GetElementByName($"Fire0");
                e.Enabled = true;
                e.refActorObjectID = MasterFireClone;
            }
            if(DarknessClones.Count < 2 && MasterDarknessClone != 0 && MasterDarknessClone.TryGetBattleNpc(out var darkness))
            {
                DarknessClones = Svc.Objects.OfType<IBattleNpc>().Where(x => x.DataId == 19204 && Vector3.Distance(darkness.Position, x.Position).ApproximatelyEquals(5f, 0.1f)).Select(x => x.ObjectId).ToList();
            }
            if(FireClones.Count < 2 && MasterFireClone != 0 && MasterFireClone.TryGetBattleNpc(out var fire))
            {
                FireClones = Svc.Objects.OfType<IBattleNpc>().Where(x => x.DataId == 19204 && Vector3.Distance(fire.Position, x.Position).ApproximatelyEquals(5f, 0.1f)).Select(x => x.ObjectId).ToList();
            }
            if(DarknessClones.Count == 2)
            {
                for(int i = 0; i < DarknessClones.Count; i++)
                {
                    var e = Controller.GetElementByName($"Dark{i}");
                    e.Enabled = true;
                    e.refActorObjectID = DarknessClones[i];
                }
            }
            if(FireClones.Count == 2)
            {
                for(int i = 0; i < FireClones.Count; i++)
                {
                    var e = Controller.GetElementByName($"Fire{i}");
                    e.Enabled = true;
                    e.refActorObjectID = FireClones[i];
                }
            }
        }
    }
}
