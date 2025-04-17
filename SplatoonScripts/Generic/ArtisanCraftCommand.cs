using ECommons;
using ECommons.DalamudServices;
using ECommons.EzIpcManager;
using ECommons.Logging;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Generic;
public class ArtisanCraftCommand : SplatoonScript
{
    public override Metadata? Metadata { get; } = new(2, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;
#nullable disable
    [EzIPC] private Action<ushort, int> CraftItem;
    public override void OnSetup()
    {
        EzIPC.Init(this, "Artisan");
    }

    public override void OnEnable()
    {
        Svc.Commands.AddHandler("/artisancraft", new(OnCommand));
    }

    private void OnCommand(string command, string arguments)
    {
        var split = arguments.Split(" ");
        if(int.TryParse(split[^1], out var amt))
        {
            arguments = split[0..^1].Join(" ");
        }
        else
        {
            amt = 1;
        }
        var recipe = Svc.Data.GetExcelSheet<Recipe>().FirstOrNull(x => arguments.EqualsIgnoreCase(x.ItemResult.Value.Name.ExtractText())) ?? Svc.Data.GetExcelSheet<Recipe>().FirstOrNull(x => x.ItemResult.Value.Name.ExtractText().Contains(arguments, StringComparison.OrdinalIgnoreCase));
        if(recipe != null)
        {
            DuoLog.Information($"Crafting {recipe.Value.ItemResult.Value.Name} x{amt}");
            CraftItem((ushort)recipe.Value.RowId, amt);
        }
        else
        {
            DuoLog.Error("Can't find recipe");
        }
    }

    public override void OnDisable()
    {
        Svc.Commands.RemoveHandler("/artisancraft");
    }
}
