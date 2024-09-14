using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public class SanctityOfTheWardFirst : SplatoonScript
{
    public enum ClockwiseDirection
    {
        None,
        Clockwise,
        CounterClockwise
    }

    public enum ResolvePosition
    {
        ZephiranFaceToFace,
        ZephiranBack
    }


    public enum ZephirinDirection
    {
        None,
        NorthEast,
        SouthEast,
        SouthWest,
        NorthWest
    }

    private readonly Vector2 Center = new(100, 100);
    private ClockwiseDirection _clockwiseDirection;
    private IGameObject? _sword1;
    private IGameObject? _sword2;

    private ZephirinDirection _zephirinDirection;
    public override HashSet<uint>? ValidTerritories => [968];
    public override Metadata? Metadata => new(1, "Garume");
    private bool IsStart => _sword1 != null && _sword2 != null;
    private Config C => Controller.GetConfig<Config>();
    private IBattleChara? Zephirin => Svc.Objects.OfType<IBattleChara>().FirstOrDefault(x => x.NameId == 0xE31);
    private IBattleChara? Adelphel => Svc.Objects.OfType<IBattleChara>().FirstOrDefault(x => x.NameId == 0xE32);


    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (IsStart) return;

        // 1 sword
        if (vfxPath == "vfx/lockon/eff/m0244trg_a1t.avfx") _sword1 = target.GetObject();

        // 2 sword
        if (vfxPath == "vfx/lockon/eff/m0244trg_a2t.avfx") _sword2 = target.GetObject();

        if (IsStart)
        {
            var zephirin = Zephirin;
            var adelphel = Adelphel;

            PluginLog.Log("Zephirin: " + zephirin);
            PluginLog.Log("Adelphel: " + adelphel);

            if (zephirin == null || adelphel == null) return;

            _zephirinDirection = GetZephirinDirection(zephirin);
            _clockwiseDirection = adelphel.Position.X > Center.X
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
            "{\"Name\":\"G2 CCW\",\"type\":1,\"offX\":3.24,\"offY\":35.0,\"radius\":0.5,\"color\":3372158208,\"Filled\":false,\"thicc\":5.0,\"refActorNPCNameID\":3633,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyVisible\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("G2CW",
            "{\"Name\":\"G2 CW\",\"type\":1,\"offX\":-3.44,\"offY\":35.0,\"radius\":0.5,\"color\":3372158208,\"Filled\":false,\"thicc\":5.0,\"refActorNPCNameID\":3633,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyVisible\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

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
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(e => e.Value.Enabled = false);

        if (!IsStart) return;

        if (_zephirinDirection != ZephirinDirection.None)
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
    }

    public override void OnReset()
    {
        _sword1 = null;
        _sword2 = null;
    }


    public Element? ResolveElement(ResolvePosition resolvePosition, ClockwiseDirection clockwiseDirection)
    {
        PluginLog.Log("Clockwise Direction: " + clockwiseDirection);
        PluginLog.Log("Resolve Position: " + resolvePosition);

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

    public ZephirinDirection GetZephirinDirection(IBattleChara target)
    {
        if (target.NameId == 0xE31)
        {
            var isEast = target.Position.X > Center.X;
            var isNorth = target.Position.Y > Center.Y;
            return (isEast, isNorth) switch
            {
                (true, true) => ZephirinDirection.NorthEast,
                (true, false) => ZephirinDirection.SouthEast,
                (false, false) => ZephirinDirection.SouthWest,
                (false, true) => ZephirinDirection.NorthWest
            };
        }

        return ZephirinDirection.None;
    }


    public override void OnSettingsDraw()
    {
        ImGui.Text("Pair Character Name");
        ImGui.InputText("##PairCharacterName", ref C.PairCharacterName, 32);
        ImGuiEx.EnumCombo("Resolve Position", ref C.ResolvePosition);
    }

    public class Config : IEzConfig
    {
        public string PairCharacterName = "";
        public ResolvePosition ResolvePosition = ResolvePosition.ZephiranFaceToFace;
    }
}