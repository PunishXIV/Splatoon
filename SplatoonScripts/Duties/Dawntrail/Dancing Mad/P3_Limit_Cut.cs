using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Utility;
using ECommons;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplaSim.SplatoonScripts.Duties.Dawntrail.DancingMadUltimate;

public class P3_Limit_Cut : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [TerritoryDancingMadUltimate];
    public override Metadata Metadata => new(13, "Garume, NightmareXIV");

    private const uint TerritoryDancingMadUltimate = 1363;
    private const uint BowelsOfAgony = 47858;
    private const uint LimitCutSetupCast = 47872;
    private const uint LimitCutPartnerCast = 47891;
    private const uint KefkaDashPreview = 47843;
    private const uint KefkaDashStartDataId = 19451;
    private const uint ThunderIIICast = 47881;
    private const uint UltimateEmbrace = 49740;
    private const float DestinationRadius = 19.0f;
    private const float DestinationHalfStepOffset = 0.5f;
    private const float CenterIgnoreRadius = 5.0f;
    private const string DestinationElementName = "Destination";
    private const string InstructionElementName = "SelfInstruction";
    private const string FirstDashCalloutElementName = "FirstDashCallout";

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
        En = "Text shown to the local player. Waiting and destination text use {0} for the Limit Cut number. First line callout uses {0}=first dash endpoint/new north, {1}=Kefka dash rotation, {2}=first dash start.",
        Jp = "自分に表示する文言です。待機/目的地テキストの {0} はリミットカット番号です。初回ラインAoEコールアウトは {0}=初回ダッシュ終点/新北、{1}=ケフカのダッシュ回転、{2}=初回ダッシュ開始側です。"
    };

    private static readonly InternationalString ShowFirstDashCalloutSettingText = new()
    {
        En = "Show first line callout",
        Jp = "初回ラインAoEコールアウトを表示"
    };

    private readonly Dictionary<uint, int> _numbersByObjectId = [];
    private readonly Dictionary<string, int> _numbersByName = new(StringComparer.OrdinalIgnoreCase);

    private bool _active;
    private bool _hasDashSolution;
    private int _dashStartCount;
    private int _firstStartDirection;
    private int _firstDashDirection;
    private int _dashStep;
    private bool ChatSent = false;


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

        Controller.RegisterElement(FirstDashCalloutElementName, new Element(0)
        {
            Enabled = false,
            radius = 0.0f,
            thicc = 0.0f,
            overlayBGColor = 0xC8000000,
            overlayTextColor = 0xFFFFFFFF,
            overlayVOffset = 5.0f,
            overlayFScale = 1.65f,
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
        ChatSent = false;
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

        if (_hasDashSolution)
        {
            ShowFirstDashCallout(me);
            if (TryGetDestination(me, out var destination))
                ShowDestination(destination, me);
            return;
        }

        ShowInstruction(me);
    }

    void DrawMacroCallouts(ref List<string> list8, string suffix)
    {
        /*
         * 0 => C.NorthLabelText.Get(),
            1 => C.NorthEastLabelText.Get(),
            2 => C.EastLabelText.Get(),
            3 => C.SouthEastLabelText.Get(),
            4 => C.SouthLabelText.Get(),
            5 => C.SouthWestLabelText.Get(),
            6 => C.WestLabelText.Get(),
            7 => C.NorthWestLabelText.Get(),
        */
        string[] dirs = ["North", "NorthEast", "East", "SouthEast", "South", "SouthWest", "West", "NorthWest"];
        for(int i = 0; i < dirs.Length; i++)
        {
            ImGui.PushID(i.ToString());
            var str = list8[i];
            ImGuiEx.Text($"{dirs[i]} {suffix}:");
            ImGui.SameLine();
            if(ImGuiEx.SmallButton("Test", ImGuiEx.Ctrl))
            {
                Controller.DangerousEnqueueCommand(str, C.TestMacro);
            }
            ImGuiEx.Tooltip("Hold CTRL and click");
            ImGui.Indent();
            if(ImGuiEx.InputTextMultilineExpanding("##macro", ref str, 2000))
            {
                list8[i] = str;
            }
            ImGui.Unindent();
            ImGui.PopID();
        }
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
        ImGui.Checkbox(ShowFirstDashCalloutSettingText.Get(), ref C.ShowFirstDashCallout);
        DrawInternationalString("First line callout", C.FirstDashCalloutText);
        DrawInternationalString("Clockwise label", C.CounterclockwiseLabelText);
        DrawInternationalString("Counter-Clockwise label", C.ClockwiseLabelText);
        DrawInternationalString("North label", C.NorthLabelText);
        DrawInternationalString("Northeast label", C.NorthEastLabelText);
        DrawInternationalString("East label", C.EastLabelText);
        DrawInternationalString("Southeast label", C.SouthEastLabelText);
        DrawInternationalString("South label", C.SouthLabelText);
        DrawInternationalString("Southwest label", C.SouthWestLabelText);
        DrawInternationalString("West label", C.WestLabelText);
        DrawInternationalString("Northwest label", C.NorthWestLabelText);
        ImGui.Unindent();
        ImGui.Checkbox("Print direction into chat (local chat only)", ref C.Callout);
        if(C.Callout)
        {
            ImGuiEx.Checkbox("[Dangerous] Send direction into party chat", ref C.PartyCallout, enabled: C.PartyCallout || ImGuiEx.Ctrl);
            ImGuiEx.HelpMarker("Before enabling, do extensive testing in replays and ensure it call out correctly. Hold CTRL and click to enable.");
        }
        ImGuiEx.Checkbox("[Dangerous] Enable sending macro to chat", ref C.PartyMacroA, enabled: C.PartyMacroA || ImGuiEx.Ctrl);
        ImGuiEx.HelpMarker("Hold CTRL and click to enable.");
        if(C.Callout || C.PartyMacroA)
        {
            ImGui.Checkbox("Test mode", ref C.TestMacro);
            ImGuiEx.TextV($"Send delay, ms:");
            ImGui.SameLine();
            ImGuiEx.DragInt(100f, "+ 0-##sd1", ref C.CalloutDelay.ValidateRange(0, 2000));
            ImGui.SameLine(0,0  );
            ImGuiEx.DragInt(100f, "##sd1", ref C.CalloutDelayRng.ValidateRange(0, 2000));
        }
        if(C.PartyMacroA)
        {
            ImGuiEx.HelpMarker("Will not send but write into your chat instead locally. Test this before using. Really, test it very well. I'm serious. ");
            ImGuiEx.Text($"All lines must start with /party prefix, like normal macro. ");
            ImGui.Indent();
            ImGui.PushID("Cw");
            ImGuiEx.Text($"Clockwise macro list:");
            DrawMacroCallouts(ref C.MacroCw, "clockwise");
            ImGui.PopID();
            ImGui.Unindent();
            ImGui.Separator();

            ImGui.Indent();
            ImGui.PushID("Ccw");
            ImGuiEx.Text($"Counter-Clockwise macro list:");
            DrawMacroCallouts(ref C.MacroCcw, "counter-clockwise");
            ImGui.PopID();
            ImGui.Unindent();
        }
    }

    private static bool IsResetAction(uint actionId) => actionId is UltimateEmbrace or BowelsOfAgony;

    private static bool IsLimitCutStartCast(uint castId) => castId is LimitCutSetupCast or LimitCutPartnerCast;

    private static bool IsLimitCutEndAction(uint actionId) => actionId == ThunderIIICast;

    private static void DrawInternationalString(string label, InternationalString text)
    {
        ImGui.PushID(label);
        ImGuiEx.TextV(label);
        ImGui.SameLine();
        var ds = 200f - ImGui.CalcTextSize(label).X;
        if(ds > 0)
        {
            ImGui.Dummy(new(ds, 1));
            ImGui.SameLine();
        }
        var current = text.Get();
        text.ImGuiEdit(ref current);
        ImGui.PopID();
        ImGui.Separator();
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
        if(!ChatSent)
        {
            ChatSent = true;
            var rng = C.CalloutDelay.ValidateRange(0, 2000) + Random.Shared.Next(C.CalloutDelayRng.ValidateRange(0, 2000));
            if(C.Callout)
            {
                if(C.PartyCallout)
                {
                    Controller.Schedule(() =>
                    {
                        if(EzThrottler.Throttle("Chat", 1000) && GenericHelpers.IsScreenReady())
                        {
                            Controller.DangerousEnqueueCommand($"/party {GetLabel()}", false);
                        }
                    }, rng);
                }
                else
                {
                    Svc.Chat.PrintChat(new()
                    {
                        Type = Dalamud.Game.Text.XivChatType.Echo,
                        Message = $"{GetLabel()}"
                    });
                }
            }
            if(C.PartyMacroA)
            {
                var isCw = _dashStep > 0;
                var macro = (!isCw ? C.MacroCw : C.MacroCcw)[_firstDashDirection];
                Controller.Schedule(() =>
                {
                    Controller.DangerousEnqueueCommand(macro, C.TestMacro);
                }, rng);
            }
        }
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
        element.color = Controller.AttentionColor;
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

    private void ShowFirstDashCallout(IPlayerCharacter player)
    {
        if (!C.ShowFirstDashCallout)
            return;

        if (!Controller.TryGetElementByName(FirstDashCalloutElementName, out var element))
            return;

        element.Enabled = true;
        element.SetRefPosition(player.Position);
        element.overlayText = GetLabel();
        Controller.DisplayAttentionWindowLine(GetLabel());
    }

    string GetLabel()
    {
        var format = C.FirstDashCalloutText.Get();
        var dl1 = DirectionLabel(_firstDashDirection);
        var dl2 = DashRotationLabel();
        var dl3 = DirectionLabel(_firstStartDirection);
        if(format.IsNullOrWhitespace() || dl1.IsNullOrWhitespace() || dl2.IsNullOrWhitespace() || dl3.IsNullOrWhitespace()) return "";
        return string.Format(format, dl1, dl2, dl3);
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

    private string? DirectionLabel(int direction)
    {
        return WrapDirection(direction) switch
        {
            0 => C.NorthLabelText.Get(),
            1 => C.NorthEastLabelText.Get(),
            2 => C.EastLabelText.Get(),
            3 => C.SouthEastLabelText.Get(),
            4 => C.SouthLabelText.Get(),
            5 => C.SouthWestLabelText.Get(),
            6 => C.WestLabelText.Get(),
            7 => C.NorthWestLabelText.Get(),
            _ => null
        };
    }

    private string DashRotationLabel()
    {
        return _dashStep > 0
            ? C.ClockwiseLabelText.Get()
            : C.CounterclockwiseLabelText.Get();
    }

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

    public sealed class Config
    {
        public bool Callout = false;
        public int CalloutDelay = 1000;
        public int CalloutDelayRng = 1000;
        public bool PartyCallout = false;
        public bool PartyMacroA = false;
        public bool TestMacro = true;
        public List<string> MacroCw = ["", "", "", "", "", "", "", ""];
        public List<string> MacroCcw = ["", "", "", "", "", "", "", ""];
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

        public InternationalString FirstDashCalloutText = new()
        {
            En = "New north: {0} {1}",
            Jp = "新北: {0} {1}"
        };

        public bool ShowFirstDashCallout;

        public InternationalString ClockwiseLabelText = new()
        {
            En = "CCW",
            Jp = "反時計回り"
        };

        public InternationalString CounterclockwiseLabelText = new()
        {
            En = "CW",
            Jp = "時計回り"
        };

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

        public InternationalString WestLabelText = new()
        {
            En = "W",
            Jp = "西"
        };

        public void EnsureDefaults()
        {
            DestinationOverlayText ??= new InternationalString { En = "LC {0}", Jp = "LC {0}" };
            WaitingForDashText ??= new InternationalString
            {
                En = "Limit Cut {0}: waiting for dash direction",
                Jp = "リミットカット{0}: 回転方向待ち"
            };
            FirstDashCalloutText ??= new InternationalString { En = "New north: {0} {1}", Jp = "新北: {0} {1}" };
            ClockwiseLabelText ??= new InternationalString { En = "CW", Jp = "時計回り" };
            CounterclockwiseLabelText ??= new InternationalString { En = "CCW", Jp = "反時計回り" };
            EastLabelText ??= new InternationalString { En = "E", Jp = "東" };
            NorthEastLabelText ??= new InternationalString { En = "NE", Jp = "北東" };
            NorthLabelText ??= new InternationalString { En = "N", Jp = "北" };
            NorthWestLabelText ??= new InternationalString { En = "NW", Jp = "北西" };
            SouthEastLabelText ??= new InternationalString { En = "SE", Jp = "南東" };
            SouthLabelText ??= new InternationalString { En = "S", Jp = "南" };
            SouthWestLabelText ??= new InternationalString { En = "SW", Jp = "南西" };
            WestLabelText ??= new InternationalString { En = "W", Jp = "西" };
        }
    }
}
