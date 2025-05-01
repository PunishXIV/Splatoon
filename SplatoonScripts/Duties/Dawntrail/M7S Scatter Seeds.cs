using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using Splatoon;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
internal class M7S_Scatter_Seeds :SplatoonScript
{
    private enum Role
    {
        None = 0,
        TD,
        HD,
        TH,
        DPS
    }
    public override HashSet<uint>? ValidTerritories { get; } = [1261];
    public override Metadata? Metadata => new(1, "Redmoon");

    private int _scatterSeedCounts = 0;
    private int _hitCounts = 0;
    private bool _gimmickActive = false;
    private Role _targetRole = Role.None;
    private bool _seedPosIsAvoidNE = false;
    private bool _seedPosIsAvoidNW = false;
    private Job _job = 0;

    public override void OnSetup()
    {
        Element element = new Element(0);
        element.Name = "Bait";
        element.tether = true;
        element.thicc = 15f;
        element.radius = 1f;
        element.color = 0xC800FF00u;
        element.Filled = false;
        Controller.RegisterElement("Bait", element);

        Element element2 = new Element(0);
        element2.Name = "Guide1";
        element2.tether = true;
        element2.thicc = 15f;
        element2.radius = 0.3f;
        element2.color = 0xC800FF00u;
        element2.Filled = false;
        element2.SetRefPosition(new Vector3(89.555f, -200f, -2.335f));
        Controller.RegisterElement("Guide1", element2);

        Element element3 = new Element(0);
        element3.Name = "Guide2";
        element3.tether = true;
        element3.thicc = 15f;
        element3.radius = 0.3f;
        element3.color = 0xC800FF00u;
        element3.Filled = false;
        element3.SetRefPosition(new Vector3(97.555f, -200f, -2.335f));
        Controller.RegisterElement("Guide2", element3);

        Element element4 = new Element(0);
        element4.Name = "Guide3";
        element4.tether = true;
        element4.thicc = 15f;
        element.radius = 0.3f;
        element4.color = 0xC800FF00u;
        element4.Filled = false;
        element4.SetRefPosition(new Vector3(105.555f, -200f, -2.335f));
        Controller.RegisterElement("Guide3", element4);

        Element element5 = new Element(0);
        element5.Name = "Guide3";
        element5.tether = true;
        element5.thicc = 15f;
        element.radius = 0.3f;
        element5.color = 0xC800FF00u;
        element5.Filled = false;
        element5.SetRefPosition(new Vector3(105.555f, -200f, 6.335f));
        Controller.RegisterElement("Guide4", element5);
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 42349)
        {
            _scatterSeedCounts++;
            _job = Player.Job;
            _gimmickActive = true;
        }
        if (castId == 42347 && source.TryGetObject(out var obj))
        {
            if (obj.Position.X == 83.5f && obj.Position.Z == 83.5f)
            {
                _seedPosIsAvoidNE = true;
            }
            if (obj.Position.X == 116.5f && obj.Position.Z == 83.5f)
            {
                _seedPosIsAvoidNW = true;
            }
        }
        if (castId == 42353)
        {
            _hitCounts++;
            if (_hitCounts >= 16)
            {
                WormReset();
                if (_scatterSeedCounts == 2)
                {
                    this.OnReset();
                }
            }
        }

        if (!_gimmickActive) return;
        CheckShouldShowElements();
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (!_gimmickActive) return;
        if (_targetRole != Role.None) return;
        if (vfxPath.Contains("vfx/lockon/eff/target_ae_s7k1.avfx"))
        {
            if (target.TryGetObject(out var obj) && obj is IPlayerCharacter pc)
            {
                if (pc.GetRole() == CombatRole.Tank)
                {
                    _targetRole = Role.TD;
                }
                if (pc.GetRole() == CombatRole.Healer)
                {
                    _targetRole = Role.HD;
                }
            }
        }
        CheckShouldShowElements();
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"Seed Count: {_scatterSeedCounts}");
            ImGuiEx.Text($"Hit Count: {_hitCounts}");
            ImGuiEx.Text($"Job: {_job}");
            ImGuiEx.Text($"Target Role: {_targetRole}");
            ImGuiEx.Text($"Seed Pos NE: {_seedPosIsAvoidNE}");
            ImGuiEx.Text($"Seed Pos NW: {_seedPosIsAvoidNW}");
            ImGuiEx.Text($"Gimmick Active: {_gimmickActive}");
        }
    }

    public override void OnReset()
    {
        _scatterSeedCounts = 0;
        WormReset();
    }

    private void WormReset()
    {
        _gimmickActive = false;
        _targetRole = Role.None;
        _seedPosIsAvoidNE = false;
        _seedPosIsAvoidNW = false;
        _job = 0;
        _hitCounts = 0;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    private void CheckShouldShowElements()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if (_scatterSeedCounts == 0) return;
        if (!Controller.TryGetElementByName("Bait", out var element)) return;
        if (!Controller.TryGetElementByName("Guide1", out var element2)) return;
        if (!Controller.TryGetElementByName("Guide2", out var element3)) return;
        if (!Controller.TryGetElementByName("Guide3", out var element4)) return;
        if (!Controller.TryGetElementByName("Guide4", out var element5)) return;

        if (_scatterSeedCounts == 2)
        {
            if ((_targetRole == Role.None) && (_job is Job.DRK or Job.PLD or Job.MNK or Job.VPR))
            {
                element.SetRefPosition(new Vector3(89.555f, -200f, -2.335f));
                element.Enabled = true;
            }
            if (_targetRole == Role.TD)
            {
                if (_job is Job.DRK) element.SetRefPosition(new Vector3(100f, -200f, 15f));
                if (_job is Job.PLD) element.SetRefPosition(new Vector3(100f, -200f, -5f));
                if (_job is Job.MNK) element.SetRefPosition(new Vector3(90f, -200f, 5f));
                if (_job is Job.VPR) element.SetRefPosition(new Vector3(110f, -200f, 5f));
                if (_job is Job.DRK or Job.PLD or Job.MNK or Job.VPR) element.Enabled = true;
            }
            if (_targetRole == Role.HD)
            {
                if (_job is Job.DRK or Job.PLD or Job.MNK or Job.VPR)
                {
                    if (_hitCounts == 0) element2.Enabled = true;
                    if (_hitCounts == 4) element3.Enabled = true;
                    if (_hitCounts == 8) element4.Enabled = true;
                    if (_hitCounts == 12) element5.Enabled = true;
                }
            }
        }
    }
}
