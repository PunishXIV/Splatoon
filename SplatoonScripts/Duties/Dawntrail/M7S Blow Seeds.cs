using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
internal class M7S_Blow_Seeds :SplatoonScript
{
    private enum Role
    {
        None = 0,
        TH,
        DPS
    }

    public override HashSet<uint>? ValidTerritories { get; } = [1261];
    public override Metadata? Metadata => new(2, "Redmoon");

    private readonly Dictionary<string, Vector3> kMarkerPos = new Dictionary<string, Vector3>
    {
        { "A", new Vector3(100f, -200f, -10f) },
        { "1", new Vector3(110f, -200f, -5f) },
        { "B", new Vector3(85f, -200f, 5f) },
        { "2", new Vector3(110f, -200f, 15f) },
        { "C", new Vector3(100f, -200f, 20f) },
        { "3", new Vector3(90f, -200f, 15f) },
        { "D", new Vector3(115f, -200f, 5f) },
        { "4", new Vector3(90f, -200f, -5f) },
    };

    private int _blowSeedsCount = 0;
    private Role _aoeRole = Role.None;
    private bool _gimmickActive = false;
    private Vector3 _enemyPos = Vector3.Zero;

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
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 43274)
        {
            _gimmickActive = true;
            CheckShouldShowElements();
        }

        if (!_gimmickActive) return;

        if (castId == 42405)
        {
            if (source.TryGetObject(out var obj) && obj is IBattleNpc enemy)
            {
                _enemyPos = enemy.Position;
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action == null) return;
        if (set.Action.Value.RowId == 42392)
        {
            _blowSeedsCount++;
            _aoeRole = Role.None;
            if (_blowSeedsCount == 8)
            {
                this.OnReset();
            }
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (!_gimmickActive) return;
        if (vfxPath.Contains("vfx/lockon/eff/loc06sp_05ak1.avfx"))
        {
            if (target.TryGetObject(out var obj) && obj is IPlayerCharacter pc)
            {
                if (pc.GetRole() == CombatRole.Tank)
                {
                    _aoeRole = Role.TH;
                }
                if (pc.GetRole() == CombatRole.DPS)
                {
                    _aoeRole = Role.DPS;
                }
            }
            CheckShouldShowElements();
        }
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"Blow Seeds Count: {_blowSeedsCount}");
            ImGuiEx.Text($"AOE Role: {_aoeRole}");
            ImGuiEx.Text($"Gimmick Active: {_gimmickActive}");
        }
    }

    public override void OnReset()
    {
        _gimmickActive = false;
        _blowSeedsCount = 0;
        _aoeRole = Role.None;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    private void CheckShouldShowElements()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if (!_gimmickActive) return;
        if (!Controller.TryGetElementByName("Bait", out var element)) return;
        if (_aoeRole == Role.None) return;

        Job job = Player.Job;
        if (_blowSeedsCount == 0)
        {
            if (_aoeRole == Role.TH)
            {
                // PLD 4
                if (job == Job.PLD) element.SetRefPosition(kMarkerPos["4"]);
                // DRK 1
                if (job == Job.DRK) element.SetRefPosition(kMarkerPos["1"]);
                // AST 3
                if (job == Job.AST) element.SetRefPosition(kMarkerPos["3"]);
                // SCH 2
                if (job == Job.SCH) element.SetRefPosition(kMarkerPos["2"]);
                if (job == Job.SCH || job == Job.AST || job == Job.DRK || job == Job.PLD)
                {
                    element.Enabled = true;
                }
            }
            if (_aoeRole == Role.DPS)
            {
                // DNC 4
                if (job == Job.DNC) element.SetRefPosition(kMarkerPos["4"]);
                // PCT 1
                if (job == Job.PCT) element.SetRefPosition(kMarkerPos["1"]);
                // VPR 2
                if (job == Job.VPR) element.SetRefPosition(kMarkerPos["2"]);
                // MNK 3
                if (job == Job.MNK) element.SetRefPosition(kMarkerPos["3"]);
                if (job == Job.DNC || job == Job.PCT || job == Job.VPR || job == Job.MNK)
                {
                    element.Enabled = true;
                }
            }
        }
        if (_blowSeedsCount == 4)
        {
            // エネミーから近いのはA,B,C,Dどれが近いか確認する
            var enemyPos = _enemyPos;
            var posA = kMarkerPos["A"];
            var posB = kMarkerPos["B"];
            var posC = kMarkerPos["C"];
            var posD = kMarkerPos["D"];
            var distA = Vector3.Distance(enemyPos, posA);
            var distB = Vector3.Distance(enemyPos, posB);
            var distC = Vector3.Distance(enemyPos, posC);
            var distD = Vector3.Distance(enemyPos, posD);
            var minDist = MathF.Min(distA, MathF.Min(distB, MathF.Min(distC, distD)));

            // minPosを12時として配置
            if (_aoeRole == Role.TH)
            {
                // A
                if (minDist == distC)
                {
                    // PLD 4
                    if (job == Job.PLD) element.SetRefPosition(kMarkerPos["4"]);
                    // DRK 1
                    if (job == Job.DRK) element.SetRefPosition(kMarkerPos["1"]);
                    // AST 3
                    if (job == Job.AST) element.SetRefPosition(kMarkerPos["3"]);
                    // SCH 2
                    if (job == Job.SCH) element.SetRefPosition(kMarkerPos["2"]);
                }
                // B
                if (minDist == distD)
                {
                    // PLD 1
                    if (job == Job.PLD) element.SetRefPosition(kMarkerPos["1"]);
                    // DRK 2
                    if (job == Job.DRK) element.SetRefPosition(kMarkerPos["4"]);
                    // AST 4
                    if (job == Job.AST) element.SetRefPosition(kMarkerPos["2"]);
                    // SCH 3
                    if (job == Job.SCH) element.SetRefPosition(kMarkerPos["3"]);
                }
                // C
                if (minDist == distA)
                {
                    // PLD 2
                    if (job == Job.PLD) element.SetRefPosition(kMarkerPos["2"]);
                    // DRK 3
                    if (job == Job.DRK) element.SetRefPosition(kMarkerPos["3"]);
                    // AST 1
                    if (job == Job.AST) element.SetRefPosition(kMarkerPos["4"]);
                    // SCH 4
                    if (job == Job.SCH) element.SetRefPosition(kMarkerPos["1"]);
                }
                // D
                if (minDist == distB)
                {
                    // PLD 3
                    if (job == Job.PLD) element.SetRefPosition(kMarkerPos["3"]);
                    // DRK 4
                    if (job == Job.DRK) element.SetRefPosition(kMarkerPos["2"]);
                    // AST 2
                    if (job == Job.AST) element.SetRefPosition(kMarkerPos["1"]);
                    // SCH 1
                    if (job == Job.SCH) element.SetRefPosition(kMarkerPos["4"]);
                }
                if (job == Job.SCH || job == Job.AST || job == Job.DRK || job == Job.PLD)
                {
                    element.Enabled = true;
                }
            }
            if (_aoeRole == Role.DPS)
            {
                // A
                if (minDist == distC)
                {
                    // DNC 3
                    if (job == Job.DNC) element.SetRefPosition(kMarkerPos["4"]);
                    // PCT 2
                    if (job == Job.PCT) element.SetRefPosition(kMarkerPos["1"]);
                    // VPR 4
                    if (job == Job.VPR) element.SetRefPosition(kMarkerPos["3"]);
                    // MNK 1
                    if (job == Job.MNK) element.SetRefPosition(kMarkerPos["2"]);
                }
                // B
                if (minDist == distD)
                {
                    // DNC 4
                    if (job == Job.DNC) element.SetRefPosition(kMarkerPos["1"]);
                    // PCT 3
                    if (job == Job.PCT) element.SetRefPosition(kMarkerPos["4"]);
                    // VPR 1
                    if (job == Job.VPR) element.SetRefPosition(kMarkerPos["2"]);
                    // MNK 2
                    if (job == Job.MNK) element.SetRefPosition(kMarkerPos["3"]);
                }
                // C
                if (minDist == distA)
                {
                    // DNC 1
                    if (job == Job.DNC) element.SetRefPosition(kMarkerPos["2"]);
                    // PCT 4
                    if (job == Job.PCT) element.SetRefPosition(kMarkerPos["3"]);
                    // VPR 2
                    if (job == Job.VPR) element.SetRefPosition(kMarkerPos["4"]);
                    // MNK 3
                    if (job == Job.MNK) element.SetRefPosition(kMarkerPos["1"]);
                }
                // D
                if (minDist == distB)
                {
                    // DNC 2
                    if (job == Job.DNC) element.SetRefPosition(kMarkerPos["3"]);
                    // PCT 1
                    if (job == Job.PCT) element.SetRefPosition(kMarkerPos["4"]);
                    // VPR 3
                    if (job == Job.VPR) element.SetRefPosition(kMarkerPos["1"]);
                    // MNK 4
                    if (job == Job.MNK) element.SetRefPosition(kMarkerPos["2"]);
                }
                if (job == Job.DNC || job == Job.PCT || job == Job.VPR || job == Job.MNK)
                {
                    element.Enabled = true;
                }
            }
        }
    }
}
