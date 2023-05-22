using Dalamud.Data;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Plugin;
using Dalamud.Utility.Signatures;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic
{
    public unsafe class ShowEmote : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new();
        public override Metadata? Metadata => new(1, "NightmareXIV");

        delegate long OnEmoteFuncDelegate(IntPtr a1, GameObject* source, ushort emoteId, long targetId, long a5);
        [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 30 4C 8B 74 24 ?? 48 8B D9", DetourName = nameof(OnEmoteFuncDetour))]
        Hook<OnEmoteFuncDelegate>? OnEmoteFuncHook;

        public override void OnEnable()
        {
            SignatureHelper.Initialise(this);
            OnEmoteFuncHook?.Enable();
        }

        public override void OnDisable()
        {
            OnEmoteFuncHook?.Disable();
            OnEmoteFuncHook?.Dispose();
        }

        public override void OnSettingsDraw()
        {
            ImGui.Checkbox("Display emotes on all targets", ref this.Controller.GetConfig<Config>().ShowOnOthers);
        }

        long OnEmoteFuncDetour(IntPtr a1, GameObject* source, ushort emoteId, long targetId, long a5)
        {
            try
            {
                if (targetId == Svc.ClientState.LocalPlayer?.ObjectId)
                {
                    var emoteName = Svc.Data.GetExcelSheet<Emote>()?.GetRow(emoteId)?.Name;
                    Svc.Chat.Print($">> {MemoryHelper.ReadStringNullTerminated((IntPtr)source->Name)} uses {emoteName} on you.");
                }
                else if (this.Controller.GetConfig<Config>().ShowOnOthers)
                {
                    var emoteName = Svc.Data.GetExcelSheet<Emote>()?.GetRow(emoteId)?.Name;
                    var target = Svc.Objects.FirstOrDefault(x => x.Struct()->GetObjectID() == targetId);
                    Svc.Chat.Print($">> {MemoryHelper.ReadStringNullTerminated((IntPtr)source->Name)} uses {emoteName}" + (target != null ? $" on {target.Name}" : ""));
                }
            }
            catch (Exception e)
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
}
