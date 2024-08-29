using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using ImGuiNET;
using Splatoon;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Shadowbringers;

public class TEA_P3_Wormhole_Formation : SplatoonScript
{
    private const uint WormholeDataId = 0x1EA1DF;

    private const uint ChakramCastId = 18517;

    private readonly Dictionary<int, Vector2[]> _baitPositions = new()
    {
        { 1, [new Vector2(86.5f, 86f), new Vector2(86.5f, 86f), new Vector2(81f, 99f), new Vector2(90f, 97f)] },
        { 2, [new Vector2(113.5f, 86f), new Vector2(113.5f, 86f), new Vector2(119f, 99f), new Vector2(110f, 103f)] },
        { 3, [new Vector2(87, 113), new Vector2(87f, 113f), new Vector2(81f, 101f), new Vector2(81f, 101f)] },
        { 4, [new Vector2(113, 113), new Vector2(113f, 113f), new Vector2(119f, 101f), new Vector2(119f, 101f)] },
        { 5, [new Vector2(84.45f, 89.65f), new Vector2(82f, 96f), new Vector2(86.5f, 86f), new Vector2(81f, 99f)] },
        { 6, [new Vector2(115.55f, 89.65f), new Vector2(118, 104f), new Vector2(113.5f, 86f), new Vector2(119f, 99f)] },
        { 7, [new Vector2(83, 93), new Vector2(81.5f, 100f), new Vector2(85f, 93f), new Vector2(86.5f, 114f)] },
        { 8, [new Vector2(117, 93), new Vector2(118.5f, 100f), new Vector2(115f, 107f), new Vector2(113.5f, 114f)] }
    };

    private readonly List<List<int>> _invertApplyIndex =
    [
        [],
        [5, 6],
        [7, 8],
        [1, 2]
    ];

    private TickScheduler? _chakramScheduler;
    private int _currentPhase;
    private bool _isStartWormholeFormation;
    private int _myNumber;
    private bool _shouldInvert;
    private int _wormholeChangedCount;

    public override HashSet<uint>? ValidTerritories => [887];
    public override Metadata? Metadata => new(2, "Garume");

    private Config C => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        for (var i = 1; i <= 8; i++)
        {
            var element = new Element(0)
            {
                radius = 0.35f,
                overlayVOffset = 2f,
                overlayFScale = 2f
            };

            Controller.RegisterElement($"Bait{i}", element, true);
        }
    }

    public override void OnMessage(string message)
    {
        if (message.Contains("アレキサンダー・プライムの「次元断絶のマーチ」")) _isStartWormholeFormation = true;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (!_isStartWormholeFormation) return;
        if (castId == ChakramCastId) _chakramScheduler ??= new TickScheduler(() => _currentPhase = 1, 5700);
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (!vfxPath.StartsWith("vfx/lockon/eff/m0361trg_a")) return;
        if (!AttachedInfo.VFXInfos.TryGetValue(Svc.ClientState.LocalPlayer.Address, out var info)) return;
        if (info.OrderBy(x => x.Value.Age)
            .TryGetFirst(x => x.Key.StartsWith("vfx/lockon/eff/m0361trg_a"), out var effect))
            _myNumber = int.Parse(effect.Key.Replace("vfx/lockon/eff/m0361trg_a", "")[0].ToString());
    }

    public override void OnObjectEffect(uint target, ushort data1, ushort data2)
    {
        var targetObject = target.GetObject();
        if (targetObject?.DataId != WormholeDataId) return;
        var wormholePosition = targetObject.Position.ToVector2();
        if (wormholePosition is { X: > 100, Y: < 100 } or { X: < 100, Y: > 100 }) _shouldInvert = true;
        _wormholeChangedCount++;
        if (_wormholeChangedCount is 3 or 5 or 7) _currentPhase++;
    }

    public override void OnReset()
    {
        _isStartWormholeFormation = false;
        _myNumber = 0;
        _shouldInvert = false;
        _currentPhase = 0;
        _wormholeChangedCount = 0;
        _chakramScheduler?.Dispose();
        _chakramScheduler = null;
    }

    public override void OnUpdate()
    {
        if (!_isStartWormholeFormation)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            return;
        }

        for (var i = 1; i <= 8; i++)
            if (Controller.TryGetElementByName($"Bait{i}", out var element))
            {
                var position = _baitPositions[i][_currentPhase];
                var number = i;
                if (_shouldInvert && _invertApplyIndex[_currentPhase].Contains(i))
                {
                    position = position with { X = 200 - position.X };
                    number = number % 2 == 0 ? number - 1 : number + 1;
                }

                element.SetOffPosition(position.ToVector3());
                element.Enabled = true;
                if (number == _myNumber)
                {
                    element.overlayText = C.BaitText;
                    element.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
                    element.tether = true;
                    element.thicc = 10f;
                }
                else if (C.ShouldDisplayOtherBait)
                {
                    element.overlayText = number.ToString();
                    element.color = C.OtherBaitColor.ToUint();
                    element.tether = false;
                    element.thicc = 2f;
                }
                else
                {
                    element.Enabled = false;
                }
            }
    }

    public override void OnSettingsDraw()
    {
        if (ImGui.CollapsingHeader("My Bait Settings:"))
        {
            ImGui.Indent();
            ImGui.Text("Bait Text:");
            ImGui.InputText("", ref C.BaitText, 100);
            ImGui.Text("Bait Color:");
            ImGuiComponents.HelpMarker(
                "Change the color of the bait and the text that will be displayed on your bait.\nSetting different values makes it rainbow.");
            ImGui.Indent();
            ImGui.ColorEdit4("Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
            ImGui.SameLine();
            ImGui.ColorEdit4("Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
            ImGui.Unindent();
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Other Bait Settings:"))
        {
            ImGui.Indent();
            ImGui.Checkbox("Display Other Bait", ref C.ShouldDisplayOtherBait);
            ImGui.Text("Other Bait Color:");
            ImGuiComponents.HelpMarker("Change the color of the bait that will be displayed on other bait.");
            ImGui.Indent();
            ImGui.ColorEdit4("Color", ref C.OtherBaitColor, ImGuiColorEditFlags.NoInputs);
            ImGui.Unindent();
            ImGui.Unindent();
        }
    }

    public class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public string BaitText = "Go Here";
        public Vector4 OtherBaitColor = 0xFFFF0000.ToVector4();
        public bool ShouldDisplayOtherBait = true;
    }
}