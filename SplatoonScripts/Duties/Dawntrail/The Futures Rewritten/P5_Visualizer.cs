using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
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
    private const uint CastPolarizing = 40316;
    private const uint CastWingLight = 40313;
    private const uint CastWingDark = 40233;
    private const uint CastWave = 40307;

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
    private uint _swordCaster;
    private bool _polarizing;
    private int _polarizingHits;
    private uint _wingCast;
    private bool _wingSecond;
    private uint _mainTank;
    private uint _subTank;
    private readonly List<WaveOrigin> _waves = [];

    #endregion

    #region Types

    private sealed class Config : IEzConfig
    {
        public bool ShowAkhMorn = true;
        public bool ShowPolarizing = true;
        public bool ShowParadise = true;
        public bool ShowFulgent = true;
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
            ~Lv2~{"Name":"Akh Morn","ZoneLockH":[1238],"ElementsL":[{"Name":"Pandra Right","type":1,"Enabled":false,"radius":2.75,"Donut":0.25,"color":4294902015,"fillIntensity":0.214,"refActorComparisonType":2},{"Name":"Pandra Left","type":1,"Enabled":false,"radius":2.75,"Donut":0.25,"color":4278255615,"fillIntensity":0.214,"refActorComparisonType":2},{"Name":"Separator","type":3,"Enabled":false,"refY":10.0,"offY":-10.0,"radius":0.0,"fillIntensity":0.197,"thicc":10.0,"refActorDataID":17839,"refActorRequireCast":true,"refActorCastId":[40310],"refActorComparisonType":3,"includeRotation":true}]}
            """, out _, overwrite: true);
        Controller.TryRegisterLayoutFromCode(LayoutParadise, """
            ~Lv2~{"Name":"Paradise Regained","ZoneLockH":[1238],"ElementsL":[{"Name":"MT","type":1,"Enabled":false,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"ST","type":1,"Enabled":false,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"[Light to Dark] 1st Cone","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-180,"coneAngleMax":45,"color":3355497983,"fillIntensity":0.2,"refActorDataID":17839,"refActorComparisonType":3,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:MT>"},{"Name":"[Light to Dark] 1st AOE","type":1,"Enabled":false,"radius":5.0,"color":3355497983,"fillIntensity":0.2,"refActorDataID":17839,"TargetAlteration":2100,"refActorComparisonType":3,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:MT>"},{"Name":"[Light to Dark] 2nd Cone","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-45,"coneAngleMax":180,"color":3372155135,"fillIntensity":0.2,"refActorDataID":17839,"refActorComparisonType":3,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:ST>"},{"Name":"[Light to Dark] 2nd AOE","type":1,"Enabled":false,"radius":5.0,"color":3372155135,"fillIntensity":0.2,"refActorDataID":17839,"TargetAlteration":1100,"refActorComparisonType":3,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:MT>"},{"Name":"[Dark to Light] 1st Cone","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-45,"coneAngleMax":180,"color":3372155135,"fillIntensity":0.2,"refActorDataID":17839,"refActorComparisonType":3,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:ST>"},{"Name":"[Dark to Light] 1st AOE","type":1,"Enabled":false,"radius":5.0,"color":3372155135,"fillIntensity":0.2,"refActorDataID":17839,"TargetAlteration":1100,"refActorComparisonType":3,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:MT>"},{"Name":"[Dark to Light] 2nd Cone","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-180,"coneAngleMax":45,"color":3355497983,"fillIntensity":0.2,"refActorDataID":17839,"refActorComparisonType":3,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:MT>"},{"Name":"[Dark to Light] 2nd AOE","type":1,"Enabled":false,"radius":5.0,"color":3355497983,"fillIntensity":0.2,"refActorDataID":17839,"TargetAlteration":2100,"refActorComparisonType":3,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:MT>"}]}
            """, out _, overwrite: true);
        Controller.TryRegisterLayoutFromCode(LayoutPolarizing, """
            ~Lv2~{"Name":"Polarizing Strikes","ZoneLockH":[1238],"ElementsL":[{"Name":"L","type":1,"Enabled":false,"radius":0.0,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Left Line","type":3,"Enabled":false,"offY":20.0,"radius":3.0,"color":4278255615,"fillIntensity":0.345,"refActorDataID":17839,"refActorComparisonType":3,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:L>"},{"Name":"R","type":1,"Enabled":false,"radius":0.0,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Right Line","type":3,"Enabled":false,"offY":20.0,"radius":3.0,"color":4294902015,"fillIntensity":0.345,"refActorDataID":17839,"refActorComparisonType":3,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:R>"}]}
            """, out _, overwrite: true);
    }

    public override void OnUpdate()
    {
        ShowAkhMorn();
        ShowPolarizing();
        ShowParadise();
        ShowFulgent();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == CastAkhMorn)
            _akhCaster = source;
        else if(castId == CastPolarizing)
        {
            _polarizing = true;
            _polarizingHits = 0;
            _swordCaster = source;
        }
        else if(castId == 40319)
            ClearParadise();
        else if(castId is CastWingLight or CastWingDark)
        {
            _wingCast = castId;
            _wingSecond = false;
            _mainTank = TopHate();
            _subTank = 0;
        }
        else if(castId == CastWave)
            RememberWave(source);
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null)
            return;

        var id = set.Action.Value.RowId;
        if(_wingCast is CastWingLight or CastWingDark && id is 40314 or 40315)
        {
            var end = _wingCast == CastWingLight ? 40315u : 40314u;
            if(id == end)
                ClearParadise();
            else if(!_wingSecond)
            {
                _wingSecond = true;
                _subTank = TopHate();
            }
        }
        else if(_polarizing && id == CastPolarizing && _polarizingHits == 0)
            _polarizingHits = 1;
        else if(_polarizing && id == 40234 && _polarizingHits > 0)
        {
            _polarizingHits++;
            if(_polarizingHits >= 4)
                ClearPolarizing();
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
        ClearPolarizing();
        ClearParadise();
        ClearWaves();
        ShowAkhMorn();
        ShowPolarizing();
        ShowParadise();
        ShowFulgent();
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Fulgent Blade", ref C.ShowFulgent);
        ImGui.Checkbox("Akh Morn", ref C.ShowAkhMorn);
        ImGui.Checkbox("Paradise Regained", ref C.ShowParadise);
        ImGui.Checkbox("Polarizing Strikes", ref C.ShowPolarizing);
    }

    #endregion

    #region Private Method

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

    // Wraps a degree into the range 0 inclusive to 360 exclusive.
    private static float NormalizeAngle(float degree)
        => (degree % 360f + 360f) % 360f;

    // Closest living party member on one side. Right is 0-180, left is 180-360.
    private uint PickPlayer(IGameObject origin, bool right)
    {
        var facing = NormalizeAngle(-origin.Rotation * (180f / MathF.PI) + 180f);
        var picked = Controller.GetPartyMembers()
            .Where(player => player.CurrentHp > 0)
            .Select(player => (
                Player: player,
                Angle: NormalizeAngle(MathHelper.GetRelativeAngle(origin.Position, player.Position) - facing),
                Distance: DistanceXz(origin.Position, player.Position)))
            .Where(x => right ? x.Angle < 180f : x.Angle >= 180f)
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Player.EntityId)
            .FirstOrDefault();
        return picked.Player?.EntityId ?? 0;

        static float DistanceXz(Vector3 left, Vector3 right)
        {
            var dx = left.X - right.X;
            var dz = left.Z - right.Z;
            return MathF.Sqrt(dx * dx + dz * dz);
        }
    }

    // Writes the closest player on each side from one caster.
    private void RefreshSides(string layoutName, string rightName, string leftName, uint source)
    {
        if(!source.TryGetObject(out var caster))
            return;

        SetActor(layoutName, rightName, PickPlayer(caster, true));
        SetActor(layoutName, leftName, PickPlayer(caster, false));
    }

    // Shows or hides the Akh Morn donuts from the current caster.
    private void ShowAkhMorn()
    {
        var active = C.ShowAkhMorn && _akhCaster != 0;
        SetEnabled(LayoutAkhMorn, "Separator", active);
        SetEnabled(LayoutAkhMorn, "Pandra Right", active);
        SetEnabled(LayoutAkhMorn, "Pandra Left", active);
        if(!active)
        {
            SetActor(LayoutAkhMorn, "Pandra Right", 0);
            SetActor(LayoutAkhMorn, "Pandra Left", 0);
            return;
        }

        RefreshSides(LayoutAkhMorn, "Pandra Right", "Pandra Left", _akhCaster);
    }

    // Shows the sword lines from the current caster.
    private void ShowPolarizing()
    {
        var active = C.ShowPolarizing && _polarizing;
        SetEnabled(LayoutPolarizing, "Right Line", active);
        SetEnabled(LayoutPolarizing, "Left Line", active);
        SetEnabled(LayoutPolarizing, "R", active);
        SetEnabled(LayoutPolarizing, "L", active);
        if(!active)
        {
            SetActor(LayoutPolarizing, "R", 0);
            SetActor(LayoutPolarizing, "L", 0);
            return;
        }

        RefreshSides(LayoutPolarizing, "R", "L", _swordCaster);
    }

    // Drops the polarizing lines back to idle.
    private void ClearPolarizing()
    {
        _polarizing = false;
        _polarizingHits = 0;
        _swordCaster = 0;
    }

    // Shows the active wing pair and tank captures.
    private void ShowParadise()
    {
        var active = C.ShowParadise && _wingCast != 0;
        var light = active && _wingCast == CastWingLight;
        var dark = active && _wingCast == CastWingDark;
        SetPair("[Light to Dark] 1st", light && !_wingSecond);
        SetPair("[Light to Dark] 2nd", light && _wingSecond);
        SetPair("[Dark to Light] 1st", dark && !_wingSecond);
        SetPair("[Dark to Light] 2nd", dark && _wingSecond);
        SetEnabled(LayoutParadise, "MT", active);
        SetEnabled(LayoutParadise, "ST", active);
        if(!active)
            return;

        SetActor(LayoutParadise, "MT", _mainTank);
        SetActor(LayoutParadise, "ST", _subTank);
    }

    // Shows or hides the cone and circle of one wing step.
    private void SetPair(string prefix, bool enabled)
    {
        SetEnabled(LayoutParadise, $"{prefix} Cone", enabled);
        SetEnabled(LayoutParadise, $"{prefix} AOE", enabled);
    }

    // Drops Paradise Regained back to idle.
    private void ClearParadise()
    {
        _wingCast = 0;
        _wingSecond = false;
        _mainTank = 0;
        _subTank = 0;
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

        AddBand(layout, wave.Plus, 40f);
        AddBand(layout, wave.Minus, 40.42f);
    }

    // Adds one fixed line if that side is not registered yet.
    private static void AddBand(Layout layout, string name, float radius)
    {
        if(layout.GetElement(name) != null)
            return;

        layout.ElementsL.Add(new Element(2)
        {
            Name = name,
            Enabled = false,
            radius = radius,
        });
    }

    // Places each queued band from its own timer.
    private void ShowFulgent()
    {
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

    #endregion
}
