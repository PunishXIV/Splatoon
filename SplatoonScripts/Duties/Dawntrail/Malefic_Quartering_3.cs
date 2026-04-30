using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.GameFunctions;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ECommons.DalamudServices.Legacy;

using ECommons.DalamudServices.Legacy;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.AnotherMerchantTale;

public class Malefic_Quartering_3 : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1317];
    public override Metadata? Metadata => new(1, "Poneglyph");

    private bool isActive = false;
    private uint pdebuff = 0;
    private int mechanicStep = 1;
    private HashSet<uint> procslashes = [];
    private List<Vector3> forces = [];
    private string currentElement = "";
    private string activeGreenTether = "";
    private DateTime? mechanicEndTime = null;

    private int? step4PSlot = null;
    private int? step4SSlot = null;

    private readonly Vector3 N1 = new(165f, -16f, -835f);
    private readonly Vector3 N2 = new(175f, -16f, -835f);
    private readonly Vector3 W1 = new(150f, -16f, -820f);
    private readonly Vector3 W2 = new(150f, -16f, -810f);
    private readonly Vector3 E1 = new(190f, -16f, -820f);
    private readonly Vector3 E2 = new(190f, -16f, -810f);
    private readonly Vector3 S1 = new(165f, -16f, -795f);
    private readonly Vector3 S2 = new(175f, -16f, -795f);

    public override void OnSetup()
    {
        RegisterElements();
        OnReset();
    }

    private void RegisterElements()
    {
        Controller.RegisterElementFromCode("Tether NW", """{"Name":"Tether NW","refX":165.0,"refY":-820.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
        Controller.RegisterElementFromCode("Tether NE", """{"Name":"Tether NE","refX":175.0,"refY":-820.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
        Controller.RegisterElementFromCode("Tether SW", """{"Name":"Tether SW","refX":165.0,"refY":-810.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
        Controller.RegisterElementFromCode("Tether SE", """{"Name":"Tether SE","refX":175.0,"refY":-810.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
        Controller.RegisterElementFromCode("Tether N", """{"Name":"Tether N","refX":164.5,"refY":-830.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
        Controller.RegisterElementFromCode("Tether E", """{"Name":"Tether E","refX":185.0,"refY":-820.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
        Controller.RegisterElementFromCode("Tether S", """{"Name":"Tether S","refX":175.0,"refY":-800.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
        Controller.RegisterElementFromCode("Tether W", """{"Name":"Tether W","refX":155.0,"refY":-810.0,"refZ":-16.0,"radius":1.0,"color":3355508484,"fillIntensity":0.5,"tether":true,"LineEndA":1}""");
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 46693) { OnReset(); isActive = true; }
    }

    public override void OnUpdate()
    {
        if(!isActive) return;

        if(mechanicEndTime.HasValue && (DateTime.Now - mechanicEndTime.Value).TotalSeconds >= 10)
        {
            OnReset();
            return;
        }

        if(pdebuff == 0 && Svc.ClientState.LocalPlayer is { } p)
        {
            var status = p.StatusList;
            if(status.Any(s => s.StatusId == 4782)) pdebuff = 4782;
            else if(status.Any(s => s.StatusId == 4781)) pdebuff = 4781;
            else if(status.Any(s => s.StatusId == 4778)) pdebuff = 4778;
            else if(status.Any(s => s.StatusId == 4777)) pdebuff = 4777;
        }

        var slashes = Svc.Objects.Where(o => o.DataId == 19227 && !procslashes.Contains((uint)o.GameObjectId)).ToList();
        foreach(var obj in slashes)
        {
            procslashes.Add((uint)obj.GameObjectId);
            forces.Add(obj.Position);
            if(forces.Count >= 4) { ResolveForces(); forces.Clear(); }
        }

        if(mechanicStep == 4 && string.IsNullOrEmpty(currentElement) && !string.IsNullOrEmpty(activeGreenTether))
        {
            ResolveStep4();
        }

        string[] allTethers = { "Tether NW", "Tether NE", "Tether SW", "Tether SE", "Tether N", "Tether E", "Tether S", "Tether W" };

        foreach(var name in allTethers)
        {
            if(Controller.GetElementByName(name) is { } e)
            {
                e.Enabled = (name == currentElement);
                if(e.Enabled)
                {
                    e.color = GetRainbowColor(1f).ToUint();
                }
            }
        }
    }

    public Vector4 GetRainbowColor(double cycleSeconds)
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

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(!isActive || Svc.ClientState.LocalPlayer == null) return;
        if(target != (uint)Svc.ClientState.LocalPlayer.GameObjectId) return;
        if(vfxPath.Contains("chn_ambd_n_p")) activeGreenTether = "N";
        else if(vfxPath.Contains("chn_ambd_s_p")) activeGreenTether = "S";
        else if(vfxPath.Contains("chn_ambd_e_p")) activeGreenTether = "E";
        else if(vfxPath.Contains("chn_ambd_w_p")) activeGreenTether = "W";
    }

    private void ResolveForces()
    {
        int? pSlot = null; int? sSlot = null;
        foreach(var pos in forces)
        {
            switch(pdebuff)
            {
                case 4782: if(Vector3.Distance(pos, S1) < 3f) pSlot = 1; if(Vector3.Distance(pos, S2) < 3f) pSlot = 2; if(Vector3.Distance(pos, E1) < 3f) sSlot = 1; if(Vector3.Distance(pos, E2) < 3f) sSlot = 2; break;
                case 4781: if(Vector3.Distance(pos, S1) < 3f) pSlot = 1; if(Vector3.Distance(pos, S2) < 3f) pSlot = 2; if(Vector3.Distance(pos, W1) < 3f) sSlot = 1; if(Vector3.Distance(pos, W2) < 3f) sSlot = 2; break;
                case 4778: if(Vector3.Distance(pos, N1) < 3f) pSlot = 1; if(Vector3.Distance(pos, N2) < 3f) pSlot = 2; if(Vector3.Distance(pos, E1) < 3f) sSlot = 1; if(Vector3.Distance(pos, E2) < 3f) sSlot = 2; break;
                case 4777: if(Vector3.Distance(pos, N1) < 3f) pSlot = 1; if(Vector3.Distance(pos, N2) < 3f) pSlot = 2; if(Vector3.Distance(pos, W1) < 3f) sSlot = 1; if(Vector3.Distance(pos, W2) < 3f) sSlot = 2; break;
            }
        }
        if(mechanicStep <= 3)
        {
            if(pSlot == 1 && sSlot == 1) currentElement = "Tether NW";
            else if(pSlot == 1 && sSlot == 2) currentElement = "Tether SW";
            else if(pSlot == 2 && sSlot == 1) currentElement = "Tether NE";
            else if(pSlot == 2 && sSlot == 2) currentElement = "Tether SE";
            mechanicStep++;
        }
        else if(mechanicStep == 4) { step4PSlot = pSlot; step4SSlot = sSlot; ResolveStep4(); }
    }

    private void ResolveStep4()
    {
        if(string.IsNullOrEmpty(activeGreenTether) || step4PSlot == null || step4SSlot == null) return;
        switch(pdebuff)
        {
            case 4777: if(activeGreenTether == "N") currentElement = step4SSlot == 2 ? "Tether W" : "Tether E"; else if(activeGreenTether == "W") currentElement = step4PSlot == 2 ? "Tether S" : "Tether N"; break;
            case 4778: if(activeGreenTether == "N") currentElement = step4SSlot == 2 ? "Tether W" : "Tether E"; else if(activeGreenTether == "E") currentElement = step4PSlot == 2 ? "Tether S" : "Tether N"; break;
            case 4782: if(activeGreenTether == "S") currentElement = step4SSlot == 2 ? "Tether W" : "Tether E"; else if(activeGreenTether == "E") currentElement = step4PSlot == 2 ? "Tether S" : "Tether N"; break;
            case 4781: if(activeGreenTether == "S") currentElement = step4SSlot == 2 ? "Tether W" : "Tether E"; else if(activeGreenTether == "W") currentElement = step4PSlot == 2 ? "Tether S" : "Tether N"; break;
        }
        if(!string.IsNullOrEmpty(currentElement)) { mechanicStep++; mechanicEndTime = DateTime.Now; }
    }

    public override void OnReset()
    {
        isActive = false; pdebuff = 0; mechanicStep = 1; activeGreenTether = "";
        step4PSlot = null; step4SSlot = null; mechanicEndTime = null;
        procslashes.Clear(); forces.Clear(); currentElement = "";
        string[] all = { "Tether NW", "Tether NE", "Tether SW", "Tether SE", "Tether N", "Tether E", "Tether S", "Tether W" };
        foreach(var name in all) if(Controller.GetElementByName(name) is { } e) e.Enabled = false;
    }
}