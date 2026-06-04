using Dalamud.Bindings.ImGui;
using ECommons;
using ECommons.CircularBuffers;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Splatoon;
using Splatoon.Services;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using TerraFX.Interop.Windows;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public unsafe class P2_Forsaken : SplatoonScript<P2_Forsaken.Config>
{
    public override Metadata Metadata { get; } = new(4, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1363];

    public uint EffectSpread = 5085;
    public uint EffectStack = 5084;
    public uint EffectFan = 5086;

    public uint ActionTowerExplode = 47806;
    List<uint> FirstTakers = [];
    uint TowerCount = 0;

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
                {"Name":"Stack","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayTextColor":4278779648,"overlayVOffset":1.2,"thicc":0.0,"overlayText":">>> Stack <<<","refActorComparisonType":2}
                """);
            Controller.RegisterElementFromCode($"Spread{i}", """
                {"Name":"Spread","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayTextColor":4278190335,"overlayVOffset":1.2,"thicc":0.0,"overlayText":"<<< Spread >>>","refActorComparisonType":2}
                """);
            Controller.RegisterElementFromCode($"Fan{i}", """
                {"Name":"Cone","type":1,"radius":0.0,"color":3372220160,"Filled":false,"fillIntensity":0.5,"overlayTextColor":4294180608,"overlayVOffset":1.2,"thicc":0.0,"overlayText":"^^^ Cone ^^^","refActorComparisonType":2}
                """);
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action != null && set.Action.Value.RowId == this.ActionTowerExplode)
        {
            this.TowerCount++;
            if(this.FirstTakers.Count < 4)
            {
                foreach(var x in set.TargetEffects)
                {
                    if(((uint)x.TargetID).TryGetPlayer(out var p))
                    {
                        FirstTakers.Add(p.ObjectId);
                    }
                }
            }
        }
    }

    public override void OnReset()
    {
        FirstTakers.Clear();
        this.TowerCount = 0;
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
                if(FirstTakers.Count > 0)
                {
                    var isTaking = FirstTakers.Contains(id);
                    if((this.TowerCount/2).EqualsAny<uint>(1, 2, 5, 6)) isTaking = !isTaking;
                    if(!isTaking)
                    {
                        e.overlayText = Controller.OriginalElements[eName].overlayText + "| -- OUT --";
                    }
                    else
                    {
                        e.overlayText = Controller.OriginalElements[eName].overlayText + "| ++ IN ++";
                    }
                }
                else
                {
                    e.overlayText = Controller.OriginalElements[eName].overlayText;
                }
                return;
            }
        }
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        foreach(var x in Controller.GetPartyMembers())
        {
            if(x.StatusList.Any(s => s.StatusId == this.EffectFan)) ShowNextElement(x.ObjectId, "Fan");
            if(x.StatusList.Any(s => s.StatusId == this.EffectStack)) ShowNextElement(x.ObjectId, "Stack");
            if(x.StatusList.Any(s => s.StatusId == this.EffectSpread)) ShowNextElement(x.ObjectId, "Spread");
        }
    }

    CircularArray<uint> ActiveMapEffects = new(2);

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(this.MapEffect2TowerPos.ContainsKey(position) && data1 == 1)
        {
            ActiveMapEffects.Push(position);
        }
    }

    public override void OnSettingsDraw()
    {
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.InputUInt("Tower count", ref this.TowerCount);
            ImGuiEx.Text($"First takers: \n{FirstTakers.Select(x => x.TryGetPlayer(out var p) ? p.ToString() : "").Print("\n")}");
            foreach(var x in MapEffect2TowerPos)
            {
                ImGuiEx.Text(ActiveMapEffects.Contains(x.Key) ?EColor.GreenBright:null, $"{x.Key}: {x.Value}");
            }
        }
    }

    public class Config
    {
        public bool ShowAll = false;
    }
}
