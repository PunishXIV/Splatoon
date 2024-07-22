using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.DalamudServices;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests;
public class DMParser : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = new();

    public override Metadata? Metadata => new(1, "NightmareXIV");

    public override void OnEnable()
    {
        Svc.Chat.ChatMessage += Chat_ChatMessage;
    }

    public override void OnDisable()
    {
        Svc.Chat.ChatMessage -= Chat_ChatMessage;
    }

    private void Chat_ChatMessage(Dalamud.Game.Text.XivChatType type, int timestamp, ref Dalamud.Game.Text.SeStringHandling.SeString sender, ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        if (type == Dalamud.Game.Text.XivChatType.TellIncoming || type == Dalamud.Game.Text.XivChatType.TellOutgoing)
        {
            var player = sender.Payloads.OfType<PlayerPayload>().FirstOrDefault();
            if (player != null)
            {
                PluginLog.Information($"Detected {type}. Detected player name={player.PlayerName} and home world={player.World.RowId} ({player.World.Name})");
            }
            else
            {
                PluginLog.Information($"Detected {type} but player payload was null.");
            }
        }
    }
}
