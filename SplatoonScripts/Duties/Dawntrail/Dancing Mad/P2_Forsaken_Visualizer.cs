using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using Dalamud.Bindings.ImGui;
using ECommons;
using ECommons.CircularBuffers;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public unsafe class P2_Forsaken_Visualizer : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV, Poneglyph");
    public override HashSet<uint>? ValidTerritories { get; } = [1363];

    public uint EffectSpread = 5085;
    public uint EffectStack = 5084;
    public uint EffectFan = 5086;

    public uint DebuffSpellsTrouble = 5083;

    Dictionary<uint, Vector2> MapEffect2TowerPos
    {
        get
        {
            if(field == null)
            {
                field = [];
                for(uint i = 1; i <= 8; i++)
                {
                    field[i] = MathHelper.RotateWorldPoint(new(100, 0, 100), (45f * (i - 1)).DegreesToRadians(), new(100, 0, 92)).ToVector2();
                }
                for(uint i = 9; i <= 16; i++)
                {
                    field[i] = MathHelper.RotateWorldPoint(new(100, 0, 100), (45f * (i - 1)).DegreesToRadians(), new(100, 0, 88)).ToVector2();
                }
            }
            return field;
        }
    }

    public override void OnSetup()
    {
        for(int i = 0; i < 8; i++)
        {
            Controller.RegisterElementFromCode($"Stack{i}", """
                {"Name":"Stack","type":1,"radius":5.0,"Donut":0.5,"color":3357277952,"fillIntensity":0.5,"overlayTextColor":4278779648,"overlayVOffset":1.2,"overlayText":"","refActorComparisonType":2}
                """);
            Controller.RegisterElementFromCode($"Spread{i}", """
                {"Name":"Spread","type":1,"radius":5.0,"fillIntensity":0.5,"Donut":0.5,"overlayTextColor":4278190335,"overlayVOffset":1.2,"overlayText":"","refActorComparisonType":2}
                """);
            Controller.RegisterElementFromCode($"Fan{i}", """
                {"Name":"Cone","type":4,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.3,"overlayTextColor":4294180608,"overlayVOffset":1.2,"overlayText":"","thicc":8.0,"includeRotation":true,"FaceMe":true,"refActorComparisonType":2}
                """);
        }

    }

    void ShowNextElement(uint id, string kind)
    {
        for(int i = 0; i < 8; i++)
        {
            var eName = $"{kind}{i}";
            if(Controller.TryGetElementByName(eName, out var e) && !e.Enabled)
            {
                e.Enabled = true;
                e.refActorObjectID = id;
                e.overlayText = Controller.OriginalElements[eName].overlayText;
                return;
            }
        }
    }

    private const float TowerCoordinateRadius = 4f;

    private bool IsPlayerInActiveTower(IPlayerCharacter player)
    {
        if(player.IsDead)
        {
            return false;
        }

        if(ActiveMapEffects.Count() == 0)
        {
            return false;
        }

        var playerPos = new Vector2(player.Position.X, player.Position.Z);
        var threshold = TowerCoordinateRadius * TowerCoordinateRadius;

        foreach(var effectPosition in ActiveMapEffects)
        {
            if(!MapEffect2TowerPos.TryGetValue(effectPosition, out var towerPos))
            {
                continue;
            }

            if(Vector2.DistanceSquared(playerPos, towerPos) <= threshold)
            {
                return true;
            }
        }

        return false;
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        var pcs = Svc.Objects.OfType<IPlayerCharacter>().ToList();
        
        if(Controller.GetPartyMembers().Any(x => x.StatusList.Any(s => s.StatusId == DebuffSpellsTrouble)))
        {
            for (int j = 0; j < pcs.Count && j < 8; j++)
            {
                var source = pcs[j];

                if (!IsPlayerInActiveTower(source))
                {
                    continue;
                }
                
                if (source.StatusList.Any(s => s.StatusId == this.EffectFan))
                {
                    var nearest = pcs
                        .Where(x => x.EntityId != source.EntityId)
                        .OrderBy(x => Vector3.DistanceSquared(x.Position, source.Position))
                        .FirstOrDefault();

                    if (nearest != null && Controller.TryGetElementByName($"Fan{j}", out var e))
                    {
                        e.refActorComparisonType = 2;
                        e.refActorObjectID = source.EntityId;
                        e.faceplayer = GetPlayerOrder(nearest);
                        e.Enabled = true;
                        
                        e.overlayText = Controller.OriginalElements[$"Fan{j}"].overlayText;
                    }
                }
            }

            foreach(var x in Controller.GetPartyMembers().Where(IsPlayerInActiveTower))
            {
                if(x.StatusList.Any(s => s.StatusId == this.EffectStack)) ShowNextElement(x.ObjectId, "Stack");
                if(x.StatusList.Any(s => s.StatusId == this.EffectSpread)) ShowNextElement(x.ObjectId, "Spread");
            }
        }
    }

    private string GetPlayerOrder(IPlayerCharacter c)
    {
        for (var i = 1; i <= 8; i++)
        {
            if ((nint)FakePronoun.Resolve($"<{i}>") == c.Address)
                return $"<{i}>";
        }
        throw new Exception("Could not determine player order");
    }

    CircularArray<uint> ActiveMapEffects = new(2);

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(this.MapEffect2TowerPos.ContainsKey(position) && data1 == 1)
        {
            ActiveMapEffects.Push(position);
        }
    }

    public class Config
    {
        public bool ShowAll = false;
    }
}
