using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Generic;

public unsafe class SimpleZoomEdit : SplatoonScript<SimpleZoomEdit.Config>
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(200f);
        ImGui.InputFloat("Maximum zoom level (game default is 20)", ref C.MaxZoom);
    }

    public override void OnUpdate()
    {
        if(GetMaxZoom() != C.MaxZoom)
        {
            SetMaxZoom(C.MaxZoom);
        }
    }

    public override void OnDisable()
    {
        SetMaxZoom(20);
    }

    void SetMaxZoom(float zoom)
    {
        var camSpan = CameraManager.Instance()->Cameras;
        if(camSpan.Length > 0)
        {
            var c = camSpan[0];
            if(c != null)
            {
                *(float*)(((nint)c.Value) + 0x12C) = zoom;
            }
        }
    }

    float GetMaxZoom()
    {
        var camSpan = CameraManager.Instance()->Cameras;
        if(camSpan.Length > 0)
        {
            var c = camSpan[0];
            if(c != null)
            {
                return *(float*)(((nint)c.Value) + 0x12C);
            }
        }
        return 0;
    }

    public class Config
    {
        public float MaxZoom = 40;
    }
}
