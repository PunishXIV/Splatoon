using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M8S_Ultraviolent_Ray : SplatoonScript
{
    private const string MarkerVfxPath = "vfx/lockon/eff/m0005sp_19o0t.avfx";
    private const uint UltraviolentRayCastId = 42076;

    private List<IntPtr> _aoeList = [];
    private string _basePlayerOverride = "";
    public override HashSet<uint>? ValidTerritories => [1263];
    public override Metadata? Metadata => new(1, "Garume");

    private IPlayerCharacter BasePlayer
    {
        get
        {
            if (_basePlayerOverride == "")
                return Player.Object;
            return Svc.Objects.OfType<IPlayerCharacter>()
                .FirstOrDefault(x => x.Name.ToString().EqualsIgnoreCase(_basePlayerOverride)) ?? Player.Object;
        }
    }


    public Config C => Controller.GetConfig<Config>();

    public override void OnSettingsDraw()
    {
        C.PriorityData.Draw();

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.SetNextItemWidth(200);
            ImGui.InputText("Player override", ref _basePlayerOverride, 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.BeginCombo("Select..", "Select..."))
            {
                foreach (var x in Svc.Objects.OfType<IPlayerCharacter>())
                    if (ImGui.Selectable(x.GetNameWithWorld()))
                        _basePlayerOverride = x.Name.ToString();
                ImGui.EndCombo();
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action is { RowId: UltraviolentRayCastId })
        {
            _aoeList.Clear();
            if (Controller.TryGetElementByName("Bait", out var e)) e.Enabled = false;
        }
    }

    public override void OnReset()
    {
        _aoeList.Clear();
        if (Controller.TryGetElementByName("Bait", out var e)) e.Enabled = false;
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (target.GetObject() is IPlayerCharacter player && vfxPath == MarkerVfxPath)
        {
            _aoeList.Add(player.Address);
            if (_aoeList.Count == 5 && Controller.TryGetElementByName("Bait", out var e))
            {
                var ownIndex = C.PriorityData.GetPlayers(x => _aoeList.Contains(x.IGameObject.Address))?
                    .IndexOf(x => x.IGameObject.Address == BasePlayer.Address);
                var noPredicateOwnIndex = C.PriorityData.GetPlayers(_ => true)?
                    .IndexOf(x => x.IGameObject.Address == BasePlayer.Address);

                var direction = ownIndex switch
                {
                    -1 => noPredicateOwnIndex < 4 ? Direction.West : Direction.East,
                    0 => Direction.NorthWest,
                    1 => Direction.West,
                    2 => Direction.South,
                    3 => Direction.East,
                    4 => Direction.NorthEast,
                    _ => Direction.West
                };

                e.Enabled = true;
                e.SetRefPosition(DirectionToPosition(direction));
            }
        }
    }

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            radius = 5f,
            thicc = 15f,
            tether = true,
            Donut = 0.3f
        };
        Controller.RegisterElement("Bait", element);
    }

    public override void OnUpdate()
    {
        if (_aoeList.Count != 0)
            Controller.GetRegisteredElements()
                .Each(x => x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
    }

    private Vector3 DirectionToPosition(Direction direction)
    {
        const float radius = 17f;
        var angle = (int)direction;
        var center = new Vector2(100f, 100f);
        var x = center.X + radius * MathF.Cos(angle * MathF.PI / 180);
        var y = center.Y + radius * MathF.Sin(angle * MathF.PI / 180);
        return new Vector3(x, -150f, y);
    }

    private enum Direction
    {
        NorthEast = 306,
        East = 18,
        South = 90,
        West = 162,
        NorthWest = 234
    }

    public class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public PriorityData PriorityData = new();
    }
}