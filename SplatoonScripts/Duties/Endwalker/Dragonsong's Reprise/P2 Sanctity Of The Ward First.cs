using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using ECommons;
using ECommons.ChatMethods;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public unsafe class P2_Sanctity_Of_The_Ward_First : SplatoonScript
{
    private readonly Vector2 _center = new(100, 100);

    private readonly Dictionary<uint, Vector2> _eyesPositions = new()
    {
        { 0, new Vector2(100.00f, 60.00f) },
        { 1, new Vector2(128.28f, 71.72f) },
        { 2, new Vector2(140.00f, 100.00f) },
        { 3, new Vector2(128.28f, 128.28f) },
        { 4, new Vector2(100.00f, 140.00f) },
        { 5, new Vector2(71.72f, 128.28f) },
        { 6, new Vector2(60.00f, 100.00f) },
        { 7, new Vector2(71.72f, 71.72f) }
    };

    private ClockwiseDirection _clockwiseDirection;

    private Vector2 _eyesPosition;

    private Vector3 _lastPlayerPosition = Vector3.Zero;

    private IGameObject? _sword1;
    private IGameObject? _sword2;

    private ZephiranDirection _zephiranDirection;
    public override HashSet<uint>? ValidTerritories => [968];
    public override Metadata? Metadata => new(4, "Garume");
    private bool IsStart => _sword1 != null && _sword2 != null;
    private Config C => Controller.GetConfig<Config>();
    private IBattleChara? Zephiran => Svc.Objects.OfType<IBattleChara>().FirstOrDefault(x => x.NameId == 0xE31);

    private IBattleChara? Adelphel => Svc.Objects.OfType<IBattleChara>()
        .FirstOrDefault(x => x.NameId == 0xE32 && x.IsCharacterVisible());

    private IBattleChara? Thordan => Svc.Objects.OfType<IBattleChara>()
        .FirstOrDefault(x => x.NameId == 0xE30 && x.IsCharacterVisible());

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if (!IsStart) return;
        switch (data1)
        {
            case 1:
            {
                if (_eyesPositions.TryGetValue(position, out var eyesPosition))
                    _eyesPosition = eyesPosition;
                break;
            }
            case 32:
                _eyesPosition = Vector2.Zero;
                break;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (IsStart) return;

        switch (vfxPath)
        {
            // 1 sword
            case "vfx/lockon/eff/m0244trg_a1t.avfx":
                _sword1 = target.GetObject();
                break;
            // 2 sword
            case "vfx/lockon/eff/m0244trg_a2t.avfx":
                _sword2 = target.GetObject();
                break;
        }

        if (IsStart)
        {
            var zephiran = Zephiran;
            var adelphel = Adelphel;

            if (zephiran == null || adelphel == null) return;
            _zephiranDirection = GetZephiranDirection(zephiran);
            _clockwiseDirection = adelphel.Position.X > _center.X
                ? ClockwiseDirection.Clockwise
                : ClockwiseDirection.CounterClockwise;

            Controller.Schedule(() =>
            {
                _sword1 = null;
                _sword2 = null;
            }, 1000 * 10);
        }
    }

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            tether = true
        };
        Controller.TryRegisterElement("bait", element, true);


        Controller.RegisterElementFromCode("G1CCW",
            "{\"Name\":\"G1 CCW\",\"type\":1,\"offX\":3.52,\"offY\":-5.0,\"radius\":0.5,\"color\":3372158208,\"Filled\":false,\"thicc\":5.0,\"refActorNPCNameID\":3633,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyVisible\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("G1CW",
            "{\"Name\":\"G1 CW\",\"type\":1,\"offX\":-3.24,\"offY\":-5.0,\"radius\":0.5,\"color\":3372158208,\"Filled\":false,\"thicc\":5.0,\"refActorNPCNameID\":3633,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyVisible\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("G2CCW",
            "{\"Name\":\"G2 CCW\",\"type\":1,\"offX\":-3.44,\"offY\":35.0,\"radius\":0.5,\"color\":3372158208,\"Filled\":false,\"thicc\":5.0,\"refActorNPCNameID\":3633,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyVisible\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("G2CW",
            "{\"Name\":\"G2 CW\",\"type\":1,\"offX\":3.24,\"offY\":35.0,\"radius\":0.5,\"color\":3372158208,\"Filled\":false,\"thicc\":5.0,\"refActorNPCNameID\":3633,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyVisible\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        var clockwiseTextElement = new Element(0)
        {
            overlayText = "←←←",
            overlayFScale = 5f,
            overlayVOffset = 5f,
            offX = 100f,
            offY = 100f
        };
        Controller.TryRegisterElement("clockwise", clockwiseTextElement, true);

        var counterClockwiseTextElement = new Element(0)
        {
            overlayText = "→→→",
            overlayFScale = 5f,
            overlayVOffset = 5f,
            offX = 100f,
            offY = 100f
        };

        Controller.TryRegisterElement("counterClockwise", counterClockwiseTextElement, true);

        var eyesElement = new Element(0)
        {
            radius = 2f,
            color = 0xFFFF00FF,
            thicc = 5f
        };

        Controller.TryRegisterElement("eyes", eyesElement, true);
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(e => e.Value.Enabled = false);

        if (!IsStart) return;

        if (_zephiranDirection != ZephiranDirection.None)
        {
            var resolvePosition = C.ResolvePosition;

            if (_sword1.Name.ToString() == Player.Name)
                resolvePosition = ResolvePosition.ZephiranFaceToFace;
            else if (_sword2.Name.ToString() == Player.Name)
                resolvePosition = ResolvePosition.ZephiranBack;
            else if (_sword1.Name.ToString() == C.PairCharacterName)
                resolvePosition = ResolvePosition.ZephiranBack;
            else if (_sword2.Name.ToString() == C.PairCharacterName)
                resolvePosition = ResolvePosition.ZephiranFaceToFace;

            var element = ResolveElement(resolvePosition, _clockwiseDirection);
            if (element != null)
            {
                element.Enabled = true;
                element.tether = true;
                element.color = GradientColor.Get(0xFFFF00FF.ToVector4(), 0xFFFFFF00.ToVector4()).ToUint();
            }

            var thordan = Thordan;
            if (thordan != null && _eyesPosition != Vector2.Zero && C.LockFace)
            {
                if (Player.Position != _lastPlayerPosition && C.LockFaceEnableWhenNotMoving) return;
                var resolveFacePosition = CalculateExtendedBisectorPoint(thordan.Position.ToVector2(), _eyesPosition);
                FaceTarget(resolveFacePosition.ToVector3(0f));
            }
        }

        if (_clockwiseDirection != ClockwiseDirection.None)
        {
            var elementName = _clockwiseDirection == ClockwiseDirection.Clockwise ? "clockwise" : "counterClockwise";
            if (Controller.TryGetElementByName(elementName, out var element)) element.Enabled = true;
        }

        if (_eyesPosition != Vector2.Zero)
            if (Controller.TryGetElementByName("eyes", out var element))
            {
                element.Enabled = true;
                element.offX = _eyesPosition.X;
                element.offY = _eyesPosition.Y;
            }

        _lastPlayerPosition = Player.Position;
        return;

        void FaceTarget(Vector3 position, ulong unkObjId = 0xE0000000)
        {
            ActionManager.Instance()->AutoFaceTargetPosition(&position, unkObjId);
        }
    }

    private static Vector2 CalculateExtendedBisectorPoint(Vector2 point1, Vector2 point2, Vector2? center = null,
        float? radius = null)
    {
        center ??= new Vector2(100f, 100f);
        radius ??= 20f;

        var dir1 = point1 - center.Value;
        var dir2 = point2 - center.Value;

        var angle1 = MathF.Atan2(dir1.Y, dir1.X);
        var angle2 = MathF.Atan2(dir2.Y, dir2.X);

        var bisectorAngle = (angle1 + angle2) / 2f;

        var bisectorDir = new Vector2(MathF.Cos(bisectorAngle), MathF.Sin(bisectorAngle));

        var intersectionPoint1 = center.Value + bisectorDir * radius.Value;
        var intersectionPoint2 = center.Value - bisectorDir * radius.Value;

        return Vector2.Distance(intersectionPoint1, point1) > Vector2.Distance(intersectionPoint2, point1)
            ? intersectionPoint1
            : intersectionPoint2;
    }


    public override void OnReset()
    {
        _sword1 = null;
        _sword2 = null;
    }


    private Element? ResolveElement(ResolvePosition resolvePosition, ClockwiseDirection clockwiseDirection)
    {
        var elementName = (resolvePosition, clockwiseDirection) switch
        {
            (ResolvePosition.ZephiranFaceToFace, ClockwiseDirection.Clockwise) => "G2CW",
            (ResolvePosition.ZephiranFaceToFace, ClockwiseDirection.CounterClockwise) => "G2CCW",
            (ResolvePosition.ZephiranBack, ClockwiseDirection.Clockwise) => "G1CW",
            (ResolvePosition.ZephiranBack, ClockwiseDirection.CounterClockwise) => "G1CCW",
            _ => ""
        };

        return Controller.GetElementByName(elementName);
    }

    private ZephiranDirection GetZephiranDirection(IBattleChara target)
    {
        if (target.NameId != 0xE31) return ZephiranDirection.None;
        var isEast = target.Position.X > _center.X;
        var isNorth = target.Position.Z < _center.Y;
        return (isEast, isNorth) switch
        {
            (true, true) => ZephiranDirection.NorthEast,
            (true, false) => ZephiranDirection.SouthEast,
            (false, false) => ZephiranDirection.SouthWest,
            (false, true) => ZephiranDirection.NorthWest
        };
    }


    public override void OnSettingsDraw()
    {
        ImGui.Text("General Settings");
        ImGui.Indent();
        ImGui.Text("Pair Character Name");
        ImGui.SameLine();
        ImGuiEx.Spacing();
        if (ImGui.Button("Perform test")) SelfTest();

        ImGui.InputText("##PairCharacterName", ref C.PairCharacterName, 32);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150);
        if (ImGui.BeginCombo("##partysel", "Select from party"))
        {
            foreach (var x in FakeParty.Get())
                if (ImGui.Selectable(x.Name.ToString()))
                    C.PairCharacterName = x.Name.ToString();
            ImGui.EndCombo();
        }

        ImGui.Text("Resolve Position");
        ImGuiEx.EnumCombo("##Resolve Position", ref C.ResolvePosition);
        ImGui.Unindent();

        ImGui.Text("Other Settings");
        ImGui.Indent();
        ImGui.Checkbox("Look Face", ref C.LockFace);
        ImGui.SameLine();
        ImGuiEx.HelpMarker(
            "This feature might be dangerous. Do NOT use when streaming. Make sure no other software implements similar option.\n\nThis will lock your face to the monitor, use with caution.\n\n自動で視線を調整します。ストリーミング中は使用しないでください。他のソフトウェアが同様の機能を実装していないことを確認してください。",
            EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());

        if (C.LockFace)
        {
            ImGui.Indent();
            ImGui.Checkbox("Lock Face Enable When Not Moving", ref C.LockFaceEnableWhenNotMoving);
            ImGui.SameLine();
            ImGuiEx.HelpMarker(
                "This will enable lock face when you are not moving. Be sure to enable it..\n\n動いていないときに視線をロックします。必ず有効にしてください。",
                EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
            ImGui.Unindent();
        }

        ImGui.Checkbox("Check on Start", ref C.ShouldCheckOnStart);

        ImGui.Unindent();
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (!C.ShouldCheckOnStart)
            return;
        if (category == DirectorUpdateCategory.Commence ||
            (category == DirectorUpdateCategory.Recommence && Controller.Phase == 2))
            SelfTest();
    }

    private void SelfTest()
    {
        Svc.Chat.PrintChat(new XivChatEntry
        {
            Message = new SeStringBuilder()
                .AddUiForeground("= P2 Sancity of The Ward First self-test =", (ushort)UIColor.LightBlue).Build()
        });
        var party = FakeParty.Get();
        var hasPairCharacter = party.Any(x => x.Name.ToString() == C.PairCharacterName);
        if (hasPairCharacter)
            Svc.Chat.PrintChat(new XivChatEntry
                { Message = new SeStringBuilder().AddUiForeground("Test Success!", (ushort)UIColor.Green).Build() });
        else
            Svc.Chat.PrintChat(new XivChatEntry
            {
                Message = new SeStringBuilder()
                    .AddUiForeground($"Could not find player {C.PairCharacterName}\n", (ushort)UIColor.Red)
                    .AddUiForeground("!!! Test failed !!!", (ushort)UIColor.Red).Build()
            });
    }

    private enum ClockwiseDirection
    {
        None,
        Clockwise,
        CounterClockwise
    }

    private enum ResolvePosition
    {
        ZephiranFaceToFace,
        ZephiranBack
    }


    private enum ZephiranDirection
    {
        None,
        NorthEast,
        SouthEast,
        SouthWest,
        NorthWest
    }

    private class Config : IEzConfig
    {
        public bool LockFace = true;
        public bool LockFaceEnableWhenNotMoving = true;
        public string PairCharacterName = "";
        public ResolvePosition ResolvePosition = ResolvePosition.ZephiranFaceToFace;
        public bool ShouldCheckOnStart = true;
    }
}