using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.ExcelServices.Sheets;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Generic;

public unsafe class LittleLadiesDay2026AutoFarm : SplatoonScript
{
    public override Metadata Metadata { get; } = new(6, "NightmareXIV, Knightmore");
    public override HashSet<uint>? ValidTerritories { get; } = [130];

    Dictionary<uint, uint> DataIdToActionId = new()
    {
        [18859] = 44501,
        [18860] = 44502,
        [18861] = 44503,
        [18862] = 44504
    };

    float RandomTimer = Random.Shared.Next(5, 15) + Random.Shared.Next(99) * 0.01f;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("NPC", """{"Name":"","type":1,"offZ":-0.5,"radius":6.8,"color":3356425984,"Filled":false,"fillIntensity":0.3,"overlayTextColor":4278386432,"overlayVOffset":3.0,"thicc":8.0,"overlayText":"Stay within green zone for auto farm","refActorDataID":1055771,"refActorComparisonType":3,"includeRotation":true,"FillStep":4.0}""");
    }

    List<IBattleChara> AllChara => Svc.Objects.OfType<IBattleChara>().Where(x => x.BaseId.EqualsAny(this.DataIdToActionId.Keys) && x.IsTargetable && Player.DistanceTo(x) < 35f).ToList();
    long NoTarget;

    public override void OnUpdate()
    {
        Controller.Hide();
        var inAutoZone = Vector2.Distance(new(-37.500f, -140.000f), Player.Position.ToVector2()) < 20 && Player.Position.Y < 5.1f;
        if(inAutoZone)
        {
            var all = this.AllChara;
            foreach(var x in this.DataIdToActionId)
            {
                if(!all.Any(c => c.DataId == x.Key))
                {
                    EzThrottler.Throttle($"Untargetable{x.Key}", 500, true);
                }
            }
            var maiden = Svc.Objects.FirstOrDefault(x => x.DataId == 1055771 && x.IsTargetable);
            if(maiden != null)
            {
                Controller.GetElementByName("NPC").Enabled = true;
            }
            if(Player.Status.Any(x => x.StatusId == 1494))
            {
                if(Svc.Targets.Target?.DataId.EqualsAny(this.DataIdToActionId.Keys) == true)
                {
                    NoTarget = Environment.TickCount64;
                }
                EzThrottler.Throttle("HaveStatus", Random.Shared.Next(3000, 7000), true);
                var fate = FateManager.Instance()->CurrentFate;
                if(fate != null && fate->FateId != 0)
                {
                    EzThrottler.Throttle("Fate", 30000, true);
                    if(!all.All(x => !EzThrottler.Check($"Untargetable{x.BaseId}")) 
                        && EzThrottler.Check($"Use{AllChara.Select(x => x.DataId).Print()}")
                        && Environment.TickCount64 - NoTarget > 500)
                    {
                        var candidate = all.GetRandom();

                        if(EzThrottler.Check("UseAction"))
                        {
                            EzThrottler.Throttle("UseAction", 1000);
                            Svc.Targets.Target = candidate;
                            var actionId = this.DataIdToActionId[Svc.Targets.Target.DataId];
                            Controller.Schedule(() =>
                            {
                                if(Svc.Targets.Target != null)
                                {
                                    EzThrottler.Throttle($"Use{AllChara.Select(x => x.DataId).Print()}", 12000, true);
                                    ActionManager.Instance()->UseAction(ActionType.Action, actionId, Svc.Targets.Target.ObjectId);
                                }
                            }, Random.Shared.Next(1000));

                            PluginLog.Information($"Use action {ExcelActionHelper.GetActionName(actionId, true)}");
                        }
                    }
                }
            }
            if(maiden != null)
            {
                if(!AgentMap.Instance()->IsPlayerMoving && Vector2.Distance(maiden.Position.ToVector2(), Player.Position.ToVector2()) <= 6.8f)
                {
                    if(!IsOccupied())
                    {
                        if(EzThrottler.Check("HaveStatus"))
                        {
                            if(Svc.Targets.Target?.DataId != 1055771)
                            {
                                if(EzThrottler.Throttle("Target"))
                                {
                                    Svc.Targets.Target = maiden;
                                }
                            }
                            else
                            {
                                if(EzThrottler.Throttle("Interact"))
                                {
                                    TargetSystem.Instance()->InteractWithObject(maiden.Struct(), false);
                                    EzThrottler.Throttle("HaveStatus", 30000, true);
                                    RandomTimer = Random.Shared.Next(5, 15) + Random.Shared.Next(99) * 0.01f;
                                }
                            }
                        }
                        if(Player.Status.Any(x => x.StatusId == 1494 && x.RemainingTime < RandomTimer * 60f) && !EzThrottler.Check("Fate"))
                        {
                            if(EzThrottler.Throttle("Undisguise", 10000))
                            {
                                Controller.Schedule(() =>
                                {
                                    ActionManager.Instance()->UseAction(ActionType.Action, 11063);
                                }, Random.Shared.Next(3000));
                            }
                        }
                    }
                    {
                        if(GenericHelpers.TryGetAddonMaster<AddonMaster.SelectString>(out var m) && m.IsAddonReady)
                        {
                            if(FrameThrottler.Throttle("Select", 8))
                            {
                                foreach(var x in m.Entries)
                                {
                                    if(x.Text.EqualsAny(Svc.Data.GetExcelSheet<QuestDialogueText>(name: "custom/009/FesPdy2026FateDisguise_00951").GetRow(2).Value.GetText(), Svc.Data.GetExcelSheet<QuestDialogueText>(name: "custom/009/FesPdy2026FateDisguise_00951").GetRow(12).Value.GetText()))
                                    {
                                        x.Select();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    {
                        if(GenericHelpers.TryGetAddonMaster<AddonMaster.SelectYesno>(out var m) && m.IsAddonReady)
                        {
                            if(m.Text == Svc.Data.GetExcelSheet<QuestDialogueText>(name: "custom/009/FesPdy2026FateDisguise_00951").GetRow(18).Value.GetText())
                            {
                                if(FrameThrottler.Throttle("Select", 8))
                                {
                                    m.Yes();
                                }
                            }
                        }
                    }
                    {
                        if(GenericHelpers.TryGetAddonMaster<AddonMaster.Talk>(out var m))
                        {
                            m.Click();
                        }
                    }
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.InputFloat("Random", ref RandomTimer);
            EzThrottler.ImGuiPrintDebugInfo();
        }
    }

    public static bool IsOccupied()
    {
        return Svc.Condition[ConditionFlag.Occupied]
               || Svc.Condition[ConditionFlag.Occupied30]
               || Svc.Condition[ConditionFlag.Occupied33]
               || Svc.Condition[ConditionFlag.Occupied38]
               || Svc.Condition[ConditionFlag.Occupied39]
               || Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
               || Svc.Condition[ConditionFlag.OccupiedInEvent]
               || Svc.Condition[ConditionFlag.OccupiedInQuestEvent]
               || Svc.Condition[ConditionFlag.OccupiedSummoningBell]
               || Svc.Condition[ConditionFlag.WatchingCutscene]
               || Svc.Condition[ConditionFlag.WatchingCutscene78]
               || Svc.Condition[ConditionFlag.BetweenAreas]
               || Svc.Condition[ConditionFlag.BetweenAreas51]
               || Svc.Condition[ConditionFlag.InThatPosition]
               //|| Svc.Condition[ConditionFlag.TradeOpen]
               || Svc.Condition[ConditionFlag.Crafting]
               || Svc.Condition[ConditionFlag.ExecutingCraftingAction]
               || Svc.Condition[ConditionFlag.PreparingToCraft]
               || Svc.Condition[ConditionFlag.InThatPosition]
               || Svc.Condition[ConditionFlag.Unconscious]
               || Svc.Condition[ConditionFlag.MeldingMateria]
               || Svc.Condition[ConditionFlag.Gathering]
               || Svc.Condition[ConditionFlag.OperatingSiegeMachine]
               || Svc.Condition[ConditionFlag.CarryingItem]
               || Svc.Condition[ConditionFlag.CarryingObject]
               || Svc.Condition[ConditionFlag.BeingMoved]
               || Svc.Condition[ConditionFlag.RidingPillion]
               || Svc.Condition[ConditionFlag.Mounting]
               || Svc.Condition[ConditionFlag.Mounting71]
               || Svc.Condition[ConditionFlag.ParticipatingInCustomMatch]
               || Svc.Condition[ConditionFlag.PlayingLordOfVerminion]
               || Svc.Condition[ConditionFlag.ChocoboRacing]
               || Svc.Condition[ConditionFlag.PlayingMiniGame]
               || Svc.Condition[ConditionFlag.Performing]
               || Svc.Condition[ConditionFlag.PreparingToCraft]
               || Svc.Condition[ConditionFlag.Fishing]
               || Svc.Condition[ConditionFlag.UsingHousingFunctions]
               || Svc.Objects.LocalPlayer?.IsTargetable != true;
    }
}
