using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Shadowbringers.The_Epic_Of_Alexander;

public class TEA_P2_1211_Transition : SplatoonScript
{
    private const uint HawkBlastActionEffectId = 18480;

    // May be an array, but use a dictionary to highlight numbers.
    private readonly Dictionary<int, Vector2> _baitPositions = new()
    {
        { 1, new Vector2(120, 100) },
        { 2, new Vector2(110, 100) },
        { 3, new Vector2(100, 120) },
        { 4, new Vector2(100, 110) },
        { 5, new Vector2(85, 113) },
        { 6, new Vector2(90, 108) },
        { 7, new Vector2(80, 100) },
        { 8, new Vector2(85, 105) }
    };

    private readonly Vector2[] _flarePositions =
    [
        new Vector2(90, 90),
        new Vector2(100, 86),
        new Vector2(110, 90),
        new Vector2(114, 100),
        new Vector2(110, 110),
        new Vector2(100, 114),
        new Vector2(90, 110),
        new Vector2(86, 100)
    ];

    private readonly Vector2[] _safePositions =
    [
        new Vector2(108, 90),
        new Vector2(110, 98),
        new Vector2(108, 106),
        new Vector2(102, 112),
        new Vector2(90, 110),
        new Vector2(85, 102),
        new Vector2(88, 90)
    ];

    private HawkBlastDirection _firstBlastDirection = HawkBlastDirection.None;

    private int _hawkBlastCount;
    private bool _mechanicActive;

    private int _myNumber;

    public override HashSet<uint> ValidTerritories => [887];
    public override Metadata Metadata => new(4, "Garume");

    private Config C => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        for (var i = 1; i <= 8; i++)
        {
            var bait = new Element(0)
            {
                Enabled = false,
                overlayText = i.ToString(),
                color = 0xFFFF0000
            };
            Controller.RegisterElement($"Bait{i}", bait);
        }

        for (var i = 1; i <= 7; i++)
        {
            var safe = new Element(0)
            {
                Enabled = false,
                overlayText = $"Safe {i}",
                radius = 1.5f,
                color = 0xFF00FF00
            };
            Controller.RegisterElement($"Safe{i}", safe);
        }

        var fa = new Element(0)
        {
            Enabled = false,
            radius = 10f,
            Filled = true
        };
        Controller.RegisterElement("Flare_a", fa, true);

        var fb = new Element(0)
        {
            Enabled = false,
            radius = 10f,
            Filled = true
        };
        Controller.RegisterElement("Flare_b", fb, true);

        var fm = new Element(0)
        {
            Enabled = false,
            offX = 100f,
            offY = 100f,
            radius = 10f,
            Filled = true
        };
        Controller.RegisterElement("Flare_m", fm, true);

        C.BaitMessageIS.En = "Turn to face the outside here.";
        C.BaitMessageIS.Jp = "ここで外を向け！";
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (vfxPath.StartsWith("vfx/lockon/eff/m0361trg_a"))
        {
            if (AttachedInfo.VFXInfos.TryGetValue(Svc.ClientState.LocalPlayer.Address, out var info))
                if (info.OrderBy(x => x.Value.Age)
                    .TryGetFirst(x => x.Key.StartsWith("vfx/lockon/eff/m0361trg_a"), out var effect))
                    _myNumber = int.Parse(effect.Key.Replace("vfx/lockon/eff/m0361trg_a", "")[0].ToString());

            _mechanicActive = true;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!_mechanicActive || set.Action.RowId != HawkBlastActionEffectId) return;

        _hawkBlastCount++;
        if (_myNumber == 0 || _hawkBlastCount >= 19) return;
        if (_firstBlastDirection == HawkBlastDirection.None)
        {
            if (Vector2.Distance(set.Position.ToVector2(), new Vector2(100f, 85f)) < 5f)
                _firstBlastDirection = HawkBlastDirection.North;
            else if (Vector2.Distance(set.Position.ToVector2(), new Vector2(110f, 90f)) < 5f)
                _firstBlastDirection = HawkBlastDirection.Northeast;
            else if (Vector2.Distance(set.Position.ToVector2(), new Vector2(115f, 100f)) < 5f)
                _firstBlastDirection = HawkBlastDirection.East;
            else if (Vector2.Distance(set.Position.ToVector2(), new Vector2(110f, 110f)) < 5f)
                _firstBlastDirection = HawkBlastDirection.Southeast;
            else
                return;

            PluginLog.Log(
                $"Blast Position: {set.Position.X} {set.Position.Y} {set.Position.Z}  First Blast Direction: {_firstBlastDirection} for MyNumber: {_myNumber}");
        }

        Controller.GetRegisteredElements().Where(x => !x.Key.StartsWith("Flare")).Each(x => x.Value.Enabled = false);

        for (var i = 0; i < 8; i++)
        {
            var bait = Controller.GetElementByName($"Bait{i + 1}");
            bait!.Enabled = true;
            bait.tether = false;
            RotatedElement(ref bait, _baitPositions[i + 1], _firstBlastDirection);
            if (i + 1 == _myNumber)
            {
                bait.overlayText = C.BaitMessageIS.Get();
                bait.overlayFScale = 2f;
            }
            else
            {
                bait.overlayText = (i + 1).ToString();
                bait.overlayFScale = 1f;
            }
        }

        for (var i = 0; i < 7; i++)
        {
            var safe = Controller.GetElementByName($"Safe{i + 1}");
            safe!.Enabled = true;
            safe.tether = false;
            RotatedElement(ref safe, _safePositions[i], _firstBlastDirection);
        }

        // display safe and bait positions
        switch (_hawkBlastCount)
        {
            // To Safe 1
            case 1 or 2:
                EnableTetherElement("Safe1", "<1>");
                break;
            // To Safe 2
            case 3 or 4:
                EnableTetherElement("Safe2", "<1>");
                break;
            // To Safe 3 and Bait 1
            case 5 or 6 when _myNumber == 1:
                EnableTetherElement("Bait1", "<1>");
                break;
            case 5 or 6 when _myNumber == 2:
                EnableTetherElement("Bait2", "<1>");
                break;
            case 5 or 6:
                EnableTetherElement("Safe3", "<1>");
                break;
            // To Safe 4
            case 7 or 8:
                EnableTetherElement("Safe4", "<1>");
                break;
            // To Safe 4 and Bait 2
            case 9 or 10 when _myNumber == 3:
                EnableTetherElement("Bait3", "<1>");
                break;
            case 9 or 10 when _myNumber == 4:
                EnableTetherElement("Bait4", "<1>");
                break;
            case 9 or 10:
                EnableTetherElement("Safe4", "<1>");
                break;
            // To Safe 5
            case 11 or 12:
                EnableTetherElement("Safe5", "<1>");
                break;
            // To Safe 6 and Bait 3
            case 13 or 14 when _myNumber == 5:
                EnableTetherElement("Bait6", "<1>");
                break;
            case 13 or 14 when _myNumber == 6:
                EnableTetherElement("Bait6", "<1>");
                break;
            case 13 or 14:
                EnableTetherElement("Safe6", "<1>");
                break;
            // To Safe 7 and Bait 4
            case 15 or 16 or 17 when _myNumber == 7:
                EnableTetherElement("Bait7", "<1>");
                break;
            case 15 or 16 or 17 when _myNumber == 8:
                EnableTetherElement("Bait8", "<1>");
                break;
            case 15 or 16 or 17:
                EnableTetherElement("Safe7", "<1>");
                break;
        }

        if (C.ShoulDisplayFlares)
            switch (_hawkBlastCount)
            {
                // display flares
                case 1:
                case 3:
                case 5:
                case 10:
                case 12:
                case 14:
                    SetNextFlareElement("Flare_a", set.Position.ToVector2());
                    break;
                case 2:
                case 4:
                case 6:
                case 11:
                case 13:
                case 15:
                    SetNextFlareElement("Flare_b", set.Position.ToVector2());
                    break;
                case 7:
                case 16:
                    SetNextFlareElement("Flare_a", set.Position.ToVector2(), false);
                    break;
                case 8:
                case 17:
                {
                    SetNextFlareElement("Flare_b", set.Position.ToVector2(), false);
                    var flareM = Controller.GetElementByName("Flare_m");
                    flareM!.Enabled = true;
                    break;
                }
                case 9:
                {
                    var flareM = Controller.GetElementByName("Flare_m");
                    flareM!.Enabled = false;
                    var flareA = Controller.GetElementByName("Flare_a");
                    flareA!.Enabled = true;
                    var flareB = Controller.GetElementByName("Flare_b");
                    flareB!.Enabled = true;
                    break;
                }
            }

        if (_hawkBlastCount >= 18) Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnReset()
    {
        Reset();
    }

    private void Reset()
    {
        _mechanicActive = false;
        _myNumber = 0;
        _hawkBlastCount = 0;
        _firstBlastDirection = HawkBlastDirection.None;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    private void RotatedElement(ref Element element, Vector2 position, HawkBlastDirection direction)
    {
        var rotationDegree = direction switch
        {
            HawkBlastDirection.North => -45,
            HawkBlastDirection.Northeast => 0,
            HawkBlastDirection.East => 45,
            HawkBlastDirection.Southeast => -90,
            _ => 0
        };

        var center = new Vector2(100f, 100f);
        var x = position.X - center.X;
        var y = position.Y - center.Y;
        var x2 = x * Math.Cos(rotationDegree * Math.PI / 180) - y * Math.Sin(rotationDegree * Math.PI / 180);
        var y2 = x * Math.Sin(rotationDegree * Math.PI / 180) + y * Math.Cos(rotationDegree * Math.PI / 180);
        element.offX = (float)(x2 + center.X);
        element.offY = (float)(y2 + center.Y);
    }

    private void EnableTetherElement(string elementName, string actorPlaceholder)
    {
        if (Controller.TryGetElementByName(elementName, out var element))
        {
            element.Enabled = true;
            element.tether = true;
            element.refActorPlaceholder = [actorPlaceholder];
        }
    }


    private void SetNextFlareElement(string elementName, Vector2 current, bool enabled = true)
    {
        if (Controller.TryGetElementByName(elementName, out var element))
        {
            element.Enabled = enabled;
            var nextFlare = new Vector2(100f, 100f);
            for (var i = 0; i < _flarePositions.Length; i++)
            {
                if (!(Vector2.Distance(current, _flarePositions[i]) < 5f)) continue;
                nextFlare = _flarePositions[(i + 1) % _flarePositions.Length];
                break;
            }

            element.offX = nextFlare.X;
            element.offY = nextFlare.Y;
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Bait Message");
        ImGuiEx.HelpMarker(Loc(en:"The message that will be displayed on your bait.", jp:"あなたの番号の立ち位置に表示されるメッセージ。"));
        var showString = C.BaitMessageIS.Get();
        C.BaitMessageIS.ImGuiEdit(ref showString, "The message that will be displayed on your bait.");
        ImGui.Text("Display Flares");
        ImGuiEx.HelpMarker(Loc(en:"Display flares on the ground to indicate the next safe spot.", jp:"次の安全地帯を示すために地面にフレアを表示します。"));
        ImGui.Checkbox("", ref C.ShoulDisplayFlares);
    }

    private enum HawkBlastDirection : byte
    {
        None,
        North,
        Northeast,
        East,
        Southeast
    }

    private class Config : IEzConfig
    {
        //public string BaitMessage = "Turn to face the outside here.";
        public InternationalString BaitMessageIS = new();
        public bool ShoulDisplayFlares = true;
    }
}