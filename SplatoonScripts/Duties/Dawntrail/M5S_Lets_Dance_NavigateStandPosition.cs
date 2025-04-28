using ECommons;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public sealed class M5S_Lets_Dance_NavigateStandPosition :SplatoonScript
{
    private enum State
    {
        None,
        Casting,
        End
    }

    private class PartyData
    {
        public uint Id;
        public Job Job;
        public uint StatusId;
        public float OriginalRemainingTime;
        public uint RemainingTime;
        public PartyData(uint id, Job job, uint statusId, float originalRemainingTime)
        {
            Id = id;
            Job = job;
            StatusId = statusId;
            OriginalRemainingTime = originalRemainingTime;
        }
    }

    public override HashSet<uint>? ValidTerritories => [1257];

    private const ushort AlphaDebuff = 0x116E;
    private const ushort BetaDebuff = 0x116F;

    private State _state = State.None;
    private List<PartyData> _partyData = new();

    private Config C => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            radius = 1.5f,
            thicc = 15f,
            tether = true,
            Donut = 0.3f
        };
        Controller.RegisterElement("Bait", element);
    }

    public override void OnSettingsDraw()
    {
        if (ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"Party Data: {_partyData.Count}");
            if (_partyData.Count > 0)
            {
                foreach (var data in _partyData.OrderBy(x => x.OriginalRemainingTime).ToList())
                {
                    string statusName = data.StatusId switch
                    {
                        AlphaDebuff => "Alpha",
                        BetaDebuff => "Beta",
                        _ => "Unknown"
                    };
                    ImGui.Text($"ID: {data.Id}, Job: {data.Job}, Status: {statusName}, Remaining Time: {data.RemainingTime}, Original Remaining Time: {data.OriginalRemainingTime}");
                }
            }
            else
            {
                ImGui.Text("No party data available.");
            }
        }
    }

    public override void OnUpdate()
    {
        if (_state == State.Casting)
            Controller.GetRegisteredElements()
                .Each(x => x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
        else
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnReset()
    {
        _state = State.None;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 42858 && _state == State.None)
        {
            _state = State.Casting;
            _partyData.Clear();
            foreach (var pc in FakeParty.Get())
            {
                var status = pc.StatusList.FirstOrDefault(x => x.StatusId is AlphaDebuff or BetaDebuff);
                if (status != null)
                {
                    var partyData = new PartyData(pc.EntityId, pc.GetJob(), status.StatusId, status.RemainingTime);
                    _partyData.Add(partyData);
                }

                if (_partyData.Count < 8) continue;

                var sortedPartyData = _partyData.OrderBy(x => x.OriginalRemainingTime).ToList();
                int assignedCount = 0;
                uint currentSec = 10u;
                foreach (var partyData in sortedPartyData)
                {
                    partyData.RemainingTime = currentSec;
                    assignedCount++;
                    if (assignedCount >= 2)
                    {
                        assignedCount = 0;
                        currentSec += 5;
                    }
                }

                if (!Controller.TryGetElementByName("Bait", out var baitElement))
                    return;

                var mine = _partyData.FirstOrDefault(x => x.Id == Player.Object.EntityId);
                if (mine == null) return;

                switch (mine.RemainingTime)
                {
                    case 25:
                        baitElement.SetOffPosition(new Vector3(100, 0, 106));
                        break;
                    case 20:
                        baitElement.SetOffPosition(new Vector3(100, 0, 102));
                        break;
                    case 15:
                        baitElement.SetOffPosition(new Vector3(100, 0, 98));
                        break;
                    case 10:
                        baitElement.SetOffPosition(new Vector3(100, 0, 94));
                        break;
                }

                baitElement.Enabled = true;
            }
        }
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status status)
    {
        if (_state is State.Casting && status.StatusId is AlphaDebuff or BetaDebuff) _state = State.End;
    }

    private class Config :IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
    }
}