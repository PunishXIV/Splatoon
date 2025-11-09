using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Schedulers;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public class EX5_Relentless_Reaping : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1296];

    public override Metadata Metadata => new(1, "damolitionn");
    // Cast VFXs
    private readonly Dictionary<string, string> vfxToMechanic = new()
    {
        { "vfx/lockon/eff/m0946_etherstock_c4t1.avfx", "Side Safe" },
        { "vfx/lockon/eff/m0946_etherstock_c2t1.avfx", "In" },
        { "vfx/lockon/eff/m0946_etherstock_c1t1.avfx", "Out" },
        { "vfx/lockon/eff/m0946_etherstock_c3t1.avfx", "Mid Safe" },
    };

    private readonly Dictionary<string, int> castVfxToIndex = new()
    {
        { "vfx/common/eff/m0946_cast05_c0t1.avfx", 0 },
        { "vfx/common/eff/m0946_cast05_c1t1.avfx", 1 },
        { "vfx/common/eff/m0946_cast05_c2t1.avfx", 2 },
        { "vfx/common/eff/m0946_cast05_c3t1.avfx", 3 },
    };

    private List<(IGameObject obj, string vfxPath)> spawnedVfx = new();


    private List<string> vfxOrder = new();
    private TickScheduler? sched = null;
    private bool trackVfxOrder = false;

    private uint RelentlessReaping = 44564;

    private IBattleNpc? Necron => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == 18699 && b.IsTargetable) as IBattleNpc;

    public override void OnSetup()
    {
        //In safe
        Controller.RegisterElementFromCode("InSafe", "{\"Name\":\"InSafe\",\"enabled\": false,\"type\":5,\"refX\":100.0,\"refY\":85,\"refZ\":1.9073486E-06,\"offY\":-3.5,\"radius\":16.0,\"Donut\":35.0,\"coneAngleMin\":-65,\"coneAngleMax\":65,\"fillIntensity\":0.5,\"refActorDataID\":9020,\"refActorComparisonType\":3,\"includeRotation\":true}");

        //Out safe
        Controller.RegisterElementFromCode("OutSafe", "{\"Name\":\"OutSafe\",\"enabled\": false,\"type\":5,\"refX\":100.0,\"refY\":85,\"refZ\":1.9073486E-06,\"offY\":-3.5,\"radius\":20.0,\"coneAngleMin\":-65,\"coneAngleMax\":65,\"fillIntensity\":0.5,\"refActorDataID\":9020,\"refActorComparisonType\":3,\"includeRotation\":true}");

        //Mid safe
        Controller.RegisterElementFromCode("Left", "{\"Name\":\"Left\",\"enabled\": false,\"type\":2,\"refX\":88.0,\"refY\":85.0,\"refZ\":1.9073486E-06,\"offX\":88.0,\"offY\":115.0,\"offZ\":1.9073486E-06,\"radius\":6.0,\"fillIntensity\":0.345,\"refActorDataID\":9020,\"refActorComparisonType\":3,\"includeRotation\":true}");
        Controller.RegisterElementFromCode("Right", "{\"Name\":\"Right\",\"enabled\": false,\"type\":2,\"refX\":112.0,\"refY\":85.0,\"refZ\":1.9073486E-06,\"offX\":112.0,\"offY\":115.0,\"offZ\":1.9073486E-06,\"radius\":6.0,\"fillIntensity\":0.345,\"refActorDataID\":9020,\"refActorComparisonType\":3,\"includeRotation\":true}");
       
        //Side safe
        Controller.RegisterElementFromCode("Mid", "{\"Name\":\"Middle\",\"enabled\": false,\"type\":2,\"refX\":100.0,\"refY\":85.0,\"refZ\":1.9073486E-06,\"offX\":100.0,\"offY\":115.0,\"offZ\":1.9073486E-06,\"radius\":6.0,\"fillIntensity\":0.345,\"refActorDataID\":9020,\"refActorComparisonType\":3,\"includeRotation\":true}");
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == RelentlessReaping)
        {
            vfxOrder.Clear();
            spawnedVfx.Clear();
            trackVfxOrder = true;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (trackVfxOrder && vfxToMechanic.TryGetValue(vfxPath, out var mechanic))
        {
            if (vfxOrder.Count < 4 && !vfxOrder.Contains(mechanic))
            {
                vfxOrder.Add(mechanic);
                if (vfxOrder.Count == 4)
                {
                    trackVfxOrder = false;
                }
            }
        }

        if (castVfxToIndex.ContainsKey(vfxPath))
        {
            var obj = Svc.Objects.OfType<IGameObject>().FirstOrDefault(o => o.DataId == 18757 && o.GameObjectId == target);

            if (obj != null)
            {
                spawnedVfx.Add((obj, vfxPath));
            }
        }
    }

    public override void OnUpdate()
    {
        if (spawnedVfx.Count == 4 && vfxOrder.Count == 4)
        {
            var southernMost = spawnedVfx.OrderBy(x => x.obj.Position.Y).First();
            var vfxPath = southernMost.vfxPath;

            if (castVfxToIndex.TryGetValue(vfxPath, out int startIndex))
            {
                var newOrder = new List<string>(4);
                for (int i = 0; i < vfxOrder.Count; i++)
                {
                    newOrder.Add(vfxOrder[(startIndex + i) % vfxOrder.Count]);
                }
                vfxOrder = newOrder;
                if (C.PrintVfxOrder)
                {
                    Svc.Chat.Print($"Mechanic order: {string.Join(", ", vfxOrder)}");
                }

                void SetMechanicEnabled(string mechanic, bool enabled)
                {
                    switch (mechanic)
                    {
                        case "In":
                            if (Controller.TryGetElementByName("InSafe", out var inSafe)) inSafe.Enabled = enabled;
                            break;
                        case "Out":
                            if (Controller.TryGetElementByName("OutSafe", out var outSafe)) outSafe.Enabled = enabled;
                            break;
                        case "Mid Safe":
                            if (Controller.TryGetElementByName("Left", out var leftCleave) && Controller.TryGetElementByName("Right", out var rightCleave))
                            {
                                leftCleave.Enabled = enabled;
                                rightCleave.Enabled = enabled;
                            }
                            break;
                        case "Side Safe":
                            if (Controller.TryGetElementByName("Mid", out var middleCleave)) middleCleave.Enabled = enabled;
                            break;
                    }
                }

                int[] durations = { 10000, 3000, 3000, 3000 };
                int totalDelay = 0;
                for (int i = 0; i < vfxOrder.Count; i++)
                {
                    int idx = i;
                    Controller.Schedule(() =>
                    {
                        for (int j = 0; j < vfxOrder.Count; j++)
                        {
                            SetMechanicEnabled(vfxOrder[j], false);
                        }
                        SetMechanicEnabled(vfxOrder[idx], true);
                    }, totalDelay);

                    totalDelay += durations[idx];
                }

                Controller.Schedule(() =>
                {
                    for (int i = 0; i < vfxOrder.Count; i++)
                    {
                        SetMechanicEnabled(vfxOrder[i], false);
                    }
                }, totalDelay);

                spawnedVfx.Clear();
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Print mechanic order in echo chat", ref C.PrintVfxOrder);
        Controller.SaveConfig();
    }

    public class Config : IEzConfig
    {
        public bool PrintVfxOrder = false;
    }
    
    private Config C => Controller.GetConfig<Config>();


    public override void OnReset()
    {
        sched?.Dispose();
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        vfxOrder.Clear();
        spawnedVfx.Clear();
        trackVfxOrder = false;
    }
}