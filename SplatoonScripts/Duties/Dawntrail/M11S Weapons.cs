using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using ECommons.Throttlers;
using Splatoon;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using TerraFX.Interop.DirectX;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
#pragma warning disable
public class M11S_Weapons : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1325];

    public enum Weapons : uint
    {
        Aoe_Axe = 19184,
        Donut_Scythe = 19185,
        Cross_Sword = 19186,
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Aoe_Axe", """
            {"Name":"","type":1,"offY":-12.0,"radius":4.5,"Donut":0.5,"color":3355639552,"fillIntensity":0.5,"thicc":5.0,"overlayText":"Stack","refActorComparisonType":2,"includeRotation":true,"onlyVisible":true,"tether":true}
            """);
        Controller.RegisterElementFromCode("Cross_Sword1", """
            {"Name":"","type":1,"offX":-7.5,"offY":-7.5,"radius":2.0,"Donut":0.5,"color":3371433728,"fillIntensity":0.5,"thicc":5.0,"overlayText":"Group 1","refActorComparisonType":2,"includeRotation":true,"onlyVisible":true,"tether":true}
            """);
        Controller.RegisterElementFromCode("Cross_Sword2", """
            {"Name":"","type":1,"offX":7.5,"offY":-7.5,"radius":2.0,"Donut":0.5,"color":3371433728,"fillIntensity":0.5,"thicc":5.0,"overlayText":"Group 2","refActorDataID":19186,"refActorComparisonType":2,"includeRotation":true,"onlyVisible":true,"tether":true}
            """);
        int i = 0;
        foreach(var x in Enum.GetValues<Protean>())
        {
            Controller.RegisterElementFromCode($"Donut_Scythe{x}", $$"""
                {"Name":"","type":1,"offY":4.0,"radius":0.5,"Donut":0.2,"color":3356884736,"fillIntensity":0.5,"thicc":5.0,"refActorDataID":19185,"refActorComparisonType":2,"includeRotation":true,"onlyVisible":true,"tether":true,"AdditionalRotation":{{i.DegreesToRadians():F2}}}
                """);
            i += 45;
        }

        Controller.RegisterElementFromCode($"Tornado{Tornado.North_Left}", """
            {"Name":"","tether":true,"type":1,"offX":4.0,"offY":-5.0,"radius":2.0,"Donut":0.2,"color":3358850816,"fillIntensity":0.5,"refActorDataID":19183,"refActorComparisonType":3,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":80.0,"DistanceMax":15.0,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            """);

        Controller.RegisterElementFromCode($"Tornado{Tornado.North_Right}", """
            {"Name":"","tether":true,"type":1,"offX":-4.0,"offY":-5.0,"radius":2.0,"Donut":0.2,"color":3358850816,"fillIntensity":0.5,"refActorDataID":19183,"refActorComparisonType":3,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":80.0,"DistanceMax":15.0,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            """);

        Controller.RegisterElementFromCode($"Tornado{Tornado.East_Left}", """
            {"Name":"","tether":true,"type":1,"offX":4.0,"offY":-5.0,"radius":2.0,"Donut":0.2,"color":3358850816,"fillIntensity":0.5,"refActorDataID":19183,"refActorComparisonType":3,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":120.0,"DistanceSourceY":100.0,"DistanceMax":15.0,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            """);

        Controller.RegisterElementFromCode($"Tornado{Tornado.East_Right}", """
            {"Name":"","tether":true,"type":1,"offX":-4.0,"offY":-5.0,"radius":2.0,"Donut":0.2,"color":3358850816,"fillIntensity":0.5,"refActorDataID":19183,"refActorComparisonType":3,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":120.0,"DistanceSourceY":100.0,"DistanceMax":15.0,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            """);

        Controller.RegisterElementFromCode($"Tornado{Tornado.South_Left}", """
            {"Name":"","tether":true,"type":1,"offX":4.0,"offY":-5.0,"radius":2.0,"Donut":0.2,"color":3358850816,"fillIntensity":0.5,"refActorDataID":19183,"refActorComparisonType":3,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":120.0,"DistanceMax":15.0,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            """);

        Controller.RegisterElementFromCode($"Tornado{Tornado.South_Right}", """
            {"Name":"","tether":true,"type":1,"offX":-4.0,"offY":-5.0,"radius":2.0,"Donut":0.2,"color":3358850816,"fillIntensity":0.5,"refActorDataID":19183,"refActorComparisonType":3,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":120.0,"DistanceMax":15.0,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            """);

        Controller.RegisterElementFromCode($"Tornado{Tornado.West_Left}", """
            {"Name":"","tether":true,"type":1,"offX":4.0,"offY":-5.0,"radius":2.0,"Donut":0.2,"color":3358850816,"fillIntensity":0.5,"refActorDataID":19183,"refActorComparisonType":3,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":80.0,"DistanceSourceY":100.0,"DistanceMax":15.0,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            """);

        Controller.RegisterElementFromCode($"Tornado{Tornado.West_Right}", """
            {"Name":"","tether":true,"type":1,"offX":-4.0,"offY":-5.0,"radius":2.0,"Donut":0.2,"color":3358850816,"fillIntensity":0.5,"refActorDataID":19183,"refActorComparisonType":3,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":80.0,"DistanceSourceY":100.0,"DistanceMax":15.0,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}}
            """);

        Controller.RegisterElementFromCode("CenterStack", """{"Name":"","refX":100.0,"refY":100.0,"radius":4.5,"Donut":0.5,"color":4294932480,"fillIntensity":0.5,"thicc":5.0,"overlayText":"Stack","refActorObjectID":1073774869,"refActorComparisonType":2,"includeRotation":true,"onlyVisible":true,"tether":true}""");
    }

    public enum Tornado { North_Left, North_Right, East_Left, East_Right, South_Left, South_Right, West_Left, West_Right }
    public enum Protean { North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest }

    List<uint> SortedWeapons = [];
    List<uint> TrackedWeapons = [];

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if(packet->ActionID == 46103 && Controller.Scene == 1 && sourceId.TryGetBattleNpc(out var source) && packet->TargetID.TryGetBattleNpc(out var target))
        {
            SortedWeapons = Svc.Objects.OfType<IBattleNpc>()
                .Where(x => x.IsCharacterVisible() && Enum.GetValues<Weapons>()
                .Contains((Weapons)x.DataId))
                .OrderBy(x =>
                {
                    var angle = MathHelper.GetRelativeAngle(new(100, 0, 100), x.Position);
                    var ret = (angle - packet->RotationFromNorth.RadToDeg() + 360) % 360;
                    if(ret < 10 || ret > 350) ret = 0;
                    PluginLog.Information($"{(Weapons)x.DataId}: {ret} (base {packet->RotationFromNorth.RadToDeg()})");
                    return ret;
                })
                .Select(x => x.ObjectId)
                .ToList();
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(SortedWeapons.Count > 0)
        {
            
            if(set.Action?.RowId.EqualsAny<uint>(46107, 46109, 46108) == true)
            {
                if(EzThrottler.Throttle(this.InternalData.FullName + "Cast"))
                {
                    SortedWeapons.RemoveAt(0);
                }
            }
        }
        if(set.Action?.RowId == 46119)
        {
            EzThrottler.Throttle(this.InternalData.FullName + "Gust", 5000, true);
        }
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        if(SortedWeapons.Count > 0 && SortedWeapons[0].TryGetBattleNpc(out var b))
        {
            Element e = null;
            if(b.DataId == (uint)Weapons.Cross_Sword)
            {
                e = Controller.GetElementByName($"{Weapons.Cross_Sword}{(C.RightForSword?"2":"1")}");
            }
            if(b.DataId == (uint)Weapons.Aoe_Axe)
            {
                e = Controller.GetElementByName(Controller.Scene == 1?$"{Weapons.Aoe_Axe}": "CenterStack");
            }
            if(b.DataId == (uint)Weapons.Donut_Scythe)
            {
                e = Controller.GetElementByName($"{Weapons.Donut_Scythe}{C.Protean}");
            }
            e?.Enabled = true;
            e?.color = GetRainbowColor(3).ToUint();
            e?.refActorObjectID = b.ObjectId;
        }
        if(Controller.Scene == 2)
        {
            var numTornado = 0;
            foreach(var x in Svc.Objects.OfType<IBattleNpc>())
            {
                if(x.DataId == 19183 && x.IsCharacterVisible()) numTornado++;
                if(Enum.GetValues<Weapons>().Contains((Weapons)x.DataId) && x.IsCharacterVisible() && !TrackedWeapons.Contains(x.ObjectId))
                {
                    TrackedWeapons.Add(x.ObjectId);
                    this.SortedWeapons.Add(x.ObjectId);
                }
            }
            if(numTornado == 4 && EzThrottler.Check(this.InternalData.FullName + "Gust"))
            {
                var e = Controller.GetElementByName($"Tornado{C.Tornado}");
                e?.Enabled = true;
                e?.color = GetRainbowColor(3).ToUint();
            }
        }
    }

    public override void OnReset()
    {
        this.SortedWeapons.Clear();
        this.TrackedWeapons.Clear();
    }

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("Select protean position, boss relative", ref C.Protean);
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("Select tornado bait position, looking from middle of the arena at tornado", ref C.Tornado);
        ImGuiEx.TextV("Select light party stacks, boss relative:");
        ImGuiEx.RadioButtonBool("Right", "Left", ref C.RightForSword);
    }

    Config C => Controller.GetConfig<Config>();

    public class Config : IEzConfig
    {
        public Tornado Tornado = default;
        public Protean Protean = default;
        public bool RightForSword = false;
    }

    public static Vector4 GetRainbowColor(double cycleSeconds)
    {
        if(cycleSeconds <= 0d)
        {
            cycleSeconds = 1d;
        }

        var ms = Environment.TickCount64;
        var t = (ms / 1000d) / cycleSeconds;
        var hue = t % 1f;
        return HsvToVector4(hue, 1f, 1f);
    }

    public static Vector4 HsvToVector4(double h, double s, double v)
    {
        double r = 0f, g = 0f, b = 0f;
        var i = (int)(h * 6f);
        var f = h * 6f - i;
        var p = v * (1f - s);
        var q = v * (1f - f * s);
        var t = v * (1f - (1f - f) * s);

        switch(i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }

        return new Vector4((float)r, (float)g, (float)b, 1f);
    }
}
