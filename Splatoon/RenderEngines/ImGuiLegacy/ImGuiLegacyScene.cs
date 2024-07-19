using Dalamud.Game.ClientState.Conditions;
using Splatoon.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.RenderEngines.ImGuiLegacy;
internal class ImGuiLegacyScene
{
    /*
    internal readonly ImGuiLegacyRenderer ImGuiLegacyRenderer;

    public ImGuiLegacyScene(ImGuiLegacyRenderer imGuiLegacyRenderer)
    {
        ImGuiLegacyRenderer = imGuiLegacyRenderer;
    }

    void Draw()
    {
        if (p.Profiler.Enabled) p.Profiler.Gui.StartTick();
        try
        {
            if (!Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
                && !Svc.Condition[ConditionFlag.WatchingCutscene78])
            {
                uid = 0;
                if (p.Config.segments > 1000 || p.Config.segments < 4)
                {
                    p.Config.segments = 100;
                    p.Log("Your smoothness setting was unsafe. It was reset to 100.");
                }
                if (p.Config.lineSegments > 50 || p.Config.lineSegments < 4)
                {
                    p.Config.lineSegments = 20;
                    p.Log("Your line segment setting was unsafe. It was reset to 20.");
                }
                try
                {
                    void Draw()
                    {
                        foreach (var element in p.displayObjects)
                        {
                            if (element is DisplayObjectCircle elementCircle)
                            {
                                DrawRingWorld(elementCircle);
                            }
                            else if (element is DisplayObjectDot elementDot)
                            {
                                DrawPoint(elementDot);
                            }
                            else if (element is DisplayObjectText elementText)
                            {
                                DrawTextWorld(elementText);
                            }
                            else if (element is DisplayObjectLine elementLine)
                            {
                                DrawLineWorld(elementLine);
                            }
                            else if (element is DisplayObjectRect elementRect)
                            {
                                DrawRectWorld(elementRect);
                            }
                            else if (element is DisplayObjectDonut elementDonut)
                            {
                                DrawDonutWorld(elementDonut);
                            }
                            else if (element is DisplayObjectCone elementCone)
                            {
                                DrawConeWorld(elementCone);
                            }
                        }
                    }

                    ImGuiHelpers.ForceNextWindowMainViewport();
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                    ImGuiHelpers.SetNextWindowPosRelativeMainViewport(Vector2.Zero);
                    ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);
                    ImGui.Begin("Splatoon scene", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoTitleBar
                        | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.AlwaysUseWindowPadding);
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
                            //var trans = e.Trans != 1.0f;
                            //if (trans) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, e.Trans);
                            ImGui.PushClipRect(new Vector2(e.Rect.X, e.Rect.Y), new Vector2(e.Rect.Right, e.Rect.Bottom), false);
                            Draw();
                            ImGui.PopClipRect();
                            //if(trans)ImGui.PopStyleVar();
                        }
                    }
                    ImGui.End();
                    ImGui.PopStyleVar();
                }
                catch (Exception e)
                {
                    p.Log("Splatoon exception: please report it to developer", true);
                    p.Log(e.Message, true);
                    p.Log(e.StackTrace, true);
                }
            }
        }
        catch (Exception e)
        {
            p.Log("Caught exception: " + e.Message, true);
            p.Log(e.StackTrace, true);
        }
        if (p.Profiler.Enabled) p.Profiler.Gui.StopTick();
    }

    internal Vector3 TranslateToScreen(double x, double y, double z)
    {
        Vector2 tenp;
        Svc.GameGui.WorldToScreen(
            new Vector3((float)x, (float)y, (float)z),
            out tenp
        );
        return new Vector3(tenp.X, tenp.Y, (float)z);
    }

    private void DrawDonutWorld(DisplayObjectDonut elementDonut)
    {
        Vector3 v1, v2, v3, v4;
        var outerradiuschonk = elementDonut.radius + elementDonut.donut;
        v1 = TranslateToScreen(
            elementDonut.x + (elementDonut.radius * Math.Sin((Math.PI / 23.0) * 0)),
            elementDonut.z,
            elementDonut.y + (elementDonut.radius * Math.Cos((Math.PI / 24.0) * 0))
        );
        v4 = TranslateToScreen(
            elementDonut.x + (outerradiuschonk * Math.Sin((Math.PI / 24.0) * 0)),
            elementDonut.z,
            elementDonut.y + (outerradiuschonk * Math.Cos((Math.PI / 24.0) * 0))
        );
        for (int i = 0; i <= 47; i++)
        {
            v2 = TranslateToScreen(
                elementDonut.x + (elementDonut.radius * Math.Sin((Math.PI / 24.0) * (i + 1))),
                elementDonut.z,
                elementDonut.y + (elementDonut.radius * Math.Cos((Math.PI / 24.0) * (i + 1)))
            );
            v3 = TranslateToScreen(
                elementDonut.x + (outerradiuschonk * Math.Sin((Math.PI / 24.0) * (i + 1))),
                elementDonut.z,
                elementDonut.y + (outerradiuschonk * Math.Cos((Math.PI / 24.0) * (i + 1)))
            );
            ImGui.GetWindowDrawList().PathLineTo(new Vector2(v1.X, v1.Y));
            ImGui.GetWindowDrawList().PathLineTo(new Vector2(v2.X, v2.Y));
            ImGui.GetWindowDrawList().PathLineTo(new Vector2(v3.X, v3.Y));
            ImGui.GetWindowDrawList().PathLineTo(new Vector2(v4.X, v4.Y));
            ImGui.GetWindowDrawList().PathLineTo(new Vector2(v1.X, v1.Y));
            ImGui.GetWindowDrawList().PathFillConvex(
                elementDonut.color
            );
            v1 = v2;
            v4 = v3;
        }
    }

    void DrawLineWorld(DisplayObjectLine e)
    {
        if (p.Profiler.Enabled) p.Profiler.GuiLines.StartTick();
        var result = GetAdjustedLine(new Vector3(e.ax, e.ay, e.az), new Vector3(e.bx, e.by, e.bz));
        if (result.posA == null) return;
        ImGui.GetWindowDrawList().PathLineTo(new Vector2(result.posA.Value.X, result.posA.Value.Y));
        ImGui.GetWindowDrawList().PathLineTo(new Vector2(result.posB.Value.X, result.posB.Value.Y));
        ImGui.GetWindowDrawList().PathStroke(e.color, ImDrawFlags.None, e.thickness);
        if (p.Profiler.Enabled) p.Profiler.GuiLines.StopTick();
    }

    (Vector2? posA, Vector2? posB) GetAdjustedLine(Vector3 pointA, Vector3 pointB)
    {
        var resultA = Svc.GameGui.WorldToScreen(new Vector3(pointA.X, pointA.Z, pointA.Y), out Vector2 posA);
        if (!resultA && !p.DisableLineFix)
        {
            var posA2 = GetLineClosestToVisiblePoint(pointA,
            (pointB - pointA) / p.CurrentLineSegments, 0, p.CurrentLineSegments);
            if (posA2 == null)
            {
                if (p.Profiler.Enabled) p.Profiler.GuiLines.StopTick();
                return (null, null);
            }
            else
            {
                posA = posA2.Value;
            }
        }
        var resultB = Svc.GameGui.WorldToScreen(new Vector3(pointB.X, pointB.Z, pointB.Y), out Vector2 posB);
        if (!resultB && !p.DisableLineFix)
        {
            var posB2 = GetLineClosestToVisiblePoint(pointB,
            (pointA - pointB) / p.CurrentLineSegments, 0, p.CurrentLineSegments);
            if (posB2 == null)
            {
                if (p.Profiler.Enabled) p.Profiler.GuiLines.StopTick();
                return (null, null);
            }
            else
            {
                posB = posB2.Value;
            }
        }

        return (posA, posB);
    }

    void DrawRectWorld(DisplayObjectRect e) //oof
    {
        if (p.Profiler.Enabled) p.Profiler.GuiLines.StartTick();

        if (!p.Config.AltRectFill)
        {
            Svc.GameGui.WorldToScreen(new Vector3(e.l1.ax, e.l1.az, e.l1.ay), out Vector2 v1);
            Svc.GameGui.WorldToScreen(new Vector3(e.l1.bx, e.l1.bz, e.l1.by), out Vector2 v2);
            Svc.GameGui.WorldToScreen(new Vector3(e.l2.bx, e.l2.bz, e.l2.by), out Vector2 v3);
            Svc.GameGui.WorldToScreen(new Vector3(e.l2.ax, e.l2.az, e.l2.ay), out Vector2 v4);
            ImGui.GetWindowDrawList().AddQuadFilled(v1, v2, v3, v4, e.l1.color);
            goto Quit;
        }

        var result1 = GetAdjustedLine(new Vector3(e.l1.ax, e.l1.ay, e.l1.az), new Vector3(e.l1.bx, e.l1.by, e.l1.bz));
        if (result1.posA == null) goto Alternative;
        var result2 = GetAdjustedLine(new Vector3(e.l2.ax, e.l2.ay, e.l2.az), new Vector3(e.l2.bx, e.l2.by, e.l2.bz));
        if (result2.posA == null) goto Alternative;
        goto Build;
    Alternative:
        result1 = GetAdjustedLine(new Vector3(e.l1.ax, e.l1.ay, e.l1.az), new Vector3(e.l2.ax, e.l2.ay, e.l2.az));
        if (result1.posA == null) goto Quit;
        result2 = GetAdjustedLine(new Vector3(e.l1.bx, e.l1.by, e.l1.bz), new Vector3(e.l2.bx, e.l2.by, e.l2.bz));
        if (result2.posA == null) goto Quit;
        Build:
        ImGui.GetWindowDrawList().AddQuadFilled(
            new Vector2(result1.posA.Value.X, result1.posA.Value.Y),
            new Vector2(result1.posB.Value.X, result1.posB.Value.Y),
            new Vector2(result2.posB.Value.X, result2.posB.Value.Y),
            new Vector2(result2.posA.Value.X, result2.posA.Value.Y), e.l1.color
            );
    Quit:
        if (p.Profiler.Enabled) p.Profiler.GuiLines.StopTick();
    }

    Vector2? GetLineClosestToVisiblePoint(Vector3 currentPos, Vector3 targetPos, float eps)
    {
        if (!Svc.GameGui.WorldToScreen(targetPos, out Vector2 res)) return null;

        while (true)
        {
            var mid = (currentPos + targetPos) / 2;
            if (Svc.GameGui.WorldToScreen(mid, out Vector2 pos))
            {
                if ((res - pos).Length() < eps) return res;
                targetPos = mid;
                res = pos;
            }
            else currentPos = mid;
        }
    }

    Vector2? GetLineClosestToVisiblePoint(Vector3 currentPos, Vector3 delta, int curSegment, int numSegments)
    {
        if (curSegment > numSegments) return null;
        var nextPos = currentPos + delta;
        if (Svc.GameGui.WorldToScreen(new Vector3(nextPos.X, nextPos.Z, nextPos.Y), out Vector2 pos))
        {
            var preciseVector = GetLineClosestToVisiblePoint(currentPos, (nextPos - currentPos) / p.Config.lineSegments, 0, p.Config.lineSegments);
            return preciseVector.HasValue ? preciseVector.Value : pos;
        }
        else
        {
            return GetLineClosestToVisiblePoint(nextPos, delta, ++curSegment, numSegments);
        }
    }

    public void DrawTextWorld(DisplayObjectText e)
    {
        if (Svc.GameGui.WorldToScreen(
                        new Vector3(e.x, e.z, e.y),
                        out Vector2 pos))
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

    public void DrawRingWorld(DisplayObjectCircle e)
    {
        int seg = p.Config.segments / 2;
        Svc.GameGui.WorldToScreen(new Vector3(
            e.x + e.radius * (float)Math.Sin(p.CamAngleX),
            e.z,
            e.y + e.radius * (float)Math.Cos(p.CamAngleX)
            ), out Vector2 refpos);
        var visible = false;
        Vector2?[] elements = new Vector2?[p.Config.segments];
        for (int i = 0; i < p.Config.segments; i++)
        {
            visible = Svc.GameGui.WorldToScreen(
                new Vector3(e.x + e.radius * (float)Math.Sin(Math.PI / seg * i),
                e.z,
                e.y + e.radius * (float)Math.Cos(Math.PI / seg * i)
                ),
                out Vector2 pos)
                || visible;
            if (pos.Y > refpos.Y || P.Config.NoCircleFix) elements[i] = new Vector2(pos.X, pos.Y);
        }
        if (visible)
        {
            foreach (var pos in elements)
            {
                if (pos == null) continue;
                ImGui.GetWindowDrawList().PathLineTo(pos.Value);
            }

            if (e.filled)
            {
                ImGui.GetWindowDrawList().PathFillConvex(e.color);
            }
            else
            {
                ImGui.GetWindowDrawList().PathStroke(e.color, ImDrawFlags.Closed, e.thickness);
            }
        }
    }

    public void DrawConeWorld(DisplayObjectCone e)
    {
        var drawList = ImGui.GetWindowDrawList();
        (e.y, e.z) = (e.z, e.y);

        Vector2 v;
        Svc.GameGui.WorldToScreen(new Vector3(e.x, e.y, e.z), out v);
        drawList.PathLineTo(v);

        Svc.GameGui.WorldToScreen(new Vector3(e.x + e.radius * MathF.Cos(e.startRad), e.y, e.z + e.radius * MathF.Sin(e.startRad)), out v);
        drawList.PathLineTo(v);

        for (float i = e.startRad; i < e.endRad; i += MathF.PI / 2)
        {
            float theta = MathF.Min(e.endRad - i, MathF.PI / 2);
            float h = 1.3f * (1 - MathF.Cos(theta / 2)) / MathF.Sin(theta / 2);

            Vector3 arcMid1 = new Vector3(
                e.x + e.radius * (MathF.Cos(i) - h * MathF.Sin(i)),
                e.y,
                e.z + e.radius * (MathF.Sin(i) + h * MathF.Cos(i)));
            Vector3 arcMid2 = new Vector3(
                e.x + e.radius * (MathF.Cos(i + theta) + h * MathF.Sin(i + theta)),
                e.y,
                e.z + e.radius * (MathF.Sin(i + theta) - h * MathF.Cos(i + theta)));
            Vector3 endPoint = new Vector3(
                e.x + e.radius * MathF.Cos(i + theta), e.y, e.z + e.radius * MathF.Sin(i + theta));

            Svc.GameGui.WorldToScreen(arcMid1, out Vector2 v1);
            Svc.GameGui.WorldToScreen(arcMid2, out Vector2 v2);
            Svc.GameGui.WorldToScreen(endPoint, out v);
            drawList.PathBezierCubicCurveTo(v1, v2, v);
        }

        if (e.filled)
        {
            drawList.PathFillConvex(e.color);
        }
        else
        {
            drawList.PathStroke(e.color, ImDrawFlags.Closed);
        }
    }

    public void DrawPoint(DisplayObjectDot e)
    {
        if (Svc.GameGui.WorldToScreen(new Vector3(e.x, e.z, e.y), out Vector2 pos))
            ImGui.GetWindowDrawList().AddCircleFilled(
            new Vector2(pos.X, pos.Y),
            e.thickness,
            ImGui.GetColorU32(e.color),
            100);
    }*/
}
