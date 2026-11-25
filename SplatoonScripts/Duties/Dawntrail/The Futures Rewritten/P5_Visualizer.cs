using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P5_Visualizer : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [1238];

    #endregion

    #region Constants

    private const uint CastAkhMorn = 40310;
    private const uint CastWingLight = 40313;
    private const uint CastWingDark = 40233;
    private const uint HitLight = 40314;
    private const uint HitDark = 40315;
    private const uint CastWave = 40307;
    private const float FillIntensity = 0.5f;

    private const string LayoutAkhMorn = "Akh Morn";
    private const string LayoutPolarizing = "Polarizing Strikes";
    private const string LayoutParadise = "Paradise Regained";
    private const string LayoutFulgent = "Fulgent Blade";

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();

    #endregion

    #region State

    private uint _akhCaster;
    private uint _wingCast;
    private uint _wingSource;
    private bool _wingSecond;
    private uint _wingTarget;
    private readonly List<WaveOrigin> _waves = [];

    #endregion

    #region Types

    private sealed class Config : IEzConfig
    {
        public bool ShowAkhMorn = true;
        public bool ShowPolarizing = true;
        public bool ShowParadise = true;
        public bool ShowFulgent = true;
        public Vector4 LightColor = 0xC800D5FFu.ToVector4();
        public Vector4 DarkColor = 0xC8FF00FFu.ToVector4();
    }

    private sealed class WaveOrigin
    {
        public Vector3 Position;
        public float Rotation;
        public long StartedAt;
        public string Plus = "";
        public string Minus = "";
    }

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        Controller.TryRegisterLayoutFromCode(LayoutFulgent, """
            ~Lv2~{"Name":"Fulgent Blade","ZoneLockH":[1238],"ElementsL":[]}
            """, out _, overwrite: true);
        Controller.TryRegisterLayoutFromCode(LayoutAkhMorn, """
            ~Lv2~{"Name":"Akh Morn","ZoneLockH":[1238],"ElementsL":[{"Name":"Pandra Right","type":1,"Enabled":false,"radius":2.75,"Donut":0.25,"color":4294902015,"fillIntensity":0.5,"refActorComparisonType":2},{"Name":"Pandra Left","type":1,"Enabled":false,"radius":2.75,"Donut":0.25,"color":4278255615,"fillIntensity":0.5,"refActorComparisonType":2},{"Name":"Separator","type":3,"Enabled":false,"refY":10.0,"offY":-10.0,"radius":0.0,"color":4294967295,"fillIntensity":0.5,"thicc":10.0,"refActorDataID":17839,"refActorRequireCast":true,"refActorCastId":[40310],"refActorComparisonType":3,"includeRotation":true}]}
            """, out _, overwrite: true);
        Controller.TryRegisterLayoutFromCode(LayoutParadise, """
            ~Lv2~{"Name":"Paradise Regained","ZoneLockH":[1238],"ElementsL":[{"Name":"Target","type":1,"Enabled":false,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"[Light to Dark] 1st Cone","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-180,"coneAngleMax":45,"color":3355497983,"fillIntensity":0.5,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Target>"},{"Name":"[Light to Dark] 1st AOE","type":1,"Enabled":false,"radius":5.0,"color":3355497983,"fillIntensity":0.5,"refActorNPCNameID":13561,"TargetAlteration":1,"refActorComparisonType":6},{"Name":"[Light to Dark] 2nd Cone","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-45,"coneAngleMax":180,"color":3372155135,"fillIntensity":0.5,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Target>"},{"Name":"[Light to Dark] 2nd AOE","type":1,"Enabled":false,"radius":5.0,"color":3372155135,"fillIntensity":0.5,"refActorNPCNameID":13561,"TargetAlteration":1,"refActorComparisonType":6},{"Name":"[Dark to Light] 1st Cone","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-45,"coneAngleMax":180,"color":3372155135,"fillIntensity":0.5,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Target>"},{"Name":"[Dark to Light] 1st AOE","type":1,"Enabled":false,"radius":5.0,"color":3372155135,"fillIntensity":0.5,"refActorNPCNameID":13561,"TargetAlteration":1,"refActorComparisonType":6},{"Name":"[Dark to Light] 2nd Cone","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-180,"coneAngleMax":45,"color":3355497983,"fillIntensity":0.5,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Target>"},{"Name":"[Dark to Light] 2nd AOE","type":1,"Enabled":false,"radius":5.0,"color":3355497983,"fillIntensity":0.5,"refActorNPCNameID":13561,"TargetAlteration":1,"refActorComparisonType":6}]}
            """, out _, overwrite: true);
        Controller.TryRegisterLayoutFromCode(LayoutPolarizing, """
            ~Lv2~{"Name":"Polarizing Strikes","ZoneLockH":[1238],"ElementsL":[{"Name":"L","type":1,"Enabled":false,"radius":0.0,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Left Line","type":3,"Enabled":false,"offY":20.0,"radius":3.0,"color":4278255615,"fillIntensity":0.5,"refActorRequireCast":true,"refActorCastId":[40234,40316],"refActorDataID":17839,"refActorComparisonType":3,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:L>"},{"Name":"R","type":1,"Enabled":false,"radius":0.0,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Right Line","type":3,"Enabled":false,"offY":20.0,"radius":3.0,"color":4294902015,"fillIntensity":0.5,"refActorRequireCast":true,"refActorCastId":[40234,40316],"refActorDataID":17839,"refActorComparisonType":3,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:R>"}]}
            """, out _, overwrite: true);
    }

    public override void OnUpdate()
        => Refresh();

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == CastAkhMorn)
            _akhCaster = source;
        else if(castId == 40319)
            ClearParadise();
        else if(castId is CastWingLight or CastWingDark)
            BeginWing(castId, source);
        else if(castId == CastWave)
            RememberWave(source);
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null)
            return;

        var id = set.Action.Value.RowId;
        if(id == CastWingDark && _wingCast == 0)
            BeginWing(CastWingDark, set.Source?.EntityId ?? 0);
        else if(_wingCast is CastWingLight or CastWingDark && id is HitLight or HitDark)
        {
            var end = _wingCast == CastWingLight ? HitDark : HitLight;
            if(id == end)
                ClearParadise();
            else if(!_wingSecond)
            {
                _wingSecond = true;
                _wingTarget = TopHate();
            }
        }
        else if(id == CastAkhMorn)
        {
            _akhCaster = 0;
            ClearWaves();
        }
        else if(id == CastWave)
        {
            var wave = _waves.FirstOrDefault(item => item.StartedAt == 0);
            if(wave != null)
                wave.StartedAt = Environment.TickCount64;
        }
    }

    public override void OnReset()
    {
        _akhCaster = 0;
        ClearParadise();
        ClearWaves();
        Refresh();
    }

    public override void OnSettingsDraw()
    {
        ImGui.TextDisabled("Visualizer");
        ImGui.Separator();
        ImGui.Checkbox("Fulgent Blade", ref C.ShowFulgent);
        ImGui.Checkbox("Akh Morn", ref C.ShowAkhMorn);
        ImGui.Checkbox("Paradise Regained", ref C.ShowParadise);
        ImGui.Checkbox("Polarizing Strikes", ref C.ShowPolarizing);

        ImGui.Spacing();
        ImGui.TextDisabled("Colors");
        ImGui.Separator();
        ImGui.ColorEdit4("Light", ref C.LightColor, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Dark", ref C.DarkColor, ImGuiColorEditFlags.NoInputs);
    }

    #endregion

    #region Private Method

    // Refreshes every mechanic from the current state.
    private void Refresh()
    {
        var light = C.LightColor.ToUint();
        var dark = C.DarkColor.ToUint();
        ShowAkhMorn(light, dark);
        ShowPolarizing(light, dark);
        ShowParadise(light, dark);
        ShowFulgent(light, dark);
    }

    // Returns one element from a registered layout.
    private Element? FindElement(string layoutName, string elementName)
        => Controller.GetRegisteredLayouts().SafeSelect(layoutName)?.GetElement(elementName);

    // Shows or hides one layout element.
    private void SetEnabled(string layoutName, string elementName, bool enabled)
    {
        var element = FindElement(layoutName, elementName);
        if(element != null)
            element.Enabled = enabled;
    }

    // Writes an object id onto one layout element.
    private void SetActor(string layoutName, string elementName, uint objectId)
    {
        var element = FindElement(layoutName, elementName);
        if(element == null)
            return;

        element.refActorObjectID = objectId;
    }

    // Writes one element's color.
    private void SetColor(string layoutName, string elementName, uint color)
    {
        var element = FindElement(layoutName, elementName);
        if(element == null)
            return;

        element.color = color;
        element.fillIntensity = FillIntensity;
    }

    // Shows or hides the Akh Morn donuts from the current caster.
    private void ShowAkhMorn(uint light, uint dark)
    {
        SetColor(LayoutAkhMorn, "Pandra Left", light);
        SetColor(LayoutAkhMorn, "Pandra Right", dark);
        ShowSides(LayoutAkhMorn, C.ShowAkhMorn && _akhCaster != 0, _akhCaster, "Pandra Right", "Pandra Left", "Separator");
    }

    // Shows the sword lines while either sword cast is in progress.
    private void ShowPolarizing(uint light, uint dark)
    {
        SetColor(LayoutPolarizing, "Left Line", light);
        SetColor(LayoutPolarizing, "Right Line", dark);
        var caster = C.ShowPolarizing
            ? Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.IsCasting(40316, 40234))
            : null;
        ShowSides(LayoutPolarizing, caster != null, caster?.EntityId ?? 0, "R", "L", "Right Line", "Left Line");
    }

    // Enables a facing pair and aims it at the closest player on each side.
    private void ShowSides(string layout, bool active, uint source, string right, string left, params string[] extras)
    {
        SetEnabled(layout, right, active);
        SetEnabled(layout, left, active);
        foreach(var name in extras)
            SetEnabled(layout, name, active);

        if(!active || !source.TryGetObject(out var caster))
        {
            SetActor(layout, right, 0);
            SetActor(layout, left, 0);
            return;
        }

        SetActor(layout, right, PickPlayer(caster, true));
        SetActor(layout, left, PickPlayer(caster, false));
    }

    // Closest living party member on one side. Right is 0-180, left is 180-360.
    private uint PickPlayer(IGameObject origin, bool right)
    {
        var facing = NormalizeAngle(-origin.Rotation * (180f / MathF.PI) + 180f);
        var picked = Controller.GetPartyMembers()
            .Where(player => player.CurrentHp > 0)
            .Select(player =>
            {
                var flat = player.Position - origin.Position;
                return (
                    Player: player,
                    Angle: NormalizeAngle(MathHelper.GetRelativeAngle(origin.Position, player.Position) - facing),
                    Distance: MathF.Sqrt(flat.X * flat.X + flat.Z * flat.Z));
            })
            .Where(x => right ? x.Angle < 180f : x.Angle >= 180f)
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Player.EntityId)
            .FirstOrDefault();
        return picked.Player?.EntityId ?? 0;
    }

    // Wraps a degree into the range 0 inclusive to 360 exclusive.
    private static float NormalizeAngle(float degree)
        => (degree % 360f + 360f) % 360f;

    // Shows the active wing pair. The circle follows the tether by itself.
    private void ShowParadise(uint light, uint dark)
    {
        var active = C.ShowParadise && _wingCast != 0;
        var wingLight = active && _wingCast == CastWingLight;
        var wingDark = active && _wingCast == CastWingDark;
        ShowWing("[Light to Dark] 1st", wingLight && !_wingSecond, light);
        ShowWing("[Light to Dark] 2nd", wingLight && _wingSecond, dark);
        ShowWing("[Dark to Light] 1st", wingDark && !_wingSecond, dark);
        ShowWing("[Dark to Light] 2nd", wingDark && _wingSecond, light);
        SetEnabled(LayoutParadise, "Target", active);
        if(active)
            SetActor(LayoutParadise, "Target", _wingTarget);
    }

    // Shows or hides one wing step and paints its cone and circle.
    private void ShowWing(string prefix, bool enabled, uint color)
    {
        SetColor(LayoutParadise, $"{prefix} Cone", color);
        SetColor(LayoutParadise, $"{prefix} AOE", color);
        SetEnabled(LayoutParadise, $"{prefix} Cone", enabled);
        SetEnabled(LayoutParadise, $"{prefix} AOE", enabled);
        SetActor(LayoutParadise, $"{prefix} Cone", enabled ? _wingSource : 0);
    }

    // Party member with the highest HUD enmity.
    private static unsafe uint TopHate()
    {
        var hud = AgentHUD.Instance();
        if(hud == null)
            return 0;

        var span = hud->HudPartyMemberEnmity;
        var limit = Math.Min(hud->PartyMemberCount, span.Length);
        uint bestId = 0;
        var best = int.MinValue;
        for(var i = 0; i < limit; i++)
        {
            var entry = span[i];
            if(entry.EntityId == 0 || entry.Enmity <= best)
                continue;
            best = entry.Enmity;
            bestId = entry.EntityId;
        }

        return bestId;
    }

    // Places each queued band from its own timer.
    private void ShowFulgent(uint light, uint dark)
    {
        foreach(var wave in _waves)
        {
            SetColor(LayoutFulgent, wave.Plus, light);
            SetColor(LayoutFulgent, wave.Minus, dark);
        }

        if(!C.ShowFulgent)
        {
            HideFulgent();
            return;
        }

        var now = Environment.TickCount64;
        foreach(var wave in _waves)
        {
            var step = wave.StartedAt == 0 ? 0 : 1 + (int)((now - wave.StartedAt) / 2000);
            var near = 5f * step;
            var far = near + 5f;
            SetEnabled(LayoutFulgent, wave.Plus, true);
            SetEnabled(LayoutFulgent, wave.Minus, true);
            PlaceBand(wave.Plus, wave, far, near);
            PlaceBand(wave.Minus, wave, -far, -near);
        }
    }

    // Writes one side of a band from a saved position and facing.
    private void PlaceBand(string name, WaveOrigin origin, float near, float far)
    {
        var element = FindElement(LayoutFulgent, name);
        if(element == null)
            return;

        var start = Shift(near);
        var stop = Shift(far);
        element.refX = start.X;
        element.refY = start.Y;
        element.refZ = start.Z;
        element.offX = stop.X;
        element.offY = stop.Y;
        element.offZ = stop.Z;

        Vector3 Shift(float along)
            => Utils.RotatePoint(origin.Position.X, origin.Position.Z, -origin.Rotation, new Vector3(
                origin.Position.X,
                origin.Position.Z + along,
                origin.Position.Y));
    }

    // Hides every queued band.
    private void HideFulgent()
    {
        foreach(var wave in _waves)
        {
            SetEnabled(LayoutFulgent, wave.Plus, false);
            SetEnabled(LayoutFulgent, wave.Minus, false);
        }
    }

    // Drops the saved wave origins.
    private void ClearWaves()
    {
        HideFulgent();
        _waves.Clear();
    }

    // Starts one wing color on its caster. The first cone faces the current top hate.
    private void BeginWing(uint castId, uint source)
    {
        _wingCast = castId;
        _wingSource = source;
        _wingSecond = false;
        _wingTarget = TopHate();
    }

    // Drops Paradise Regained back to idle.
    private void ClearParadise()
    {
        _wingCast = 0;
        _wingSource = 0;
        _wingSecond = false;
        _wingTarget = 0;
    }

    // Queues one cast position and facing, and makes sure its two lines exist.
    private void RememberWave(uint source)
    {
        if(!source.TryGetObject(out var caster))
            return;

        var index = _waves.Count + 1;
        var wave = new WaveOrigin
        {
            Position = caster.Position,
            Rotation = caster.Rotation,
            Plus = $"{index}+",
            Minus = $"{index}-",
        };
        _waves.Add(wave);

        var layout = Controller.GetRegisteredLayouts().SafeSelect(LayoutFulgent);
        if(layout == null)
            return;

        AddBand(layout, wave.Plus, 40f, C.LightColor.ToUint());
        AddBand(layout, wave.Minus, 40.42f, C.DarkColor.ToUint());
    }

    // Adds one fixed line if that side is not registered yet.
    private static void AddBand(Layout layout, string name, float radius, uint color)
    {
        var element = layout.GetElement(name);
        if(element == null)
        {
            element = new Element(2)
            {
                Name = name,
                Enabled = false,
                radius = radius,
            };
            layout.ElementsL.Add(element);
        }

        element.color = color;
        element.fillIntensity = FillIntensity;
    }

    #endregion
}
