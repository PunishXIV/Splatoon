using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
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

    private const float _distStack = 10f; // 配置距離（頭割り）
    private const float _distMeteor = 8f; // 配置距離（通常隕石）
    private const float _distFinal = 22.0f; // 最終隕石配置距離
    private const float _distSWAlt1 = 14.0f; // SW 近め配置距離
    private const float _distSWAlt2 = 20.0f; // SW 遠め配置距離

    private readonly uint _actionAfterWave3 = 46134;
    private readonly uint _actionEnd = 46139;
    private readonly uint _actionMeteorPlace = 46133;
    private readonly uint _castMeteorainStart = 46132;

    private readonly Vector2 _center = new(100f, 100f);

    private readonly float _northPullDist = 18f;
    private readonly Vector2 _posNE = new(107f, 93f);
    private readonly Vector2 _posNW = new(93f, 93f);
    private readonly Vector2 _posSE = new(107f, 107f);
    private readonly Vector2 _posSW = new(93f, 107f);
    private readonly float _southPullDist = 18f;

    private readonly HashSet<(uint source, uint target)> _tetherMaps = new();

    private readonly string _vfxLockon = "vfx/lockon/eff/lockon8_t0w.avfx";
    private uint _correctSources;

    private Element? _eNav;
    private Element? _eTether;

    private State _state = State.Idle;
    private int _tetherCount;

    public override Metadata Metadata => new(3, "Garume");
    public override HashSet<uint>? ValidTerritories { get; } = [1325];

    private Config C => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        Controller.RegisterElement("Nav", new Element(0)
        {
            radius = 2f,
            thicc = 10f,
            overlayVOffset = 3f,
            overlayFScale = 2.6f,
            tether = true
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
        _tetherCount = 0;
        _tetherMaps.Clear();
        _correctSources = 0;
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

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (data3 != 356) return;

        if (_state != State.Idle && _state != State.Done)
        {
            _tetherCount++;
            _tetherMaps.Add((source, target));
            if (_tetherCount == 2)
            {
                var desiredDirections = C.UseSecondMeteorDirection
                    ? new[]
                    {
                        C.MyTetherOrigin,
                        C.MySecondTetherOrigin
                    }
                    : new[] { C.MyTetherOrigin };
                var normalizedDirections = desiredDirections.Select(Dir).ToArray();
                foreach (var (s, t) in _tetherMaps)
                {
                    var targetObject = t.GetObject();
                    var targetDirection = Dir(targetObject!.Position.ToVector2());

                    if (normalizedDirections.Any(d => Vector2.Dot(d, targetDirection) > 0.95f)) _correctSources　= s;
                }
            }
        }
    }

    public override void OnTetherRemoval(uint source, uint data2, uint data3, uint data5)
    {
        if (data2 != 356) return;

        _tetherMaps.RemoveWhere(x => x.source == source);
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action == null) return;

        var actionId = set.Action.Value.RowId;

        if (actionId == _actionMeteorPlace)
        {
            _tetherCount = 0;
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
        var (wave, phase) = GetWavePhase();

        if (C.MyRole == Role.Tether && (wave == 2 || wave == 3 || wave == 4))
        {
            ApplyCommonStyle(_eTether, grad);
            var t = GetTetherInstruction();
            _eTether.SetOffPosition(new Vector3(t.sourcePos.X, 0f, t.sourcePos.Y));
            _eTether.SetRefPosition(new Vector3(t.targetPos.X, 0f, t.targetPos.Y));
        }
        else
        {
            ApplyCommonStyle(_eNav, grad);

            var nav = GetNavInstruction(wave, phase);
            SetElement(_eNav, nav.pos, nav.text);
        }
    }

    private unsafe (Vector2 sourcePos, Vector2 targetPos, string text) GetTetherInstruction()
    {
        var sourceObj = _correctSources.GetObject() as IBattleNpc;
        if (sourceObj == null)
            return (Vector2.Zero, Vector2.Zero, "");
        var targetObj = sourceObj.Struct()->Vfx.Tethers.ToArray().ToArray().First().TargetId.ObjectId.GetObject();

        var text = targetObj != null && targetObj.EntityId == Controller.BasePlayer.EntityId
            ? "Correct !!!"
            : "Pick this";
        if (targetObj == null)
            return (Vector2.Zero, Vector2.Zero, text);

        var sourcePos = sourceObj.Position.ToVector2();
        var targetPos = targetObj.Position.ToVector2();
        return (sourcePos, targetPos, text);
    }

    private (Vector2 pos, string text) GetNavInstruction(int wave, Phase phase)
    {
        if (phase == Phase.Final)
            return (PosDir(Dir(MeteorSpot.SouthWest), _distFinal), "Final");

        var stackCorner = wave switch
        {
            1 => C.Stack1,
            2 => C.Stack2,
            _ => C.Stack3
        };
        var p = CornerToPos(stackCorner);
        if (C.MyRole == Role.Tether && wave == 1)
            return (p, "Stack1");
        if (phase == Phase.StackOnly)
        {
            p = CornerToPos(C.Stack4);
            return (p, "Stack4");
        }

        var isMyMeteorWave = C.MyMeteorWave == wave switch
        {
            1 => MeteorWave.Wave1,
            2 => MeteorWave.Wave2,
            _ => MeteorWave.Wave3
        };
        var isPrepareMeteorWave = C.MyMeteorWave == (wave == 1 ? MeteorWave.Wave2 : MeteorWave.Wave3);
        if (phase == Phase.Prepare)
        {
            if (isMyMeteorWave)
                return (_center, $"Wave{wave}: {C.NearBoss.Get()}");
            return (p, "Stack");
        }

        if (isMyMeteorWave) return MeteorToNav(C.MyMeteorSpot, wave);

        if (isPrepareMeteorWave) return (p, $"Wave{wave}: {C.PrepareNearBoss.Get()}");

        return (p, "Stack");
    }

    private (int wave, Phase phase) GetWavePhase()
    {
        return _state switch
        {
            State.Wave1Prepare => (1, Phase.Prepare),
            State.Wave1Active => (1, Phase.Active),
            State.Wave2Prepare => (2, Phase.Prepare),
            State.Wave2Active => (2, Phase.Active),
            State.Wave3Prepare => (3, Phase.Prepare),
            State.Wave3Active => (3, Phase.Active),
            State.Wave4StackOnly => (4, Phase.StackOnly),
            State.FinalSW => (4, Phase.Final),
            _ => (0, Phase.None)
        };
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
            ImGuiEx.EnumCombo("Meteor Direction", ref C.MyTetherOrigin);
            ImGuiEx.HelpMarker("Please select which direction’s meteor tether to track.\nどの方角のメテオについたテザーを見るか選択してください");
            ImGui.Checkbox("Use Second Direction", ref C.UseSecondMeteorDirection);
            ImGuiEx.EnumCombo("Second Meteor Direction", ref C.MySecondTetherOrigin);
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

        var prepareNearBoss = C.PrepareNearBoss.Get();
        C.PrepareNearBoss.ImGuiEdit(ref prepareNearBoss);
        var nearBoss = C.NearBoss.Get();
        C.NearBoss.ImGuiEdit(ref nearBoss);

        if (ImGui.CollapsingHeader("Guide (JP)"))
        {
            ImGui.BulletText("頭割りの位置は 1〜4回目すべて選択してください。");
            ImGui.BulletText("例：1回目の頭割りを北東で受ける場合は NorthEast を選びます。");
            ImGui.BulletText("隕石を誘導する場合は、MyRole を Meteor に変更し、何回目（Wave）に、どの方角へ置くかを選択してください。");
            ImGui.BulletText(
                "例：2回目の隕石を南西に置きたい場合は、Wave2 を選び、配置先を SouthWest / SouthWestAlt1 / SouthWestAlt2 のいずれかに設定します。");
            ImGui.BulletText("なお、南西の配置位置は SouthWest < SouthWestAlt1 < SouthWestAlt2 の順に外周寄り（外側）になります。");
        }

        if (ImGui.CollapsingHeader("Guide (EN)"))
        {
            ImGui.BulletText(" Please select the stack position for all four stacks (Stack 1–4).");
            ImGui.BulletText(" Example: If you take the first stack at the northeast, choose NorthEast.");
            ImGui.BulletText(
                " If you are baiting/placing a meteor, set MyRole to Meteor, then choose which wave (1–3) and which direction you will place it.");
            ImGui.BulletText(
                " Example: If you want to place the second meteor to the southwest, select Wave2 and then choose one of SouthWest / SouthWestAlt1 / SouthWestAlt2.");
            ImGui.BulletText(
                " For the southwest options, the placement is farther out in this order: SouthWest < SouthWestAlt1 < SouthWestAlt2.");
        }

        if (ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"CastStartId: {_castMeteorainStart}");
            ImGui.Text($"VFX: {_vfxLockon}");
            ImGui.Text($"ActionPlace: {_actionMeteorPlace}  AfterWave3: {_actionAfterWave3}  End: {_actionEnd}");
        }
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
        public MeteorSpot MySecondTetherOrigin = MeteorSpot.SouthEast;
        public MeteorSpot MyTetherOrigin = MeteorSpot.NorthEast;

        public InternationalString NearBoss = new()
        {
            En = "Near boss",
            Jp = "足元へ"
        };

        public InternationalString PrepareNearBoss = new()
        {
            En = "Next, near boss",
            Jp = "次はボスの足元へ"
        };

        public Corner Stack1 = Corner.NorthEast;
        public Corner Stack2 = Corner.NorthWest;
        public Corner Stack3 = Corner.SouthEast;
        public Corner Stack4 = Corner.NorthEast;
        public bool UseSecondMeteorDirection;
    }

    private enum Phase
    {
        None,
        Prepare,
        Active,
        StackOnly,
        Final
    }
}
