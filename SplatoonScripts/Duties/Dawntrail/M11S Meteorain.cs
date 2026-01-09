using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Configuration;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M11S_Meteorain : SplatoonScript
{
    public enum Corner
    {
        NorthWest,
        NorthEast,
        SouthEast
    }

    public enum MeteorSpot
    {
        NorthWest,
        NorthEast,
        SouthEast,
        SouthWest,
        SouthWestAlt1,
        SouthWestAlt2
    }

    public enum MeteorWave
    {
        Wave1,
        Wave2,
        Wave3
    }

    public enum Role
    {
        Meteor,
        Tether
    }

    public enum TetherOrigin
    {
        NorthWest,
        SouthEast
    }

    private readonly uint _actionAfterWave3 = 46134;
    private readonly uint _actionEnd = 46139;
    private readonly uint _actionMeteorPlace = 46133;
    private readonly uint _castMeteorainStart = 46132;

    private readonly Vector2 _center = new(100f, 100f);
    private const float _distStack = 10f; // 配置距離（頭割り）
    private const float _distMeteor = 8f; // 配置距離（通常隕石）
    private const float _distFinal = 22.0f; // 最終隕石配置距離
    private const float _distSWAlt1 = 14.0f; // SW 近め配置距離
    private const float _distSWAlt2 = 20.0f; // SW 遠め配置距離

    private readonly float _northPullDist = 18f;
    private readonly Vector2 _posNE = new(107f, 93f);
    private readonly Vector2 _posNW = new(93f, 93f);
    private readonly Vector2 _posSE = new(107f, 107f);
    private readonly Vector2 _posSW = new(93f, 107f);
    private readonly float _southPullDist = 18f;

    private readonly string _vfxLockon = "vfx/lockon/eff/lockon8_t0w.avfx";

    private Element? _eNav;
    private Element? _eTether;

    private State _state = State.Idle;

    public override Metadata Metadata => new(1, "Garume");
    public override HashSet<uint>? ValidTerritories { get; } = [1325];

    private Config C => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        Controller.RegisterElement("Nav", new Element(0)
        {
            radius = 2f,
            thicc = 10f,
            overlayVOffset = 3f,
            overlayFScale = 2.6f
        });

        Controller.RegisterElement("Tether", new Element(2)
        {
            thicc = 10f,
            overlayVOffset = 3f,
            overlayFScale = 2.6f
        });

        _eNav = Controller.GetElementByName("Nav");
        _eTether = Controller.GetElementByName("Tether");
    }

    public override void OnEnable()
    {
        ResetAll();
    }

    public override void OnReset()
    {
        ResetAll();
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence,
                DirectorUpdateCategory.Wipe))
            ResetAll();
    }

    private void ResetAll()
    {
        _state = State.Idle;
        DisableAll();
    }

    private void DisableAll()
    {
        if (_eNav != null) _eNav.Enabled = false;
        if (_eTether != null) _eTether.Enabled = false;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (_castMeteorainStart == 0) return;
        if (castId != _castMeteorainStart) return;
        if (_state != State.Idle && _state != State.Done) return;

        ResetAll();
        _state = State.Wave1Prepare;
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (string.IsNullOrEmpty(_vfxLockon)) return;

        if (!vfxPath.Equals(_vfxLockon, StringComparison.OrdinalIgnoreCase) &&
            !vfxPath.StartsWith(_vfxLockon, StringComparison.OrdinalIgnoreCase))
            return;

        if (_state == State.Wave1Prepare) _state = State.Wave1Active;
        else if (_state == State.Wave2Prepare) _state = State.Wave2Active;
        else if (_state == State.Wave3Prepare) _state = State.Wave3Active;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action == null) return;

        var actionId = set.Action.Value.RowId;

        if (actionId == _actionMeteorPlace)
        {
            if (_state == State.Wave1Active) _state = State.Wave2Prepare;
            else if (_state == State.Wave2Active) _state = State.Wave3Prepare;
            else if (_state == State.Wave3Active) _state = State.Wave4StackOnly;
            return;
        }

        if (actionId == _actionAfterWave3)
        {
            if (_state == State.Wave4StackOnly) _state = State.FinalSW;
            return;
        }

        if (actionId == _actionEnd)
            if (_state == State.FinalSW)
            {
                _state = State.Done;
                DisableAll();
            }
    }

    public override void OnUpdate()
    {
        DisableAll();
        if (_state is State.Idle or State.Done) return;

        var grad = GradientColor.Get(C.GradientA, C.GradientB, 333).ToUint();

        if (_eNav != null)
        {
            ApplyCommonStyle(_eNav, grad);

            var nav = GetNavInstruction();
            SetElement(_eNav, nav.pos, nav.text);
        }

        if (_eTether != null)
            if (ShouldShowTether())
            {
                ApplyCommonStyle(_eTether, grad);
                var t = GetTetherInstruction();
                SetElement(_eTether, t.pos, t.text);
            }
    }

    private bool ShouldShowTether()
    {
        if (C.MyRole != Role.Tether) return false;
        return _state == State.Wave2Active || _state == State.Wave3Active;
    }

    private (Vector2 pos, string text) GetTetherInstruction()
    {
        var originPos = C.MyTetherOrigin == TetherOrigin.NorthWest
            ? PosDir(Dir(Corner.NorthWest), _distMeteor)
            : PosDir(Dir(Corner.SouthEast), _distMeteor);
        var pullPos = C.MyTetherOrigin == TetherOrigin.NorthWest
            ? _center with { Y = _center.Y - _northPullDist }
            : _center with { Y = _center.Y + _southPullDist };

        var txt = C.MyTetherOrigin == TetherOrigin.NorthWest
            ? "Tether: Take NW -> Pull N"
            : "Tether: Take SE -> Pull S";

        var useOrigin = true;
        if (_state == State.Wave2Active || _state == State.Wave3Active) useOrigin = true;

        return useOrigin ? (originPos, txt) : (pullPos, txt);
    }

    private (Vector2 pos,  string text) GetNavInstruction()
    {
        var (wave, phase) = GetWavePhase();

        if (phase == Phase.Final)
            return (PosDir(Dir(MeteorSpot.SouthWest), _distFinal), "Final: SW");

        if (phase == Phase.StackOnly)
        {
            var p = CornerToPos(C.Stack4);
            return (p, $"Stack4 {Short(C.Stack4)}");
        }

        var stackCorner = wave switch
        {
            1 => C.Stack1,
            2 => C.Stack2,
            _ => C.Stack3
        };

        var isMyMeteorWave = C.MyRole == Role.Meteor && C.MyMeteorWave ==
            (wave == 1 ? MeteorWave.Wave1 : wave == 2 ? MeteorWave.Wave2 : MeteorWave.Wave3);

        if (phase == Phase.Prepare)
        {
            if (isMyMeteorWave)
                return (_center, $"Wave{wave}: Near Boss");
            var p = CornerToPos(stackCorner);
            return (p, $"Stack{wave} {Short(stackCorner)}");
        }

        if (isMyMeteorWave)
        {
            var (mp, mt) = MeteorToNav(C.MyMeteorSpot, wave);
            return (mp, mt);
        }

        var sp = CornerToPos(stackCorner);
        return (sp, $"Stack{wave} {Short(stackCorner)}");
    }

    private (int wave, Phase phase) GetWavePhase()
    {
        if (_state == State.Wave1Prepare) return (1, Phase.Prepare);
        if (_state == State.Wave1Active) return (1, Phase.Active);
        if (_state == State.Wave2Prepare) return (2, Phase.Prepare);
        if (_state == State.Wave2Active) return (2, Phase.Active);
        if (_state == State.Wave3Prepare) return (3, Phase.Prepare);
        if (_state == State.Wave3Active) return (3, Phase.Active);
        if (_state == State.Wave4StackOnly) return (4, Phase.StackOnly);
        if (_state == State.FinalSW) return (4, Phase.Final);
        return (0, Phase.None);
    }

    private (Vector2 pos, string text) MeteorToNav(MeteorSpot spot, int wave)
    {
        var dir = Dir(spot);
        var pos = spot switch
        {
            MeteorSpot.SouthWestAlt1 => PosDir(dir, _distSWAlt1),
            MeteorSpot.SouthWestAlt2 => PosDir(dir, _distSWAlt2),
            _ => PosDir(dir, _distMeteor)
        };

        var txt = $"Meteor{wave} {Short(spot)}";
        return (pos, txt);
    }

    private Vector2 CornerToPos(Corner c)
    {
        return PosDir(Dir(c), _distStack);
    }

    private void ApplyCommonStyle(Element e, uint color)
    {
        e.color = color;
        e.overlayBGColor = 0xFF000000;
        e.overlayTextColor = 0xFFFFFFFF;
        e.Enabled = true;
    }

    private void SetElement(Element e, Vector2 xz, string text)
    {
        e.refX = xz.X;
        e.refY = xz.Y;
        e.refZ = 0f;
        e.overlayText = text;
        e.Enabled = true;
    }

    private string Short(Corner c)
    {
        return c switch
        {
            Corner.NorthWest => "NW",
            Corner.NorthEast => "NE",
            _ => "SE"
        };
    }

    private string Short(MeteorSpot s)
    {
        return s switch
        {
            MeteorSpot.NorthWest => "NW",
            MeteorSpot.NorthEast => "NE",
            MeteorSpot.SouthEast => "SE",
            MeteorSpot.SouthWest => "SW",
            MeteorSpot.SouthWestAlt1 => "SW1",
            _ => "SW2"
        };
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text("■ 設定 / Settings");
        ImGui.Separator();
        ImGuiEx.EnumCombo("My Role", ref C.MyRole);

        if (C.MyRole == Role.Tether)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "対応していません。/ Not supported.");
            ImGuiEx.EnumCombo("Tether Origin", ref C.MyTetherOrigin);
        }
        else
        {
            ImGuiEx.EnumCombo("Meteor Wave", ref C.MyMeteorWave);
            ImGuiEx.EnumCombo("Meteor Spot", ref C.MyMeteorSpot);
        }

        ImGui.Separator();
        ImGuiEx.Text("Stack corners");
        ImGuiEx.EnumCombo("Stack 1", ref C.Stack1);
        ImGuiEx.EnumCombo("Stack 2", ref C.Stack2);
        ImGuiEx.EnumCombo("Stack 3", ref C.Stack3);
        ImGuiEx.EnumCombo("Stack 4", ref C.Stack4);

        ImGui.Separator();
        ImGuiEx.Text("Gradient (2 colors)");
        ImGui.ColorEdit4("Color A", ref C.GradientA, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Color B", ref C.GradientB, ImGuiColorEditFlags.NoInputs);

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Guide (JP)"))
        {
            ImGui.BulletText("頭割りの位置は 1〜4回目すべて選択してください。");
            ImGui.BulletText("例：1回目の頭割りを北東で受ける場合は NorthEast を選びます。");
            ImGui.BulletText("隕石を誘導する場合は、MyRole を Meteor に変更し、何回目（Wave）に、どの方角へ置くかを選択してください。");
            ImGui.BulletText("例：2回目の隕石を南西に置きたい場合は、Wave2 を選び、配置先を SouthWest / SouthWestAlt1 / SouthWestAlt2 のいずれかに設定します。");
            ImGui.BulletText("なお、南西の配置位置は SouthWest < SouthWestAlt1 < SouthWestAlt2 の順に外周寄り（外側）になります。");
        }

        if (ImGui.CollapsingHeader("Guide (EN)"))
        {
            ImGui.BulletText(" Please select the stack position for all four stacks (Stack 1–4).");
            ImGui.BulletText(" Example: If you take the first stack at the northeast, choose NorthEast.");
            ImGui.BulletText("  If you are baiting/placing a meteor, set MyRole to Meteor, then choose which wave (1–3) and which direction you will place it.");
            ImGui.BulletText(" Example: If you want to place the second meteor to the southwest, select Wave2 and then choose one of SouthWest / SouthWestAlt1 / SouthWestAlt2.");
            ImGui.BulletText("  For the southwest options, the placement is farther out in this order: SouthWest < SouthWestAlt1 < SouthWestAlt2.");
        }

        if (ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"CastStartId: {_castMeteorainStart}");
            ImGui.Text($"VFX: {_vfxLockon}");
            ImGui.Text($"ActionPlace: {_actionMeteorPlace}  AfterWave3: {_actionAfterWave3}  End: {_actionEnd}");
        }
    }

    private enum State
    {
        Idle = 0,
        Wave1Prepare,
        Wave1Active,
        Wave2Prepare,
        Wave2Active,
        Wave3Prepare,
        Wave3Active,
        Wave4StackOnly,
        FinalSW,
        Done
    }

    public class Config : IEzConfig
    {
        public Vector4 GradientA = ImGuiColors.DalamudYellow;
        public Vector4 GradientB = ImGuiColors.DalamudRed;
        public MeteorSpot MyMeteorSpot = MeteorSpot.NorthWest;

        public MeteorWave MyMeteorWave = MeteorWave.Wave1;

        public Role MyRole = Role.Meteor;

        public TetherOrigin MyTetherOrigin = TetherOrigin.NorthWest;

        public Corner Stack1 = Corner.NorthEast;
        public Corner Stack2 = Corner.NorthWest;
        public Corner Stack3 = Corner.SouthEast;
        public Corner Stack4 = Corner.NorthEast;
    }

    private enum Phase
    {
        None,
        Prepare,
        Active,
        StackOnly,
        Final
    }

    private Vector2 Dir(Corner c)
    {
        return c switch
        {
            Corner.NorthWest => Dir(_posNW),
            Corner.NorthEast => Dir(_posNE),
            _ => Dir(_posSE)
        };
    }

    private Vector2 Dir(MeteorSpot s)
    {
        return s switch
        {
            MeteorSpot.NorthWest => Dir(_posNW),
            MeteorSpot.NorthEast => Dir(_posNE),
            MeteorSpot.SouthEast => Dir(_posSE),
            _ => Dir(_posSW)
        };
    }

    private Vector2 Dir(Vector2 pos)
    {
        return Vector2.Normalize(pos - _center);
    }

    private Vector2 PosDir(Vector2 dir, float dist)
    {
        return _center + dir * dist;
    }
}

