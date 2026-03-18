using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.GameFunctions;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.AnotherMerchantTale;

public class Pari_Carpet_Tracker : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1317];
    public override Metadata? Metadata => new(2, "Poneglyph");

    private uint lockedCarpetObjectId = 0;
    private bool isScanning = false;
    private DateTime scanStartTime = DateTime.MinValue;

    private const uint ANCHOR_DATA_ID = 19059;
    private const uint CARPET_DATA_ID = 19060;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Carpet 1", """{"Name":"Carpet 1","type":3,"refY":-50.0,"offY":50.0,"radius":5.0,"color":3371826944,"fillIntensity":0.345,"thicc":0.0,"refActorDataID":19059,"refActorComparisonType":3,"includeRotation":false,"onlyVisible":true}""");
        Controller.RegisterElementFromCode("Carpet 2", """{"Name":"Carpet 2","type":3,"refX":-50.0,"offX":50.0,"radius":5.0,"color":3371826944,"fillIntensity":0.345,"thicc":0.0,"refActorDataID":19059,"refActorComparisonType":3,"includeRotation":false,"onlyVisible":true}""");

        OnReset();
    }

    public override void OnStartingCast(uint sourceId, uint castId)
    {
        if(castId == 45438 || castId == 45439)
        {
            OnReset();
            isScanning = true;
            scanStartTime = DateTime.Now;
        }
    }

    public override void OnUpdate()
    {
        Controller.Hide();

        if(isScanning && lockedCarpetObjectId == 0)
        {
            if((DateTime.Now - scanStartTime).TotalSeconds > 15)
            {
                isScanning = false;
                return;
            }

            var anchor = Svc.Objects.FirstOrDefault(x => x.DataId == ANCHOR_DATA_ID);

            if(anchor != null)
            {
                var correctCarpet = Svc.Objects
                    .Where(x => x.DataId == CARPET_DATA_ID)
                    .OrderBy(x => Vector2.Distance(new Vector2(x.Position.X, x.Position.Z),
                                                 new Vector2(anchor.Position.X, anchor.Position.Z)))
                    .FirstOrDefault();

                if(correctCarpet != null && Vector2.Distance(new Vector2(correctCarpet.Position.X, correctCarpet.Position.Z),
                                                              new Vector2(anchor.Position.X, anchor.Position.Z)) < 4.0f)
                {
                    lockedCarpetObjectId = correctCarpet.ObjectId;
                    isScanning = false;
                }
            }
        }

        if(lockedCarpetObjectId != 0 && lockedCarpetObjectId.TryGetBattleNpc(out var carpetNpc))
        {
            var e1 = Controller.GetElementByName("Carpet 1");
            var e2 = Controller.GetElementByName("Carpet 2");

            if(e1 != null)
            {
                e1.Enabled = true;
                e1.refActorComparisonType = 2;
                e1.refActorObjectID = carpetNpc.ObjectId;
            }
            if(e2 != null)
            {
                e2.Enabled = true;
                e2.refActorComparisonType = 2;
                e2.refActorObjectID = carpetNpc.ObjectId;
            }
        }
    }

    public override void OnReset()
    {
        lockedCarpetObjectId = 0;
        isScanning = false;
        scanStartTime = DateTime.MinValue;
    }
}