using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.Network;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Reflection;
using TerraFX.Interop.Windows;

namespace SplatoonScriptsOfficial.Tests;

public class ContextMenuSubscribers : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [9999];

    public record SubscriberInfo(string AssemblyName, string TypeName, string MethodName, object? Target);

    public static IReadOnlyList<SubscriberInfo> GetSubscribers()
    {
        var results = new List<SubscriberInfo>();

        var contextMenuInstance = DalamudReflector.GetService("Dalamud.Game.Gui.ContextMenu.ContextMenu");

        var parentDelegate = contextMenuInstance.GetFoP<Delegate>("OnMenuOpened");
        if(parentDelegate == null) return results; 

        var scopedType = Svc.PluginInterface.GetType().Assembly.GetType("Dalamud.Game.Gui.ContextMenu.ContextMenuPluginScoped", throwOnError: true);

        var scopedEventField = scopedType.GetField("OnMenuOpened", BindingFlags.Instance | BindingFlags.NonPublic);

        foreach(var parentInvocation in parentDelegate.GetInvocationList())
        {
            var scopedInstance = parentInvocation.Target;
            if(scopedInstance == null || scopedInstance.GetType() != scopedType) continue;

            var scopedDelegate = scopedEventField.GetValue(scopedInstance) as Delegate;
            if(scopedDelegate == null) continue; 

            foreach(var pluginInvocation in scopedDelegate.GetInvocationList())
            {
                var method = pluginInvocation.Method;
                var declaringType = method.DeclaringType;
                var assemblyName = declaringType?.Assembly.GetName().Name ?? "<unknown>";

                results.Add(new SubscriberInfo(AssemblyName: assemblyName, TypeName: declaringType?.FullName ?? "<unknown>", MethodName: method.Name, Target: pluginInvocation.Target));
            }
        }
        return results;
    }

    public override void OnSettingsDraw()
    {
        var subscribers = GetSubscribers();
        foreach(var s in subscribers)
        {
            ImGuiEx.Text($"{s.AssemblyName} :: {s.TypeName} :: {s.MethodName}");
        }
    }
}
