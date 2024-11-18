using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
#nullable disable

namespace SplatoonScriptsOfficial.Generic;

public unsafe class ShowEmote : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => null;
    public override Metadata Metadata => new(5, "NightmareXIV");

    private delegate long OnEmoteFuncDelegate(IntPtr a1, GameObject* source, ushort emoteId, GameObjectId targetId, long a5);
    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 30 4C 8B 74 24 ?? 48 8B D9", DetourName = nameof(OnEmoteFuncDetour))]
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
                var emoteName = Svc.Data.GetExcelSheet<Emote>()?.GetRowOrDefault(emoteId)?.Name;
                Svc.Chat.Print($">> {GenericHelpers.Read(source->Name)} uses {emoteName} on you.");
            }
            else if(Controller.GetConfig<Config>().ShowOnOthers)
            {
                var emoteName = Svc.Data.GetExcelSheet<Emote>()?.GetRowOrDefault(emoteId)?.Name;
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
