using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public class SanctityOfTheWardFirst : SplatoonScript
{
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

    private readonly Vector2 _center = new(100, 100);
    private ClockwiseDirection _clockwiseDirection;

    private Vector2 _eyesPosition;

    private IGameObject? _sword1;
    private IGameObject? _sword2;

    private ZephiranDirection _zephiranDirection;
    public override HashSet<uint>? ValidTerritories => [968];
    public override Metadata? Metadata => new(2, "Garume");
    private bool IsStart => _sword1 != null && _sword2 != null;
    private Config C => Controller.GetConfig<Config>();
    private IBattleChara? Zephiran => Svc.Objects.OfType<IBattleChara>().FirstOrDefault(x => x.NameId == 0xE31);

    private IBattleChara? Adelphel => Svc.Objects.OfType<IBattleChara>()
        .FirstOrDefault(x => x.NameId == 0xE32 && x.IsCharacterVisible());

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if (!IsStart) return;

        PluginLog.Log($"MapEffect: {position}, {data1}, {data2}");

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

        // 1 sword
        if (vfxPath == "vfx/lockon/eff/m0244trg_a1t.avfx") _sword1 = target.GetObject();

        // 2 sword
        if (vfxPath == "vfx/lockon/eff/m0244trg_a2t.avfx") _sword2 = target.GetObject();

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
        if (target.NameId == 0xE31)
        {
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

        return ZephiranDirection.None;
    }


    public override void OnSettingsDraw()
    {
        ImGui.Text("Pair Character Name");
        ImGui.InputText("##PairCharacterName", ref C.PairCharacterName, 32);
        ImGuiEx.EnumCombo("Resolve Position", ref C.ResolvePosition);
    }

    private class Config : IEzConfig
    {
        public string PairCharacterName = "";
        public ResolvePosition ResolvePosition = ResolvePosition.ZephiranFaceToFace;
    }
}