using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Schedulers;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;
internal class P1_Powder_Mark_Trail :SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1238];
    public override Metadata? Metadata => new(1, "Redmoon");

    uint _buffPlayer = 0;
    uint _tooClosePlayer = 0;

    public override void OnSetup()
    {
        Controller.RegisterElement("PowderMark1", new(1) { radius = 10.0f, refActorComparisonType = 2 });
        Controller.RegisterElement("PowderMark2", new(1) { radius = 10.0f, refActorComparisonType = 2 });
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action == null) return;
        if (set.Action.Value.RowId == 40168)
        {
            _ = new TickScheduler(delegate
            {
                var buffPlayer = FakeParty.Get().FirstOrDefault(x => x is IPlayerCharacter pc && pc.StatusList.Any(y => y.StatusId == 4166));
                if (buffPlayer == null)
                {
                    _ = new TickScheduler(delegate
                    {
                        var buffPlayer = FakeParty.Get().FirstOrDefault(x => x is IPlayerCharacter pc && pc.StatusList.Any(y => y.StatusId == 4166));
                        if (buffPlayer == null) return;
                        _buffPlayer = buffPlayer.EntityId;
                    });
                }
                _buffPlayer = buffPlayer.EntityId;
            });
        }
        if (set.Action.Value.RowId == 40169)
        {
            _buffPlayer = 0;
            _tooClosePlayer = 0;
            Controller.GetElementByName("PowderMark1").Enabled = false;
            Controller.GetElementByName("PowderMark2").Enabled = false;
        }
    }

    public override void OnUpdate()
    {
        if (_buffPlayer == 0) return;
        var pc = _buffPlayer.GetObject() as IPlayerCharacter;
        if (pc == null) return;
        var status = pc.StatusList.Where(x => x.StatusId == 4166 && x.RemainingTime < 6.0f).ToList();
        if (status.Count == 0) return;
        Controller.GetElementByName("PowderMark1").refActorObjectID = _buffPlayer;
        if (_buffPlayer == Svc.ClientState.LocalPlayer.EntityId) Controller.GetElementByName("PowderMark1").Filled = false;
        else Controller.GetElementByName("PowderMark1").Filled = true;
        Controller.GetElementByName("PowderMark1").Enabled = true;

        Vector3 bufferPos = pc.Position;
        uint tooClosePlayer = 0;
        float tooClosePlayerDistance = float.MaxValue;
        var pcList = FakeParty.Get().ToList();
        foreach (var player in pcList)
        {
            if (player.EntityId == _buffPlayer) continue;
            var playerPos = player.Position;
            if (Vector3.Distance(bufferPos, playerPos) < tooClosePlayerDistance)
            {
                tooClosePlayer = player.EntityId;
                tooClosePlayerDistance = Vector3.Distance(bufferPos, playerPos);
            }
        }

        if (_tooClosePlayer == tooClosePlayer) return;

        _tooClosePlayer = tooClosePlayer;
        Controller.GetElementByName("PowderMark2").refActorObjectID = _tooClosePlayer;
        if (_tooClosePlayer == Svc.ClientState.LocalPlayer.EntityId) Controller.GetElementByName("PowderMark2").Filled = false;
        else Controller.GetElementByName("PowderMark2").Filled = true;
        Controller.GetElementByName("PowderMark2").Enabled = true;
    }

    public override void OnReset()
    {
        _buffPlayer = 0;
        _tooClosePlayer = 0;
        Controller.GetElementByName("PowderMark1").Enabled = false;
        Controller.GetElementByName("PowderMark2").Enabled = false;
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"Buff Player: {_buffPlayer}");
            ImGui.Text($"Too Close Player: {_tooClosePlayer}");
        }
    }
}
