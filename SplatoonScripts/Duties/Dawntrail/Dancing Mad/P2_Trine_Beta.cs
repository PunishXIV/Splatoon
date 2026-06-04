using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.PartyFunctions;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate;

public class P2_Trine_Beta : SplatoonScript
{
    private const uint TerritoryDancingMadUltimate = 1363;
    private const uint TrineSetup = 47839;
    private const uint TrineWave = 47840;
    private const uint WingCleaveLeftOrRight = 47821;
    private const uint WingCleaveOtherSide = 47822;
    private const uint TankbusterCast = 50311;
    private const uint TankbusterHit = 47823;
    private const uint UltimateEmbrace = 49740;
    private const uint DefinitionOfInsanity = 47842;

    private static readonly int[] ExpectedWaveCounts = [9, 3, 9];

    private static readonly InternationalString MainDescriptionText = new()
    {
        En =
            "FFLogs-only Trine beta. It starts on Trine, asks you to dodge Wings by the visible glowing wing, collects Trine source positions from action 47840, and after the first wave sends only you to a configured tank or party destination. If enabled, the destination snaps to the nearest first-wave Trine source position. Wing side and the exact single/double route are not fully provable from FFLogs alone.",
        Jp =
            "FFLogsのみで組んだトラインβです。トライン開始で起動し、羽は実際に光っている側を見て避ける指示を出します。47840の発生源座標を集め、1回目の波後に自分だけをタンク用またはパーティ用の設定座標へ誘導します。有効時は1回目のトライン発生源のうち設定座標に最も近い点へスナップします。羽の左右と単発/二連ルートの完全判定はFFLogsだけでは未確定です。"
    };

    private static readonly InternationalString MainSettingsHeaderText = new()
    {
        En = "Main settings",
        Jp = "主設定"
    };

    private static readonly InternationalString CoordinateHeaderText = new()
    {
        En = "Destination coordinates",
        Jp = "誘導座標"
    };

    private static readonly InternationalString CoordinateDescriptionText = new()
    {
        En =
            "Coordinates are absolute arena X/Z positions. The beta defaults are based on the sampled FFLogs/RaidPlan pattern and should be adjusted for your strategy. With snapping enabled, the script uses these coordinates only as references and moves the marker to the nearest observed first-wave Trine source.",
        Jp =
            "座標は絶対のフィールドX/Zです。β初期値は調査したFFLogs/RaidPlanパターンに基づくため、固定の処理法に合わせて調整してください。スナップ有効時はこの座標を基準点として使い、1回目に観測したトライン発生源の最も近い点へマーカーを移します。"
    };

    private static readonly InternationalString PriorityDescriptionText = new()
    {
        En =
            "Priority is used only to distinguish MT from OT for the tankbuster text. Non-tanks ignore this priority.",
        Jp =
            "優先順位はタンク強攻撃のMT/ST表示にだけ使います。タンク以外には影響しません。"
    };

    private static readonly InternationalString DisplayTextHeaderText = new()
    {
        En = "Display text",
        Jp = "表示テキスト"
    };

    private static readonly InternationalString DisplayTextDescriptionText = new()
    {
        En = "Edit the instructions and marker labels shown on screen.",
        Jp = "画面に表示する指示文とマーカー名を編集します。"
    };

    private static readonly InternationalString SnapDestinationSettingText = new()
    {
        En = "Snap destination to first-wave Trine source",
        Jp = "誘導先を1回目トライン発生源へスナップ"
    };

    private static readonly InternationalString TankReferenceXText = new()
    {
        En = "Tank reference X",
        Jp = "タンク基準X"
    };

    private static readonly InternationalString TankReferenceZText = new()
    {
        En = "Tank reference Z",
        Jp = "タンク基準Z"
    };

    private static readonly InternationalString PartyReferenceXText = new()
    {
        En = "Party reference X",
        Jp = "パーティ基準X"
    };

    private static readonly InternationalString PartyReferenceZText = new()
    {
        En = "Party reference Z",
        Jp = "パーティ基準Z"
    };

    private readonly List<Vector3> _currentWavePositions = [];
    private readonly HashSet<uint> _currentWaveSources = [];
    private readonly List<Vector3> _firstWavePositions = [];

    private bool _active;
    private bool _hasDestination;
    private bool _tankbusterStarted;
    private int _currentWaveIndex;
    private int _noSourceSignals;
    private string _currentInstruction = "";
    private Vector3 _myDestination = Vector3.Zero;

    public override HashSet<uint>? ValidTerritories { get; } = [TerritoryDancingMadUltimate];
    public override Metadata Metadata => new(1, "Garume");

    private Config C => Controller.GetConfig<Config>();
    private IPlayerCharacter BasePlayer => Controller.BasePlayer;

    public override void OnSetup()
    {
        Controller.RegisterElement("SelfInstruction", new Element(0)
        {
            Enabled = false,
            radius = 0.0f,
            thicc = 0.0f,
            overlayBGColor = 0xC8000000,
            overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 3.0f,
            overlayFScale = 3.0f,
            overlayText = ""
        });

        Controller.RegisterElement("Destination", new Element(0)
        {
            Enabled = false,
            radius = 1.8f,
            thicc = 6.0f,
            fillIntensity = 0.25f,
            color = 0xC800BFFF,
            tether = true,
            overlayBGColor = 0xC8000000,
            overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 2.4f,
            overlayFScale = 1.6f,
            overlayText = ""
        });
    }

    public override void OnCombatStart()
    {
        ResetState();
    }

    public override void OnReset()
    {
        ResetState();
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence,
                DirectorUpdateCategory.Wipe))
            ResetState();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId is UltimateEmbrace or DefinitionOfInsanity)
        {
            ClearActiveState();
            return;
        }

        if (castId == TrineSetup)
        {
            StartTrine();
            return;
        }

        if (!_active) return;

        if (castId is WingCleaveLeftOrRight or WingCleaveOtherSide)
        {
            _currentInstruction = C.WingSafeSideText.Get();
            return;
        }

        if (castId == TankbusterCast)
        {
            _tankbusterStarted = true;
            TryCompleteCurrentWave(partialAllowed: true);
            _currentInstruction = GetTankbusterInstruction();
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        var actionId = set.Action?.RowId ?? 0;
        if (actionId == TankbusterHit)
        {
            ClearActiveState();
            return;
        }

        if (actionId != TrineWave) return;

        if (!_active)
            StartTrine();

        AddCurrentWaveSource(set);
        TryCompleteCurrentWave(partialAllowed: false);
    }

    public override void OnUpdate()
    {
        ApplyDisplay();
    }

    public override void OnSettingsDraw()
    {
        C.EnsureDefaults();

        ImGui.TextWrapped(MainDescriptionText.Get());
        ImGui.Separator();

        if (ImGui.CollapsingHeader(MainSettingsHeaderText.Get(), ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            ImGui.Checkbox(SnapDestinationSettingText.Get(), ref C.SnapDestinationToFirstWave);
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader(CoordinateHeaderText.Get(), ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            ImGui.TextWrapped(CoordinateDescriptionText.Get());
            ImGui.SetNextItemWidth(160f);
            ImGui.InputFloat(TankReferenceXText.Get(), ref C.TankReferenceX);
            ImGui.SetNextItemWidth(160f);
            ImGui.InputFloat(TankReferenceZText.Get(), ref C.TankReferenceZ);
            ImGui.SetNextItemWidth(160f);
            ImGui.InputFloat(PartyReferenceXText.Get(), ref C.PartyReferenceX);
            ImGui.SetNextItemWidth(160f);
            ImGui.InputFloat(PartyReferenceZText.Get(), ref C.PartyReferenceZ);
            ImGui.Unindent();
        }

        ImGui.Separator();
        ImGui.TextWrapped(PriorityDescriptionText.Get());
        C.PriorityData.Draw();

        if (ImGui.CollapsingHeader(DisplayTextHeaderText.Get()))
        {
            ImGui.Indent();
            ImGui.TextWrapped(DisplayTextDescriptionText.Get());
            DrawInternationalString("Start middle", C.StartMiddleText);
            DrawInternationalString("Wing safe side", C.WingSafeSideText);
            DrawInternationalString("Tank after first", C.TankAfterFirstText);
            DrawInternationalString("Party after first", C.PartyAfterFirstText);
            DrawInternationalString("MT tankbuster", C.MainTankBusterText);
            DrawInternationalString("OT tankbuster", C.OffTankBusterText);
            DrawInternationalString("Tank tankbuster", C.GenericTankBusterText);
            DrawInternationalString("Party tankbuster", C.PartyTankbusterText);
            DrawInternationalString("Missing source", C.MissingSourceText);
            DrawInternationalString("Tank destination overlay", C.TankDestinationOverlayText);
            DrawInternationalString("Party destination overlay", C.PartyDestinationOverlayText);
            ImGui.Unindent();
        }
    }

    private static void DrawInternationalString(string label, InternationalString text)
    {
        ImGui.PushID(label);
        ImGui.Text(label);
        ImGui.SameLine();
        var current = text.Get();
        text.ImGuiEdit(ref current);
        ImGui.PopID();
    }

    private void StartTrine()
    {
        ClearActiveState();
        _active = true;
        _currentInstruction = C.StartMiddleText.Get();
    }

    private void AddCurrentWaveSource(ActionEffectSet set)
    {
        var source = set.Source;
        if (source == null)
        {
            _noSourceSignals++;
            return;
        }

        if (!_currentWaveSources.Add(source.EntityId))
            return;

        var position = source.Position;
        if (_currentWavePositions.Any(existing => Vector3.DistanceSquared(existing, position) < 0.04f))
            return;

        _currentWavePositions.Add(position);
    }

    private void TryCompleteCurrentWave(bool partialAllowed)
    {
        if (_currentWaveIndex < 0 || _currentWaveIndex >= ExpectedWaveCounts.Length)
            return;

        var observedCount = _currentWaveSources.Count + _noSourceSignals;
        if (!partialAllowed && observedCount < ExpectedWaveCounts[_currentWaveIndex])
            return;

        if (partialAllowed && observedCount == 0 && _currentWavePositions.Count == 0)
            return;

        CompleteCurrentWave();
    }

    private void CompleteCurrentWave()
    {
        var completedWave = _currentWaveIndex;
        if (completedWave == 0)
        {
            _firstWavePositions.Clear();
            _firstWavePositions.AddRange(_currentWavePositions);
            SolveDestinationAfterFirstWave();
        }
        else if (!_tankbusterStarted)
        {
            _currentInstruction = IsTank() ? C.TankAfterFirstText.Get() : C.PartyAfterFirstText.Get();
        }

        _currentWaveIndex++;
        _currentWavePositions.Clear();
        _currentWaveSources.Clear();
        _noSourceSignals = 0;
    }

    private void SolveDestinationAfterFirstWave()
    {
        var tank = IsTank();
        var reference = tank
            ? new Vector3(C.TankReferenceX, 0.0f, C.TankReferenceZ)
            : new Vector3(C.PartyReferenceX, 0.0f, C.PartyReferenceZ);

        var destination = reference;
        if (C.SnapDestinationToFirstWave && _firstWavePositions.Count > 0)
            destination = _firstWavePositions.MinBy(position => Vector3.DistanceSquared(position, reference));

        _myDestination = destination;
        _hasDestination = true;
        _currentInstruction = _firstWavePositions.Count == 0 && C.SnapDestinationToFirstWave
            ? C.MissingSourceText.Get()
            : tank
                ? C.TankAfterFirstText.Get()
                : C.PartyAfterFirstText.Get();
    }

    private string GetTankbusterInstruction()
    {
        return GetTankbusterSlot() switch
        {
            TankbusterSlot.MainTank => C.MainTankBusterText.Get(),
            TankbusterSlot.OffTank => C.OffTankBusterText.Get(),
            TankbusterSlot.Tank => C.GenericTankBusterText.Get(),
            _ => C.PartyTankbusterText.Get()
        };
    }

    private TankbusterSlot GetTankbusterSlot()
    {
        var me = BasePlayer;
        if (me == null || me.GetRole() != CombatRole.Tank)
            return TankbusterSlot.NonTank;

        var priority = C.PriorityData.GetPlayers(_ => true);
        if (priority is { Count: > 0 })
        {
            var tanks = priority
                .Where(member => member.IGameObject is IPlayerCharacter player && player.GetRole() == CombatRole.Tank)
                .Select(member => (IPlayerCharacter)member.IGameObject)
                .ToList();

            var index = tanks.FindIndex(player => player.EntityId == me.EntityId);
            if (index == 0) return TankbusterSlot.MainTank;
            if (index == 1) return TankbusterSlot.OffTank;
        }

        var partyTanks = Controller.GetPartyMembers()
            .OfType<IPlayerCharacter>()
            .Where(player => player.GetRole() == CombatRole.Tank)
            .OrderBy(player => player.EntityId)
            .ToList();

        var partyIndex = partyTanks.FindIndex(player => player.EntityId == me.EntityId);
        return partyIndex switch
        {
            0 => TankbusterSlot.MainTank,
            1 => TankbusterSlot.OffTank,
            _ => TankbusterSlot.Tank
        };
    }

    private bool IsTank()
    {
        return BasePlayer?.GetRole() == CombatRole.Tank;
    }

    private void ApplyDisplay()
    {
        Controller.GetRegisteredElements().Each(element => element.Value.Enabled = false);

        var me = BasePlayer;
        if (me == null) return;

        if (Controller.TryGetElementByName("SelfInstruction", out var instruction))
        {
            instruction.Enabled = _active && !string.IsNullOrWhiteSpace(_currentInstruction);
            instruction.SetRefPosition(me.Position);
            instruction.overlayText = _currentInstruction;
        }

        if (_active && _hasDestination && Controller.TryGetElementByName("Destination", out var destination))
        {
            destination.Enabled = true;
            destination.SetRefPosition(_myDestination);
            destination.color = IsTank() ? 0xC800BFFF : 0xC8FF00FF;
            destination.overlayText = IsTank()
                ? C.TankDestinationOverlayText.Get()
                : C.PartyDestinationOverlayText.Get();
        }
    }

    private void ResetState()
    {
        ClearActiveState();
    }

    private void ClearActiveState()
    {
        _active = false;
        _hasDestination = false;
        _tankbusterStarted = false;
        _currentWaveIndex = 0;
        _noSourceSignals = 0;
        _currentInstruction = "";
        _myDestination = Vector3.Zero;
        _currentWavePositions.Clear();
        _currentWaveSources.Clear();
        _firstWavePositions.Clear();
        Controller.GetRegisteredElements().Each(element => element.Value.Enabled = false);
    }

    private enum TankbusterSlot
    {
        NonTank,
        MainTank,
        OffTank,
        Tank
    }

    public sealed class Config : IEzConfig
    {
        public InternationalString GenericTankBusterText = new()
        {
            En = "Wings: tank route",
            Jp = "羽強攻撃: タンク処理"
        };

        public InternationalString MainTankBusterText = new()
        {
            En = "Wings: MT close to boss",
            Jp = "羽強攻撃: MTはボス近く"
        };

        public InternationalString MissingSourceText = new()
        {
            En = "Trine source missing: use configured route",
            Jp = "トライン座標未取得: 設定ルートへ"
        };

        public InternationalString OffTankBusterText = new()
        {
            En = "Wings: OT to edge",
            Jp = "羽強攻撃: STは外周"
        };

        public InternationalString PartyAfterFirstText = new()
        {
            En = "Trine: party route, move after first",
            Jp = "トライン: パーティルート、1回目後に移動"
        };

        public InternationalString PartyDestinationOverlayText = new()
        {
            En = "Party",
            Jp = "パーティ"
        };

        public InternationalString PartyTankbusterText = new()
        {
            En = "Avoid Wings bait, stay away from boss",
            Jp = "羽強攻撃を避ける、ボスから離れる"
        };

        public float PartyReferenceX = 91.34f;
        public float PartyReferenceZ = 105.0f;

        public PriorityData PriorityData = new()
        {
            Name = "Trine MT/OT priority",
            Description = "Default: T1 then T2. Used only for MT/OT tankbuster text.",
            PriorityLists =
            [
                new PriorityList
                {
                    IsRole = true,
                    List =
                    [
                        new JobbedPlayer { Role = RolePosition.T1 },
                        new JobbedPlayer { Role = RolePosition.T2 }
                    ]
                }
            ]
        };

        public bool SnapDestinationToFirstWave = true;

        public InternationalString StartMiddleText = new()
        {
            En = "Trine: start middle",
            Jp = "トライン: 中央開始"
        };

        public InternationalString TankAfterFirstText = new()
        {
            En = "Trine: tank route, move after first",
            Jp = "トライン: タンクルート、1回目後に移動"
        };

        public InternationalString TankDestinationOverlayText = new()
        {
            En = "Tank",
            Jp = "タンク"
        };

        public float TankReferenceX = 108.66f;
        public float TankReferenceZ = 85.0f;

        public InternationalString WingSafeSideText = new()
        {
            En = "Wings: dodge by visible glowing wing",
            Jp = "羽: 光っている羽を見て安置へ"
        };

        public void EnsureDefaults()
        {
            GenericTankBusterText ??= new InternationalString { En = "Wings: tank route", Jp = "羽強攻撃: タンク処理" };
            MainTankBusterText ??= new InternationalString { En = "Wings: MT close to boss", Jp = "羽強攻撃: MTはボス近く" };
            MissingSourceText ??= new InternationalString { En = "Trine source missing: use configured route", Jp = "トライン座標未取得: 設定ルートへ" };
            OffTankBusterText ??= new InternationalString { En = "Wings: OT to edge", Jp = "羽強攻撃: STは外周" };
            PartyAfterFirstText ??= new InternationalString { En = "Trine: party route, move after first", Jp = "トライン: パーティルート、1回目後に移動" };
            PartyDestinationOverlayText ??= new InternationalString { En = "Party", Jp = "パーティ" };
            PartyTankbusterText ??= new InternationalString { En = "Avoid Wings bait, stay away from boss", Jp = "羽強攻撃を避ける、ボスから離れる" };
            PriorityData ??= new PriorityData();
            StartMiddleText ??= new InternationalString { En = "Trine: start middle", Jp = "トライン: 中央開始" };
            TankAfterFirstText ??= new InternationalString { En = "Trine: tank route, move after first", Jp = "トライン: タンクルート、1回目後に移動" };
            TankDestinationOverlayText ??= new InternationalString { En = "Tank", Jp = "タンク" };
            WingSafeSideText ??= new InternationalString { En = "Wings: dodge by visible glowing wing", Jp = "羽: 光っている羽を見て安置へ" };
        }
    }
}
