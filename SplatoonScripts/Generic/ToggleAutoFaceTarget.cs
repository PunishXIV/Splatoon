using ECommons.DalamudServices;
using ECommons.EzIpcManager;
using ECommons.Logging;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public sealed class ToggleAutoFaceTarget : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;

    [EzIPC("Questionable.IsRunning", false)] private Func<bool> QuestionableIsRunning;
    private bool QuestionableWasRunning = false;
    private bool IsModified = false;

    public override void OnSetup()
    {
        EzIPC.Init(this);
    }

    public override void OnUpdate()
    {
        if(Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.LoggingOut])
        {
            if(QuestionableWasRunning)
            {
                OnQuestionableDisable();
                QuestionableWasRunning = false;
                PluginLog.Information("Logging out");
            }
            EzThrottler.Throttle("ToggleAutoFaceTargetNoCheck", 5000, true);
            return;
        }
        if(QuestionableIsRunning() && EzThrottler.Check("ToggleAutoFaceTargetNoCheck"))
        {
            if(!QuestionableWasRunning)
            {
                OnQuestionableEnable();
                QuestionableWasRunning = true;
                PluginLog.Information("Questionable enabled");
            }
        }
        else
        {
            if(QuestionableWasRunning)
            {
                OnQuestionableDisable();
                QuestionableWasRunning = false;
                PluginLog.Information("Questionable disabled");
            }
        }
    }

    private void OnQuestionableDisable()
    {
        if(IsModified)
        {
            Svc.GameConfig.UiControl.Set("AutoFaceTargetOnAction", false);
            IsModified = false;
            PluginLog.Information("Setting restored");
        }
    }

    private void OnQuestionableEnable()
    {
        var c = Svc.GameConfig.UiControl.GetBool("AutoFaceTargetOnAction");
        if(!c)
        {
            Svc.GameConfig.UiControl.Set("AutoFaceTargetOnAction", true);
            IsModified = true;
            PluginLog.Information("Setting applied");
        }
    }
}