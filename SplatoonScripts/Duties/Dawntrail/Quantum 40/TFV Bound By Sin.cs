using ECommons;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Quantum40;
public unsafe class TFV_Bound_By_Sin : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1311];
    public override Metadata? Metadata => new(1, "lillylilim");

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode($"line11", """{"Name":"","type":3,"offY":-9.0,"radius":0.5,"color":3355508480,"fillIntensity":0.345,"thicc":1.0,"refActorComparisonType":2,"includeRotation":true}""");
        Controller.RegisterElementFromCode($"line12", """{"Name":"","type":3,"offY":-9.0,"radius":0.5,"color":3355508480,"fillIntensity":0.345,"thicc":1.0,"refActorComparisonType":2,"includeRotation":true}""");
    }

    int sequence = 0;

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 44122)
        {
            ++sequence;

            var sourceObject = source.GetObject();
            if(sourceObject == null) return;

            if(sequence == 11 || sequence == 12)
            {
                var line = Controller.GetElementByName($"line{sequence}")!;

                line.Enabled = true;
                line.refActorObjectID = source;

                this.Controller.Schedule(() => line.Enabled = false, 3000);
            }

            if(sequence == 12)
            {
                sequence = 0;
            }
        }
    }

    public override void OnReset()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        sequence = 0;
    }
}