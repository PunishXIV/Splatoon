using Dalamud.Game.Text.SeStringHandling;
using ECommons;
using ECommons.Automation;
using ECommons.ChatMethods;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class HouseFinder : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => null;
    string ToFind = "";
    int PriceMin = 0;
    int PriceMax = 0;
    EzThrottler<string> Throttler = new();
    int AutoScanValue = 0;
    bool AutoScan = false;
    int StopAt = 30;

    public override void OnUpdate()
    {
        if(ToFind != "" || PriceMax > 0)
        {
            {
                var playedSound = false;
                if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("HousingSelectBlock", out var addon) && GenericHelpers.IsAddonReady(addon))
                {
                    var reader = new ReaderHousingSelectBlock(addon);
                    for (int i = 0; i < reader.Houses.Count; i++)
                    {
                        var x = reader.Houses[i];
                        if((ToFind != "" && x.HouseName.Contains(ToFind, StringComparison.OrdinalIgnoreCase)) || (int.TryParse(x.HouseName.Replace(",", "").Split(" ")[0], out var price) && price.InRange(PriceMin, PriceMax)))
                        {
                            if(Throttler.Throttle($"Notify{x.HouseName} / {reader.WardNumber} / {i}", 10000))
                            {
                                ChatPrinter.Red($"Found {x.HouseName} ward {reader.WardNumber + 1}, plot {i + 1}");
                                if (!playedSound)
                                {
                                    UIModule.PlaySound(6);
                                    playedSound = true;
                                }
                            }
                        }
                    }
                    if (AutoScan)
                    {
                        if (reader.Houses.Count == 60)
                        {
                            if (Throttler.Check("AutoScanDelay"))
                            {
                                AutoScanValue = reader.WardNumber + 1;
                                if (AutoScanValue > 29 || AutoScanValue > StopAt - 1)
                                {
                                    AutoScan = false;
                                    DuoLog.Information($"Auto-scan finished");
                                }
                                else
                                {
                                    Throttler.Throttle("AutoScanDelay", 100, true);
                                    Callback.Fire(addon, true, 1, AutoScanValue);
                                }
                            }
                        }
                        else
                        {
                            Throttler.Throttle("AutoScanDelay", 100, true);
                        }
                    }
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.InputText($"Partial house name", ref ToFind, 50);
        ImGui.InputInt($"Price min", ref PriceMin);
        ImGui.InputInt($"Price max", ref PriceMax);
        if(ImGui.Checkbox("Auto scan", ref AutoScan))
        {
            AutoScanValue = 0;
        }
        ImGui.SameLine();
        ImGuiEx.Text($"{AutoScanValue}");
        ImGui.InputInt($"Stop autoscan at", ref StopAt);
        if (ImGui.CollapsingHeader("Debug"))
        {
            if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("HousingSelectBlock", out var addon) && GenericHelpers.IsAddonReady(addon))
            {
                var reader = new ReaderHousingSelectBlock(addon);
                foreach(var x in reader.Houses)
                {
                    ImGuiEx.Text($"{x.HouseName} {(int.TryParse(x.HouseName.Replace(",", "").Split(" ")[0], out var price)?price.ToString():"-")}");
                }
                ImGuiEx.Text($"{reader.Houses.Count}");
            }
        }
    }

    public class ReaderHousingSelectBlock : AtkReader
    {
        public ReaderHousingSelectBlock(AtkUnitBase* UnitBase, int BeginOffset = 0) : base(UnitBase, BeginOffset)
        {
        }

        public uint LoadedState => this.ReadUInt(0) ?? 0;
        public int WardNumber => this.ReadInt(1) ?? 0;
        public uint NumPlots => this.ReadUInt(35) ?? 0;
        public List<HouseInfo> Houses => this.Loop<HouseInfo>(36, 7, (int)this.NumPlots);

        public class HouseInfo : AtkReader
        {
            public HouseInfo(nint UnitBasePtr, int BeginOffset = 0) : base(UnitBasePtr, BeginOffset)
            {
            }

            public uint Unk0 => this.ReadUInt(0) ?? 0;
            public SeString PlotSize => this.ReadSeString(1);
            public string HouseName => this.ReadString(2);
            public uint Unk3 => this.ReadUInt(3) ?? 0;
            public uint Unk4 => this.ReadUInt(4) ?? 0;
            public uint Unk5 => this.ReadUInt(5) ?? 0;
            public uint Unk6 => this.ReadUInt(6) ?? 0;
        }
    }
}
