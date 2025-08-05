using ECommons;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.ExcelServices;
using ECommons.EzHookManager;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class AutoTeleport : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1252];

    delegate void DisplayPopupBanner(nint a1, int textureId, int a3, int a4);

    [EzHook("48 89 5C 24 ?? 57 48 83 EC 30 48 8B D9 89 91", false)]
    EzHook<DisplayPopupBanner> DisplayPopupBannerHook;

    TaskManager TaskManager;

    public override void OnSetup()
    {
        EzSignatureHelper.Initialize(this);
    }

    public override void OnEnable()
    {
        DisplayPopupBannerHook.Enable();
        TaskManager = new(new(timeLimitMS:20000, abortOnTimeout:true, showDebug:true));
    }

    public override void OnDisable()
    {
        DisplayPopupBannerHook.Disable();
        TaskManager.Dispose();
    }

    public override void OnSettingsDraw()
    {
        if(TaskManager.IsBusy && TaskManager.MaxTasks > 0)
        {
            ImGuiEx.Text(EColor.YellowBright, $"Plugin is processing tasks. Please wait.");
            ImGui.ProgressBar(TaskManager.Progress, new(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight()), $"{TaskManager.MaxTasks - TaskManager.NumQueuedTasks}/{TaskManager.MaxTasks}");
            if(ImGui.Button($"Abort {TaskManager.NumQueuedTasks} tasks"))
            {
                TaskManager.Abort();
            }
        }
        if(ImGui.Button("Enqueue Teleport")) TaskManager.Enqueue(TryReturn);
        if(Player.Object.IsCasting())
        {
            ImGuiEx.Text($"{ExcelActionHelper.GetActionName(Player.Object.CastActionId)}");
        }
        ImGuiEx.Text($"{ActionManager.Instance()->GetActionStatus(ActionType.Action, 41343)}");
    }

    bool TryReturn()
    {
        if(Player.Object.IsCasting(41343))
        {
            return true;
        }
        if(ActionManager.Instance()->GetActionStatus(ActionType.Action, 41343) == 0 && !AgentMap.Instance()->IsPlayerMoving)
        {
            if(FrameThrottler.Check("ReturnThrottle") && EzThrottler.Throttle("ReturnOC"))
            {
                Chat.ExecuteCommand("/return");
            }
        }
        else
        {
            FrameThrottler.Throttle("ReturnThrottle", 5, true);
        }
        return false;
    }

    void DisplayPopupBannerDetour(nint a1, int textureId, int a3, int a4)
    {
        try
        {
            if(textureId == 128388)
            {
                DuoLog.Warning("CE ended (hook)");
                TaskManager.Enqueue(TryReturn);
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        DisplayPopupBannerHook.Original(a1, textureId, a3, a4);
    }
}