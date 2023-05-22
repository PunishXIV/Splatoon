using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Logging;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol
{
    public class Limitless_Synergy : SplatoonScript
    {
        public override Metadata? Metadata => new(1, "NightmareXIV");
        public override HashSet<uint> ValidTerritories => new() { 1122 };

        Dictionary<uint, uint> Tethers = new();
        bool allowed = false;

        public override void OnSetup()
        {
            Controller.RegisterElementFromCode("T1", "{\"Name\":\"\",\"type\":4,\"Enabled\":false,\"radius\":40.0,\"coneAngleMin\":-45,\"coneAngleMax\":45,\"color\":2516582655,\"thicc\":5.0,\"refActorObjectID\":0,\"FillStep\":10.0,\"refActorComparisonType\":2,\"includeRotation\":true,\"Filled\":true}");
            Controller.RegisterElementFromCode("T2", "{\"Name\":\"\",\"type\":4,\"Enabled\":false,\"radius\":40.0,\"coneAngleMin\":-45,\"coneAngleMax\":45,\"color\":2516582655,\"thicc\":5.0,\"refActorObjectID\":0,\"FillStep\":10.0,\"refActorComparisonType\":2,\"includeRotation\":true,\"Filled\":true}");
        }
        //Dequeued message: Omega starts casting 31544 (7635>31544)
        //[3:55][Splatoon] 40018753(Omega-F - BattleNpc) at 1EF8841D7E0 -> 107CB247(Dark Knight - Player) at 1EF8839DB30
        //[3:55][Splatoon] 40018754(Omega-M - BattleNpc) at 1EF883C84C0 -> 10777E50(Samurai - Player) at 1EF883A3610
        public override void OnTetherCreate(uint source, uint target, byte data2, byte data3, byte data5)
        {
            if (!allowed) return;
            if(Svc.Objects.Any(x => x.DataId == 15713 && x.IsTargetable()))
            {
                Tethers[source] = target;
            }
        }

        public override void OnTetherRemoval(uint source, byte data2, byte data3, byte data5)
        {
            Tethers.Remove(source);
        }

        public override void OnMessage(string Message)
        {
            if (Message.Contains("(7635>31544)")) allowed = true;
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if(category.EqualsAny(DirectorUpdateCategory.Wipe, DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence))
            {
                allowed = false;
            }
        }

        public override void OnUpdate()
        {
            Controller.GetElementByName("T1").Enabled = false;
            Controller.GetElementByName("T2").Enabled = false;
            var i = 1;
            foreach(var x in Tethers)
            {
                if(x.Key.TryGetObject(out var source) && x.Value.TryGetObject(out var target) && Controller.TryGetElementByName($"T{i++}", out var e))
                {
                    e.color = Conf.Color.ToUint();
                    e.FillStep = Conf.FillStep;
                    e.Enabled = true;
                    e.refActorObjectID = x.Key;
                    e.AdditionalRotation = GetRelativeAngle(source.Position.ToVector2(), target.Position.ToVector2()) + source.Rotation;
                }
            }
        }


        static float GetRelativeAngle(Vector2 origin, Vector2 target)
        {
            var vector2 = target - origin;
            var vector1 = new Vector2(0, 1);
            return MathF.Atan2(vector2.Y, vector2.X) - MathF.Atan2(vector1.Y, vector1.X);
        }

        public override void OnSettingsDraw()
        {
            ImGui.ColorEdit4("Cone color", ref Conf.Color);
            ImGui.SetNextItemWidth(200f);
            ImGui.SliderInt("Fill step", ref Conf.FillStep, 1, 25);
        }

        Config Conf => Controller.GetConfig<Config>();
        public class Config : IEzConfig
        {
            public Vector4 Color = new(1f, 0.5f, 0f, 0.8f);
            public int FillStep = 2;
        }
    }
}
