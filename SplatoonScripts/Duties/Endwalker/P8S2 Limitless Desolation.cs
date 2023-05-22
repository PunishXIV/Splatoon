using Dalamud.Interface.Colors;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Schedulers;
using Splatoon.SplatoonScripting;
using Splatoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P8S2_Limitless_Desolation : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1088 }; //We need our script to work in P8S only.
        TickScheduler? Scheduler;
        public override Metadata? Metadata => new(1, "NightmareXIV");

        public override void OnSetup()
        {
            if(!this.Controller.TryRegisterElement("TowerDisplay", new(0) //Let's register our element
            {
                Enabled = false,
                thicc = 5,
                radius = 4,
                tether = true
            }))
            {
                DuoLog.Error("Could not register layout"); //And display error if we couldn't for some reason.
            }
        }

        public override void OnUpdate()
        {
            //just a fancy feature to make element gradually change color. Not necessary at all, just added for demonstration purposes.
            if (this.Controller.TryGetElementByName("TowerDisplay", out var e) && e.Enabled)
            {
                e.color = GradientColor.Get(Colors.Green.ToVector4(), ImGuiColors.DalamudWhite).ToUint(); 
            }
        }

        public override void OnCombatEnd()
        {
            // Once combat ended, disable element and dispose scheduler so it won't accidentally disable tower in next pull.
            if(this.Controller.TryGetElementByName("TowerDisplay", out var e))
            {
                e.Enabled = false;
                Scheduler?.Dispose();
            }
        }

        public override void OnCombatStart()
        {
            // We're doubling cleanup on combat start because sometimes game sends you combat data already after removing you out of combat. So just in case our element stuck even after combat has ended, once it starts again it will be cleaned up.
            this.OnCombatEnd();
        }

        public override void OnMapEffect(uint position, ushort data1, ushort data2) //here is where magic happens
        {
            if (data1 == 1 && data2 == 2 //if our map effect has the data we're looking for, in this case it's 1 and 2...
                && Svc.ClientState.LocalPlayer?.StatusList.Any(x => x.StatusId == 2098 && x.RemainingTime > 7.5f) == true //...and the player has status with ID 2098 (which is Fire Resistance Down II) and remaining time of that debuff is 7.5 seconds or more, which means that player has just been hit...
                && EffectData.TryGetValue(position, out var x) //...and we have MapEffect's position mapped...
                && Positions.TryGetValue(x, out var loc) //...and we can get coordinates of mapped position which should always be true...
                && (Svc.ClientState.LocalPlayer?.GetRole() == CombatRole.DPS) == IsDpsPosition(loc) //
                && this.Controller.TryGetElementByName("TowerDisplay", out var e))
            {
                e.Enabled = true;
                e.refX = loc.X;
                e.refY = loc.Y;
                Scheduler = new(() => e.Enabled = false, 10000);
                PluginLog.Information($"Displaying tower...");
            }
        }

        bool IsDpsPosition(Vector2 v)
        {
            return v.X > 100;
        }

        Dictionary<TowerPosition, Vector2> Positions = new()
        {
            {TowerPosition.Top1, new(85, 85) },
            {TowerPosition.Top2, new(95, 85) },
            {TowerPosition.Top3, new(105, 85) },
            {TowerPosition.Top4, new(115, 85) },
            {TowerPosition.Mid1, new(85, 95) },
            {TowerPosition.Mid2, new(95, 95) },
            {TowerPosition.Mid3, new(105, 95) },
            {TowerPosition.Mid4, new(115, 95) },
            {TowerPosition.Bot1, new(85, 105) },
            {TowerPosition.Bot2, new(95, 105) },
            {TowerPosition.Bot3, new(105, 105) },
            {TowerPosition.Bot4, new(115, 105) },
        };

        Dictionary<uint, TowerPosition> EffectData = new()
        {
            {70, TowerPosition.Top1 },
            {71, TowerPosition.Top2 },
            {72, TowerPosition.Top3 },
            {73, TowerPosition.Top4 },
            {74, TowerPosition.Mid1 },
            {5,  TowerPosition.Mid2 },
            {6,  TowerPosition.Mid3 },
            {75, TowerPosition.Mid4 },
            {82, TowerPosition.Bot1 },
            {7,  TowerPosition.Bot2 },
            {8,  TowerPosition.Bot3 },
            {83, TowerPosition.Bot4 },
        };

        enum TowerPosition
        {
            Top1, Top2, Top3, Top4,
            Mid1, Mid2, Mid3, Mid4,
            Bot1, Bot2, Bot3, Bot4,
        }
    }
}
