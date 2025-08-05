using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.Interop;
using Dalamud.Bindings.ImGui;
using SharpDX.Direct3D11;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe sealed class EX4_Roseblood_3 : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1271];

    bool? IsDropper = null;
    List<InnerTile>? Tiles = null;

    public Vector3[] DropperPositions =
    [
        new(102,0,95), //r1
        new(95,0,102), //m1
        new(98,0,105), //m2
        new(102,0,105), //r2
    ];

    public Vector3[] TakerPositions =
    [
        new(98,0,95), //r1
        new(92,0,96), //m1
        new(96,0,102), //m2
        new(108,0,104), //r2
    ];

    public Vector3[] SafePositions =
    [
        new(99,0,91), //r1
        new(95,0,98), //m1
        new(93,0,106), //m2
        new(105,0,102), //r2
    ];

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("GoTo", """{"Name":"","refX":96.19253,"refY":108.19956,"radius":0.7,"color":3357671168,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
    }

    public override void OnReset()
    {
        IsDropper = null;
    }

    //line and right:
    //4 = north
    //5 = northeast
    //6 = east
    //7 = southeast
    //8 = south
    //9 = southwest
    //10 = west
    //11 = northwest
    public override void OnSettingsDraw()
    {
        ImGuiEx.Text("Hector strat (tile between two inner is new north)");
        ImGuiEx.EnumCombo("Tower taking position", ref C.Position);
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Checkbox("IsDropped", ref this.IsDropped);
            ImGuiEx.Checkbox("IsDropper", ref this.IsDropper);
            ImGuiEx.TextWrappedCopy($"""
            GetFilledTiles: {GetFilledTiles().Print()}
            TryFindRelativeTile: {TryFindRelativeTile(GetFilledTiles(), out var rel)}
            rel: {rel}
            """);
            var cd = EventFramework.Instance()->GetInstanceContentDirector();
            if(cd != null && cd->MapEffects != null)
            {
                for(int i = 4; i < 3 + 8; i++)
                {
                    ref var effect = ref cd->MapEffects->Items[i];
                    ImGuiEx.Text($"{i} Effect: {effect.LayoutId}/{effect.State}/{effect.Flags:B8}");
                    ImGui.SameLine();
                    if(ImGui.SmallButton($"c##{i}"))
                    {
                        GenericHelpers.Copy($"{(nint)(&cd->MapEffects->Items.GetPointer(i)->State):X}");
                    }
                }
            }
        }
    }

    //  Magic Vulnerability Up (3414), Remains = 8.1, Param = 0, Count = 0
    bool IsDropped = Svc.Objects.OfType<IPlayerCharacter>().Any(x => x.StatusList.Any(s => s.StatusId == 3414));

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(vfxPath == "vfx/lockon/eff/x6fd_monyou_lock1v.avfx")
        {
            if(this.IsDropper != true && target.TryGetObject(out var obj))
            {
                IsDropper = obj.AddressEquals(Player.Object);
            }
        }
        //> [31.07.2025 08:16:41 +03:00] Message: VFX vfx/lockon/eff/x6fd_monyou_lock1v.avfx
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        
        {
            var eff = GetFilledTiles();
            if(eff.Count == 2 && TryFindRelativeTile(eff, out var rel))
            {
                Tiles = eff;
            }
            else if(eff.Count == 0)
            {
                Tiles = null;
            }
        }
        if(Tiles != null)
        {
            if(Tiles.Count == 2 && TryFindRelativeTile(Tiles, out var rel))
            {
                if(IsDropper != null)
                {
                    Vector3 pos;
                    if(IsDropper.Value)
                    {
                        //dropper
                        if(!IsDropped)
                        {
                            pos = DropperPositions[(int)C.Position];
                        }
                        else
                        {
                            pos = SafePositions[(int)C.Position];
                        }
                    }
                    else
                    {
                        //tower taker
                        pos = this.TakerPositions[(int)C.Position];
                    }
                    if(Controller.TryGetElementByName("GoTo", out var e))
                    {
                        e.Enabled = true;
                        var r = MathHelper.RotateWorldPoint(new(100, 0, 100), (360f / 8f * ((int)rel - 4)).DegreesToRadians(), pos);
                        e.SetRefPosition(r);
                    }
                }
            }
        }
        else
        {
            IsDropper = null;
        }
    }

    public enum InnerTile
    {
        North = 4,
        NorthEast = 5,
        East = 6,
        SouthEast = 7,
        South = 8,
        SouthWest = 9,
        West = 10,
        NorthWest = 11
    }

    List<InnerTile> GetFilledTiles()
    {
        var ret = new List<InnerTile>();
        var cd = EventFramework.Instance()->GetInstanceContentDirector();
        if(cd != null && cd->MapEffects != null)
        {
            for(int i = 4; i < 3 + 8; i++)
            {
                ref var effect = ref cd->MapEffects->Items[i];
                if(effect.State == 64)
                {
                    ret.Add((InnerTile)i);
                }
            }
        }
        return ret;
    }

    bool TryFindRelativeTile(List<InnerTile> filledTiles, out InnerTile rel)
    {
        var cd = EventFramework.Instance()->GetInstanceContentDirector();
        if(cd != null && cd->MapEffects != null)
        {
            bool[] circle = new bool[8];
            foreach(var tile in filledTiles)
                circle[(int)tile - 4] = true;

            for(int i = 0; i < 8; i++)
            {
                if(circle[i] && !circle[(i + 1) % 8] && circle[(i + 2) % 8])
                {
                    rel = (InnerTile)((i + 1) % 8 + 4);
                    return true;
                }
            }
        }
        rel = default;
        return false;
    }

    public enum Position
    {
        North, 
        West,
        South,
        East,
    }

    public Config C => Controller.GetConfig<Config>();

    public class Config: IEzConfig
    {
        public Position Position;
    }
}