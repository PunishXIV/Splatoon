using Pictomancy;
using static Splatoon.RenderEngines.DirectX11.DirectX11DisplayObjects;


namespace Splatoon.RenderEngines.DirectX11;

internal unsafe class DirectX11Scene : IDisposable
{
    private readonly TimeSpan ErrorLogFrequency = TimeSpan.FromSeconds(30);
    private const int MINIMUM_CIRCLE_SEGMENTS = 24;
    private int uid = 0;
    private DateTime lastErrorLogTime = DateTime.MinValue;
    private DirectX11Renderer DirectX11Renderer;
    public DirectX11Scene(DirectX11Renderer dx11renderer)
    {
        DirectX11Renderer = dx11renderer;
        Svc.PluginInterface.UiBuilder.Draw += Draw;
        Svc.PluginInterface.Create<PictoService>();
    }

    public void Dispose()
    {
        Svc.PluginInterface.UiBuilder.Draw -= Draw;
        PictoService.Dispose();
    }

    private void Draw()
    {
        if (!DirectX11Renderer.Enabled) return;
        try
        {
            uid = 0;
            try
            {
                var texture = PictomancyDraw();

                void Draw()
                {
                    // Draw pre-rendered pictomancy texture with shapes and strokes.
                    if (texture.HasValue)
                    {
                        ImGui.GetWindowDrawList().AddImage((nint)texture?.TextureId, ImGuiHelpers.MainViewport.Pos, ImGuiHelpers.MainViewport.Pos + new Vector2((float)texture?.Width, (float)texture?.Height));
                    }

                    // Draw dots and text last because they are most critical to be legible.
                    foreach (var element in DirectX11Renderer.DisplayObjects)
                    {
                        if (element is DisplayObjectDot elementDot)
                        {
                            DrawPoint(elementDot);
                        }
                        if (element is DisplayObjectText elementText)
                        {
                            DrawTextWorld(elementText);
                        }
                    }
                }

                ImGuiHelpers.ForceNextWindowMainViewport();
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(Vector2.Zero);
                ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);
                ImGui.Begin("Splatoon DirectX11 Scene", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoTitleBar
                    | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoSavedSettings);
                if (P.Config.SplatoonLowerZ)
                {
                    CImGui.igBringWindowToDisplayBack(CImGui.igGetCurrentWindow());
                }

                if (P.Config.RenderableZones.Count == 0 || !P.Config.RenderableZonesValid)
                {
                    Draw();
                }
                else
                {
                    foreach (var e in P.Config.RenderableZones)
                    {
                        ImGui.PushClipRect(new Vector2(e.Rect.X, e.Rect.Y), new Vector2(e.Rect.Right, e.Rect.Bottom), false);
                        Draw();
                        ImGui.PopClipRect();
                    }
                }
                ImGui.End();
                ImGui.PopStyleVar();
            }
            catch (Exception e)
            {
                var now = DateTime.Now;
                if (now - lastErrorLogTime > ErrorLogFrequency)
                {
                    lastErrorLogTime = now;
                    P.Log("Splatoon exception: please report it to developer", true);
                    P.Log(e.Message, true);
                    P.Log(e.StackTrace, true);
                }
            }
        }
        catch (Exception e)
        {
            P.Log("Caught exception: " + e.Message, true);
            P.Log(e.StackTrace, true);
        }
    }

    private PctTexture? PictomancyDraw()
    {
        PctTexture? texture = null;
        try
        {
            PctDrawHints hints = new(
                autoDraw: false,
                maxAlpha: (byte)P.Config.MaxAlpha,
                alphaBlendMode: P.Config.AlphaBlendMode,
                clipNativeUI: P.Config.AutoClipNativeUI);
            using var drawList = PictoService.Draw(ImGui.GetWindowDrawList(), hints);
            if (drawList == null)
                return null;
            foreach (var element in DirectX11Renderer.DisplayObjects)
            {
                if (element is DisplayObjectFan elementFan)
                {
                    if (elementFan.style.filled)
                        drawList.AddFanFilled(
                            elementFan.origin,
                            elementFan.innerRadius,
                            elementFan.outerRadius,
                            elementFan.angleMin,
                            elementFan.angleMax,
                            elementFan.style.originFillColor,
                            elementFan.style.endFillColor);
                    if (elementFan.style.IsStrokeVisible())
                        drawList.AddFan(
                            elementFan.origin,
                            elementFan.innerRadius,
                            elementFan.outerRadius,
                            elementFan.angleMin,
                            elementFan.angleMax,
                            elementFan.style.strokeColor,
                            thickness: elementFan.style.strokeThickness);
                }
                else if (element is DisplayObjectLine elementLine)
                {
                    if (elementLine.style.filled)
                        drawList.AddLineFilled(
                        elementLine.start,
                        elementLine.stop,
                        elementLine.radius,
                        elementLine.style.originFillColor,
                        elementLine.style.endFillColor);
                    if (elementLine.style.IsStrokeVisible())
                        drawList.AddLine(
                        elementLine.start,
                        elementLine.stop,
                        elementLine.radius,
                        elementLine.style.strokeColor);
                }
            }
            foreach (var zone in P.Config.ClipZones)
            {
                drawList.AddClipZone(zone.Rect);
            }
            texture = drawList.DrawToTexture();
        }
        catch (Exception e)
        {
            var now = DateTime.Now;
            if (now - lastErrorLogTime > ErrorLogFrequency)
            {
                if (e is IndexOutOfRangeException)
                {
                    lastErrorLogTime = now;
                    P.Log("Splatoon exception: " + e.Message + " Please adjust misconfigured presets causing excessive elements, or report it to developer if you believe this limit is too low.", true);
                }
                else
                {
                    throw;
                }
            }
        }
        return texture;
    }

    public void DrawTextWorld(DisplayObjectText e)
    {
        if (Svc.GameGui.WorldToScreen(
                        new Vector3(e.x, e.z, e.y),
                        out var pos))
        {
            DrawText(e, pos);
        }
    }

    public void DrawText(DisplayObjectText e, Vector2 pos)
    {
        var scaled = e.fscale != 1f;
        var size = scaled ? ImGui.CalcTextSize(e.text) * e.fscale : ImGui.CalcTextSize(e.text);
        size = new Vector2(size.X + 10f, size.Y + 10f);
        ImGui.SetNextWindowPos(new Vector2(pos.X - size.X / 2, pos.Y - size.Y / 2));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 10f);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.ColorConvertU32ToFloat4(e.bgcolor));
        ImGui.BeginChild("##child" + e.text + ++uid, size, false,
            ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysUseWindowPadding);
        ImGui.PushStyleColor(ImGuiCol.Text, e.fgcolor);
        if (scaled) ImGui.SetWindowFontScale(e.fscale);
        ImGuiEx.Text(e.text);
        if (scaled) ImGui.SetWindowFontScale(1f);
        ImGui.PopStyleColor();
        ImGui.EndChild();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }

    public void DrawPoint(DisplayObjectDot e)
    {
        if (Svc.GameGui.WorldToScreen(new Vector3(e.x, e.y, e.z), out var pos))
            ImGui.GetWindowDrawList().AddCircleFilled(
            new Vector2(pos.X, pos.Y),
            e.thickness,
            ImGui.GetColorU32(e.color),
            MINIMUM_CIRCLE_SEGMENTS);
    }
}
