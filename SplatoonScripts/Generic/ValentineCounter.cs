using Dalamud.Bindings.ImGui;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using static SplatoonScriptsOfficial.Generic.ValentineCounter.Delegates;

namespace SplatoonScriptsOfficial.Generic;

public class ValentineCounter : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;
    public ulong Run = 0;

    public static class Delegates
    {
        public delegate uint ItemCount(uint itemId, ulong characterId, int inventoryType);
        public delegate void EnqueueCustomAliasFromString(string aliasString, bool force, int? inclusiveStart, int? inclusiveEnd);
    }
    [EzIPC] ItemCount ItemCount;
    [EzIPC("Lifestream.%m", false)] EnqueueCustomAliasFromString EnqueueCustomAliasFromString;
    [EzIPC("AutoRetainer.%m", false)] Func<HashSet<ulong>> GetRegisteredCIDs;
    [EzIPC("AutoRetainer.%m", false)] Func<ulong, OCData> GetOfflineCharacterData;

    public class OCData
    {
        public string Name = "Unknown";
        public string World = "";
    }

    public override void OnSetup()
    {
        EzIPC.Init(this, "AllaganTools", SafeWrapper.AnyException);
    }

    public override void OnEnable()
    {
        Run = 0;
    }

    public override void OnUpdate()
    {
        if(Run == Player.CID && Player.Interactable && GenericHelpers.IsScreenReady())
        {
            Run = 0;
            EnqueueCustomAliasFromString("""{"ExportedName":"Chocolates","Commands":[{"Kind":0,"Aetheryte":2,"Timeout":5000,"RequireUiOpen":false},{"Kind":4,"Aetheryte":30,"Timeout":5000,"RequireUiOpen":false},{"Kind":1,"Point":{"X":-32.00437,"Y":7.25,"Z":-122.459335},"Territory":133,"Timeout":5000,"RequireUiOpen":false},{"Kind":6,"DataID":1054056,"Timeout":5000,"RequireUiOpen":false}]}""", false, null, null);
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.InputInt("Num. chocolates unspent to display", ref C.NumChoco);
        if(ImGuiEx.BeginDefaultTable(["Character", "Chocolate"]))
        {
            foreach(var x in GetRegisteredCIDs())
            {
                ref var c = ref Ref<uint>.Get(this.InternalData.FullName + $"{x}");
                if(EzThrottler.Throttle($"{x}", 1000))
                {
                    c = ItemCount(47863, x, -1);
                }
                if(c > C.NumChoco)
                {
                    var d = GetOfflineCharacterData(x);
                    if(d != null)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        if(ImGui.Button($"{d.Name}@{d.World}"))
                        {
                            Svc.Commands.ProcessCommand($"/li {d.Name}@{d.World}");
                            Run = x;
                        }
                        ImGui.TableNextColumn();
                        ImGuiEx.TextV($"{c}");
                    }
                }
            }
            ImGui.EndTable();
        }
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public int NumChoco = 4;
    }
}
