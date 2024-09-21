﻿#nullable enable
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.LanguageHelpers;
using Newtonsoft.Json;

namespace Splatoon.SplatoonScripting;

public abstract class SplatoonScript
{
    protected SplatoonScript()
    {
        Controller = new(this);
    }

    /// <summary>
    /// Controller provides easy access to various helper functions that may be helpful for your script.
    /// </summary>
    public Controller Controller { get; }

    /// <summary>
    /// Metadata of a script that optionally contains author, description, version and script's origin website. This data will be displayed in Splatoon's interface.
    /// </summary>
    public virtual Metadata? Metadata { get; }

    /// <summary>
    /// Indicates whether your script operates strictly within Splatoon, ECommons and Dalamud APIs. 
    /// </summary>
    public virtual bool Safe { get; } = false;

    public InternalData InternalData { get; internal set; } = null!;

    /// <summary>
    /// Valid territories where script will be executed. Specify an empty array if you want it to work in all territories. Use null if you want script to always work without interruption even when client is logged out.
    /// </summary>
    public abstract HashSet<uint>? ValidTerritories { get; }

    /// <summary>
    /// Indicates whether script is currently enabled and should be executed or not.
    /// </summary>
    public bool IsEnabled { get; private set; } = false;

    internal bool IsDisabledByUser => P.Config.DisabledScripts.Contains(this.InternalData.FullName);

    /// <summary>
    /// Executed once after script is compiled and loaded into memory. Setup your layouts, elements and other static data that is not supposed to change within a game session. You should not setup any hooks or direct Dalamud events here, as method to cleanup is not provided (by design). Such things are to be done in OnEnable method.
    /// </summary>
    public virtual void OnSetup() { }

    /// <summary>
    /// Executed when player enters whitelisted territory. Will not trigger when player moves from one whitelisted territory to another.
    /// </summary>
    public virtual void OnEnable() { }

    /// <summary>
    /// Executed when player leaves whitelisted territory. Will not trigger when player moves from one non-whitelisted territory to another.
    /// </summary>
    public virtual void OnDisable() { }

    /// <summary>
    /// Will be called on combat start. This method will only be called if a script is enabled.
    /// </summary>
    public virtual void OnCombatStart() { }

    /// <summary>
    /// Will be called on combat end. This method will only be called if a script is enabled.
    /// </summary>
    public virtual void OnCombatEnd() { }

    /// <summary>
    /// Will be called on phase change. This method will only be called if a script is enabled. This method will be called if user manually changes phase as well.
    /// </summary>
    /// <param name="newPhase">New phase</param>
    public virtual void OnPhaseChange(int newPhase) { }

    /// <summary>
    /// Will be called on receiving map effect. This method will only be called if a script is enabled.
    /// </summary>
    /// <param name="position">Positional data of map effect. It is not related to actual map coordinates.</param>
    /// <param name="data1">First parameter of map effect.</param>
    /// <param name="data2">Second parameter of map effect.</param>
    public virtual void OnMapEffect(uint position, ushort data1, ushort data2) { }

    /// <summary>
    /// Will be called on receiving object effect. This method will only be called if a script is enabled.
    /// </summary>
    /// <param name="target">Targeted object's ID</param>
    /// <param name="data1">First parameter of object effect.</param>
    /// <param name="data2">Second parameter of object effect.</param>
    public virtual void OnObjectEffect(uint target, ushort data1, ushort data2) { }

    /// <summary>
    /// Will be called when a tether created between two game objects. This method will only be called if a script is enabled.
    /// </summary>
    /// <param name="source">Source object ID of pair.</param>
    /// <param name="target">Target object ID of pair.</param>
    /// <param name="data2">Second argument of hooked method.</param>
    /// <param name="data3">Third argument of hooked method.</param>
    /// <param name="data5">Fifth argument of hooked method.</param>
    public virtual void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5) { }

    /// <summary>
    /// Will be called when a previously created tether between two game objects removed. This method will only be called if a script is enabled.
    /// </summary>
    /// <param name="source">Source object ID of pair.</param>
    /// <param name="data2">Second argument of hooked method.</param>
    /// <param name="data3">Third argument of hooked method.</param>
    /// <param name="data5">Fifth argument of hooked method.</param>
    public virtual void OnTetherRemoval(uint source, uint data2, uint data3, uint data5) { }

    /// <summary>
    /// Will be called when a VFX spawns on a certain game object. This method will only be called if a script is enabled.
    /// </summary>
    /// <param name="target">Object ID that is targeted by VFX.</param>
    /// <param name="vfxPath">VFX game path</param>
    public virtual void OnVFXSpawn(uint target, string vfxPath) { }

    /// <summary>
    /// Will be called when a hostile object starts casting. These are the same messages which layout trigger system receives. This method will only be called if a script is enabled.
    /// </summary>
    /// <param name="source">Object ID that is source by VFX.</param>
    /// <param name="castId">ID of cast action.</param>
    public virtual void OnStartingCast(uint source, uint castId) { }

    /// <summary>
    /// Will be called whenever plugin processes a message. These are the same messages which layout trigger system receives. This method will only be called if a script is enabled.
    /// </summary>
    /// <param name="Message"></param>
    public virtual void OnMessage(string Message) { }

    /// <summary>
    /// Will be called when a duty director update is happening, for example, joining, restarting, or wiping in duty. 
    /// </summary>
    /// <param name="category">Director update category</param>
    public virtual void OnDirectorUpdate(DirectorUpdateCategory category) { }

    /// <summary>
    /// Will be called after object creation.
    /// </summary>
    /// <param name="newObjectPtr"></param>
    public virtual void OnObjectCreation(nint newObjectPtr) { }

    /// <summary>
    /// Will be called on every ActorControl packet. <b>VOLATILE DATA WARNING.</b>
    /// </summary>
    /// <param name="sourceId"></param>
    /// <param name="command"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <param name="p4"></param>
    /// <param name="p5"></param>
    /// <param name="p6"></param>
    /// <param name="targetId"></param>
    /// <param name="replaying"></param>
    public virtual void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, ulong targetId, byte replaying) { }

    [Obsolete($"Please use {nameof(OnActionEffectEvent)}")]
    public virtual void OnActionEffect(uint ActionID, ushort animationID, ActionEffectType type, uint sourceID, ulong targetOID, uint damage) { }

    /// <summary>
    /// Called when an ActionEffect Event is received
    /// </summary>
    /// <param name="set"></param>
    public virtual void OnActionEffectEvent(ActionEffectSet set) { }

    /// <summary>
    /// Will be called every framework update. You can execute general logic of your script here. 
    /// </summary>
    public virtual void OnUpdate() { }

    /// <summary>
    /// Called BEFORE director update indicates commence, recommence or wipe; combat start; combat end; disabling.<br></br>
    /// Also called AFTER enabling.<br></br>
    /// You can put cleanup here.
    /// </summary>
    public virtual void OnReset() { }

    /// <summary>
    /// This method is invoked when script is updated. You may notify user about important changes.
    /// </summary>
    /// <param name="previousVersion">Is not necessarily higher than current; if user forces an update - it can be the same.</param>
    public virtual void OnScriptUpdated(uint previousVersion) { }

    /// <summary>
    /// If you override this method, settings section will be added to your script. You can call ImGui methods in this function to draw configuration UI. Keep it simple.
    /// </summary>
    public virtual void OnSettingsDraw() { }

    /// <summary>
    /// Will be called when a buff is gained by a game object. This method will only be called if a script is enabled.
    /// </summary>
    /// <param name="sourceId">Source object ID of buff gain.</param>
    /// <param name="gainBuffIds">Array of gained buff IDs.</param>
    public virtual void OnGainBuffEffect(uint sourceId, List<uint> gainBuffIds) { }

    /// <summary>
    /// Will be called when a buff is removed from a game object. This method will only be called if a script is enabled.
    /// </summary>
    /// <param name="sourceId">Source object ID of buff removal.</param>
    /// <param name="removeBuffIds">Array of removed buff IDs.</param>
    public virtual void OnRemoveBuffEffect(uint sourceId, List<uint> removeBuffIds) { }

    internal void DrawRegisteredElements()
    {
        ImGuiEx.TextWrapped(ImGuiColors.DalamudRed, $"Non-restricted editing access. Any incorrectly performed changes may cause script to stop working completely. Use reset function if it happens. \n- In general, only edit color, thickness, text, size. \n- If script has it's own color settings, they will be prioritized.\n- Not all script will take whatever you edit here into account.".Loc());
        if(ImGui.Button("Export customized settings to clipboard".Loc()))
        {
            GenericHelpers.Copy(JsonConvert.SerializeObject(InternalData.Overrides, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Populate }));
            //Notify.Success("Copied to clipboard".Loc());
        }
        ImGui.SameLine();
        if(ImGui.Button("Import customized settings from clipboard (hold CTRL+click)".Loc()))
        {
            if(ImGui.GetIO().KeyCtrl)
            {
                try
                {
                    var x = JsonConvert.DeserializeObject<OverrideData>(GenericHelpers.Paste());
                    if(x != null)
                    {
                        if(ImGui.GetIO().KeyShift || x.Elements.All(z => Controller.GetRegisteredElements().ContainsKey(z.Key)))
                        {
                            InternalData.Overrides = x;
                            Controller.ApplyOverrides();
                            Notify.Success("Import success");
                        }
                        else
                        {
                            Notify.Error("Import contains keys that were not registered by script.\nImport blocked.\nTo override, hold CTRL+SHIFT while importing.");
                        }
                    }
                }
                catch(Exception e)
                {
                    e.Log();
                    Notify.Error(e.Message);
                }
            }
        }
        ImGui.Checkbox($"Enable unconditional element preview".Loc(), ref InternalData.UnconditionalDraw);
        if(InternalData.UnconditionalDraw)
        {
            if(ImGui.Button("Preview draw all".Loc()))
            {
                Controller.GetRegisteredElements().Each(x => InternalData.UnconditionalDrawElements.Add(x.Key));
            }
            ImGui.SameLine();
            if(ImGui.Button("Preview draw none".Loc()))
            {
                InternalData.UnconditionalDrawElements.Clear();
            }
        }
        foreach(var x in Controller.GetRegisteredElements())
        {
            ImGui.PushID(x.Value.GUID);
            if(InternalData.UnconditionalDraw)
            {
                ImGuiEx.HashSetCheckbox($"Preview draw".Loc(), x.Key, InternalData.UnconditionalDrawElements);
                ImGui.SameLine();
            }
            if(ImGui.Button("Copy to clipboard".Loc()))
            {
                GenericHelpers.Copy(JsonConvert.SerializeObject(x.Value, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }));
            }
            ImGui.SameLine();
            if(ImGui.Button("Edit".Loc()))
            {
                if(!InternalData.Overrides.Elements.ContainsKey(x.Key))
                {
                    Notify.Info($"Created override for {x.Key}");
                    InternalData.Overrides.Elements[x.Key] = x.Value.JSONClone();
                }
                P.PinnedElementEditWindow.Open(this, x.Key);
            }
            ImGui.SameLine();
            if(InternalData.Overrides.Elements.ContainsKey(x.Key))
            {
                ImGuiEx.HashSetCheckbox("Reset".Loc(), x.Key, InternalData.ElementsResets);
            }
            ImGui.SameLine();
            ImGuiEx.Text($"[{x.Key}] {x.Value.Name}");
            ImGui.PopID();
        }
        if(InternalData.ElementsResets.Count > 0)
        {
            if(ImGui.Button("Reset selected elements and reload script".Loc()))
            {
                foreach(var x in InternalData.ElementsResets)
                {
                    InternalData.Overrides.Elements.Remove(x);
                }
                Controller.SaveOverrides();
                ScriptingProcessor.ReloadScript(this);
            }
        }
    }

    public bool DoSettingsDraw => this.GetType().GetMethod(nameof(OnSettingsDraw))?.DeclaringType != typeof(SplatoonScript);

    internal bool Enable()
    {
        if(IsEnabled || IsDisabledByUser || !this.InternalData.Allowed || this.InternalData.Blacklisted)
        {
            return false;
        }
        try
        {
            PluginLog.Information($"Enabling script {this.InternalData.Name}");
            this.OnEnable();
        }
        catch(Exception ex)
        {
            ScriptingProcessor.LogError(this, ex, nameof(Enable));
        }
        this.IsEnabled = true;
        ScriptingProcessor.OnReset(this);
        return true;
    }

    internal bool Disable()
    {
        this.Controller.SaveConfig();
        if(!IsEnabled)
        {
            return false;
        }
        ScriptingProcessor.OnReset(this);
        try
        {
            PluginLog.Information($"Disabling script {this}");
            this.OnDisable();
        }
        catch(Exception ex)
        {
            ScriptingProcessor.LogError(this, ex, nameof(Disable));
        }
        this.IsEnabled = false;
        return true;
    }
}