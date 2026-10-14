using Dalamud.Interface.ManagedFontAtlas;
using System;
using System.Collections.Generic;
using System.Text;

namespace Splatoon.Services;

public class CustomFontManager : IDisposable
{
    public OrderedDictionary<float, IFontHandle> Fonts = [];
    public CustomFontManager()
    {
        float[] scales = [1.5f, 2f, 2.5f, 3f];
        foreach(var x in scales)
        {
            PluginLog.Information($"Loading font for {x} scale");
            var scale = x;
            Fonts[scale] = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(
                e => e.OnPreBuild(tk => tk.AddDalamudDefaultFont(Svc.PluginInterface.UiBuilder.DefaultFontSpec.SizePx * scale)));
        }
    }

    public void Dispose()
    {
        foreach(var x in Fonts)
        {
            x.Value.Dispose();
        }
        Fonts.Clear();
    }
}
