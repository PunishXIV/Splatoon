using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SplatoonScriptsOfficial.Generic;

public class ListInstalledPlugins : SplatoonScript
{
    public override Metadata Metadata { get; } = new(2, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [9999];

    public override void OnSettingsDraw()
    {
        ImGuiEx.TextCopy(Svc.PluginInterface.InstalledPlugins.Where(x => x.IsLoaded).OrderBy(x => x.Name).Select(x => $"{x.Name} v{x.Version} [{x.InternalName}] from {x.Manifest.InstalledFromUrl?.NullWhenEmpty() ?? "DEV"}").Print("\n"));
    }
}
