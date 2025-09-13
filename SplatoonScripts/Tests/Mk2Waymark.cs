using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Tests;
public unsafe class Mk2Waymark : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [Raids.the_Weapons_Refrain_Ultimate];
    public override Metadata Metadata => new(1, "NightmareXIV");

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Point", "{\"Name\":\"\",\"radius\":0.5,\"fillIntensity\":0.5,\"thicc\":4.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnUpdate()
    {
        var e = Controller.GetElementByName("Point")!;
        e.Enabled = false;
        var m = MarkingController.Instance();
        void checkMarker(int head, int field)
        {
            if(m->Markers[head] == Player.Object.EntityId && m->FieldMarkers[field].Active) //1 and A
            {
                e.Enabled = true;
                e.SetRefPosition(m->FieldMarkers[field].Position);
            }
        }
        checkMarker(0, 0);
        checkMarker(1, 7);
        checkMarker(2, 2);
    }
}
