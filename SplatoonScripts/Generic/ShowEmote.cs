using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.EzHookManager;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
#nullable disable

namespace SplatoonScriptsOfficial.Generic;

public unsafe class ShowEmote : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => null;
    public override Metadata Metadata => new(3, "NightmareXIV");

    private delegate long OnEmoteFuncDelegate(IntPtr a1, GameObject* source, ushort emoteId, GameObjectId targetId, long a5);
    [Signature("40 53 56 41 54 41 57 48 83 EC 38", DetourName = nameof(OnEmoteFuncDetour))]
    private Hook<OnEmoteFuncDelegate> OnEmoteFuncHook;

    public override void OnEnable()
    {
        SignatureHelper.Initialise(this);
        OnEmoteFuncHook?.Enable();
    }

    public override void OnDisable()
    {
        Svc.Commands.RemoveHandler("/playemote");
        OnEmoteFuncHook?.Dispose();
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Display emotes on all targets", ref Controller.GetConfig<Config>().ShowOnOthers);
    }

    private long OnEmoteFuncDetour(IntPtr a1, GameObject* source, ushort emoteId, GameObjectId targetId, long a5)
    {
        try
        {
            if(targetId == Svc.ClientState.LocalPlayer?.EntityId)
            {
                var emoteName = Svc.Data.GetExcelSheet<Emote>()?.GetRow(emoteId)?.Name;
                Svc.Chat.Print($">> {GenericHelpers.Read(source->Name)} uses {emoteName} on you.");
            }
            else if(Controller.GetConfig<Config>().ShowOnOthers)
            {
                var emoteName = Svc.Data.GetExcelSheet<Emote>()?.GetRow(emoteId)?.Name;
                var target = Svc.Objects.FirstOrDefault(x => (ulong)x.Struct()->GetGameObjectId() == (ulong)targetId);
                Svc.Chat.Print($">> {GenericHelpers.Read(source->Name)} uses {emoteName}" + (target != null ? $" on {target.Name}" : ""));
            }
        }
        catch(Exception e)
        {
            Svc.Chat.Print($"{e.Message}\n{e.StackTrace}");
        }
        return OnEmoteFuncHook!.Original(a1, source, emoteId, targetId, a5);
    }

    public class Config : IEzConfig
    {
        public bool ShowOnOthers = false;
    }
}
