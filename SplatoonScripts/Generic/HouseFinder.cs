using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons;
using ECommons.Automation;
using ECommons.ChatMethods;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
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
    int LastScannedWard = -1;
    bool Enable = true;

    public override void OnUpdate()
    {
        if (!Enable) return;
        if(ToFind != "" || PriceMax > 0 || Sizes.Any() || Tags.Any())
        {
            var lscw = LastScannedWard;
            {
                var playedSound = false;
                if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("HousingSelectBlock", out var addon) && GenericHelpers.IsAddonReady(addon))
                {
                    var reader = new ReaderHousingSelectBlock(addon);
                    for (int i = 0; i < reader.Houses.Count; i++)
                    {
                        var x = reader.Houses[i];
                        if((ToFind != "" && x.HouseName.Contains(ToFind, StringComparison.OrdinalIgnoreCase)) || Sizes.Any() || Tags.Any() || (int.TryParse(x.HouseName.Replace(",", "").Split(" ")[0], out var price) && price.InRange(PriceMin, PriceMax)))
                        {
                            if (Sizes.Count > 0 && !Sizes.Contains(x.PlotSize)) continue;
                            if (Tags.Count > 0 && !Tags.ContainsAny(x.Tags)) continue;
                            if(ExcludeDenied && x.IsEntryDenied) continue;
                            if (LastScannedWard != reader.WardNumber)
                            {
                                lscw = reader.WardNumber;
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
            LastScannedWard = lscw;
        }
    }

    public enum HouseSize { Small, Medium, Large }
    public const string SizeLetterS = "";
    public const string SizeLetterM = "";
    public const string SizeLetterL = "";

    public List<uint> Tags = new();
    public List<HouseSize> Sizes = new();
    public bool ExcludeDenied = false;
    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Enable", ref Enable);
        ImGui.SameLine();
        if(ImGui.Button("Reset scan"))
        {
            LastScannedWard = -1;
        }
        ImGui.InputText($"Partial house name", ref ToFind, 50);
        ImGui.InputInt($"Price min", ref PriceMin);
        ImGui.InputInt($"Price max", ref PriceMax);
        if(ImGui.Checkbox("Auto scan", ref AutoScan))
        {
            Svc.Chat.Print($"Auto scan begun");
            Throttler.Throttle("AutoScanDelay", 100, true);
            LastScannedWard = -1;
            AutoScanValue = 0;
        }
        ImGui.SameLine();
        ImGuiEx.Text($"{AutoScanValue}");
        ImGui.InputInt($"Stop autoscan at", ref StopAt);
        if(ImGui.CollapsingHeader("Select tags"))
        {
            foreach (var x in Svc.Data.GetExcelSheet<HousingAppeal>()!)
            {
                if (x.Icon != 0)
                {
                    if (ThreadLoadImageHandler.TryGetIconTextureWrap(x.Icon, false, out var tex))
                    {
                        ImGui.Image(tex.ImGuiHandle, new System.Numerics.Vector2(24));
                        ImGui.SameLine();
                    }
                    ImGuiEx.CollectionCheckbox($"{x.Tag}", x.Icon, Tags);
                }
            }
        }
        ImGuiEx.TextV("Size:");
        foreach(var x in Enum.GetValues<HouseSize>())
        {
            ImGui.SameLine();
            ImGuiEx.CollectionCheckbox($"{x}", x, Sizes);
        }
        ImGui.Checkbox($"Exclude inaccessible", ref ExcludeDenied);
        if (ImGui.CollapsingHeader("Debug"))
        {
            if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("HousingSelectBlock", out var addon) && GenericHelpers.IsAddonReady(addon))
            {
                var reader = new ReaderHousingSelectBlock(addon);
                foreach(var x in reader.Houses)
                {
                    ImGuiEx.Text($"{x.HouseName} {(int.TryParse(x.HouseName.Replace(",", "").Split(" ")[0], out var price)?price.ToString():"-")} [{x.EntryPermission} / {x.Unk3}]");
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

            public uint EntryPermission => this.ReadUInt(0) ?? 0;
            public bool IsEntryDenied => this.EntryPermission.EqualsAny<uint>(0, 4);
            public string HouseName => this.ReadString(2);
            public uint Unk3 => this.ReadUInt(3) ?? 0;
            public uint[] Tags => new uint[] { this.ReadUInt(4) ?? 0, this.ReadUInt(5) ?? 0, this.ReadUInt(6) ?? 0 };
            public HouseSize PlotSize
            {
                get
                {
                    var plotSizeRaw = this.ReadSeString(1);
                    foreach(var x in plotSizeRaw.Payloads)
                    {
                        if(x is TextPayload text)
                        {
                            if (text.Text != null)
                            {
                                if (text.Text.Contains($"{SizeLetterS}{SizeLetterM}")) return HouseSize.Large;
                                if (text.Text.Contains($"{SizeLetterM}{SizeLetterL}")) return HouseSize.Small;
                            }
                        }
                    }
                    return HouseSize.Medium;
                }
            }
        }
    }
}
