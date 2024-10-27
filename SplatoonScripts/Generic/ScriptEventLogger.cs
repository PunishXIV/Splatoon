using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Configuration;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Generic;
internal class ScriptEventLogger :SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;
    public override Metadata? Metadata => new(4, "Redmoon");

    private Config Conf => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        if(!Conf.FilterOnSetup)
            return;
        PluginLog.Information("OnSetup Called");
    }

    public override void OnEnable()
    {
        if(!Conf.FilterOnEnable)
            return;
        PluginLog.Information("OnEnable Called");
    }

    public override void OnDisable()
    {
        if(!Conf.FilterOnDisable)
            return;
        PluginLog.Information("OnDisable Called");
    }

    public override void OnCombatStart()
    {
        if(!Conf.FilterOnCombatStart)
            return;
        PluginLog.Information("OnCombatStart Called");
    }

    public override void OnCombatEnd()
    {
        if(!Conf.FilterOnCombatEnd)
            return;
        PluginLog.Information("OnCombatEnd Called");
    }

    public override void OnPhaseChange(int newPhase)
    {
        if(!Conf.FilterOnPhaseChange)
            return;
        PluginLog.Information($"OnPhaseChange: {newPhase}");
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(!Conf.FilterOnMapEffect)
            return;
        PluginLog.Information($"OnMapEffect: {position} - {data1} - {data2}");
    }

    public override void OnObjectEffect(uint target, ushort data1, ushort data2)
    {
        if(!Conf.FilterOnObjectEffect)
            return;
        var targetObj = target.GetObject();
        if(targetObj == null || targetObj.DataId == 0)
            return;

        PluginLog.Information($"OnObjectEffect: {targetObj.Name}{targetObj.Position}(GID: {targetObj.GameObjectId} DID: {targetObj.DataId}) - {data1} - {data2}");
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(!Conf.FilterOnTetherCreate)
            return;

        var sourceObj = source.GetObject();
        var targetObj = target.GetObject();
        if(sourceObj != null && targetObj != null)
        {
            PluginLog.Information($"OnTetherCreate: - {data2} - {data3} - {data5}");
            PluginLog.Information($"    Source: {sourceObj.Name}{sourceObj.Position}(GID: {sourceObj.GameObjectId} DID: {sourceObj.DataId})");
            PluginLog.Information($"    Target: {targetObj.Name}{targetObj.Position}(GID: {targetObj.GameObjectId} DID: {targetObj.DataId})");
        }
        else if(sourceObj != null && targetObj == null)
        {
            PluginLog.Information($"OnTetherCreate: - {data2} - {data3} - {data5}");
            PluginLog.Information($"    Source: {sourceObj.Name}{sourceObj.Position}(GID: {sourceObj.GameObjectId} DID: {sourceObj.DataId})");
        }
        else if(sourceObj == null && targetObj != null)
        {
            PluginLog.Information($"OnTetherCreate: - {data2} - {data3} - {data5}");
            PluginLog.Information($"    Target: {targetObj.Name}{targetObj.Position}(GID: {targetObj.GameObjectId} DID: {targetObj.DataId})");
        }
        else
        {
            PluginLog.Information($"OnTetherCreate: - {data2} - {data3} - {data5}");
        }
    }

    public override void OnTetherRemoval(uint source, uint data2, uint data3, uint data5)
    {
        if(!Conf.FilterOnTetherRemoval)
            return;

        var sourceObj = source.GetObject();

        if(sourceObj == null)
        {
            PluginLog.Information($"OnTetherRemoval: - {data2} - {data3} - {data5}");
        }
        else
        {
            PluginLog.Information($"OnTetherRemoval: - {data2} - {data3} - {data5}");
            PluginLog.Information($"    Source: {sourceObj.Name}{sourceObj.Position}(GID: {sourceObj.GameObjectId} DID: {sourceObj.DataId})");
        }
    }
    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(!Conf.FilterOnVFXSpawn)
            return;

        var targetObj = target.GetObject();

        if(target.GetObject() is IBattleNpc npc && (npc.BattleNpcKind == BattleNpcSubKind.Pet || npc.BattleNpcKind == BattleNpcSubKind.None || npc.BattleNpcKind == BattleNpcSubKind.Chocobo))
            return;

        if(Conf.FilterOnVFXSpawnSubFilterPlayers && targetObj is IPlayerCharacter)
            return;

        if(Conf.FilterOnVFXSpawnSubFilterEnemies && targetObj is IBattleNpc)
            return;

        if(targetObj == null)
            PluginLog.Information($"OnVFXSpawn: {vfxPath}");
        else
            PluginLog.Information($"OnVFXSpawn: {vfxPath} - {targetObj.Name}{targetObj.Position}(GID: {targetObj.GameObjectId} DID: {targetObj.DataId})");
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(!Conf.FilterOnStartingCast)
            return;

        if(source.GetObject() is not IBattleNpc npc || npc.BattleNpcKind == BattleNpcSubKind.Pet || npc.BattleNpcKind == BattleNpcSubKind.None || npc.BattleNpcKind == BattleNpcSubKind.Chocobo)
            return;

        var sourceObj = source.GetObject();
        if(sourceObj == null || sourceObj.DataId == 0)
            return;

        PluginLog.Information($"OnStartingCast: {sourceObj.Name}{sourceObj.Position}(GID: {sourceObj.GameObjectId} DID: {sourceObj.DataId}) - Cast: {castId}");
    }

    public override void OnMessage(string Message)
    {
        if(!Conf.FilterOnMessage)
            return;
        PluginLog.Information($"OnMessage: {Message}");
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(!Conf.FilterOnDirectorUpdate)
            return;
        if(category == DirectorUpdateCategory.Commence)
            PluginLog.Information("OnDirectorUpdate: Commence");
        else if(category == DirectorUpdateCategory.Recommence)
            PluginLog.Information("OnDirectorUpdate: Recommence");
        else if(category == DirectorUpdateCategory.Complete)
            PluginLog.Information("OnDirectorUpdate: Complete");
        else if(category == DirectorUpdateCategory.Wipe)
            PluginLog.Information("OnDirectorUpdate: Wipe");
    }
    public override void OnObjectCreation(nint newObjectPtr)
    {
        if(!Conf.FilterOnObjectCreation)
            return;
        PluginLog.Information($"OnObjectCreation: {newObjectPtr}");
    }

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, ulong targetId, byte replaying)
    {
        if(!Conf.FilterOnActorControl)
            return;
        PluginLog.Information($"OnActorControl: {sourceId} - {command} - {p1} - {p2} - {p3} - {p4} - {p5} - {p6} - {targetId} - {replaying}");
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!Conf.FilterOnActionEffectEvent)
            return;
        if(set.Action == null || set.Source == null || set.Source.DataId == 0)
            return;

        if(set.Source is not IBattleNpc npc || npc.BattleNpcKind == BattleNpcSubKind.Pet || npc.BattleNpcKind == BattleNpcSubKind.None || npc.BattleNpcKind == BattleNpcSubKind.Chocobo)
            return;

        if(set.Target == null)
            PluginLog.Information($"OnActionEffectEvent: {set.Action.Name}({set.Action.RowId}) - Source: {set.Source.Name}{set.Source.Position}(GID: {set.Source.GameObjectId} DID: {set.Source.DataId})");
        else
            PluginLog.Information($"OnActionEffectEvent: {set.Action.Name}({set.Action.RowId}) - Source: {set.Source.Name}{set.Source.Position}(GID: {set.Source.GameObjectId} DID: {set.Source.DataId}) - Target: {set.Target.Name}{set.Target.Position}(GID: {set.Target.GameObjectId} DID: {set.Target.DataId})");
    }

#if false
    public override void OnGainBuffEffect(uint sourceId, IReadOnlyList<RecordedStatus> gainStatusInfos)
    {
        if(!Conf.FilterOnGainBuffEffect)
            return;
        var gameObject = sourceId.GetObject();
        foreach(var status in gainStatusInfos)
        {
            PluginLog.Information($"OnGainBuffEffect: [{gameObject.Name}({sourceId})] {status.ToStringWithName()}");
        }
    }

    public override void OnRemoveBuffEffect(uint sourceId, IReadOnlyList<RecordedStatus> removeStatusInfos)
    {
        if(!Conf.FilterOnRemoveBuffEffect)
            return;
        var gameObject = sourceId.GetObject();
        foreach(var status in removeStatusInfos)
        {
            PluginLog.Information($"OnRemoveBuffEffect: [{gameObject.Name}({sourceId})] {status.ToStringWithName()}");
        }
    }
#endif

    public override void OnReset()
    {
        if(!Conf.FilterOnReset)
            return;
        PluginLog.Information("OnReset Called");
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Filter Events");
        if(ImGui.Button("Reset Filters"))
        {
            Conf.Reset();
        }
        ImGui.Checkbox("OnSetup()", ref Conf.FilterOnSetup);
        ImGui.Checkbox("OnEnable()", ref Conf.FilterOnEnable);
        ImGui.Checkbox("OnDisable()", ref Conf.FilterOnDisable);
        ImGui.Checkbox("OnCombatStart()", ref Conf.FilterOnCombatStart);
        ImGui.Checkbox("OnCombatEnd()", ref Conf.FilterOnCombatEnd);
        ImGui.Checkbox("OnPhaseChange()", ref Conf.FilterOnPhaseChange);
        ImGui.Checkbox("OnMapEffect()", ref Conf.FilterOnMapEffect);
        ImGui.Checkbox("OnObjectEffect()", ref Conf.FilterOnObjectEffect);
        ImGui.Checkbox("OnTetherCreate()", ref Conf.FilterOnTetherCreate);
        ImGui.Checkbox("OnTetherRemoval()", ref Conf.FilterOnTetherRemoval);
        ImGui.Checkbox("OnVFXSpawn()", ref Conf.FilterOnVFXSpawn);
        ImGui.Indent();
        ImGui.Checkbox("Filter Players", ref Conf.FilterOnVFXSpawnSubFilterPlayers);
        ImGui.Checkbox("Filter Enemies", ref Conf.FilterOnVFXSpawnSubFilterEnemies);
        ImGui.Unindent();
        ImGui.Checkbox("OnStartingCast()", ref Conf.FilterOnStartingCast);
        ImGui.Checkbox("OnMessage()", ref Conf.FilterOnMessage);
        ImGui.Checkbox("OnDirectorUpdate()", ref Conf.FilterOnDirectorUpdate);
        ImGui.Checkbox("OnObjectCreation()", ref Conf.FilterOnObjectCreation);
        ImGui.Checkbox("OnActorControl()", ref Conf.FilterOnActorControl);
        ImGui.Checkbox("OnActionEffectEvent()", ref Conf.FilterOnActionEffectEvent);
        ImGui.Checkbox("OnGainBuffEffect()", ref Conf.FilterOnGainBuffEffect);
        ImGui.Checkbox("OnRemoveBuffEffect()", ref Conf.FilterOnRemoveBuffEffect);
        ImGui.Checkbox("OnReset()", ref Conf.FilterOnReset);
    }

    public class Config :IEzConfig
    {
        public bool FilterOnSetup = false;
        public bool FilterOnEnable = false;
        public bool FilterOnDisable = false;
        public bool FilterOnCombatStart = false;
        public bool FilterOnCombatEnd = false;
        public bool FilterOnPhaseChange = false;
        public bool FilterOnMapEffect = true;
        public bool FilterOnObjectEffect = false;
        public bool FilterOnTetherCreate = true;
        public bool FilterOnTetherRemoval = true;
        public bool FilterOnVFXSpawn = true;
        public bool FilterOnVFXSpawnSubFilterPlayers = false;
        public bool FilterOnVFXSpawnSubFilterEnemies = false;
        public bool FilterOnStartingCast = true;
        public bool FilterOnMessage = false;
        public bool FilterOnDirectorUpdate = false;
        public bool FilterOnObjectCreation = false;
        public bool FilterOnActorControl = false;
        public bool FilterOnActionEffectEvent = true;
        public bool FilterOnGainBuffEffect = false;
        public bool FilterOnRemoveBuffEffect = false;
        public bool FilterOnReset = false;

        public void Reset()
        {
            FilterOnSetup = false;
            FilterOnEnable = false;
            FilterOnDisable = false;
            FilterOnCombatStart = false;
            FilterOnCombatEnd = false;
            FilterOnPhaseChange = false;
            FilterOnMapEffect = true;
            FilterOnObjectEffect = false;
            FilterOnTetherCreate = true;
            FilterOnTetherRemoval = true;
            FilterOnVFXSpawn = true;
            FilterOnVFXSpawnSubFilterPlayers = false;
            FilterOnVFXSpawnSubFilterEnemies = false;
            FilterOnStartingCast = true;
            FilterOnMessage = false;
            FilterOnDirectorUpdate = false;
            FilterOnObjectCreation = false;
            FilterOnActorControl = false;
            FilterOnActionEffectEvent = true;
            FilterOnGainBuffEffect = false;
            FilterOnRemoveBuffEffect = false;
            FilterOnReset = false;
        }
    }
}
