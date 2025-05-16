using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Schedulers;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Stormblood
{
    public unsafe class UCOB_Earthshakers : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => [733];

        public override Metadata? Metadata => new(1, "damolitionn");
        private const string HeadVFX = "vfx/lockon/eff/m0117_earth_shake_01s.avfx";
        private TickScheduler? sched = null;
        private readonly List<EarthshakerSet> ActiveSets = new();

        private class EarthshakerSet
        {
            public List<IPlayerCharacter> Players = new(4);
            public TickScheduler? Scheduler;
        }

        public override void OnSetup()
        {
            for (int i = 0; i < 4; i++)
            {
                if (!Controller.TryRegisterElement($"EarthshakerCone{i}", new(0)
                {
                    Name = "",
                    Enabled = false,
                    type = 5,
                    radius = 22.0f,
                    coneAngleMin = -50,
                    coneAngleMax = 50,
                    color = 4278190335,
                    fillIntensity = 0.3f,
                    originFillColor = 1677721855,
                    endFillColor = 1677721855,
                    thicc = 3.0f,
                    refActorNPCNameID = 3210,
                    includeRotation = true,
                    FaceMe = true,
                    onlyVisible = true,
                }))
                {
                    DuoLog.Error("Could not register layout");
                }
            }
        }

        public override void OnVFXSpawn(uint target, string vfxPath)
        {
            if (vfxPath == HeadVFX)
            {
                var player = Controller.GetPartyMembers().FirstOrDefault(x => x.GameObjectId == target);
                if (player == null) return;

                var set = ActiveSets.FirstOrDefault(s => s.Players.Count < 4 && !s.Players.Contains(player));
                if (set == null)
                {
                    set = new EarthshakerSet();
                    ActiveSets.Add(set);
                    set.Scheduler = new TickScheduler(() =>
                    {
                        ActiveSets.Remove(set);
                        for (int i = 0; i < 4; i++)
                        {
                            var cone = Controller.GetElementByName($"EarthshakerCone{set.GetHashCode() % 10000}_{i}");
                            if (cone != null)
                                cone.Enabled = false;
                        }
                    }, 6000);
                }

                if (!set.Players.Contains(player) && set.Players.Count < 4)
                    set.Players.Add(player);
            }
        }


        private int GetPlayerOrder(IPlayerCharacter c)
        {
            for (var i = 1; i <= 8; i++)
            {
                if ((nint)FakePronoun.Resolve($"<{i}>") == c.Address) return i;
            }
            return 0;
        }

        public override void OnUpdate()
        {
            for (int i = 0; i < 8; i++)
            {
                var cone = Controller.GetElementByName($"EarthshakerCone{i}");
                if (cone != null)
                    cone.Enabled = false;
            }

            int coneIdx = 0;
            foreach (var set in ActiveSets)
            {
                for (int i = 0; i < set.Players.Count && coneIdx < 8; i++, coneIdx++)
                {
                    var player = set.Players[i];
                    var order = GetPlayerOrder(player);
                    if (Controller.TryGetElementByName($"EarthshakerCone{coneIdx}", out var e))
                    {
                        e.Enabled = true;
                        e.faceplayer = $"<{order}>";
                    }
                }
            }
        }

        public override void OnReset()
        {
            foreach (var set in ActiveSets)
                set.Scheduler?.Dispose();
            ActiveSets.Clear();

            for (int i = 0; i < 8; i++)
            {
                var cone = Controller.GetElementByName($"EarthshakerCone{i}");
                if (cone != null)
                    cone.Enabled = false;
            }
        }

        public override void OnSettingsDraw()
        {
            if (ImGui.CollapsingHeader("Debug:"))
            {
                ImGuiEx.Text($"Earthshaker Sets: {ActiveSets.Count}");
                int setIdx = 1;
                foreach (var set in ActiveSets)
                {
                    ImGuiEx.Text($"Set {setIdx}:");
                    foreach (var player in set.Players)
                    {
                        ImGuiEx.Text($"  {player.Name}");
                    }
                    setIdx++;
                }
            }
        }
    }
}
