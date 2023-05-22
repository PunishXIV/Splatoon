using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.Configuration;
using ECommons.GameFunctions;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
#nullable enable
namespace Splatoon.SplatoonScripting;

public unsafe class Controller
{
    internal SplatoonScript Script;
    internal Dictionary<string, Layout> Layouts = new();
    internal Dictionary<string, Element> Elements = new();
    internal IEzConfig? Configuration;

    internal int autoIncrement = 0;
    internal int AutoIncrement => ++autoIncrement;

    internal Controller(SplatoonScript s)
    {
        Script = s;
    }

    public Splatoon Plugin => Splatoon.P;

    /// <summary>
    /// Indicates whether player is in combat.
    /// </summary>
    public bool InCombat => Svc.Condition[ConditionFlag.InCombat];

    /// <summary>
    /// Indicates phase of a battle.
    /// </summary>
    public int Phase => P.Phase;

    /// <summary>
    /// Amount of seconds that have passed since combat start. Returns -1 if not in combat.
    /// </summary>
    public float CombatSeconds => InCombat ? (float)CombatMiliseconds / 1000f : -1;

    /// <summary>
    /// Amount of miliseconds that have passed since combat start. Returns -1 if not in combat.
    /// </summary>
    public float CombatMiliseconds => InCombat? Environment.TickCount64 - P.CombatStarted : -1;

    public int Scene => *global::Splatoon.Memory.Scene.ActiveScene;

    /// <summary>
    /// Loads if unloaded and returns script configuration file.
    /// </summary>
    /// <typeparam name="T">Configuration class, implementing IEzConfig</typeparam>
    /// <returns>Loaded configuration</returns>
    public T GetConfig<T>() where T : IEzConfig, new()
    {
        Configuration ??= EzConfig.LoadConfiguration<T>($"{Script.InternalData.Path}.json", false);
        return (T)Configuration;
    }

    /// <summary>
    /// Saves script's configuration, if present.
    /// </summary>
    public void SaveConfig()
    {
        if (Configuration != null)
        {
            EzConfig.SaveConfiguration(Configuration, $"{Script.InternalData.Path}.json", true, false);
        }
    }

    /// <summary>
    /// Attempts to register previously exported from plugin layout for further usage. End user will be able to edit this layout as they wish and results of the edit will be saved. Enabled layouts are subject for immediate processing when the script is enabled.
    /// </summary>
    /// <param name="UniqueName">Internal unique (within current script) name of the layout.</param>
    /// <param name="ExportString">An exported layout string.</param>
    /// <param name="layout">Decoded layout object.</param>
    /// <param name="overwrite">Whether to overwrite existing layout with same name if it's present.</param>
    /// <returns>Whether layout was successfully registered.</returns>
    public bool TryRegisterLayoutFromCode(string UniqueName, string ExportString, [NotNullWhen(true)] out Layout? layout, bool overwrite = false)
    {
        return ScriptingEngine.TryDecodeLayout(ExportString, out layout) && TryRegisterLayout(UniqueName, layout, overwrite);
    }

    public bool TryRegisterLayoutFromCode(string ExportString, [NotNullWhen(true)] out Layout? layout, bool overwrite = false)
    {
        return TryRegisterLayoutFromCode($"unnamed-{AutoIncrement}", ExportString, out layout, overwrite);
    }

    /// <summary>
    /// Attempts to register previously constructed layout for further usage. End user will be able to edit this layout as they wish and results of the edit will be saved. Enabled layouts are subject for immediate processing when the script is enabled.
    /// </summary>
    /// <param name="UniqueName">Internal unique (within current script) name of the layout.</param>
    /// <param name="layout">Layout object.</param>
    /// <param name="overwrite">Whether to overwrite existing layout with same name if it's present.</param>
    /// <returns>Whether layout was successfully registered.</returns>
    public bool TryRegisterLayout(string UniqueName, Layout layout, bool overwrite = false)
    {
        if (!overwrite && Layouts.ContainsKey(UniqueName))
        {
            PluginLog.Warning($"There is a layout named {UniqueName} already.");
            return false;
        }
        Layouts[UniqueName] = layout;
        return true;
    }


    public bool TryRegisterLayout(Layout layout, bool overwrite = false)
    {
        return TryRegisterLayout($"unnamed-{AutoIncrement}", layout, overwrite);
    }

    /// <summary>
    /// Attempts to register previously constructed element for further usage. End user will be able to edit this element as they wish and results of the edit will be saved. Enabled elements are subject for immediate processing when the script is enabled.
    /// </summary>
    /// <param name="UniqueName">Internal unique (within current script) name of the element.</param>
    /// <param name="element">Element object.</param>
    /// <param name="overwrite">Whether to overwrite existing element with same name if it's present.</param>
    /// <returns>Whether element was successfully registered.</returns>
    public bool TryRegisterElement(string UniqueName, Element element, bool overwrite = false)
    {
        if (!overwrite && Layouts.ContainsKey(UniqueName))
        {
            PluginLog.Warning($"There is an element named {UniqueName} already.");
            return false;
        }
        Elements[UniqueName] = element;
        return true;
    }

    /// <summary>
    /// Attempts to register previously exported from plugin element for further usage. End user will be able to edit this element as they wish and results of the edit will be saved. Enabled elements are subject for immediate processing when the script is enabled.
    /// </summary>
    /// <param name="UniqueName">Internal unique (within current script) name of the element</param>
    /// <param name="ExportString">An exported element string.</param>
    /// <param name="element">Decoded element object.</param>
    /// <param name="overwrite">Whether to overwrite existing element with same name if it's present.</param>
    /// <returns>Whether element was successfully registered.</returns>
    public bool TryRegisterElementFromCode(string UniqueName, string ExportString, [NotNullWhen(true)] out Element? element, bool overwrite = false)
    {
        return ScriptingEngine.TryDecodeElement(ExportString, out element) && TryRegisterElement(UniqueName, element, overwrite);
    }

    /// <summary>
    /// Tries to get previously registered layout by name.
    /// </summary>
    /// <param name="name">Layout's internal name.</param>
    /// <param name="layout">Result.</param>
    /// <returns>Whether operation succeeded.</returns>
    public bool TryGetLayoutByName(string name, [NotNullWhen(true)] out Layout? layout)
    {
        return Layouts.TryGetValue(name, out layout);
    }

    /// <summary>
    /// Tries to get previously registered element by name.
    /// </summary>
    /// <param name="name">Element's internal name.</param>
    /// <param name="element">Result.</param>
    /// <returns>Whether operation succeeded.</returns>
    public bool TryGetElementByName(string name, [NotNullWhen(true)] out Element? element)
    {
        return Elements.TryGetValue(name, out element);
    }

    /// <summary>
    /// Unregisters previously registered layout.
    /// </summary>
    /// <param name="name">Layout name.</param>
    /// <returns>Whether operation succeeded.</returns>
    public bool TryUnregisterLayout(string name)
    {
        return Layouts.Remove(name);
    }

    /// <summary>
    /// Unregisters previously registered element.
    /// </summary>
    /// <param name="name">Element name.</param>
    /// <returns>Whether operation succeeded.</returns>
    public bool TryUnregisterElement(string name)
    {
        return Elements.Remove(name);
    }

    public void RegisterElement(string UniqueName, Element element, bool overwrite = false) 
    {
        if(!TryRegisterElement(UniqueName, element, overwrite))
        {
            throw new InvalidOperationException($"RegisterElement failed: Could not register element {UniqueName}");
        }
    }

    public Element RegisterElementFromCode(string UniqueName, string ExportString, bool overwrite = false)
    {
        if(TryRegisterElementFromCode(UniqueName, ExportString, out var ret, overwrite))
        {
            return ret;
        }
        else
        {
            throw new InvalidOperationException($"RegisterElementFromCode failed: Could not register element {UniqueName}");
        }
    }

    public Element? GetElementByName(string name)
    {
        if(TryGetElementByName(name, out var ret))
        {
            return ret;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Returns a dictionary of currently registered layouts.
    /// </summary>
    /// <returns>Read only dictionary of currently registered layouts.</returns>
    public ReadOnlyDictionary<string, Layout> GetRegisteredLayouts()
    {
        return new ReadOnlyDictionary<string, Layout>(Layouts);
    }

    /// <summary>
    /// Returns a dictionary of currently registered elements.
    /// </summary>
    /// <returns>Read only dictionary of currently registered elements.</returns>
    public ReadOnlyDictionary<string, Element> GetRegisteredElements()
    {
        return new ReadOnlyDictionary<string, Element>(Elements);
    }

    /// <summary>
    /// Removes all layouts.
    /// </summary>
    public void ClearRegisteredLayouts()
    {
        Layouts.Clear();
    }

    /// <summary>
    /// Removes all elements
    /// </summary>
    public void ClearRegisteredElements()
    {
        Elements.Clear();
    }

    /// <summary>
    /// Removes all elements and layouts
    /// </summary>
    public void Clear()
    {
        ClearRegisteredElements();
        ClearRegisteredLayouts();
    }

    /// <summary>
    /// Retrieve valid and visible party members. Non cross-world parties only. Duty recorder supported.
    /// </summary>
    /// <returns>Enumberator of PlayerCharacter objects.</returns>
    public IEnumerable<PlayerCharacter> GetPartyMembers()
    {
        return FakeParty.Get();
    }

    public void ApplyOverrides()
    {
        foreach(var x in Script.InternalData.Overrides.Elements)
        {
            if (Elements.ContainsKey(x.Key))
            {
                PluginLog.Debug($"[{Script.InternalData.FullName}] Overriding {x.Key} element with custom data");
                Elements[x.Key] = x.Value.JSONClone();
            }
        }
    }

    public void SaveOverrides()
    {
        if(Script.InternalData.Overrides.Elements.Count > 0)
        {
            EzConfig.SaveConfiguration(Script.InternalData.Overrides, Script.InternalData.OverridesPath, true, false);
        }
        else
        {
            if (File.Exists(Script.InternalData.OverridesPath))
            {
                PluginLog.Debug($"No overrides for {Script.InternalData.FullName}, deleting {Script.InternalData.OverridesPath}");
                File.Delete(Script.InternalData.OverridesPath);
            }
        }
    }
}
