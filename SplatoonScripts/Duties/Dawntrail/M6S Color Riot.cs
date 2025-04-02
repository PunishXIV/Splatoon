using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M6S_Color_Riot :SplatoonScript
{
    private const uint RedDebuff = 0x1163;
    private const uint BlueDebuff = 0x1164;

    private bool _isActive = true;

    private bool _nearIsRed;

    public override HashSet<uint>? ValidTerritories => [1259];
    public override Metadata? Metadata => new(2, "Garume,Redmoon");
    private static IBattleNpc? Enemy => Svc.Objects.Where(x => x.DataId == 0x479F).OfType<IBattleNpc>().FirstOrDefault();
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

    private string _basePlayerOverride = "";
    public override void OnSetup()
    {
        var nearElement = new Element(0)
        {
            radius = 3.9f,
        };
        Controller.RegisterElement("Near", nearElement);

        var farElement = new Element(0)
        {
            radius = 3.9f,
        };
        Controller.RegisterElement("Far", farElement);

        var avoidElement = new Element(1)
        {
            refActorComparisonType = 2,
            radius = 0.2f,
        };
        Controller.RegisterElement("Avoid", avoidElement);

        var textElement = new Element(0)
        {
            radius = 0f,
            overlayFScale = 2f,
            overlayVOffset = 2f
        };
        Controller.RegisterElement("Text", textElement);
    }

    public override void OnSettingsDraw()
    {
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
            ImGui.Text($"_isActive: {_isActive}");
            ImGui.Text($"_nearIsRed: {_nearIsRed}");
            ImGui.Text($"{BasePlayer.Name.ToString()}");
            ImGui.Text($"{BasePlayer.GetJob().ToString()}");
            ImGui.Text($"{BasePlayer.GetJob().IsTank().ToString()}");
            if (Enemy == null) return;
            ImGui.Text($"IN: {Svc.Objects.OfType<IPlayerCharacter>()
                .Where(x => !x.GetJob().IsTank())
                .OrderBy(x => Vector2.Distance(x.Position.ToVector2(), Enemy.Position.ToVector2())).ToList().SafeSelect(0)}");
            ImGui.Text($"OUT: {Svc.Objects.OfType<IPlayerCharacter>()
                .Where(x => !x.GetJob().IsTank())
                .OrderBy(x => Vector2.Distance(x.Position.ToVector2(), Enemy.Position.ToVector2())).ToList().SafeSelect(5)}");
            ImGuiEx.Text($"{Svc.Objects.OfType<IPlayerCharacter>().OrderBy(x => Vector3.Distance(x.Position, Enemy.Position)).Print("\n")}");
        }
    }

    public override void OnUpdate()
    {
        if (_isActive && Enemy is { } enemy)
        {
            var positions = FakeParty.Get().Select(x => (x.Address, x.Position))
                .OrderBy(x => Vector3.Distance(enemy.Position, x.Position));
            var farPositionMap = positions.Last();
            var nearPositionMap = positions.First();
            var hasRedDebuff = BasePlayer.StatusList.Any(x => x.StatusId == RedDebuff);
            var hasBlueDebuff = BasePlayer.StatusList.Any(x => x.StatusId == BlueDebuff);

            if (!Controller.TryGetElementByName("Far", out var far))
                return;
            if (!Controller.TryGetElementByName("Near", out var near))
                return;
            if (!Controller.TryGetElementByName("Text", out var text))
                return;
            if (!Controller.TryGetElementByName("Avoid", out var avoid))
                return;

            if (_nearIsRed)
            {
                if (hasRedDebuff)
                {
                    avoid.refActorObjectID = Enemy.EntityId;
                    avoid.Donut = 0f;
                    avoid.radius = GetRadius(false);
                    text.overlayText = farPositionMap.Address == BasePlayer.Address ? "Correct!!" : "Go Far!!";
                }
                else if (hasBlueDebuff)
                {
                    avoid.refActorObjectID = Enemy.EntityId;
                    avoid.Donut = 25f;
                    avoid.radius = GetRadius(true);
                    text.overlayText = nearPositionMap.Address == BasePlayer.Address ? "Correct!!" : "Go Near!!";
                }
                else if (BasePlayer.GetJob().IsTank() && BasePlayer.GetJob() == Job.DRK)
                {
                    avoid.refActorObjectID = Enemy.EntityId;
                    avoid.Donut = 25f;
                    avoid.radius = GetRadius(true);
                    text.overlayText = nearPositionMap.Address == BasePlayer.Address ? "Correct!!" : "Go Near!!";
                }
                else if (BasePlayer.GetJob().IsTank() && BasePlayer.GetJob() == Job.PLD)
                {
                    avoid.refActorObjectID = Enemy.EntityId;
                    avoid.Donut = 0f;
                    avoid.radius = GetRadius(false);
                    text.overlayText = farPositionMap.Address == BasePlayer.Address ? "Correct!!" : "Go Far!!";
                }
            }
            else
            {
                if (hasBlueDebuff)
                {
                    avoid.refActorObjectID = Enemy.EntityId;
                    avoid.Donut = 25f;
                    avoid.radius = GetRadius(false);
                    text.overlayText = farPositionMap.Address == BasePlayer.Address ? "Correct!!" : "Go Far!!";
                }
                else if (hasRedDebuff)
                {
                    avoid.refActorObjectID = Enemy.EntityId;
                    avoid.Donut = 0f;
                    avoid.radius = GetRadius(true);
                    text.overlayText = nearPositionMap.Address == BasePlayer.Address ? "Correct!!" : "Go Near!!";
                }
                else if (BasePlayer.GetJob().IsTank() && BasePlayer.GetJob() == Job.DRK)
                {
                    avoid.refActorObjectID = Enemy.EntityId;
                    avoid.Donut = 25f;
                    avoid.radius = GetRadius(true);
                    text.overlayText = nearPositionMap.Address == BasePlayer.Address ? "Correct!!" : "Go Near!!";

                }
                else if (BasePlayer.GetJob().IsTank() && BasePlayer.GetJob() == Job.PLD)
                {
                    avoid.refActorObjectID = Enemy.EntityId;
                    avoid.Donut = 0f;
                    avoid.radius = GetRadius(false);
                    text.overlayText = farPositionMap.Address == BasePlayer.Address ? "Correct!!" : "Go Far!!";
                }
            }

            if (hasRedDebuff || hasBlueDebuff || BasePlayer.GetJob().IsTank())
            {
                avoid.Enabled = true;
                text.Enabled = true;
                text.SetOffPosition(BasePlayer.Position);
            }
            else
            {
                avoid.Enabled = false;
                text.Enabled = false;
                text.overlayText = "";
            }

            far.Enabled = true;
            far.SetOffPosition(farPositionMap.Position);
            far.color = _nearIsRed ? EColor.BlueBright.ToUint() : EColor.RedBright.ToUint();

            near.Enabled = true;
            near.SetOffPosition(nearPositionMap.Position);
            near.color = _nearIsRed ? EColor.RedBright.ToUint() : EColor.BlueBright.ToUint();
        }
        else
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        }
    }

    public override void OnReset()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        _isActive = false;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        switch (castId)
        {
            case 42641:
                _nearIsRed = false;
                _isActive = true;
                break;
            case 42642:
                _nearIsRed = true;
                _isActive = true;
                break;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action is { RowId: 42641 } or { RowId: 42642 }) _isActive = false;
    }

    private float GetRadius(bool isIn)
    {
        var z = Enemy;
        if (z == null) return 5f;
        var breakpoint =
            Svc.Objects.OfType<IPlayerCharacter>()
                .Where(x => !x.GetJob().IsTank())
                .OrderBy(x => Vector2.Distance(x.Position.ToVector2(), z.Position.ToVector2())).ToList().SafeSelect(isIn ? 0 : 5);
        if (breakpoint == null) return 5f;
        var distance = Vector2.Distance(z.Position.ToVector2(), breakpoint.Position.ToVector2());
        //distance += isIn ? -0.5f : 0.5f;
        return Math.Max(0.5f, distance);
    }
}