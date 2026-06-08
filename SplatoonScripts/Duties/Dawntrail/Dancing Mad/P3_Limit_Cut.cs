using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate;

public class P3_Limit_Cut : SplatoonScript
{
    private const uint TerritoryDancingMadUltimate = 1363;
    private const uint BowelsOfAgony = 47858;
    private const uint LimitCutSetupCast = 47872;
    private const uint LimitCutPartnerCast = 47891;
    private const uint KefkaDashPreview = 47843;
    private const uint KefkaDashStartDataId = 19451;
    private const uint ThunderIIICast = 47881;
    private const uint UltimateEmbrace = 49740;
    private const float DestinationRadius = 17.0f;
    private const float DestinationHalfStepOffset = 0.5f;
    private const float CenterIgnoreRadius = 5.0f;
    private const string DestinationElementName = "Destination";
    private const string InstructionElementName = "SelfInstruction";

    private static readonly Vector3 ArenaCenter = new(100.0f, 0.0f, 100.0f);
    private static readonly Regex LimitCutVfxRegex =
        new(@"^vfx/lockon/eff/(?:m0361trg_[ab](?<num>[1-8])t|sph_lockon2_num0(?<num>[1-8])_s8[pt])\.avfx$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly InternationalString MainDescriptionText = new()
    {
        En = "Guides you to the position for your P3 Limit Cut number.",
        Jp = "P3リミットカットで自分の番号に対応した位置へナビします。"
    };

    private static readonly InternationalString DisplayTextHeaderText = new()
    {
        En = "Display text",
        Jp = "表示テキスト"
    };

    private static readonly InternationalString DisplayTextDescriptionText = new()
    {
        En = "Text shown to the local player. Keep the {0} placeholder for the Limit Cut number.",
        Jp = "自分に表示する文言です。{0} はリミットカット番号です。"
    };

    private readonly Dictionary<uint, int> _numbersByObjectId = [];
    private readonly Dictionary<string, int> _numbersByName = new(StringComparer.OrdinalIgnoreCase);

    private bool _active;
    private bool _hasDashSolution;
    private int _dashStartCount;
    private int _firstStartDirection;
    private int _firstDashDirection;
    private int _dashStep;

    public override HashSet<uint>? ValidTerritories { get; } = [TerritoryDancingMadUltimate];
    public override Metadata Metadata => new(1, "Garume");

    private Config C
    {
        get
        {
            var config = Controller.GetConfig<Config>();
            config.EnsureDefaults();
            return config;
        }
    }

    private IPlayerCharacter BasePlayer => Controller.BasePlayer;

    public override void OnSetup()
    {
        C.EnsureDefaults();

        Controller.RegisterElement(DestinationElementName, new Element(0)
        {
            Enabled = false,
            radius = 1.15f,
            thicc = 5.0f,
            fillIntensity = 0.25f,
            color = 0xC800BFFF,
            tether = true,
            overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 2.5f,
            overlayFScale = 1.8f,
            overlayText = ""
        });

        Controller.RegisterElement(InstructionElementName, new Element(0)
        {
            Enabled = false,
            radius = 0.0f,
            thicc = 0.0f,
            overlayBGColor = 0xC8000000,
            overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 3.0f,
            overlayFScale = 1.8f,
            overlayText = ""
        });
    }

    public override void OnCombatStart()
    {
        ResetState();
    }

    public override void OnCombatEnd()
    {
        ResetState();
    }

    public override void OnReset()
    {
        ResetState();
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (category is DirectorUpdateCategory.Commence or DirectorUpdateCategory.Recommence or DirectorUpdateCategory.Wipe)
            ResetState();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (IsResetAction(castId))
        {
            ResetState();
            return;
        }

        if (IsLimitCutStartCast(castId))
            StartLimitCut();

        if (castId == KefkaDashPreview)
        {
            StartLimitCut();
            CaptureDashStart(source);
            return;
        }

        if (_active && IsLimitCutEndAction(castId))
            ClearActiveState();
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        var actionId = set.Action?.RowId ?? 0;
        if (IsResetAction(actionId))
        {
            ResetState();
            return;
        }

        if (actionId == KefkaDashPreview)
        {
            StartLimitCut();
            CaptureDashStart(set.Source);
            return;
        }

        if (actionId == LimitCutSetupCast)
        {
            StartLimitCut();
            return;
        }

        if (_active && IsLimitCutEndAction(actionId))
            ClearActiveState();
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        var match = LimitCutVfxRegex.Match(vfxPath);
        if (!match.Success) return;

        StartLimitCut();
        RecordNumber(target, int.Parse(match.Groups["num"].Value));
    }

    public override void OnUpdate()
    {
        DisableElements();

        var me = BasePlayer;
        if (!_active || me == null)
            return;

        if (_hasDashSolution && TryGetDestination(me, out var destination))
        {
            ShowDestination(destination, me);
            return;
        }

        ShowInstruction(me);
    }

    public override void OnSettingsDraw()
    {
        C.EnsureDefaults();
        ImGui.TextWrapped(MainDescriptionText.Get());
        ImGui.Separator();

        if (!ImGui.CollapsingHeader(DisplayTextHeaderText.Get(), ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.Indent();
        ImGui.TextWrapped(DisplayTextDescriptionText.Get());
        DrawInternationalString("Waiting for dash", C.WaitingForDashText);
        DrawInternationalString("Destination overlay", C.DestinationOverlayText);
        ImGui.Unindent();
    }

    private static bool IsResetAction(uint actionId) => actionId is UltimateEmbrace or BowelsOfAgony;

    private static bool IsLimitCutStartCast(uint castId) => castId is LimitCutSetupCast or LimitCutPartnerCast;

    private static bool IsLimitCutEndAction(uint actionId) => actionId == ThunderIIICast;

    private static void DrawInternationalString(string label, InternationalString text)
    {
        ImGui.PushID(label);
        ImGui.Text(label);
        ImGui.SameLine();
        var current = text.Get();
        text.ImGuiEdit(ref current);
        ImGui.PopID();
    }

    private void StartLimitCut() => _active = true;

    private void CaptureDashStart(uint sourceId)
    {
        if (sourceId.GetObject() is { } source)
            CaptureDashStart(source);
    }

    private void CaptureDashStart(IGameObject? source)
    {
        if (source == null || _hasDashSolution || _dashStartCount >= 2 || source.DataId != KefkaDashStartDataId)
            return;

        var position = NormalizeY(source.Position);
        if (DistanceXZ(position, ArenaCenter) < CenterIgnoreRadius)
            return;

        var direction = DirectionFromPosition(position);
        if (_dashStartCount == 0)
        {
            _firstStartDirection = direction;
            _dashStartCount = 1;
            return;
        }

        if (direction == _firstStartDirection)
            return;

        _dashStartCount = 2;
        TrySolveDashRotation(direction);
    }

    private void TrySolveDashRotation(int secondStartDirection)
    {
        var delta = WrapDirection(secondStartDirection - _firstStartDirection);
        if (delta == 0 || delta == 4)
            return;

        _firstDashDirection = WrapDirection(_firstStartDirection + 4);
        _dashStep = delta < 4 ? 1 : -1;
        _hasDashSolution = true;
    }

    private void RecordNumber(uint target, int number)
    {
        if (number is < 1 or > 8)
            return;

        _numbersByObjectId[target] = number;

        if (target.GetObject() is { } obj)
        {
            _numbersByObjectId[obj.EntityId] = number;
            RecordName(obj, number);
            return;
        }

        var me = BasePlayer;
        if (me != null && (target == me.EntityId || target == me.GameObjectId))
            RecordPlayer(me, number);
    }

    private void RecordName(IGameObject obj, int number)
    {
        var name = obj.Name.ToString();
        if (!string.IsNullOrWhiteSpace(name))
            _numbersByName[name] = number;

        if (obj is IPlayerCharacter player)
            RecordPlayer(player, number);
    }

    private void RecordPlayer(IPlayerCharacter player, int number)
    {
        _numbersByObjectId[player.EntityId] = number;
        if (player.GameObjectId <= uint.MaxValue)
            _numbersByObjectId[(uint)player.GameObjectId] = number;
        _numbersByName[GetPlayerKey(player)] = number;
    }

    private bool TryGetNumber(IPlayerCharacter player, out int number)
    {
        if (_numbersByObjectId.TryGetValue(player.EntityId, out number))
            return true;

        if (player.GameObjectId <= uint.MaxValue && _numbersByObjectId.TryGetValue((uint)player.GameObjectId, out number))
            return true;

        return _numbersByName.TryGetValue(GetPlayerKey(player), out number);
    }

    private bool TryGetDestination(IPlayerCharacter player, out Vector3 destination)
    {
        if (!TryGetNumber(player, out var number))
        {
            destination = default;
            return false;
        }

        var playerStep = -_dashStep;
        var direction = _firstDashDirection + playerStep * (number - 1 + DestinationHalfStepOffset);
        destination = PositionForDirection(direction, DestinationRadius);
        return true;
    }

    private void ShowDestination(Vector3 destination, IPlayerCharacter player)
    {
        if (!Controller.TryGetElementByName(DestinationElementName, out var element))
            return;

        element.Enabled = true;
        element.color = RainbowColor();
        element.tether = true;
        element.SetRefPosition(destination);
        element.overlayText = TryGetNumber(player, out var number)
            ? FormatText(C.DestinationOverlayText, number)
            : "";
    }

    private void ShowInstruction(IPlayerCharacter player)
    {
        if (!TryGetNumber(player, out var number) ||
            !Controller.TryGetElementByName(InstructionElementName, out var element))
            return;

        element.Enabled = true;
        element.SetRefPosition(player.Position);
        element.overlayText = FormatText(C.WaitingForDashText, number);
    }

    private void DisableElements()
    {
        foreach (var element in Controller.GetRegisteredElements().Values)
            element.Enabled = false;
    }

    private static int DirectionFromPosition(Vector3 position)
    {
        var angle = MathF.Atan2(position.X - ArenaCenter.X, ArenaCenter.Z - position.Z);
        return WrapDirection((int)MathF.Round(angle / (MathF.PI / 4.0f)));
    }

    private static Vector3 PositionForDirection(float direction, float radius)
    {
        var angle = direction * MathF.PI / 4.0f;
        return new Vector3(
            ArenaCenter.X + MathF.Sin(angle) * radius,
            0.0f,
            ArenaCenter.Z - MathF.Cos(angle) * radius);
    }

    private static int WrapDirection(int direction) => (direction % 8 + 8) % 8;

    private static Vector3 NormalizeY(Vector3 position) => new(position.X, 0.0f, position.Z);

    private static float DistanceXZ(Vector3 left, Vector3 right)
    {
        var dx = left.X - right.X;
        var dz = left.Z - right.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    private static string GetPlayerKey(IPlayerCharacter player)
    {
        var name = player.Name.ToString();
        return string.IsNullOrWhiteSpace(name) ? $"entity:{player.EntityId:X8}" : name;
    }

    private static string FormatText(InternationalString text, params object[] args) => string.Format(text.Get(), args);

    private static uint RainbowColor()
    {
        var t = Environment.TickCount64 % 2400 / 2400f * MathF.PI * 2.0f;
        uint r = (uint)((MathF.Sin(t) * 0.5f + 0.5f) * 255.0f);
        uint g = (uint)((MathF.Sin(t + MathF.PI * 2.0f / 3.0f) * 0.5f + 0.5f) * 255.0f);
        uint b = (uint)((MathF.Sin(t + MathF.PI * 4.0f / 3.0f) * 0.5f + 0.5f) * 255.0f);
        return 0xC8000000u | (r << 16) | (g << 8) | b;
    }

    private void ResetState()
    {
        ClearActiveState();
        _numbersByObjectId.Clear();
        _numbersByName.Clear();
    }

    private void ClearActiveState()
    {
        _active = false;
        _hasDashSolution = false;
        _dashStartCount = 0;
        _firstStartDirection = 0;
        _firstDashDirection = 0;
        _dashStep = 0;
        DisableElements();
    }

    public sealed class Config : IEzConfig
    {
        public InternationalString DestinationOverlayText = new()
        {
            En = "LC {0}",
            Jp = "LC {0}"
        };

        public InternationalString WaitingForDashText = new()
        {
            En = "Limit Cut {0}: waiting for dash direction",
            Jp = "リミットカット{0}: 回転方向待ち"
        };

        public void EnsureDefaults()
        {
            DestinationOverlayText ??= new InternationalString { En = "LC {0}", Jp = "LC {0}" };
            WaitingForDashText ??= new InternationalString
            {
                En = "Limit Cut {0}: waiting for dash direction",
                Jp = "リミットカット{0}: 回転方向待ち"
            };
        }
    }
}
