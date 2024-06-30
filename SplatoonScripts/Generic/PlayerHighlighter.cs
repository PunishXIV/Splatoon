using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public class PlayerHighlighter : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => new();
    Config C => Controller.GetConfig<Config>();
    List<Element> Elements = new();

    public class Config : IEzConfig
    {
        public int MaxDistance2D = 30;
        public bool ShowName = true;
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if (Svc.ClientState.TerritoryType.EqualsAny(MainCities.List)) return;
        int i = 0;
        foreach(var x in Svc.Objects)
        {
            if(x is PlayerCharacter pc && pc.Address != Player.Object.Address && Vector2.Distance(Player.Object.Position.ToVector2(), pc.Position.ToVector2()) <= C.MaxDistance2D)
            {
                var element = GetElement(i++);
                element.refActorObjectID = pc.ObjectId;
                element.Enabled = true;
            }
        }
    }

    public Element GetElement(int i)
    {
        if (Controller.TryGetElementByName($"Player{i}", out var element))
        {
            return element;
        }
        else
        {
            var ret = new Element(1)
            {
                refActorType = 0,
                radius = 0,
                refActorComparisonType = 2,
                overlayText = "$NAME",
                tether = true,
                overlayPlaceholders = true,
            };
            Controller.RegisterElement($"Player{i}", ret);
            return ret;
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.DragInt("Max 2D distance", ref C.MaxDistance2D);
    }
}
