using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using Action = Lumina.Excel.Sheets.Action;

namespace SplatoonScriptsOfficial.Generic;
internal unsafe class ScriptEventLogger :SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;
    public override Metadata? Metadata => new(8, "Redmoon");

    private Config Conf => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        if (!Conf.FilterOnSetup) return;
        PluginLog.Information("OnSetup Called");
    }

    public override void OnEnable()
    {
        if (!Conf.FilterOnEnable)
            return;
        PluginLog.Information("OnEnable Called");
    }

    public override void OnDisable()
    {
        if (!Conf.FilterOnDisable)
            return;
        PluginLog.Information("OnDisable Called");
    }

    public override void OnCombatStart()
    {
        if (!Conf.FilterOnCombatStart)
            return;
        PluginLog.Information("OnCombatStart Called");
    }

    public override void OnCombatEnd()
    {
        if (!Conf.FilterOnCombatEnd)
            return;
        PluginLog.Information("OnCombatEnd Called");
    }

    public override void OnPhaseChange(int newPhase)
    {
        if (!Conf.FilterOnPhaseChange)
            return;
        PluginLog.Information($"OnPhaseChange: {newPhase}");
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if (!Conf.FilterOnMapEffect)
            return;
        PluginLog.Information($"OnMapEffect: {position} - {data1} - {data2}");
    }

    public override void OnObjectEffect(uint target, ushort data1, ushort data2)
    {
        if (!Conf.FilterOnObjectEffect)
            return;
        var targetObj = target.GetObject();
        if (targetObj == null || targetObj.DataId == 0)
            return;

        PluginLog.Information($"OnObjectEffect: {targetObj.Name}{targetObj.Position}(GID: {targetObj.GameObjectId} DID: {targetObj.DataId}) - {data1} - {data2}");
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (!Conf.FilterOnTetherCreate)
            return;

        var sourceObj = source.GetObject();
        var targetObj = target.GetObject();
        if (sourceObj != null && targetObj != null)
        {
            PluginLog.Information($"OnTetherCreate: - {data2} - {data3} - {data5}");
            PluginLog.Information($"    Source: {sourceObj.Name}{sourceObj.Position}(GID: {sourceObj.GameObjectId} DID: {sourceObj.DataId})");
            PluginLog.Information($"    Target: {targetObj.Name}{targetObj.Position}(GID: {targetObj.GameObjectId} DID: {targetObj.DataId})");
        }
        else if (sourceObj != null && targetObj == null)
        {
            PluginLog.Information($"OnTetherCreate: - {data2} - {data3} - {data5}");
            PluginLog.Information($"    Source: {sourceObj.Name}{sourceObj.Position}(GID: {sourceObj.GameObjectId} DID: {sourceObj.DataId})");
        }
        else if (sourceObj == null && targetObj != null)
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
        if (!Conf.FilterOnTetherRemoval)
            return;

        var sourceObj = source.GetObject();

        if (sourceObj == null)
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
        if (!Conf.FilterOnVFXSpawn) return;
        var targetObj = target.GetObject();

        if (target.GetObject() is IBattleNpc npc && (npc.BattleNpcKind == BattleNpcSubKind.Pet ||
            npc.BattleNpcKind == BattleNpcSubKind.None || npc.BattleNpcKind == BattleNpcSubKind.Chocobo)) return;

        if (Conf.FilterOnVFXSpawnSubFilterPlayers && targetObj is IPlayerCharacter) return;
        if (Conf.FilterOnVFXSpawnSubFilterEnemies && targetObj is IBattleNpc) return;

        if (targetObj is IPlayerCharacter && vfxPath.Contains("vfx/common/eff/")) return;

        if (targetObj == null)
            PluginLog.Information($"OnVFXSpawn: {vfxPath}");
        else
            PluginLog.Information($"OnVFXSpawn: {vfxPath} - {targetObj.Name}{targetObj.Position}(GID: {targetObj.GameObjectId} DID: {targetObj.DataId})");
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (!Conf.FilterOnStartingCast) return;
        if (source.GetObject() is not IBattleNpc npc ||
            npc.BattleNpcKind == BattleNpcSubKind.Pet ||
            npc.BattleNpcKind == BattleNpcSubKind.None ||
            npc.BattleNpcKind == BattleNpcSubKind.Chocobo) return;
        if (npc == null || npc.DataId == 0) return;

        var action = Svc.Data.GetExcelSheet<Action>()!.GetRowOrDefault(castId);

        if (action.HasValue)
        {
            PluginLog.Information($"OnStartingCast: Cast: {action.Value.Name}({castId}) - source: {npc.Name}{npc.Position}(GID: 0x{npc.GameObjectId.ToString("X8")} DID: {npc.DataId})");
        }
        else
        {
            PluginLog.Information($"OnStartingCast: Cast: {castId} - source: {npc.Name}{npc.Position}(GID: 0x{npc.GameObjectId.ToString("X8")} DID: {npc.DataId})");
        }
    }

    public override void OnMessage(string Message)
    {
        if (!Conf.FilterOnMessage) return;
        PluginLog.Information($"OnMessage: {Message}");
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (!Conf.FilterOnDirectorUpdate) return;
        if (category == DirectorUpdateCategory.Commence)
            PluginLog.Information("OnDirectorUpdate: Commence");
        else if (category == DirectorUpdateCategory.Recommence)
            PluginLog.Information("OnDirectorUpdate: Recommence");
        else if (category == DirectorUpdateCategory.Complete)
            PluginLog.Information("OnDirectorUpdate: Complete");
        else if (category == DirectorUpdateCategory.Wipe)
            PluginLog.Information("OnDirectorUpdate: Wipe");
    }

    public override void OnObjectCreation(nint newObjectPtr)
    {
        if (!Conf.FilterOnObjectCreation) return;
        PluginLog.Information($"OnObjectCreation: {newObjectPtr}");
    }

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, ulong targetId, byte replaying)
    {
        if (!Conf.FilterOnActorControl) return;
        PluginLog.Information($"OnActorControl: {sourceId} - {command} - {p1} - {p2} - {p3} - {p4} - {p5} - {p6} - {targetId} - {replaying}");
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!Conf.FilterOnActionEffectEvent) return;
        if (set.Action == null || set.Source == null || set.Source.DataId == 0) return;

        if (set.Source is not IBattleNpc npc ||
            npc.BattleNpcKind == BattleNpcSubKind.Pet ||
            npc.BattleNpcKind == BattleNpcSubKind.None ||
            npc.BattleNpcKind == BattleNpcSubKind.Chocobo) return;

        if (set.Target == null)
            PluginLog.Information(
                $"OnActionEffectEvent: " +
                $"{set.Action.Value.Name}({set.Action.Value.RowId}) - " +
                $"Source: {set.Source.Name}{set.Source.Position}" +
                $"(GID: 0x{set.Source.GameObjectId.ToString("X8")} DID: {set.Source.DataId})");
        else
            PluginLog.Information(
                $"OnActionEffectEvent: {set.Action.Value.Name}({set.Action.Value.RowId}) - " +
                $"Source: {set.Source.Name}{set.Source.Position}" +
                $"(GID: 0x{set.Source.GameObjectId.ToString("X8")} DID: {set.Source.DataId}) - " +
                $"Target: {set.Target.Name}{set.Target.Position}" +
                $"(GID: 0x{set.Target.GameObjectId.ToString("X8")} DID: {set.Target.DataId})");
    }

    public override void OnGainBuffEffect(uint sourceId, Status Status)
    {
        if (!Conf.FilterOnGainBuffEffect) return;
        if (!sourceId.TryGetObject(out var gameObject)) return;
        if (gameObject == null) return;
        if (Conf.FilterOnGainBuffEffectSubFilterPlayers && gameObject is IPlayerCharacter) return;
        if (Conf.FilterOnGainBuffEffectSubFilterEnemies && gameObject is IBattleNpc) return;

        var BuffName = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Status>().GetRowOrDefault(Status.StatusId)?.Name.ToString();

        if (BuffName == null)
        {
            PluginLog.Information($"OnGainBuffEffect: [{gameObject.Name}] ({Status.StatusId})");
        }
        else
        {
            PluginLog.Information($"OnGainBuffEffect: [{gameObject.Name}({BuffName})] ({Status.StatusId})");
        }
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status Status)
    {
        if (!Conf.FilterOnRemoveBuffEffect) return;
        if (!sourceId.TryGetObject(out var gameObject)) return;
        if (Conf.FilterOnRemoveBuffEffectSubFilterPlayers && gameObject is IPlayerCharacter) return;
        if (Conf.FilterOnRemoveBuffEffectSubFilterEnemies && gameObject is IBattleNpc) return;

        var BuffName = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Status>().GetRowOrDefault(Status.StatusId)?.Name.ToString();

        if (BuffName == null)
        {
            PluginLog.Information($"OnRemoveBuffEffect: [{gameObject.Name}({sourceId})] ({Status.StatusId})");
        }
        else
        {
            PluginLog.Information($"OnRemoveBuffEffect: [{gameObject.Name}({sourceId})({BuffName})] ({Status.StatusId})");
        }
    }

    public override void OnUpdateBuffEffect(uint sourceId, Status status)
    {
        if (!Conf.FilterOnUpdateBuffEffect) return;
        if (!sourceId.TryGetObject(out var gameObject)) return;
        if (Conf.FilterOnUpdateBuffEffectSubFilterPlayers && gameObject is IPlayerCharacter) return;
        if (Conf.FilterOnUpdateBuffEffectSubFilterEnemies && gameObject is IBattleNpc) return;

        var BuffName = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Status>().GetRowOrDefault(status.StatusId)?.Name.ToString();

        if (BuffName == null)
        {
            PluginLog.Information($"OnUpdateBuffEffect: [{gameObject.Name}({sourceId})] ({status.StatusId})");
        }
        else
        {
            PluginLog.Information($"OnUpdateBuffEffect: [{gameObject.Name}({sourceId})({BuffName})] ({status.StatusId})");
        }
    }


    public override void OnReset()
    {
        if (!Conf.FilterOnReset)
            return;
        PluginLog.Information("OnReset Called");
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Filter Events");
        if (ImGui.Button("Reset Filters")) Conf.Reset();

        ImGui.Checkbox($"OnSetup()##{nameof(Conf.FilterOnSetup)}", ref Conf.FilterOnSetup);
        ImGui.Checkbox($"OnEnable()##{nameof(Conf.FilterOnEnable)}", ref Conf.FilterOnEnable);
        ImGui.Checkbox($"OnDisable()##{nameof(Conf.FilterOnDisable)}", ref Conf.FilterOnDisable);
        ImGui.Checkbox($"OnCombatStart()##{nameof(Conf.FilterOnCombatStart)}", ref Conf.FilterOnCombatStart);
        ImGui.Checkbox($"OnCombatEnd()##{nameof(Conf.FilterOnCombatEnd)}", ref Conf.FilterOnCombatEnd);
        ImGui.Checkbox($"OnPhaseChange()##{nameof(Conf.FilterOnPhaseChange)}", ref Conf.FilterOnPhaseChange);
        ImGui.Checkbox($"OnMapEffect()##{nameof(Conf.FilterOnMapEffect)}", ref Conf.FilterOnMapEffect);
        ImGui.Checkbox($"OnObjectEffect()##{nameof(Conf.FilterOnObjectEffect)}", ref Conf.FilterOnObjectEffect);
        ImGui.Checkbox($"OnTetherCreate()##{nameof(Conf.FilterOnTetherCreate)}", ref Conf.FilterOnTetherCreate);
        ImGui.Checkbox($"OnTetherRemoval()##{nameof(Conf.FilterOnTetherRemoval)}", ref Conf.FilterOnTetherRemoval);
        ImGui.Checkbox($"OnVFXSpawn()##{nameof(Conf.FilterOnVFXSpawn)}", ref Conf.FilterOnVFXSpawn);
        ImGui.Indent();
        ImGui.Checkbox($"Filter Players##{nameof(Conf.FilterOnVFXSpawnSubFilterPlayers)}", ref Conf.FilterOnVFXSpawnSubFilterPlayers);
        ImGui.Checkbox($"Filter Enemies##{nameof(Conf.FilterOnVFXSpawnSubFilterEnemies)}", ref Conf.FilterOnVFXSpawnSubFilterEnemies);
        ImGui.Unindent();
        ImGui.Checkbox($"OnStartingCast()##{nameof(Conf.FilterOnStartingCast)}", ref Conf.FilterOnStartingCast);
        ImGui.Checkbox($"OnMessage()##{nameof(Conf.FilterOnMessage)}", ref Conf.FilterOnMessage);
        ImGui.Checkbox($"OnDirectorUpdate()##{nameof(Conf.FilterOnDirectorUpdate)}", ref Conf.FilterOnDirectorUpdate);
        ImGui.Checkbox($"OnObjectCreation()##{nameof(Conf.FilterOnObjectCreation)}", ref Conf.FilterOnObjectCreation);
        ImGui.Checkbox($"OnActorControl()##{nameof(Conf.FilterOnActorControl)}", ref Conf.FilterOnActorControl);
        ImGui.Checkbox($"OnActionEffectEvent()##{nameof(Conf.FilterOnActionEffectEvent)}", ref Conf.FilterOnActionEffectEvent);
        ImGui.Checkbox($"OnGainBuffEffect()##{nameof(Conf.FilterOnGainBuffEffect)}", ref Conf.FilterOnGainBuffEffect);
        ImGui.Indent();
        ImGui.Checkbox($"Filter Players##{nameof(Conf.FilterOnGainBuffEffectSubFilterPlayers)}", ref Conf.FilterOnGainBuffEffectSubFilterPlayers);
        ImGui.Checkbox($"Filter Enemies##{nameof(Conf.FilterOnGainBuffEffectSubFilterEnemies)}", ref Conf.FilterOnGainBuffEffectSubFilterEnemies);
        ImGui.Unindent();
        ImGui.Checkbox($"OnRemoveBuffEffect()##{nameof(Conf.FilterOnRemoveBuffEffect)}", ref Conf.FilterOnRemoveBuffEffect);
        ImGui.Indent();
        ImGui.Checkbox($"Filter Players##{nameof(Conf.FilterOnRemoveBuffEffectSubFilterPlayers)}", ref Conf.FilterOnRemoveBuffEffectSubFilterPlayers);
        ImGui.Checkbox($"Filter Enemies##{nameof(Conf.FilterOnRemoveBuffEffectSubFilterEnemies)}", ref Conf.FilterOnRemoveBuffEffectSubFilterEnemies);
        ImGui.Unindent();
        ImGui.Checkbox($"OnUpdateBuffEffect()##{nameof(Conf.FilterOnUpdateBuffEffect)}", ref Conf.FilterOnUpdateBuffEffect);
        ImGui.Indent();
        ImGui.Checkbox($"Filter Players##{nameof(Conf.FilterOnUpdateBuffEffectSubFilterPlayers)}", ref Conf.FilterOnUpdateBuffEffectSubFilterPlayers);
        ImGui.Checkbox($"Filter Enemies##{nameof(Conf.FilterOnUpdateBuffEffectSubFilterEnemies)}", ref Conf.FilterOnUpdateBuffEffectSubFilterEnemies);
        ImGui.Unindent();
        ImGui.Checkbox($"OnReset()##{nameof(Conf.FilterOnReset)}", ref Conf.FilterOnReset);
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
        public bool FilterOnGainBuffEffectSubFilterPlayers = false;
        public bool FilterOnGainBuffEffectSubFilterEnemies = false;
        public bool FilterOnRemoveBuffEffect = false;
        public bool FilterOnRemoveBuffEffectSubFilterPlayers = false;
        public bool FilterOnRemoveBuffEffectSubFilterEnemies = false;
        public bool FilterOnUpdateBuffEffect = false;
        public bool FilterOnUpdateBuffEffectSubFilterPlayers = false;
        public bool FilterOnUpdateBuffEffectSubFilterEnemies = false;
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
            FilterOnGainBuffEffectSubFilterPlayers = false;
            FilterOnGainBuffEffectSubFilterEnemies = false;
            FilterOnRemoveBuffEffect = false;
            FilterOnRemoveBuffEffectSubFilterPlayers = false;
            FilterOnRemoveBuffEffectSubFilterEnemies = false;
            FilterOnUpdateBuffEffect = false;
            FilterOnUpdateBuffEffectSubFilterPlayers = false;
            FilterOnUpdateBuffEffectSubFilterEnemies = false;
            FilterOnReset = false;
        }
    }
}
