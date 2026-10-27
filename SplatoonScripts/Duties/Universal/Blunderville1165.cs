using ECommons;
using ECommons.ImGuiMethods;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using TerraFX.Interop.DirectX;

namespace SplatoonScriptsOfficial.Duties.Universal;

public unsafe class Blunderville1165 : SplatoonScript
{
    public override Metadata Metadata => new(1, "NightmareXIV");

    public override HashSet<uint>? ValidTerritories => [1165];

    public override void OnSetup()
    {
        for(int i = 0; i < 13; i++)
        {
            Controller.RegisterElementFromCode($"Circle{i}", """{"Name":"","refX":-10.0,"refY":-232.0,"refZ":10.0,"radius":5.0}""");
        }
    }

    Dictionary<uint, Vector3> SpinwheelPositions = new()
    {
        [18] = new Vector3(-10f, 10f, -202f),
        [19] = new Vector3(10f, 10f, -202f),
        [20] = new Vector3(0f, 10f, -222f),
        [21] = new Vector3(-10f, 10f, -242f),
        [22] = new Vector3(10f, 10f, -242f),
        [23] = new Vector3(-0f, 10f, -202f),
        [24] = new Vector3(-10f, 10f, -222f),
        [25] = new Vector3(10f, 10f, -222f),
        [26] = new Vector3(-0f, 10f, -242f),
        [27] = new Vector3(-10f, 10f, -212f),
        [28] = new Vector3(10f, 10f, -232f),
        [29] = new Vector3(10f, 10f, -212f),
        [30] = new Vector3(-10f, 10f, -232f),
    };

    Dictionary<uint, long> LastUpdate = [];

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(SpinwheelPositions.ContainsKey(position) && data1 == 1)
        {
            LastUpdate[position] = Environment.TickCount64;
        }
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        var array = SpinwheelPositions.Keys.ToArray();
        for(var i = 0; i < array.Length; i++)
        {
            var x = array[i];
            if(Controller.TryGetElementByName($"Circle{i}", out var e) && SpinwheelPositions.TryGetValue(x, out var position) && LastUpdate.TryGetValue(x, out var lastUpdate))
            {
                var elapsed = Environment.TickCount64 - lastUpdate;
                e.Enabled = elapsed < 6000;
                e.SetRefPosition(position);
                if(elapsed < 3000)
                {
                    e.CastFractionOverride = 1f;
                    e.castAnimation = Splatoon.Serializables.CastAnimationKind.Fill;
                    e.animationColor = ImGuiEx.Vector4FromRGBA(0x000000C8).ToUint();
                }
                else
                {
                    e.CastFractionOverride = ((float)elapsed - 3000f) / 3000f;
                    e.animationColor = ImGuiEx.Vector4FromRGBA(0x00000077).ToUint();
                }
                
            }
        }
    }
}
