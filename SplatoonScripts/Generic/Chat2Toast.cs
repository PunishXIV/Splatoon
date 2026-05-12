using ECommons.DalamudServices;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace SplatoonScriptsOfficial.Generic;

public class Chat2Toast : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;

    public override void OnEnable()
    {
        Svc.Chat.ChatMessageUnhandled += Chat_ChatMessage;
        Svc.Chat.ChatMessageHandled += Chat_ChatMessage;
    }

    private void Chat_ChatMessage(Dalamud.Game.Chat.IChatMessage message)
    {
        if(message.LogKind == Dalamud.Game.Text.XivChatType.Echo)
        {
            Svc.Toasts.ShowQuest(message.Message);
        }
    }

    public override void OnDisable()
    {

        Svc.Chat.ChatMessageUnhandled -= Chat_ChatMessage;
        Svc.Chat.ChatMessageHandled -= Chat_ChatMessage;
    }
}
