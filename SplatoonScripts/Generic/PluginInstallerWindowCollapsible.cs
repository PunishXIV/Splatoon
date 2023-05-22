using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.Reflection;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Reflection;
#nullable disable

namespace SplatoonScriptsOfficial.Generic
{
    public class PluginInstallerWindowCollapsible : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 0 };
        public override Metadata? Metadata => new(1, "NightmareXIV");

        public override void OnSetup()
        {
            var di = Svc.PluginInterface.GetType().Assembly.
                    GetType("Dalamud.Service`1", true).MakeGenericType(Svc.PluginInterface.GetType().Assembly.GetType("Dalamud.Interface.Internal.DalamudInterface", true)).
                    GetMethod("Get").Invoke(null, BindingFlags.Default, null, Array.Empty<object>(), null);
            var w = (Window)(di.GetFoP("pluginWindow"));
            w.Flags = ImGuiNET.ImGuiWindowFlags.NoScrollbar;
        }
    }
}
