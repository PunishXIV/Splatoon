using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M8S_Elemental_Purge_Cleave :SplatoonScript
{
    public class Config :IEzConfig
    {
        public bool OutputCommand = false;
    }

    private bool _isActive = false;
    private bool _isLock = false;
    private bool _isTank => Player.Object.GetRole() == CombatRole.Tank;
    private uint _tankId1 = 0u;
    private uint _tankId2 = 0u;
    private Config C => Controller.GetConfig<Config>();

    public override HashSet<uint>? ValidTerritories => [1263];
    public override Metadata? Metadata => new(3, "Garume");

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Cone",
            "{\"Name\":\"\",\"type\":4,\"radius\":30.0,\"coneAngleMin\":-105,\"coneAngleMax\":105,\"color\":1677787135,\"fillIntensity\":0.5,\"thicc\":1.0,\"refActorDataID\":18222,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true,\"FaceMe\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"faceplayer\":\"<t1>\"}");
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 42085 && !_isActive)
        {
            FakeParty.Get().Where(x => x.GetRole() == CombatRole.Tank).ToList().ForEach(x =>
            {
                if (_tankId1 == 0) _tankId1 = x.EntityId;
                else if (_tankId2 == 0) _tankId2 = x.EntityId;
            });
            if (_tankId1 == 0 || _tankId2 == 0) return;
            _isActive = true;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (!_isActive) return;
        if (vfxPath == "vfx/lockon/eff/lockon5_t0h.avfx")
        {
            if (target != _tankId1 && target != _tankId2) return;
            var notTargetTankId = target == _tankId1 ? _tankId2 : _tankId1;
            if (Controller.TryGetElementByName("Cone", out var e) && notTargetTankId.TryGetObject(out var moonTank))
            {
                e.Enabled = true;
                e.faceplayer = $"<{GetPlayerOrder(moonTank)}>";
            }

            if (!_isTank && !C.OutputCommand) return;
            // moon tank
            if (notTargetTankId == Player.Object.EntityId)
            {
                Chat.Instance.ExecuteCommand("/e ComProvoke");
            }
            else
            {
                Chat.Instance.ExecuteCommand("/e ComShark");
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action is { RowId: 42093 })
        {
            _isActive = false;
            if (Controller.TryGetElementByName("Cone", out var e)) e.Enabled = false;
            _isLock = false;
        }
    }

    public override void OnReset()
    {
        _isActive = false;
        _tankId1 = 0;
        _tankId2 = 0;
        if (Controller.TryGetElementByName("Cone", out var e)) e.Enabled = false;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Output command", ref C.OutputCommand);
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"_active: {_isActive}");
            ImGui.Text($"_tankId1: {_tankId1}");
            ImGui.Text($"_tankId2: {_tankId2}");
            ImGui.Text($"_isTank: {_isTank}");
            ImGui.Text($"_isLock: {_isLock}");
        }
    }

    private void WormReset()
    {
        _isActive = false;
        if (Controller.TryGetElementByName("Cone", out var e)) e.Enabled = false;
    }

    private static unsafe int GetPlayerOrder(IGameObject c)
    {
        for (var i = 1; i <= 8; i++)
            if ((nint)FakePronoun.Resolve($"<{i}>") == c.Address)
                return i;

        return 0;
    }


}