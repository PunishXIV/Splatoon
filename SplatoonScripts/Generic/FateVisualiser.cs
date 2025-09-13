using ECommons.Configuration;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SplatoonScriptsOfficial.Generic;
/**
 * Displays the border of nearby active FATEs as a series of rotating dots.
 */
internal class FateVisualiser : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [];
    public override Metadata? Metadata => new(0, "sourpuh");

    private class Config : IEzConfig
    {
        public float FadeOutDistance = 35;
        public int Period = 1000;
        public float Density = 1;
    }
    private Config C => Controller.GetConfig<Config>();
    public override void OnSettingsDraw()
    {
        ImGui.DragFloat("Fadeout Distance", ref C.FadeOutDistance, 0.1f, 1, 100);
        ImGui.DragInt("Dot Period (ms)", ref C.Period, 1, 1, 10000);
        ImGui.DragFloat("Dot Density", ref C.Density, 0.1f, 0.1f, 5);
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Fate Border Dot",
        @"{
            ""Name"": ""Fate Border Dot"",
            ""type"": 0,
            ""Enabled"": false,
            ""Filled"": false,
            ""radius"": 0.0,
            ""color"": 4294952960,
            ""thicc"": 4.0
        }");
    }

    public override void OnUpdate()
    {
        foreach(var fate in Svc.Fates)
        {
            if(fate.StartTimeEpoch == 0) continue;
            DrawFateCircle(fate.Position, fate.Radius);
        }
    }

    private void DrawFateCircle(Vector3 origin, float radius, float castHeight = 20)
    {
        var playerPos = Svc.ClientState.LocalPlayer?.Position;
        if(playerPos.HasValue && Controller.TryGetElementByName("Fate Border Dot", out var template))
        {
            var fadeoutDistance = C.FadeOutDistance;
            Vector3 castOffset = new(0, castHeight / 2, 0);

            const float totalAngle = MathF.PI * 2;
            var numSegments = (int)(totalAngle * radius * C.Density);
            var angleStep = totalAngle / numSegments;
            var angleStart = angleStep * (Environment.TickCount64 % C.Period) / C.Period;
            for(var step = 0; step <= numSegments; step++)
            {
                var angle = angleStart + step * angleStep;
                var offset = castOffset + radius * new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle));
                var castOrigin = origin with { Y = playerPos.Value.Y } + offset;
                if(BGCollisionModule.RaycastMaterialFilter(castOrigin, -Vector3.UnitY, out var hit, castHeight))
                {
                    var distance = Vector3.Distance(playerPos.Value, hit.Point);
                    var alpha = 1 - MathF.Min(1, distance / fadeoutDistance);
                    var color = MultiplyAlpha(template.color, alpha);
                    if((color & 0xFF000000) == 0) continue;
                    DrawDot(template, hit.Point, color, 1);
                    DrawDot(template, hit.Point, MultiplyAlpha(0xFFFFFFFF, alpha), 0.5f);
                }
            }
        }
    }

    public static void DrawDot(Element template, Vector3 pos, uint color, float scale)
    {
        Element e = new(0)
        {
            Filled = template.Filled,
            radius = template.radius,
            color = color,
            thicc = template.thicc * scale
        };
        e.SetRefPosition(pos);
        Splatoon.Splatoon.P.InjectElement(e);
    }

    public static uint MultiplyAlpha(uint color, float alphaMultiplier)
    {
        var alpha = (uint)(((color & 0xFF000000) >> 24) * alphaMultiplier);
        return color & 0x00FFFFFF | alpha << 24;
    }
}

