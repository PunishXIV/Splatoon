using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using Splatoon.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe class M6S_Lava_Towers : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1259];
    public override Metadata? Metadata => new(1, "NightmareXIV");

    bool ReadyToSoak = false;
    bool IsSecondTowers => Svc.Objects.OfType<IPlayerCharacter>().Any(x => x.StatusList.Any(s => s.StatusId == 4450));

    Dictionary<int, Vector2> Towers = new()
    {
        //top left
        [69] = new(83, 91),
        [70] = new(93, 89),
        [71] = new(92,96),
        [72] = new(83,102),
        [73] = new(94,84),
        [74] = new(83,88),
        [75] = new(90,89),
        [76] = new(83,95),
        [77] = new(90,97.5f),
        [78] = new(83,104),
        //top right
        [79] = new(110,93),
        [80] = new(117,92),
        [81] = new(109,97),
        [82] = new(115,105),
        [83] = new(110,83),
        [84] = new(117,85),
        [85] = new(110,91),
        [86] = new(117,96),
        [87] = new(111,100),
        [88] = new(117,106),
        //bottom
        [89] = new(100,108),
        [90] = new(85,114),
        [91] = new(98,117),
        [92] = new(112,116),
        [93] = new(92,110),
        [94] = new(91,117),
        [95] = new(107,111),
        [96] = new(105,117),
    };

    HashSet<int> ActiveTowers = [];

    TowerPosition GetTowerPosition(Vector2 position)
    {
        if(Vector2.Distance(position, new(98, 123)) < 20f) return TowerPosition.Bottom;
        if(Vector2.Distance(position, new(81,88)) < 20f) return TowerPosition.Left;
        if(Vector2.Distance(position, new(121,91)) < 20f) return TowerPosition.Right;
        return TowerPosition.Undefined;
    }

    public enum TowerPosition { Undefined, Left, Right, Bottom }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("BaitLeft", "{\"Name\":\"\",\"refX\":86.55319,\"refY\":86.70691,\"radius\":4.0,\"color\":3355508503,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":4278190080,\"overlayTextColor\":4279172864,\"overlayFScale\":2.0,\"thicc\":8.0,\"overlayText\":\"Bait here\",\"tether\":true}");
        Controller.RegisterElementFromCode("BaitRight", "{\"Name\":\"\",\"refX\":114.13297,\"refY\":88.13472,\"refZ\":9.536743E-07,\"radius\":4.0,\"color\":3355508503,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":4278190080,\"overlayTextColor\":4279172864,\"overlayFScale\":2.0,\"thicc\":8.0,\"overlayText\":\"Bait here\",\"tether\":true}");
        Controller.RegisterElementFromCode("StayLeft", "{\"Name\":\"\",\"refX\":93.60492,\"refY\":96.64029,\"refZ\":1.9073486E-06,\"radius\":2.0,\"color\":3355508503,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":4278190080,\"overlayTextColor\":4279172864,\"overlayFScale\":2.0,\"thicc\":8.0,\"overlayText\":\"Stay Close\",\"tether\":true}");
        Controller.RegisterElementFromCode("StayRight", "{\"Name\":\"\",\"refX\":106.86854,\"refY\":98.85092,\"refZ\":-1.9073486E-06,\"radius\":2.0,\"color\":3355508503,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":4278190080,\"overlayTextColor\":4279172864,\"overlayFScale\":2.0,\"thicc\":8.0,\"overlayText\":\"Stay Close\",\"tether\":true}");
        Controller.RegisterElementFromCode("Prepare", "{\"Name\":\"\",\"refX\":84.394325,\"refY\":98.02954,\"refZ\":1.9073486E-06,\"radius\":3.0,\"fillIntensity\":0.8,\"overlayBGColor\":3355443200,\"overlayTextColor\":4278190335,\"overlayFScale\":2.0,\"thicc\":4.0,\"overlayText\":\"!!! PREPARE !!!\",\"tether\":true}");
        Controller.RegisterElementFromCode("Take", "{\"Name\":\"\",\"refX\":84.394325,\"refY\":98.02954,\"refZ\":1.9073486E-06,\"radius\":2.5,\"Donut\":0.5,\"color\":3356032768,\"fillIntensity\":0.8,\"overlayBGColor\":3355443200,\"overlayTextColor\":4279172864,\"overlayFScale\":2.0,\"thicc\":4.0,\"overlayText\":\"Take tower\",\"tether\":true}");
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(Svc.Objects.OfType<IBattleNpc>().TryGetFirst(x => x.IsTargetable && x.DataId == 18335, out var result)
            && result.Struct()->GetCastInfo() != null && ((result.CastActionId == 42649 && result.CurrentCastTime <= 6.6f) || result.IsCasting(42679)))
        {
            this.ReadyToSoak = false;
            if(C.StartingPosition == TowerPosition.Left || C.StartingPosition == TowerPosition.Right)
            {
                Controller.GetElementByName($"{(C.BaitStack ? "Bait" : "Stay")}{C.StartingPosition}")!.Enabled = true;
            }
        }

        if(this.ActiveTowers.Count > 0)
        {
            var myTowers = this.ActiveTowers.Select(x => Towers[x]).Where(x => GetTowerPosition(x) == C.StartingPosition).OrderBy(x => Vector2.Distance(x, new(100, 100)));
            if(!IsSecondTowers)
            {
                if(Controller.TryGetElementByName(ReadyToSoak ? "Take" : "Prepare", out var e))
                {
                    e.Enabled = true;
                    e.SetRefPosition((C.TwoTowerCloserToMiddle ? myTowers.First() : myTowers.Last()).ToVector3(0));
                }
            }
            else
            {
                return;
                if(myTowers.Count() == 4)
                {
                    var adjTowers = this.ActiveTowers.Select(x => Towers[x]).Where(x => GetTowerPosition(x) == C.EscapeFrom4Towers).OrderBy(x => Vector2.Distance(x, new(100, 100)));
                    if(Controller.TryGetElementByName(ReadyToSoak ? "Take" : "Prepare", out var e))
                    {
                        e.Enabled = true;
                        e.SetRefPosition((C.TwoTowerCloserToMiddle ? adjTowers.First() : adjTowers.Last()).ToVector3(0));
                    }
                }
                else
                {
                    
                }
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action?.RowId == 42652)
        {
            this.ReadyToSoak = true;
        }
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(Towers.ContainsKey((int)position))
        {
            if(data1 == 1 && data2 == 2)
            {
                ActiveTowers.Add((int)position);
            }
            else if(data1 == 4 && data2 == 8)
            {
                this.ReadyToSoak = false;
                ActiveTowers.Clear();
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.EnumCombo("Starting Position", ref C.StartingPosition);
        if(C.StartingPosition == TowerPosition.Left || C.StartingPosition == TowerPosition.Right)
        {
            ImGui.Checkbox("Bait stack", ref C.BaitStack);
            ImGuiEx.Text($"Tower to take:");
            ImGuiEx.RadioButtonBool("Closer to Middle of the arena", "Further from Middle of the arena", ref C.TwoTowerCloserToMiddle);
            ImGuiEx.EnumCombo("Flying from 4 towers to", ref C.EscapeFrom4Towers);
        }
    }
    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public TowerPosition StartingPosition = TowerPosition.Undefined;
        public bool BaitStack = false;
        public bool TwoTowerCloserToMiddle = true;
        public TowerPosition EscapeFrom4Towers = TowerPosition.Undefined;
    }
}
