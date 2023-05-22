using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
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

namespace SplatoonScriptsOfficial.Duties.Stormblood;

public class UCOB_Heavensfall_Trio_Towers : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => new() { 733 };

    public override Metadata? Metadata => new(5, "NightmareXIV");

    Config Conf => this.Controller.GetConfig<Config>();
    int NaelTowerPosAngleModifier => Conf.NaelTowerPos == NaelTower.Right_1 ? 3 : -3;

    public override void OnSetup()
    {
        for(var i = 0; i < 8; i++)
        {
            this.Controller.TryRegisterElement($"tower{i}", new(0) { Enabled = false, radius = 3f, thicc = 2f });
        }
    }

    public override void OnUpdate()
    {
        var towers = FindTowers();
        if (towers.Count() == 8 && FindNael().NotNull(out var nael))
        {
            var zeroAngle = (int)(MathHelper.GetRelativeAngle(Vector2.Zero, nael.Position.ToVector2()) - NaelTowerPosAngleModifier + 360) % 360;
            var i = 0;
            foreach(var x in towers.OrderBy(z => (int)(MathHelper.GetRelativeAngle(Vector2.Zero, z.Position.ToVector2()) - zeroAngle + 360) % 360 ))
            {
                if(this.Controller.TryGetElementByName($"tower{i}", out var e))
                {
                    e.SetRefPosition(x.Position);
                    if (Conf.Debug)
                    {
                        e.overlayText = $"Tower {(TowerPosition)i}\n" +
                            $"Angle: {(int)(MathHelper.GetRelativeAngle(Vector2.Zero, x.Position.ToVector2()) - zeroAngle + 360) % 360}\n" +
                            $"Raw angle: {(int)(MathHelper.GetRelativeAngle(Vector2.Zero, x.Position.ToVector2()))}\n" +
                            $"Zero angle: {zeroAngle}";
                    }
                    else
                    {
                        e.overlayText = $"Tower {(TowerPosition)i}";
                    }
                    if(i == (int)Conf.TowerNum)
                    {
                        e.Enabled = true;
                        e.tether = true;
                        e.thicc = 10;
                    }
                    else
                    {
                        if (Conf.ShowAll)
                        {
                            e.Enabled = true;
                            e.tether = false;
                            e.thicc = 2;
                        }
                        else
                        {
                            e.Enabled = false;
                        }
                    }
                    i++;
                }
            }
        }
        else
        {
            for (var i = 0; i < 8; i++)
            {
                if (this.Controller.TryGetElementByName($"tower{i}", out var e))
                {
                    e.Enabled = false;
                }
            }
        }
    }

    IEnumerable<BattleChara> FindTowers()
    {
        return Svc.Objects.Where(x => x is BattleChara c && c.IsCasting && c.CastActionId == 9951).Cast<BattleChara>();
    }

    BattleChara? FindNael()
    {
        return (BattleChara?)Svc.Objects.Where(x => x is BattleChara c && c.NameId == 2612 && c.IsCharacterVisible()).FirstOrDefault();
    }

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(100f);
        ImGuiEx.EnumCombo("Your designated tower", ref Conf.TowerNum);
        ImGui.SetNextItemWidth(100f);
        ImGuiEx.EnumCombo("Tower directly at Nael", ref Conf.NaelTowerPos);
        ImGui.Checkbox("Display all towers", ref Conf.ShowAll);
        ImGui.Checkbox("Display debug info", ref Conf.Debug);
    }

    public class Config : IEzConfig
    {
        public TowerPosition TowerNum = TowerPosition.Right_1;
        public bool ShowAll = false;
        public NaelTower NaelTowerPos = NaelTower.Right_1;
        public bool Debug = false;
    }

    public enum NaelTower
    {
        Left_1 = -1, Right_1 = 1
    }

    public enum TowerPosition : int
    {
        Right_1 = 0, Right_2 = 1, Right_3 = 2, Right_4 = 3, Left_1 = 7, Left_2 = 6, Left_3 = 5, Left_4 = 4
    }
}
