using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ECommons.Schedulers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using Action = Lumina.Excel.Sheets.Action;

namespace SplatoonScriptsOfficial.Generic;

internal unsafe class ScriptEventLogger : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;
    public override Metadata? Metadata => new(9, "Redmoon");

    private Config Conf => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        if (!Conf.FilterOnSetup) return;
        PluginLog.Information("OnSetup Called");
    }

    public override void OnEnable()
    {
        if (!Conf.FilterOnEnable) return;
        PluginLog.Information("OnEnable Called");
    }

    public override void OnDisable()
    {
        if (!Conf.FilterOnDisable) return;
        PluginLog.Information("OnDisable Called");
    }

    public override void OnCombatStart()
    {
        if (!Conf.FilterOnCombatStart) return;
        PluginLog.Information("OnCombatStart Called");
    }

    public override void OnCombatEnd()
    {
        if (!Conf.FilterOnCombatEnd) return;
        PluginLog.Information("OnCombatEnd Called");
    }

    public override void OnPhaseChange(int newPhase)
    {
        if (!Conf.FilterOnPhaseChange) return;
        PluginLog.Information($"OnPhaseChange: {newPhase}");
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if (!Conf.FilterOnMapEffect) return;
        PluginLog.Information($"OnMapEffect: {position} - {data1} - {data2}");
    }

    public override void OnObjectEffect(uint target, ushort data1, ushort data2)
    {
        if (!Conf.FilterOnObjectEffect) return;
        var targetObj = target.GetObject();
        if (targetObj == null || targetObj.BaseId == 0) return;

        PluginLog.Information(
            $"OnObjectEffect: {targetObj.Name}{targetObj.Position}(GID: {targetObj.GameObjectId} DID: {targetObj.BaseId}) - {data1} - {data2}");
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (!Conf.FilterOnTetherCreate) return;

        var sourceObj = source.GetObject();
        var targetObj = target.GetObject();
        var info = $"OnTetherCreate: - {data2} - {data3} - {data5}\n";
        if (sourceObj != null)
        {
            info +=
                $"    - Source: {sourceObj.Name}{sourceObj.Position}(GID: {sourceObj.GameObjectId} DID: {sourceObj.BaseId})\n";
        }

        if (targetObj != null)
        {
            info +=
                $"    - Target: {targetObj.Name}{targetObj.Position}(GID: {targetObj.GameObjectId} DID: {targetObj.BaseId})\n";
        }

        if (info.EndsWith('\n')) info = info[..^1];
        PluginLog.Information(info);
    }

    public override void OnTetherRemoval(uint source, uint data2, uint data3, uint data5)
    {
        if (!Conf.FilterOnTetherRemoval) return;

        var sourceObj = source.GetObject();
        var info = $"OnTetherRemoval: - {data2} - {data3} - {data5}";
        if (sourceObj != null)
        {
            info +=
                $"    Source: {sourceObj.Name}{sourceObj.Position}(GID: {sourceObj.GameObjectId} DID: {sourceObj.BaseId})";
        }

        PluginLog.Information(info);
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (!Conf.FilterOnVfxSpawn) return;
        var targetObj = target.GetObject();

        if (target.GetObject() is IBattleNpc npc && (npc.BattleNpcKind == BattleNpcSubKind.Pet ||
                                                     npc.BattleNpcKind == BattleNpcSubKind.None ||
                                                     npc.BattleNpcKind == BattleNpcSubKind.Chocobo)) return;

        if (Conf.FilterOnVfxSpawnSubFilterPlayers && targetObj is IPlayerCharacter) return;
        if (Conf.FilterOnVfxSpawnSubFilterEnemies && targetObj is IBattleNpc) return;

        if (targetObj is IPlayerCharacter && vfxPath.Contains("vfx/common/eff/")) return;

        PluginLog.Information(targetObj == null
            ? $"OnVFXSpawn: {vfxPath}"
            : $"OnVFXSpawn: {vfxPath} - {targetObj.Name}{targetObj.Position}(GID: {targetObj.GameObjectId} DID: {targetObj.BaseId})");
    }

    public override void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if (!Conf.FilterOnStartingCast) return;
        if (sourceId.GetObject() is not IBattleNpc npc ||
            npc.BattleNpcKind == BattleNpcSubKind.Pet ||
            npc.BattleNpcKind == BattleNpcSubKind.None ||
            npc.BattleNpcKind == BattleNpcSubKind.Chocobo) return;
        if (npc.BaseId == 0) return;

        var action = Svc.Data.GetExcelSheet<Action>().GetRowOrDefault(packet->ActionID);
        var actionEn = Svc.Data.GetExcelSheet<Action>(ClientLanguage.English).GetRowOrDefault(packet->ActionID);

        if (actionEn == null) return;
        var castInfo = action.HasValue
            ? $"    - Cast: {action.Value.Name} CastEn: {actionEn.Value.Name} ID: {packet->ActionID}\n"
            : $"    - Cast: Unknown CastEn: Unknown ID: ({packet->ActionID})\n";

        var info =
            "OnStartingCast:\n" +
            castInfo +
            $"    - Cast Time: {packet->CastTime}\n" +
            $"    - Source  : {npc.Name} Pos: {npc.Position}\n" +
            $"        - GID     : {npc.GameObjectId} (0x{npc.GameObjectId.Format()})\n" +
            $"        - NPC ID  : {npc.Struct()->GetNameId()}\n" +
            $"        - DID     : {npc.BaseId}) (0x{npc.BaseId.Format()})\n" +
            $"        - Model ID: {npc.Struct()->ModelContainer.ModelCharaId}\n" +
            $"        - Rotation: {npc.Rotation}/{360 - (npc.Rotation.RadiansToDegrees() + 180)}\n";
        if (packet->TargetID != 0)
        {
            if (packet->TargetID.TryGetObject(out var targetObj))
            {
                info +=
                    $"    - Target  : {targetObj.Name} Pos: {targetObj.Position}\n" +
                    $"        - GID     : {targetObj.GameObjectId} (0x{targetObj.GameObjectId.Format()})\n" +
                    $"        - NPC ID  : {targetObj.Struct()->GetNameId()}\n" +
                    $"        - DID     : {targetObj.BaseId}) (0x{targetObj.BaseId.Format()})\n";
                if (targetObj is ICharacter targetCharcter)
                {
                    info +=
                        $"        - Model ID: {targetCharcter.Struct()->ModelContainer.ModelCharaId}\n";
                }
            }
            else
                info += $"    - Target  : Unknown (GID: {packet->TargetID})\n";
        }

        if (info.EndsWith('\n')) info = info[..^1];
        PluginLog.Information(info);
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
        else
            PluginLog.Information($"OnDirectorUpdate: Unknown Category {category}");
    }

    public override void OnObjectCreation(nint newObjectPtr)
    {
        if (!Conf.FilterOnObjectCreation) return;
        if (newObjectPtr == 0)
        {
            PluginLog.Information("OnObjectCreation: Invalid object pointer");
            return;
        }

        _ = new TickScheduler(() =>
            {
                var gameObject = Svc.Objects.FirstOrDefault(o => o.Address == newObjectPtr);
                if (gameObject == null)
                    PluginLog.Information($"OnObjectCreation: Object not found for pointer {newObjectPtr:X}");
                else
                {
                    PluginLog.Information(
                        $"OnObjectCreation: {gameObject.Name}({gameObject.Position})" +
                        $"(GID: 0x{gameObject.GameObjectId:X8} DID: {gameObject.BaseId})");
                }
            }
        );
    }

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5,
        uint p6, uint p7, uint p8, ulong targetId, byte replaying)
    {
        if (!Conf.FilterOnActorControl) return;
        PluginLog.Information(
            $"OnActorControl: {sourceId} - {command} - {p1} - {p2} - {p3} - {p4} - {p5} - {p6} - {p7} - {p8} - {targetId} - {replaying}");
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!Conf.FilterOnActionEffectEvent) return;
        if (set.Action == null || set.Source == null || set.Source.BaseId == 0) return;

        if (set.Source is not IBattleNpc npc ||
            npc.BattleNpcKind == BattleNpcSubKind.Pet ||
            npc.BattleNpcKind == BattleNpcSubKind.None ||
            npc.BattleNpcKind == BattleNpcSubKind.Chocobo) return;

        var actionEn = Svc.Data.GetExcelSheet<Action>(ClientLanguage.English).GetRowOrDefault(set.Action.Value.RowId);

        var info = $"OnActionEffectEvent: \n";

        var actionName = set.Action.Value.Name == "" ? "Unknown" : set.Action.Value.Name;
        var actionEnName = actionEn?.Name ?? "Unknown";
        info += $"    - Action: {actionName} CastEn: {actionEnName} ID: {set.Action.Value.RowId}\n";

        if (set.Source != null)
        {
            info +=
                $"    - Source: {set.Source.Name} Pos: {set.Source.Position}\n" +
                $"        - GID     : {set.Source.GameObjectId} (0x{set.Source.GameObjectId.Format()})\n" +
                $"        - NPC ID  : {set.Source.Struct()->GetNameId()}\n" +
                $"        - DID     : {set.Source.BaseId}) (0x{set.Source.BaseId.Format()})\n";
            if (set.Source is ICharacter sourceCharcter)
                info += $"        - Model ID: {sourceCharcter.Struct()->ModelContainer.ModelCharaId}\n";
        }

        if (set.Target != null)
        {
            info +=
                $"    - Target: {set.Target.Name} Pos: {set.Target.Position}\n" +
                $"        - GID     : {set.Target.GameObjectId} (0x{set.Target.GameObjectId.Format()})\n" +
                $"        - NPC ID  : {set.Target.Struct()->GetNameId()}\n" +
                $"        - DID     : {set.Target.BaseId}) (0x{set.Target.BaseId.Format()})\n";
            if (set.Target is ICharacter targetCharcter)
                info += $"        - Model ID: {targetCharcter.Struct()->ModelContainer.ModelCharaId}\n";
        }

        if (info.EndsWith('\n')) info = info[..^1];
        PluginLog.Information(info);
    }

    public override void OnGainBuffEffect(uint sourceId, Status Status)
    {
        if (!Conf.FilterOnGainBuffEffect) return;
        if (!sourceId.TryGetObject(out var gameObject)) return;
        if (Conf.FilterOnGainBuffEffectSubFilterPlayers && gameObject is IPlayerCharacter) return;
        if (Conf.FilterOnGainBuffEffectSubFilterEnemies && gameObject is IBattleNpc) return;

        var buffName = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Status>().GetRowOrDefault(Status.StatusId)?.Name
            .ToString();

        PluginLog.Information(buffName == null
            ? $"OnGainBuffEffect: [{gameObject.Name}] ({Status.StatusId})"
            : $"OnGainBuffEffect: [{gameObject.Name}({buffName})] ({Status.StatusId})");
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status Status)
    {
        if (!Conf.FilterOnRemoveBuffEffect) return;
        if (!sourceId.TryGetObject(out var gameObject)) return;
        if (Conf.FilterOnRemoveBuffEffectSubFilterPlayers && gameObject is IPlayerCharacter) return;
        if (Conf.FilterOnRemoveBuffEffectSubFilterEnemies && gameObject is IBattleNpc) return;

        var buffName = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Status>().GetRowOrDefault(Status.StatusId)?.Name
            .ToString();

        PluginLog.Information(buffName == null
            ? $"OnRemoveBuffEffect: [{gameObject.Name}({sourceId})] ({Status.StatusId})"
            : $"OnRemoveBuffEffect: [{gameObject.Name}({sourceId})({buffName})] ({Status.StatusId})");
    }

    public override void OnUpdateBuffEffect(uint sourceId, Status status)
    {
        if (!Conf.FilterOnUpdateBuffEffect) return;
        if (!sourceId.TryGetObject(out var gameObject)) return;
        if (Conf.FilterOnUpdateBuffEffectSubFilterPlayers && gameObject is IPlayerCharacter) return;
        if (Conf.FilterOnUpdateBuffEffectSubFilterEnemies && gameObject is IBattleNpc) return;

        var buffName = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Status>().GetRowOrDefault(status.StatusId)?.Name
            .ToString();

        PluginLog.Information(buffName == null
            ? $"OnUpdateBuffEffect: [{gameObject.Name}({sourceId})] ({status.StatusId})"
            : $"OnUpdateBuffEffect: [{gameObject.Name}({sourceId})({buffName})] ({status.StatusId})");
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
        ImGui.Checkbox($"OnVFXSpawn()##{nameof(Conf.FilterOnVfxSpawn)}", ref Conf.FilterOnVfxSpawn);
        ImGui.Indent();
        ImGui.Checkbox($"Filter Players##{nameof(Conf.FilterOnVfxSpawnSubFilterPlayers)}",
            ref Conf.FilterOnVfxSpawnSubFilterPlayers);
        ImGui.Checkbox($"Filter Enemies##{nameof(Conf.FilterOnVfxSpawnSubFilterEnemies)}",
            ref Conf.FilterOnVfxSpawnSubFilterEnemies);
        ImGui.Unindent();
        ImGui.Checkbox($"OnStartingCast()##{nameof(Conf.FilterOnStartingCast)}", ref Conf.FilterOnStartingCast);
        ImGui.Checkbox($"OnMessage()##{nameof(Conf.FilterOnMessage)}", ref Conf.FilterOnMessage);
        ImGui.Checkbox($"OnDirectorUpdate()##{nameof(Conf.FilterOnDirectorUpdate)}", ref Conf.FilterOnDirectorUpdate);
        ImGui.Checkbox($"OnObjectCreation()##{nameof(Conf.FilterOnObjectCreation)}", ref Conf.FilterOnObjectCreation);
        ImGui.Checkbox($"OnActorControl()##{nameof(Conf.FilterOnActorControl)}", ref Conf.FilterOnActorControl);
        ImGui.Checkbox($"OnActionEffectEvent()##{nameof(Conf.FilterOnActionEffectEvent)}",
            ref Conf.FilterOnActionEffectEvent);
        ImGui.Checkbox($"OnGainBuffEffect()##{nameof(Conf.FilterOnGainBuffEffect)}", ref Conf.FilterOnGainBuffEffect);
        ImGui.Indent();
        ImGui.Checkbox($"Filter Players##{nameof(Conf.FilterOnGainBuffEffectSubFilterPlayers)}",
            ref Conf.FilterOnGainBuffEffectSubFilterPlayers);
        ImGui.Checkbox($"Filter Enemies##{nameof(Conf.FilterOnGainBuffEffectSubFilterEnemies)}",
            ref Conf.FilterOnGainBuffEffectSubFilterEnemies);
        ImGui.Unindent();
        ImGui.Checkbox($"OnRemoveBuffEffect()##{nameof(Conf.FilterOnRemoveBuffEffect)}",
            ref Conf.FilterOnRemoveBuffEffect);
        ImGui.Indent();
        ImGui.Checkbox($"Filter Players##{nameof(Conf.FilterOnRemoveBuffEffectSubFilterPlayers)}",
            ref Conf.FilterOnRemoveBuffEffectSubFilterPlayers);
        ImGui.Checkbox($"Filter Enemies##{nameof(Conf.FilterOnRemoveBuffEffectSubFilterEnemies)}",
            ref Conf.FilterOnRemoveBuffEffectSubFilterEnemies);
        ImGui.Unindent();
        ImGui.Checkbox($"OnUpdateBuffEffect()##{nameof(Conf.FilterOnUpdateBuffEffect)}",
            ref Conf.FilterOnUpdateBuffEffect);
        ImGui.Indent();
        ImGui.Checkbox($"Filter Players##{nameof(Conf.FilterOnUpdateBuffEffectSubFilterPlayers)}",
            ref Conf.FilterOnUpdateBuffEffectSubFilterPlayers);
        ImGui.Checkbox($"Filter Enemies##{nameof(Conf.FilterOnUpdateBuffEffectSubFilterEnemies)}",
            ref Conf.FilterOnUpdateBuffEffectSubFilterEnemies);
        ImGui.Unindent();
        ImGui.Checkbox($"OnReset()##{nameof(Conf.FilterOnReset)}", ref Conf.FilterOnReset);
    }


    public class Config : IEzConfig
    {
        public bool FilterOnSetup;
        public bool FilterOnEnable;
        public bool FilterOnDisable;
        public bool FilterOnCombatStart;
        public bool FilterOnCombatEnd;
        public bool FilterOnPhaseChange;
        public bool FilterOnMapEffect = true;
        public bool FilterOnObjectEffect;
        public bool FilterOnTetherCreate = true;
        public bool FilterOnTetherRemoval = true;
        public bool FilterOnVfxSpawn = true;
        public bool FilterOnVfxSpawnSubFilterPlayers;
        public bool FilterOnVfxSpawnSubFilterEnemies;
        public bool FilterOnStartingCast = true;
        public bool FilterOnMessage;
        public bool FilterOnDirectorUpdate;
        public bool FilterOnObjectCreation;
        public bool FilterOnActorControl;
        public bool FilterOnActionEffectEvent = true;
        public bool FilterOnGainBuffEffect;
        public bool FilterOnGainBuffEffectSubFilterPlayers;
        public bool FilterOnGainBuffEffectSubFilterEnemies;
        public bool FilterOnRemoveBuffEffect;
        public bool FilterOnRemoveBuffEffectSubFilterPlayers;
        public bool FilterOnRemoveBuffEffectSubFilterEnemies;
        public bool FilterOnUpdateBuffEffect;
        public bool FilterOnUpdateBuffEffectSubFilterPlayers;
        public bool FilterOnUpdateBuffEffectSubFilterEnemies;
        public bool FilterOnReset;

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
            FilterOnVfxSpawn = true;
            FilterOnVfxSpawnSubFilterPlayers = false;
            FilterOnVfxSpawnSubFilterEnemies = false;
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