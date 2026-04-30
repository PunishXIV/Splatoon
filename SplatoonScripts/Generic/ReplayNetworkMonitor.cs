using Dalamud.Bindings.ImGui;
using Dalamud.Hooking;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Memory;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Application.Network;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Network;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using ECommons.DalamudServices.Legacy;

namespace SplatoonScriptsOfficial.Generic;

public unsafe class ReplayNetworkMonitor : SplatoonScript
{
    private readonly ConcurrentQueue<NetworkPacketData> packets = new();

    private bool trackZoneUp;
    private bool trackZoneDown;
    private int trackedPackets = 20;
    private ulong nextPacketIndex;
    private string filterString = string.Empty;
    private bool filterRecording = true;
    private bool autoScroll = true;
    private bool autoScrollPending;

    private enum NetworkMessageDirection
    {
        ZoneDown,
        ZoneUp,
    }

    public class Config: IEzConfig
    {
        public uint NumBytes = 0;
    }
    Config C => Controller.GetConfig<Config>();

    public override Metadata Metadata { get; } = new(1, "NightmareXIV, Dalamud");
    public override HashSet<uint>? ValidTerritories { get; } = null;

    public override void OnDisable()
    {
        Svc.GameNetwork.NetworkMessageReplayed -= GameNetwork_NetworkMessageReplayed;
    }

    private void GameNetwork_NetworkMessageReplayed(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, ECommons.DalamudServices.Legacy.NetworkMessageDirection direction)
    {
        this.RecordPacket(new NetworkPacketData(Interlocked.Increment(ref this.nextPacketIndex), DateTime.Now, opCode, NetworkMessageDirection.ZoneDown, targetActorId, "", C.NumBytes <= 0?[]:MemoryHelper.ReadRaw(dataPtr, (int)C.NumBytes)));
    }

    /// <inheritdoc/>
    public override void OnSettingsDraw()
    {
        if(ImGui.Checkbox("Track ZoneDown"u8, ref this.trackZoneDown))
        {
            if(this.trackZoneDown)
            {
                if(!this.trackZoneUp)
                    this.nextPacketIndex = 0;

                Svc.GameNetwork.NetworkMessageReplayed += GameNetwork_NetworkMessageReplayed;
            }
            else
            {
                Svc.GameNetwork.NetworkMessageReplayed -= GameNetwork_NetworkMessageReplayed;
            }
        }

        ImGui.SetNextItemWidth(100);
        if(ImGui.DragInt("Stored Number of Packets"u8, ref this.trackedPackets, 0.1f, 1, 512))
        {
            this.trackedPackets = Math.Clamp(this.trackedPackets, 1, 512);
        }

        ImGui.SetNextItemWidth(100);
        if(ImGui.InputUInt("Read data (unsafe)"u8, ref C.NumBytes))
        {
            this.trackedPackets = Math.Clamp(this.trackedPackets, 1, 512);
        }

        if(ImGui.Button("Clear Stored Packets"u8))
        {
            this.packets.Clear();
            this.nextPacketIndex = 0;
        }

        ImGui.SameLine();
        ImGui.Checkbox("Auto-Scroll"u8, ref this.autoScroll);

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (ImGui.GetStyle().ItemInnerSpacing.X + ImGui.GetFrameHeight()) * 2);
        ImGui.InputTextWithHint("##Filter"u8, "Filter OpCodes..."u8, ref this.filterString, 1024, ImGuiInputTextFlags.AutoSelectAll);
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.Checkbox("##FilterRecording"u8, ref this.filterRecording);
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("When enabled, packets are filtered before being recorded.\nWhen disabled, all packets are recorded and filtering only affects packets displayed in the table."u8);
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        ImGuiComponents.HelpMarker("Enter OpCodes in a comma-separated list.\nRanges are supported. Exclude OpCodes with exclamation mark.\nExample: -400,!50-100,650,700-980,!941");

        using var table = ImRaii.Table("NetworkMonitorTableV2"u8, 6, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if(!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Direction"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("OpCode"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("OpCode (Hex)"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Data"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var autoScrollDisabled = false;

        foreach(var packet in this.packets)
        {
            if(!this.filterRecording && !this.IsFiltered(packet.OpCode))
                continue;

            ImGui.TableNextColumn();
            ImGui.Text(packet.Index.ToString());

            ImGui.TableNextColumn();
            ImGui.Text(packet.Time.ToLongTimeString());

            ImGui.TableNextColumn();
            ImGui.Text(packet.Direction.ToString());

            ImGui.TableNextColumn();
            using(ImRaii.PushId(packet.Index.ToString()))
            {
                if(ImGui.SmallButton("X"))
                {
                    if(!string.IsNullOrEmpty(this.filterString))
                        this.filterString += ",";

                    this.filterString += $"!{packet.OpCode}";
                }
            }

            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Filter OpCode"u8);

            autoScrollDisabled |= ImGui.IsItemHovered();

            ImGui.SameLine();
            ImGuiEx.TextCopy(packet.OpCode.ToString());
            autoScrollDisabled |= ImGui.IsItemHovered();

            ImGui.TableNextColumn();
            ImGuiEx.TextCopy($"0x{packet.OpCode:X3}");
            autoScrollDisabled |= ImGui.IsItemHovered();

            ImGui.TableNextColumn();
            ImGuiEx.TextCopy($"{packet.Data.ToHexString()}");
        }

        if(this.autoScroll && this.autoScrollPending && !autoScrollDisabled)
        {
            ImGui.SetScrollHereY();
            this.autoScrollPending = false;
        }
    }

    private static string GetTargetName(uint targetId)
    {
        if(targetId == PlayerState.Instance()->EntityId)
            return "Local Player";

        var cachedName = NameCache.Instance()->GetNameByEntityId(targetId);
        if(cachedName.HasValue)
            return cachedName.ToString();

        var obj = GameObjectManager.Instance()->Objects.GetObjectByEntityId(targetId);
        if(obj != null)
            return obj->NameString;

        return string.Empty;
    }

    private void RecordPacket(NetworkPacketData packet)
    {
        if(this.filterRecording && !this.IsFiltered(packet.OpCode))
            return;

        this.packets.Enqueue(packet);

        while(this.packets.Count > this.trackedPackets)
        {
            this.packets.TryDequeue(out _);
        }

        this.autoScrollPending = true;
    }

    private bool IsFiltered(ushort opcode)
    {
        var filterString = this.filterString.Replace(" ", string.Empty);

        if(filterString.Length == 0)
            return true;

        try
        {
            var offset = 0;
            var included = false;
            var hasInclude = false;

            while(filterString.Length - offset > 0)
            {
                var remaining = filterString[offset..];

                // find the end of the current entry
                var entryEnd = remaining.IndexOf(',');
                if(entryEnd == -1)
                    entryEnd = remaining.Length;

                var entry = filterString[offset..(offset + entryEnd)];
                var dash = entry.IndexOf('-');
                var isExcluded = entry.StartsWith('!');
                var startOffset = isExcluded ? 1 : 0;

                var entryMatch = dash == -1
                    ? ushort.Parse(entry[startOffset..]) == opcode
                    : ((dash - startOffset == 0 || opcode >= ushort.Parse(entry[startOffset..dash]))
                    && (entry[(dash + 1)..].Length == 0 || opcode <= ushort.Parse(entry[(dash + 1)..])));

                if(isExcluded)
                {
                    if(entryMatch)
                        return false;
                }
                else
                {
                    hasInclude = true;
                    included |= entryMatch;
                }

                if(entryEnd == filterString.Length)
                    break;

                offset += entryEnd + 1;
            }

            return !hasInclude || included;
        }
        catch(Exception ex)
        {
            PluginLog.Error("Invalid filter string" + ex.ToStringFull());
            return false;
        }
    }

#pragma warning disable SA1313
    private readonly record struct NetworkPacketData(ulong Index, DateTime Time, ushort OpCode, NetworkMessageDirection Direction, uint TargetEntityId, string TargetName, byte[] Data);
}
#pragma warning restore SA1313
