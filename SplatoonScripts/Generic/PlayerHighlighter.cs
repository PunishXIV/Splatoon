using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameFunctions;
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
    public override HashSet<uint>? ValidTerritories => [];
    private Config C => Controller.GetConfig<Config>();
    private List<Element> Elements = [];

    public class Config : IEzConfig
    {
        public int MaxDistance2D = 30;
        public bool ShowName = true;
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(Svc.ClientState.TerritoryType.EqualsAny(MainCities.List)) return;
        var i = 0;
        foreach(var x in Svc.Objects)
        {
            if(x is IPlayerCharacter pc && pc.EntityId != 0xE0000000 && pc.Address != Player.Object.Address && Vector2.Distance(Player.Object.Position.ToVector2(), pc.Position.ToVector2()) <= C.MaxDistance2D)
            {
                var element = GetElement(i++);
                element.refActorObjectID = pc.EntityId;
                element.Enabled = true;
            }
        }
    }

    public Element GetElement(int i)
    {
        if(Controller.TryGetElementByName($"Player{i}", out var element))
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

    public unsafe override void OnSettingsDraw()
    {
        ImGui.DragInt("Max 2D distance", ref C.MaxDistance2D);
    }
}
