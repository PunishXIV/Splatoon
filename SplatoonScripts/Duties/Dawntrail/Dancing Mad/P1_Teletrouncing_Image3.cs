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
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate;

public class P1_Teletrouncing_Image3 : SplatoonScript
{
    private const uint TerritoryDancingMadUltimate = 1363;
    private const uint TeletrouncingCast = 47801;
    private const uint GravenImage = 48370;
    private const uint MysteryMagic = 47764;
    private const uint UltimateEmbrace = 49740;
    private const uint NorthStatusId = 4876;
    private const uint NorthEastStatusId = 5082;
    private const uint EastStatusId = 4878;
    private const uint SouthEastStatusId = 5079;
    private const uint SouthStatusId = 4877;
    private const uint SouthWestStatusId = 5081;
    private const uint WestStatusId = 4879;
    private const uint NorthWestStatusId = 5080;
    private const float SourceSideDeadzone = 1.0f;

    private static readonly InternationalString MainSettingsHeaderText = new()
    {
        En = "Main settings",
        Jp = "主設定"
    };

    private static readonly InternationalString DisplayTextHeaderText = new()
    {
        En = "Display text",
        Jp = "表示テキスト"
    };

    private static readonly InternationalString MainDescriptionText = new()
    {
        En =
            "This P1 late-phase helper shows only your own Tele-trouncing and Graven Image 3 destination. For Tele-trouncing, it reads your two arrow debuffs. Matching arrows show the fixed arrow direction, first at the inner radius and then at the outer radius after the arrow debuff is removed. Different arrows show the configured X direction. For Graven Image 3, your tether is classified as sleep or confusion and shown on the configured cross direction at the inner or outer radius.",
        Jp =
            "P1後半のずびずばテレポと神々の像3を自分向けに表示します。ずびずばテレポは自分の2つの矢印を見て、同じ矢印なら矢印方向へ固定表示します。最初は内側半径、矢印デバフが消えたら外側半径へ切り替えます。違う矢印なら設定したX字方向へ表示します。神々の像3は自分についた線を睡眠/混乱で判定し、睡眠なら内側十字、混乱なら外側十字へ表示します。"
    };

    private static readonly InternationalString DisplayTextDescriptionText = new()
    {
        En =
            "Edit text shown on screen. In Tele-trouncing messages, {0} is your arrow pair and {1} is the displayed direction.",
        Jp = "画面に表示する指示文を編集します。ずびずばテレポの文言では {0} が自分の矢印ペア、{1} が表示先の方向です。"
    };

    private static readonly InternationalString TeleXDirectionSettingText = new()
    {
        En = "Tele-trouncing: X direction",
        Jp = "ずびずば: X字方向"
    };

    private static readonly InternationalString ImageCrossDirectionSettingText = new()
    {
        En = "Image 3: cross direction",
        Jp = "神々3: 十字方向"
    };

    private static readonly InternationalString SameArrowInnerRadiusSettingText = new()
    {
        En = "Same-arrow inner radius",
        Jp = "同矢印: 内側半径"
    };

    private static readonly InternationalString SameArrowOuterRadiusSettingText = new()
    {
        En = "Same-arrow outer radius",
        Jp = "同矢印: 外側半径"
    };

    private static readonly InternationalString DifferentArrowXRadiusSettingText = new()
    {
        En = "Different-arrow X radius",
        Jp = "違う矢印: X字半径"
    };

    private static readonly InternationalString ImageInnerRadiusSettingText = new()
    {
        En = "Image 3 inner cross radius",
        Jp = "神々3: 内側十字半径"
    };

    private static readonly InternationalString ImageOuterRadiusSettingText = new()
    {
        En = "Image 3 outer cross radius",
        Jp = "神々3: 外側十字半径"
    };

    private static readonly Vector2 Center = new(100f, 100f);

    private static readonly Direction[] XDirections =
    [
        Direction.NorthEast,
        Direction.SouthEast,
        Direction.SouthWest,
        Direction.NorthWest
    ];

    private static readonly Direction[] CrossDirections =
    [
        Direction.North,
        Direction.East,
        Direction.South,
        Direction.West
    ];

    private static readonly string[] XDirectionLabels = ["NE", "SE", "SW", "NW"];
    private static readonly string[] CrossDirectionLabels = ["N", "E", "S", "W"];

    private readonly Dictionary<uint, DirectionSet> _directions = [];
    private readonly Dictionary<uint, LineAssignment> _lineAssignments = [];
    private readonly HashSet<uint> _sameArrowOuterPlayers = [];
    private readonly Dictionary<uint, TeleAssignment> _teleAssignments = [];
    private ActiveMechanic _active = ActiveMechanic.None;
    private string _currentInstruction = "";
    private int _gravenImageCount;

    public override HashSet<uint>? ValidTerritories { get; } = [TerritoryDancingMadUltimate];
    public override Metadata? Metadata => new(1, "Garume");

    private Config C => Controller.GetConfig<Config>();
    private IPlayerCharacter BasePlayer => Controller.BasePlayer;

    public override void OnSetup()
    {
        NormalizeStoredTextConfig();

        Controller.RegisterElement("SelfInstruction", new Element(0)
        {
            Enabled = false,
            radius = 0.0f,
            thicc = 0.0f,
            overlayBGColor = 0xC8000000,
            overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 3.0f,
            overlayFScale = 2.5f,
            overlayText = ""
        });

        Controller.RegisterElement("TeleDestination", new Element(0)
        {
            Enabled = false,
            radius = 1.4f,
            thicc = 5.0f,
            fillIntensity = 0.25f,
            color = 0xC800FFFF,
            tether = true,
            overlayBGColor = 0xC8000000,
            overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 2.2f,
            overlayFScale = 1.4f,
            overlayText = ""
        });

        Controller.RegisterElement("ImageDestination", new Element(0)
        {
            Enabled = false,
            radius = 1.4f,
            thicc = 5.0f,
            fillIntensity = 0.22f,
            color = 0xC800FF80,
            tether = true,
            overlayBGColor = 0xC8000000,
            overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 2.2f,
            overlayFScale = 1.4f,
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
        if (castId == UltimateEmbrace)
        {
            ResetState();
            return;
        }

        if (castId == MysteryMagic)
        {
            ClearActiveState();
            return;
        }

        if (castId == TeletrouncingCast)
        {
            StartTeletrouncing();
            return;
        }

        if (castId != GravenImage) return;

        _gravenImageCount++;
        if (_gravenImageCount != 3) return;

        StartImage3();
    }

    public override void OnGainBuffEffect(uint sourceId, Status status)
    {
        if (_active == ActiveMechanic.None) return;
        if (sourceId.GetObject() is not IPlayerCharacter player) return;

        var direction = DirectionFromStatus(status.StatusId);
        if (direction == Direction.None) return;

        RecordDirection(player.EntityId, direction, status.RemainingTime, $"status {status.StatusId}");
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status status)
    {
        if (_active != ActiveMechanic.Teletrouncing) return;

        var direction = DirectionFromStatus(status.StatusId);
        if (direction == Direction.None) return;

        BuildTeleAssignments();
        if (!_teleAssignments.TryGetValue(sourceId, out var assignment)) return;
        if (assignment.Kind != TeleAssignmentKind.SameArrowFixed) return;

        _sameArrowOuterPlayers.Add(sourceId);
        
        SolveTeletrouncing();
        ApplyDisplay();
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (_active != ActiveMechanic.Image3) return;
        if (!LooksLikeImageTether(data2, data3, data5)) return;
        if (target.GetObject() is not IPlayerCharacter targetPlayer) return;

        var sourceObject = source.GetObject();
        var sourcePosition = sourceObject?.Position ?? Vector3.Zero;
        var kind = ClassifySource(sourcePosition);

        _lineAssignments[targetPlayer.EntityId] = new LineAssignment(
            targetPlayer.Name.ToString(),
            targetPlayer.EntityId,
            kind);

        SolveImage3();
        ApplyDisplay();
    }

    public override void OnUpdate()
    {
        ApplyDisplay();
    }

    public override void OnSettingsDraw()
    {
        ImGui.TextWrapped(MainDescriptionText.Get(MainDescriptionText.En));
        ImGui.Separator();
        ImGui.TextUnformatted(MainSettingsHeaderText.Get(MainSettingsHeaderText.En));
        ImGui.Combo(GetLabel(TeleXDirectionSettingText), ref C.XDirectionIndex, XDirectionLabels,
            XDirectionLabels.Length);
        ImGui.Combo(GetLabel(ImageCrossDirectionSettingText), ref C.CrossDirectionIndex, CrossDirectionLabels,
            CrossDirectionLabels.Length);
        ImGui.InputFloat(GetLabel(SameArrowInnerRadiusSettingText), ref C.SameArrowInnerRadius);
        ImGui.InputFloat(GetLabel(SameArrowOuterRadiusSettingText), ref C.SameArrowOuterRadius);
        ImGui.InputFloat(GetLabel(DifferentArrowXRadiusSettingText), ref C.DifferentArrowXRadius);
        ImGui.InputFloat(GetLabel(ImageInnerRadiusSettingText), ref C.SleepSpreadRadius);
        ImGui.InputFloat(GetLabel(ImageOuterRadiusSettingText), ref C.ConfusionSpreadRadius);

        ImGui.Separator();
        DrawDisplayTextSettings();
    }

    private void DrawDisplayTextSettings()
    {
        if (!ImGui.CollapsingHeader(DisplayTextHeaderText.Get(DisplayTextHeaderText.En))) return;

        ImGui.Indent();
        ImGui.TextWrapped(DisplayTextDescriptionText.Get(DisplayTextDescriptionText.En));
        DrawInternationalString("Tele waiting for arrow", C.TeleWaitingForArrowText);
        DrawInternationalString("Tele waiting for second arrow", C.TeleWaitingForSecondArrowText);
        DrawInternationalString("Tele missing arrow", C.TeleMissingArrowText);
        DrawInternationalString("Tele same-arrow instruction", C.TeleSameArrowInstructionText);
        DrawInternationalString("Tele different-arrow instruction", C.TeleDifferentArrowInstructionText);
        DrawInternationalString("Tele fixed overlay", C.TeleFixedOverlayText);
        DrawInternationalString("Tele X overlay", C.TeleXOverlayText);
        DrawInternationalString("Image collecting lines", C.CollectingLinesText);
        DrawInternationalString("Image waiting for my line", C.WaitingForMyLineText);
        DrawInternationalString("Sleep instruction", C.SleepInstructionText);
        DrawInternationalString("Confusion instruction", C.ConfusionInstructionText);
        DrawInternationalString("Unknown line instruction", C.UnknownLineInstructionText);
        DrawInternationalString("Sleep overlay", C.SleepOverlayText);
        DrawInternationalString("Confusion overlay", C.ConfusionOverlayText);
        DrawInternationalString("North label", C.NorthLabelText);
        DrawInternationalString("Northeast label", C.NorthEastLabelText);
        DrawInternationalString("East label", C.EastLabelText);
        DrawInternationalString("Southeast label", C.SouthEastLabelText);
        DrawInternationalString("South label", C.SouthLabelText);
        DrawInternationalString("Southwest label", C.SouthWestLabelText);
        DrawInternationalString("West label", C.WestLabelText);
        DrawInternationalString("Northwest label", C.NorthWestLabelText);
        DrawInternationalString("Unknown direction label", C.UnknownDirectionLabelText);
        ImGui.Unindent();
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

    private static string GetLabel(InternationalString label)
    {
        return label.Get(label.En);
    }

    private void NormalizeStoredTextConfig()
    {
        if ((C.TeleDifferentArrowInstructionText.Jp ?? "").Contains("どこか", StringComparison.Ordinal))
            C.TeleDifferentArrowInstructionText.Jp = "矢印{0}: 違う矢印、{1}";

        if ((C.SleepInstructionText.Jp ?? "").Contains("どこか", StringComparison.Ordinal))
            C.SleepInstructionText.Jp = "内側十字";

        if ((C.ConfusionInstructionText.Jp ?? "").Contains("どこか", StringComparison.Ordinal))
            C.ConfusionInstructionText.Jp = "外側十字";

        if ((C.SleepInstructionText.En ?? "").Contains("inner cross positions", StringComparison.OrdinalIgnoreCase))
            C.SleepInstructionText.En = "INNER CROSS";

        if ((C.ConfusionInstructionText.En ?? "").Contains("outer cross positions", StringComparison.OrdinalIgnoreCase))
            C.ConfusionInstructionText.En = "OUTER CROSS";
    }

    private void StartTeletrouncing()
    {
        _active = ActiveMechanic.Teletrouncing;
        _directions.Clear();
        _teleAssignments.Clear();
        _lineAssignments.Clear();
        _sameArrowOuterPlayers.Clear();
        _currentInstruction = C.TeleWaitingForArrowText.Get();

        ReadCurrentStatuses();
        SolveTeletrouncing();
        ApplyDisplay();
    }

    private void StartImage3()
    {
        ReadCurrentStatuses();
        BuildTeleAssignments();

        _active = ActiveMechanic.Image3;
        _lineAssignments.Clear();
        _currentInstruction = C.CollectingLinesText.Get();
        ApplyDisplay();
    }

    private void ReadCurrentStatuses()
    {
        foreach (var member in FakeParty.Get().OfType<IPlayerCharacter>())
        foreach (var status in member.StatusList)
        {
            var direction = DirectionFromStatus(status.StatusId);
            if (direction != Direction.None)
                RecordDirection(member.EntityId, direction, status.RemainingTime, $"existing status {status.StatusId}");
        }
    }

    private void RecordDirection(uint playerId, Direction direction, float remainingTime, string signal)
    {
        if (!_directions.TryGetValue(playerId, out var current))
        {
            current = new DirectionSet();
            _directions[playerId] = current;
        }

        if (_active == ActiveMechanic.Teletrouncing)
            SolveTeletrouncing();
    }

    private void SolveTeletrouncing()
    {
        var me = BasePlayer;
        if (me == null) return;

        BuildTeleAssignments();

        if (!_directions.TryGetValue(me.EntityId, out var myDirections) || !myDirections.HasAny)
        {
            _currentInstruction = C.TeleMissingArrowText.Get();
            return;
        }

        if (!_teleAssignments.TryGetValue(me.EntityId, out var assignment))
        {
            _currentInstruction = C.TeleWaitingForSecondArrowText.Get();
            return;
        }

        var arrowLabel = DirectionPairLabel(myDirections);
        var slotLabel = assignment.Kind == TeleAssignmentKind.SameArrowFixed
            ? DirectionLabel(assignment.Slot)
            : DirectionLabel(GetSelectedXDirection());
        _currentInstruction = assignment.Kind == TeleAssignmentKind.SameArrowFixed
            ? FormatText(C.TeleSameArrowInstructionText, arrowLabel, slotLabel)
            : FormatText(C.TeleDifferentArrowInstructionText, arrowLabel, slotLabel);
    }

    private void BuildTeleAssignments()
    {
        _teleAssignments.Clear();
        if (_directions.Count == 0) return;

        foreach (var pair in _directions.Where(x => x.Value.IsComplete))
        {
            var first = pair.Value.First;
            var second = pair.Value.Second;

            if (first == second)
            {
                _teleAssignments[pair.Key] = new TeleAssignment(
                    pair.Key,
                    first,
                    TeleAssignmentKind.SameArrowFixed,
                    first);
                continue;
            }

            _teleAssignments[pair.Key] = new TeleAssignment(
                pair.Key,
                first,
                TeleAssignmentKind.DifferentArrowX,
                Direction.None);
        }
    }

    private void SolveImage3()
    {
        var me = BasePlayer;
        if (me == null) return;

        if (!_lineAssignments.TryGetValue(me.EntityId, out var mine))
        {
            _currentInstruction = C.WaitingForMyLineText.Get();
            return;
        }

        var crossLabel = DirectionLabel(GetSelectedCrossDirection());
        _currentInstruction = mine.Kind switch
        {
            LineKind.Sleep => FormatText(C.SleepInstructionText, crossLabel),
            LineKind.Confusion => FormatText(C.ConfusionInstructionText, crossLabel),
            _ => C.UnknownLineInstructionText.Get()
        };
    }

    private bool LooksLikeImageTether(uint data2, uint data3, uint data5)
    {
        return data2 is 45u or 15u || data3 is 45u or 15u || data5 is 45u or 15u;
    }

    private LineKind ClassifySource(Vector3 sourcePosition)
    {
        if (sourcePosition == Vector3.Zero) return LineKind.Unknown;

        if (sourcePosition.X < Center.X - SourceSideDeadzone) return LineKind.Confusion;
        if (sourcePosition.X > Center.X + SourceSideDeadzone) return LineKind.Sleep;
        return LineKind.Unknown;
    }

    private void ApplyDisplay()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

        var me = BasePlayer;
        if (me == null) return;

        if (Controller.TryGetElementByName("SelfInstruction", out var selfText))
        {
            selfText.Enabled = !string.IsNullOrWhiteSpace(_currentInstruction);
            selfText.SetRefPosition(me.Position);
            selfText.overlayText = _currentInstruction;
        }

        if (_active == ActiveMechanic.Teletrouncing)
            ApplyTeleDisplay(me);
        else if (_active == ActiveMechanic.Image3)
            ApplyImage3Display(me);
    }

    private void ApplyTeleDisplay(IPlayerCharacter me)
    {
        if (!_teleAssignments.TryGetValue(me.EntityId, out var assignment)) return;

        var destinationColor = RainbowColor();
        if (assignment.Kind == TeleAssignmentKind.SameArrowFixed &&
            Controller.TryGetElementByName("TeleDestination", out var destination))
        {
            var radius = _sameArrowOuterPlayers.Contains(me.EntityId)
                ? C.SameArrowOuterRadius
                : C.SameArrowInnerRadius;
            destination.Enabled = true;
            destination.SetRefPosition(GetPosition(assignment.Slot, radius));
            destination.color = destinationColor;
            destination.overlayText = C.TeleFixedOverlayText.Get();
            return;
        }

        if (assignment.Kind == TeleAssignmentKind.DifferentArrowX)
            ApplyDestination("TeleDestination", GetSelectedXDirection(), C.DifferentArrowXRadius,
                C.TeleXOverlayText.Get(), destinationColor);
    }

    private void ApplyImage3Display(IPlayerCharacter me)
    {
        if (!_lineAssignments.TryGetValue(me.EntityId, out var mine)) return;

        var destinationColor = RainbowColor();

        if (mine.Kind == LineKind.Sleep)
            ApplyDestination("ImageDestination", GetSelectedCrossDirection(), C.SleepSpreadRadius,
                C.SleepOverlayText.Get(), destinationColor);
        else if (mine.Kind == LineKind.Confusion)
            ApplyDestination("ImageDestination", GetSelectedCrossDirection(), C.ConfusionSpreadRadius,
                C.ConfusionOverlayText.Get(), destinationColor);
    }

    private void ApplyDestination(string elementName, Direction direction, float radius, string overlayText, uint color)
    {
        if (!Controller.TryGetElementByName(elementName, out var element))
            return;

        element.Enabled = true;
        element.SetRefPosition(GetPosition(direction, radius));
        element.color = color;
        element.overlayText = $"{overlayText} {DirectionLabel(direction)}";
    }

    private Direction DirectionFromStatus(uint statusId)
    {
        return statusId switch
        {
            NorthStatusId => Direction.North,
            NorthEastStatusId => Direction.NorthEast,
            EastStatusId => Direction.East,
            SouthEastStatusId => Direction.SouthEast,
            SouthStatusId => Direction.South,
            SouthWestStatusId => Direction.SouthWest,
            WestStatusId => Direction.West,
            NorthWestStatusId => Direction.NorthWest,
            _ => Direction.None
        };
    }

    private static Vector3 GetPosition(Direction direction, float radius)
    {
        var offset = direction switch
        {
            Direction.North => new Vector2(0f, -radius),
            Direction.NorthEast => Vector2.Normalize(new Vector2(1f, -1f)) * radius,
            Direction.East => new Vector2(radius, 0f),
            Direction.SouthEast => Vector2.Normalize(new Vector2(1f, 1f)) * radius,
            Direction.South => new Vector2(0f, radius),
            Direction.SouthWest => Vector2.Normalize(new Vector2(-1f, 1f)) * radius,
            Direction.West => new Vector2(-radius, 0f),
            Direction.NorthWest => Vector2.Normalize(new Vector2(-1f, -1f)) * radius,
            _ => Vector2.Zero
        };

        return new Vector3(Center.X + offset.X, 0f, Center.Y + offset.Y);
    }

    private string DirectionLabel(Direction direction)
    {
        return direction switch
        {
            Direction.North => C.NorthLabelText.Get(),
            Direction.NorthEast => C.NorthEastLabelText.Get(),
            Direction.East => C.EastLabelText.Get(),
            Direction.SouthEast => C.SouthEastLabelText.Get(),
            Direction.South => C.SouthLabelText.Get(),
            Direction.SouthWest => C.SouthWestLabelText.Get(),
            Direction.West => C.WestLabelText.Get(),
            Direction.NorthWest => C.NorthWestLabelText.Get(),
            _ => C.UnknownDirectionLabelText.Get()
        };
    }

    private string DirectionPairLabel(DirectionSet directions)
    {
        if (!directions.HasAny) return C.UnknownDirectionLabelText.Get();
        if (!directions.IsComplete) return DirectionLabel(directions.First);
        return $"{DirectionLabel(directions.First)}/{DirectionLabel(directions.Second)}";
    }

    private Direction GetSelectedXDirection()
    {
        var index = Math.Clamp(C.XDirectionIndex, 0, XDirections.Length - 1);
        return XDirections[index];
    }

    private Direction GetSelectedCrossDirection()
    {
        var index = Math.Clamp(C.CrossDirectionIndex, 0, CrossDirections.Length - 1);
        return CrossDirections[index];
    }

    private static string FormatText(InternationalString text, params object[] args)
    {
        var format = text.Get() ?? "";
        try
        {
            return string.Format(format, args);
        }
        catch (FormatException)
        {
            return format;
        }
    }

    private static uint RainbowColor()
    {
        var hue = Environment.TickCount64 % 2400 / 2400f;
        var (r, g, b) = HsvToRgb(hue, 1f, 1f);
        return 0xC8000000u | ((uint)(r * 255f) << 16) | ((uint)(g * 255f) << 8) | (uint)(b * 255f);
    }

    private static (float R, float G, float B) HsvToRgb(float h, float s, float v)
    {
        var i = (int)MathF.Floor(h * 6f);
        var f = h * 6f - i;
        var p = v * (1f - s);
        var q = v * (1f - f * s);
        var t = v * (1f - (1f - f) * s);

        return (i % 6) switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q)
        };
    }

    private void ResetState()
    {
        _gravenImageCount = 0;
        ClearActiveState();
    }

    private void ClearActiveState()
    {
        _active = ActiveMechanic.None;
        _directions.Clear();
        _teleAssignments.Clear();
        _lineAssignments.Clear();
        _sameArrowOuterPlayers.Clear();
        _currentInstruction = "";
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    private enum ActiveMechanic
    {
        None,
        Teletrouncing,
        Image3
    }

    private enum Direction
    {
        None,
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    private enum TeleAssignmentKind
    {
        SameArrowFixed,
        DifferentArrowX
    }

    private enum LineKind
    {
        Unknown,
        Confusion,
        Sleep
    }

    private readonly record struct DirectionObservation(Direction Direction, float RemainingTime);

    private sealed class DirectionSet
    {
        private const float SameRemainingTolerance = 0.05f;
        private readonly List<DirectionObservation> _observations = [];

        public int Count => _observations.Count;
        public bool HasAny => Count > 0;
        public bool IsComplete => Count >= 2;
        public Direction First => Count > 0 ? _observations[0].Direction : Direction.None;
        public Direction Second => Count > 1 ? _observations[1].Direction : Direction.None;

        public bool Add(DirectionObservation observation)
        {
            if (observation.Direction == Direction.None) return false;
            if (_observations.Any(x =>
                    x.Direction == observation.Direction && MathF.Abs(x.RemainingTime - observation.RemainingTime) <=
                    SameRemainingTolerance))
                return false;

            _observations.Add(observation);
            _observations.Sort((left, right) => left.RemainingTime.CompareTo(right.RemainingTime));
            if (_observations.Count > 2)
                _observations.RemoveRange(2, _observations.Count - 2);
            return true;
        }
    }

    private readonly record struct TeleAssignment(
        uint Player,
        Direction Direction,
        TeleAssignmentKind Kind,
        Direction Slot);

    private readonly record struct LineAssignment(
        string PlayerName,
        uint Player,
        LineKind Kind);

    public class Config : IEzConfig
    {
        public InternationalString CollectingLinesText = new()
        {
            En = "Image 3: collecting sleep/confusion lines",
            Jp = "神々の像3: 睡眠/混乱線を収集中"
        };

        public InternationalString ConfusionInstructionText = new()
        {
            En = "OUTER CROSS",
            Jp = "外側十字"
        };

        public InternationalString ConfusionOverlayText = new()
        {
            En = "Confusion",
            Jp = "混乱"
        };

        public float ConfusionSpreadRadius = 18.0f;
        public int CrossDirectionIndex;
        public float DifferentArrowXRadius = 12.0f;

        public InternationalString EastLabelText = new()
        {
            En = "E",
            Jp = "東"
        };

        public InternationalString NorthEastLabelText = new()
        {
            En = "NE",
            Jp = "北東"
        };

        public InternationalString NorthLabelText = new()
        {
            En = "N",
            Jp = "北"
        };

        public InternationalString NorthWestLabelText = new()
        {
            En = "NW",
            Jp = "北西"
        };

        public float SameArrowInnerRadius = 7.0f;
        public float SameArrowOuterRadius = 12.0f;

        public InternationalString SleepInstructionText = new()
        {
            En = "INNER CROSS",
            Jp = "内側十字"
        };

        public InternationalString SleepOverlayText = new()
        {
            En = "Sleep",
            Jp = "睡眠"
        };

        public float SleepSpreadRadius = 3.0f;

        public InternationalString SouthEastLabelText = new()
        {
            En = "SE",
            Jp = "南東"
        };

        public InternationalString SouthLabelText = new()
        {
            En = "S",
            Jp = "南"
        };

        public InternationalString SouthWestLabelText = new()
        {
            En = "SW",
            Jp = "南西"
        };

        public InternationalString TeleDifferentArrowInstructionText = new()
        {
            En = "Arrow {0}: different arrows, go {1}",
            Jp = "矢印{0}: 違う矢印、{1}"
        };

        public InternationalString TeleFixedOverlayText = new()
        {
            En = "Fixed",
            Jp = "固定"
        };

        public InternationalString TeleMissingArrowText = new()
        {
            En = "Tele-trouncing: arrow not found",
            Jp = "ずびずばテレポ: 矢印未検出"
        };

        public InternationalString TeleSameArrowInstructionText = new()
        {
            En = "Arrow {0}: same arrow, fixed at {1}",
            Jp = "矢印{0}: 同矢印、{1}固定へ"
        };

        public InternationalString TeleWaitingForArrowText = new()
        {
            En = "Tele-trouncing: waiting for arrow debuff",
            Jp = "ずびずばテレポ: 矢印デバフ待ち"
        };

        public InternationalString TeleWaitingForSecondArrowText = new()
        {
            En = "Tele-trouncing: waiting for second arrow",
            Jp = "ずびずばテレポ: 2つ目の矢印待ち"
        };

        public InternationalString TeleXOverlayText = new()
        {
            En = "X",
            Jp = "X字"
        };

        public InternationalString UnknownDirectionLabelText = new()
        {
            En = "?",
            Jp = "?"
        };

        public InternationalString UnknownLineInstructionText = new()
        {
            En = "Line unknown: use visual, keep logging",
            Jp = "線判定未確定: 目視しつつログ確認"
        };

        public InternationalString WaitingForMyLineText = new()
        {
            En = "Image 3: waiting for your line",
            Jp = "神々の像3: 自分の線待ち"
        };

        public InternationalString WestLabelText = new()
        {
            En = "W",
            Jp = "西"
        };

        public int XDirectionIndex;
    }
}