using Dalamud.Game.ClientState.Conditions;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.ChatMethods;
using ECommons.PartyFunctions;
using ECommons.Throttlers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Splatoon.Services;

internal class MessageService
{
    private TaskManager ChatTaskManager = new();
    private MessageService() { }

    public void StopAll() => ChatTaskManager.Abort();
    public void StopAll(string owner)
    {
        if(ChatTaskManager.CurrentTask != null && ChatTaskManager.CurrentTask.Name.EndsWith($"@{owner}"))
        {
            PluginLog.Information($"Cancelled message sending task: {ChatTaskManager.CurrentTask.Name}");
            ChatTaskManager.AbortCurrent();
        }
        for(var i = ChatTaskManager.Tasks.Count - 1; i >= 0; i--)
        {
            var x = ChatTaskManager.Tasks[i];
            if(x.Name.EndsWith($"@{owner}"))
            {
                ChatTaskManager.Tasks.Remove(x);
                PluginLog.Information($"Cancelled message sending task: {x.Name}");
            }
        }
    }

    public bool IsBusy => ChatTaskManager.IsBusy;
    public int IntervalBetweenMessages => 170;
    public int IntervalBetweenDMs => 1500;

    public bool BypassLimitations = false;
    public bool BypassCooldown = false;

    public void EnqueueDM(string owner, bool demo, string nameWithWorld, IEnumerable<string> text)
    {
        foreach(var x in text)
        {
            if(x.Length > 0)
            {
                EnqueueDM(owner, demo, nameWithWorld, x);
            }
        }
    }


    public void EnqueueDM(string owner, bool demo, string nameWithWorld, string message)
    {
        if(message == "") return;
        var split = message.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if(split.Length > 1)
        {
            EnqueueDM(owner, demo, nameWithWorld, split);
            return;
        }
        ChatTaskManager.Enqueue(() =>
        {
            if(message.StartsWith("/wait ", StringComparison.OrdinalIgnoreCase))
            {
                if(float.TryParse(message[6..], out var delay))
                {
                    EzThrottler.Throttle("ChatMessage", IntervalBetweenMessages + (int)(delay * 1000f), true);
                }
                else
                {
                    DuoLog.Error($"Error parsing wait command: {message}");
                }
                return true;
            }
            if(EzThrottler.Check("ChatMessage") && EzThrottler.Throttle("ChatDM", IntervalBetweenDMs))
            {
                EzThrottler.Throttle("ChatMessage", IntervalBetweenMessages, true);
                var cmd = $"/tell {nameWithWorld} {message}";
                if(P.Config.NoChat || Svc.Condition[ConditionFlag.DutyRecorderPlayback] || demo)
                {
                    ChatPrinter.Orange(cmd);
                }
                else
                {
                    Chat.ExecuteCommand(cmd);
                }
                return true;
            }
            return false;
        }, $"[[Send DM to {nameWithWorld}: {message}]]@{owner}");
    }

    public void EnqueueText(string owner, bool demo, IEnumerable<string> text)
    {
        foreach(var x in text)
        {
            if(x.Length > 0)
            {
                EnqueueText(owner, demo, x);
            }
        }
    }

    public void EnqueueText(string owner, bool demo, string message)
    {
        if(message == "") return;
        var split = message.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if(split.Length > 1)
        {
            EnqueueText(owner, demo, split);
            return;
        }
        ChatTaskManager.Enqueue(() =>
        {
            if(message.StartsWith("/wait ", StringComparison.OrdinalIgnoreCase))
            {
                if(float.TryParse(message[6..], out var delay))
                {
                    EzThrottler.Throttle("ChatMessage", IntervalBetweenMessages + (int)(delay * 1000f), true);
                }
                else
                {
                    DuoLog.Error($"Error parsing wait command: {message}");
                }
                return true;
            }
            if(EzThrottler.Throttle("ChatMessage", IntervalBetweenMessages))
            {
                string cmd;
                if(message.Trim().StartsWith('/'))
                {
                    cmd = $"{message.Trim()}";
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Messages must start with forward slash /");
                }
                if(P.Config.NoChat || Svc.Condition[ConditionFlag.DutyRecorderPlayback] || demo)
                {
                    ChatPrinter.Green(cmd);
                }
                else
                {
                    Chat.ExecuteCommand(cmd);
                }
                return true;
            }
            return false;
        }, $"[[Send message: {message}]]@{owner}");
    }
}
