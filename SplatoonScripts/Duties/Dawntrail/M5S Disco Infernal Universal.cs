using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe class M5S_Disco_Infernal_Universal : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1257];

    public override Metadata? Metadata => new(4, "NightmareXIV");

    TileDescriptor? TargetedTile = null;
    Element Early => Controller.GetElementByName("Prepare")!;
    Element Go => Controller.GetElementByName("Go")!;

    IPlayerCharacter BasePlayer
    {
        get
        {
            if(C.BasePlayerOverride == "") return Player.Object;
            return Svc.Objects.OfType<IPlayerCharacter>().Where(x => x.EntityId != 0xE0000000).FirstOrDefault(x => x.Name.ToString() == C.BasePlayerOverride) ?? Player.Object; 
        }
    }

    TileDescriptor[] ValidTiles = [new(1, 1), new(1, 6), new(6, 1), new(6, 6), new(2, 3), new(2, 4), new(3, 2), new(4, 2), new(3, 5), new(4, 5), new(5, 3), new(5, 4)];

    TileDescriptor[] RangedUnsafe1 = [new(1, 1), new(6, 6)];
    TileDescriptor[] RangedUnsafe2 = [new(6, 1), new(1, 6)];

    TileDescriptor[] MeleeUnsafe2 = [new(2, 3), new(3, 2), new(4, 5), new(5, 4)];
    TileDescriptor[] MeleeUnsafe1 = [new(2, 4), new(3, 5), new(4, 2), new(5, 3)];
    TileDescriptor[] MeleeLightPos1 = [new(2, 3), new(3, 5), new(4, 2), new(5, 4)];
    TileDescriptor[] MeleeLightPos2 = [new(2, 4), new(3, 2), new(4, 5), new(5, 3)];

    bool FloorInverted = false;
    bool MeleeInverted = false;
    bool RangedInverted = false;
    bool IsLong = false;
    static readonly uint Debuff = 4461;

    //MapEffect: 3, 16, 32 - fucky floor pattern 1
    //MapEffect: 3, 1, 2 - fucky floor pattern 2

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Prepare", "{\"Name\":\"\",\"refX\":0,\"refY\":0,\"radius\":2.0,\"Donut\":0.5,\"color\":3355508694,\"fillIntensity\":0.8,\"overlayBGColor\":3355443200,\"overlayTextColor\":4278255103,\"overlayFScale\":1.0,\"thicc\":4.0,\"overlayText\":\"Cleanse in X\"}");
        Controller.RegisterElementFromCode("Go", "{\"Name\":\"\",\"refX\":0,\"refY\":0,\"radius\":2.0,\"Donut\":0.5,\"color\":3355508509,\"fillIntensity\":0.8,\"overlayBGColor\":3355443200,\"overlayTextColor\":4278255389,\"overlayFScale\":2.0,\"thicc\":4.0,\"overlayText\":\"Cleanse in X\",\"tether\":true}");
        for(int i = 0; i < 8; i++)
        {
            Controller.RegisterElementFromCode($"Debug{i}", "{\"Name\":\"\",\"refX\":0,\"refY\":0,\"refZ\":-15.006033,\"radius\":2.5,\"color\":3372220415,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":4.0}");
        }
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(Controller.CombatSeconds > 60 * 6) return;
        if(Svc.Objects.OfType<IPlayerCharacter>().Where(x => x.EntityId != 0xE0000000).Any(x => x.StatusList.Any(s => s.StatusId == Debuff && s.RemainingTime > 28f)))
        {
            if(position == 3 && data1 == 1 && data2 == 2)
            {
                FloorInverted = false;
                BeginMechanic();
            }
            if(position == 3 && data1 == 16 && data2 == 32)
            {
                FloorInverted = true;
                BeginMechanic();
            }
        }
    }

    void BeginMechanic()
    {
        IsLong = BasePlayer.StatusList.Any(x => x.StatusId == Debuff && x.RemainingTime > 25f);
        //DuoLog.Information($"{Svc.Objects.Where(x => x.DataId == 18363).Select(x => x.Position).Print("\n")}");
        MeleeInverted = !Svc.Objects.Where(x => x.DataId == 18363).Any(x => Vector2.Distance(x.Position.ToVector2(), new(92.5f, 97.5f)) < 0.5f);

        var safe = ValidTiles
            .Where(x => !(FloorInverted ? RangedUnsafe1 : RangedUnsafe2).Contains(x))
            .Where(x => !(FloorInverted ? MeleeUnsafe1 : MeleeUnsafe2).Contains(x))
            .Where(x => (MeleeInverted ^ IsLong ? MeleeLightPos1 : MeleeLightPos2).Contains(x) || RangedUnsafe1.Contains(x) || RangedUnsafe2.Contains(x))
            .ToArray();
        for(int i = 0; i < safe.Length; i++)
        {
            if(C.DisplayAllSafe && Controller.TryGetElementByName($"Debug{i}", out var e))
            {
                e.Enabled = true;
                e.SetRefPosition(GetTileCoordinates(safe[i]).ToVector3());
            }
        }
        if(C.SelectedTiles.TryGetFirst(x => safe.Contains(x), out var tile))
        {
            this.TargetedTile = tile;
        }
        else
        {
            this.TargetedTile = null;
        }
    }

    public override void OnReset()
    {
        this.TargetedTile = null;
    }

    public override void OnUpdate()
    {
        Early.Enabled = false;
        Go.Enabled = false;
        if(!Svc.Objects.OfType<IPlayerCharacter>().Where(x => x.EntityId != 0xE0000000).Any(x => x.StatusList.Any(s => s.StatusId == Debuff)))
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        }
        if(TargetedTile != null && BasePlayer.StatusList.TryGetFirst(s => s.StatusId == Debuff, out var d))
        {
            var e = d.RemainingTime > 9f ? Early : Go;
            e.Enabled = true;
            e.overlayText = $"Cleanse in {d.RemainingTime:F1}";
            e.SetRefPosition(GetTileCoordinates(TargetedTile.Value).ToVector3());
        }
    }

    public override void OnSettingsDraw()
    {
        if(C.SelectedTiles.Count(x => x.IsRanged()) == 2 && C.SelectedTiles.Count == 2)
        {
            ImGuiEx.Text(EColor.GreenBright, "Ranged configuration is valid");
        }
        else if(C.SelectedTiles.Count(x => !x.IsRanged()) == 4 && C.SelectedTiles.Count == 4)
        {
            ImGuiEx.Text(EColor.GreenBright, "Melee configuration is valid");
        }
        else
        {
            ImGuiEx.Text(EColor.RedBright, "Current configuration is not valid. Select 2 (ranged) or 4 (melee) positions:");
        }
        for(int i = 0; i < 8; i++)
        {
            for(int k = 0; k < 8; k++)
            {
                var tile = new TileDescriptor(k, i);
                if(ValidTiles.Contains(tile))
                {
                    ImGui.PushStyleColor(ImGuiCol.CheckMark, EColor.GreenBright);
                    ImGuiEx.CollectionCheckbox($"##{k}{i}", tile, C.SelectedTiles);
                    ImGui.PopStyleColor();
                }
                else
                {
                    bool x = false;
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGuiColors.DalamudGrey3);
                    ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGuiColors.DalamudGrey3);
                    ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGuiColors.DalamudGrey3);
                    ImGui.Checkbox("##null", ref x);
                    ImGui.PopStyleColor(3);
                }
                ImGui.SameLine(0,ImGui.GetStyle().ItemSpacing.Y);
            }
            ImGui.NewLine();
        }
        ImGui.Checkbox("Highlight all safe tiles", ref C.DisplayAllSafe);
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.InputText("Player override", ref C.BasePlayerOverride, 50);
            ImGuiEx.Text($"Base player: {BasePlayer}");
            ImGui.Checkbox("Is long", ref IsLong);
            ImGui.Checkbox("Floor inverted", ref FloorInverted);
            ImGui.Checkbox("Melee inverted", ref MeleeInverted);
            if(ImGui.Button("Restart mechanic"))
            {
                BeginMechanic();
            }
            ImGuiEx.Text($"{Controller.CombatSeconds}");
        }
    }

    public static Vector2 GetTileCoordinates(TileDescriptor d)
    {
        const float originX = 82.5f;
        const float originY = 82.5f;
        const float step = 5f;

        float x = originX + d.X * step;
        float y = originY + d.Y * step;

        return new Vector2(x, y);
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public HashSet<TileDescriptor> SelectedTiles = [];
        public string BasePlayerOverride = "";
        public bool DisplayAllSafe = true;
    }

    public struct TileDescriptor : IEquatable<TileDescriptor>
    {
        public int X;
        public int Y;

        public TileDescriptor(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool IsRanged()
        {
            return X == 1 || X == 6 || Y == 1 || Y == 6;
        }

        public override bool Equals(object? obj)
        {
            return obj is TileDescriptor descriptor && Equals(descriptor);
        }

        public bool Equals(TileDescriptor other)
        {
            return X == other.X &&
                   Y == other.Y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(TileDescriptor left, TileDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TileDescriptor left, TileDescriptor right)
        {
            return !(left == right);
        }
    }
}
