using ECommons;
using ECommons.Reflection;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Generic;
public class ArtisanUnstageCheck : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => new(0);
    public override void OnSetup()
    {
        try
        {
            if (DalamudReflector.TryGetDalamudPlugin("Artisan", out var instance, false, true))
            {
                instance.SetStaticFoP("Artisan.RawInformation.DalamudInfo", "StagingChecked", true);
                instance.SetStaticFoP("Artisan.RawInformation.DalamudInfo", "IsStaging", false);
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
    }
}
