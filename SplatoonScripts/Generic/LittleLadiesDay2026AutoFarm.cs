using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Generic;

public unsafe class LittleLadiesDay2026AutoFarm : SplatoonScript
{
    public override Metadata Metadata { get; } = new(4, "NightmareXIV, Knightmore");
    public override HashSet<uint>? ValidTerritories { get; } = [130];

    Dictionary<uint, uint> DataIdToActionId = new()
    {
        [18859] = 44501,
        [18860] = 44502,
        [18861] = 44503,
        [18862] = 44504
    };

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("NPC", """{"Name":"","type":1,"offZ":-0.5,"radius":6.8,"color":3356425984,"Filled":false,"fillIntensity":0.3,"overlayTextColor":4278386432,"overlayVOffset":3.0,"thicc":8.0,"overlayText":"Stay within green zone for auto farm","refActorDataID":1055771,"refActorComparisonType":3,"includeRotation":true,"FillStep":4.0}""");
    }

    List<IBattleChara> AllChara => Svc.Objects.OfType<IBattleChara>().Where(x => x.BaseId.EqualsAny(this.DataIdToActionId.Keys) && x.IsTargetable && Player.DistanceTo(x) < 35f).ToList();

    public override void OnUpdate()
    {
        Controller.Hide();
        if(Vector2.Distance(new(-37.500f, -140.000f), Player.Position.ToVector2()) < 20)
        {
            if(Player.Status.Any(x => x.StatusId == 1494))
            {
                var fate = FateManager.Instance()->CurrentFate;
                if(fate != null && fate->FateId != 0)
                {
                    foreach(var (baseId, actionId) in this.DataIdToActionId)
                    {
                        var target = AllChara.Shuffle().FirstOrDefault(x => x.BaseId == baseId);
                        if(target == null) continue;

                        if(EzThrottler.Check("UseAction") && EzThrottler.Check($"Use{AllChara.Select(x => x.DataId).Print()}"))
                        {
                            EzThrottler.Throttle("UseAction", 1000);
                            Svc.Targets.Target = target;
                            Controller.Schedule(() =>
                            {
                                if(Svc.Targets.Target != null)
                                {
                                    EzThrottler.Throttle($"Use{AllChara.Select(x => x.DataId).Print()}", 12000, true);
                                    ActionManager.Instance()->UseAction(ActionType.Action, actionId, Svc.Targets.Target.ObjectId);
                                }
                            }, Random.Shared.Next(1000));

                            //DuoLog.Information($"Use action {ExcelActionHelper.GetActionName(actionId, true)}");
                        }
                        break;
                    }
                }
            }
            else
            {
                var maiden = Svc.Objects.FirstOrDefault(x => x.DataId == 1055771 && x.IsTargetable);
                if(maiden != null) 
                {
                    if(Vector2.Distance(maiden.Position.ToVector2(), Player.Position.ToVector2()) <= 6.8f)
                    {

                    }
                    //Controller.GetElementByName("NPC").Enabled = true;

                    {
                        if(GenericHelpers.TryGetAddonMaster<AddonMaster.SelectString>(out var m) && m.IsAddonReady)
                        {

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
            EzThrottler.ImGuiPrintDebugInfo();
        }
    }
}
