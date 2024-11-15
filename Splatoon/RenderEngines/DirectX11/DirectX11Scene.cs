using Pictomancy;
using Splatoon.Serializables;
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
        PictoService.Initialize(Svc.PluginInterface);
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

                void Draw(PctTexture? texture)
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
                    | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing);
                var texture = PictomancyDraw();
                if (P.Config.SplatoonLowerZ)
                {
                    CImGui.igBringWindowToDisplayBack(CImGui.igGetCurrentWindow());
                }

                if (P.Config.RenderableZones.Count == 0 || !P.Config.RenderableZonesValid)
                {
                    Draw(texture);
                }
                else
                {
                    foreach (var e in P.Config.RenderableZones)
                    {
                        ImGui.PushClipRect(new Vector2(e.Rect.X, e.Rect.Y), new Vector2(e.Rect.Right, e.Rect.Bottom), false);
                        Draw(texture);
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
                    DrawFan(elementFan, drawList);
                }
                else if (element is DisplayObjectLine elementLine)
                {
                    DrawLine(elementLine, drawList);
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

    public void DrawFan(DisplayObjectFan fan, PctDrawList drawList)
    {
        if (fan.style.filled)
            drawList.AddFanFilled(
                fan.origin,
                fan.innerRadius,
                fan.outerRadius,
                fan.angleMin,
                fan.angleMax,
                fan.style.originFillColor,
                fan.style.endFillColor);
        if (fan.style.IsStrokeVisible())
            drawList.AddFan(
                fan.origin,
                fan.innerRadius,
                fan.outerRadius,
                fan.angleMin,
                fan.angleMax,
                fan.style.strokeColor,
                thickness: fan.style.strokeThickness);
        if (fan.style.castFraction > 0)
        {
            if (fan.style.animation.kind is Serializables.CastAnimationKind.Pulse)
            {
                var size = fan.style.animation.size + fan.outerRadius - fan.innerRadius;
                var pulsePosition = size * (float)((DateTime.Now - DateTime.MinValue).TotalMilliseconds / 1000f % fan.style.animation.frequency) / fan.style.animation.frequency;
                drawList.AddFanFilled(
                    fan.origin,
                    MathF.Max(fan.innerRadius, fan.innerRadius + pulsePosition - fan.style.animation.size),
                    MathF.Min(fan.outerRadius, fan.innerRadius + pulsePosition),
                    fan.angleMin,
                    fan.angleMax,
                    fan.style.animation.color & 0x00FFFFFF,
                    fan.style.animation.color);
            }
            else if (fan.style.animation.kind is Serializables.CastAnimationKind.Fill)
            {
                var size = fan.outerRadius - fan.innerRadius;
                var castRadius = size * fan.style.castFraction;
                drawList.AddFanFilled(
                    fan.origin,
                    fan.innerRadius,
                    fan.innerRadius + castRadius,
                    fan.angleMin,
                    fan.angleMax,
                    fan.style.animation.color,
                    fan.style.animation.color);
            }
        }
    }

    public void DrawLine(DisplayObjectLine line, PctDrawList drawList)
    {
        if (line.radius == 0)
        {
            drawList.PathLineTo(line.start);
            drawList.PathLineTo(line.stop);
            drawList.PathStroke(line.style.strokeColor, PctStrokeFlags.None, line.style.strokeThickness);

            float arrowScale = MathF.Max(1, line.style.strokeThickness / 7f);
            if (line.startStyle == LineEnd.Arrow)
            {
                var arrowStart = line.start + arrowScale * 0.4f * line.Direction;
                var offset = arrowScale * 0.3f * line.Perpendicular;
                drawList.PathLineTo(arrowStart + offset);
                drawList.PathLineTo(line.start);
                drawList.PathLineTo(arrowStart - offset);
                drawList.PathStroke(line.style.strokeColor, PctStrokeFlags.None, line.style.strokeThickness);
            }

            if (line.endStyle == LineEnd.Arrow)
            {
                var arrowStart = line.stop - arrowScale * 0.4f * line.Direction;
                var offset = arrowScale * 0.3f * line.Perpendicular;
                drawList.PathLineTo(arrowStart + offset);
                drawList.PathLineTo(line.stop);
                drawList.PathLineTo(arrowStart - offset);
                drawList.PathStroke(line.style.strokeColor, PctStrokeFlags.None, line.style.strokeThickness);
            }
        }
        else
        {
            if (line.style.filled)
                drawList.AddLineFilled(
                line.start,
                line.stop,
                line.radius,
                line.style.originFillColor,
                line.style.endFillColor);
            if (line.style.IsStrokeVisible())
                drawList.AddLine(
                line.start,
                line.stop,
                line.radius,
                line.style.strokeColor,
                thickness: line.style.strokeThickness);
            if (line.style.castFraction > 0)
            {
                if (line.style.animation.kind is Serializables.CastAnimationKind.Pulse)
                {
                    var length = line.style.animation.size + line.Length;
                    var pulsePosition = length * (float)((DateTime.Now - DateTime.MinValue).TotalMilliseconds / 1000f % line.style.animation.frequency) / line.style.animation.frequency;
                    drawList.AddLineFilled(
                        line.start + line.Direction * MathF.Max(0, pulsePosition - line.style.animation.size),
                        line.start + line.Direction * MathF.Min(pulsePosition, line.Length),
                        line.radius,
                        line.style.animation.color & 0x00FFFFFF,
                        line.style.animation.color);
                }
                else if (line.style.animation.kind is Serializables.CastAnimationKind.Fill)
                {
                    var castLength = line.style.castFraction * line.Length;
                    drawList.AddLineFilled(
                        line.start,
                        line.start + line.Direction * castLength,
                        line.radius,
                        line.style.animation.color,
                        line.style.animation.color);
                }
            }
        }
    }

    public void DrawTextWorld(DisplayObjectText e)
    {
        if (Utils.WorldToScreen(
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
        if (Utils.WorldToScreen(new Vector3(e.x, e.y, e.z), out var pos))
            ImGui.GetWindowDrawList().AddCircleFilled(
            new Vector2(pos.X, pos.Y),
            e.thickness,
            ImGui.GetColorU32(e.color),
            MINIMUM_CIRCLE_SEGMENTS);
    }
}
