using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using Splatoon;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Shadowbringers.The_Epic_Of_Alexander;

public class TEA_P2_Temporal_Stasis : SplatoonScript
{
    public enum BaitType
    {
        West,
        East,
        NorthLeftBossSide,
        SouthLeftBossSide,
        NorthRightBossSide,
        SouthRightBossSide,
        JusticeSide
    }

    private enum CruiseChaserSide
    {
        East,
        West
    }

    private const uint LightningDebuffId = 1121;
    private const uint RedTetherDebuffId = 1123;
    private const uint BlueTetherDebuffId = 1124;
    private bool _isStartTemporalStasis;
    private bool _shouldDisplayElement;
    public override HashSet<uint>? ValidTerritories => [887];
    public override Metadata? Metadata => new(3, "Garume");
    private IBattleNpc? CruiseChaser => Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.DataId == 0x2C4E);

    private Config C => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            radius = 0.35f,
            color = 0xffffffff,
            overlayText = "Go Here",
            overlayVOffset = 2f,
            overlayFScale = 2f,
            tether = true
        };

        Controller.RegisterElement("TEA_P2_Temporal_Stasis_Bait", element, true);
    }

    public override void OnMessage(string message)
    {
        if(message.Contains(Loc(en: "I am Alexander...the Creator. You...who would prove yourself worthy of your utopia...will be judged.", jp: "我はアレキサンダー……機械仕掛けの神なり……。", de: "Ich bin Alexander ... der Schöpfer. Nehmt mein letztes Urteil an, auf dass ihr ins Paradies geführt werdet ..."))) _isStartTemporalStasis = true;
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(_isStartTemporalStasis) _shouldDisplayElement = true;
    }

    public override void OnTetherRemoval(uint source, uint data2, uint data3, uint data5)
    {
        if(_isStartTemporalStasis) _isStartTemporalStasis = false;
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

        var cruiseChaser = CruiseChaser;
        if(cruiseChaser == null || !_isStartTemporalStasis) return;

        var statuses = Player.Status;
        var baitType = GetBaitType(statuses);
        var cruiseChaserSide = GetCruiseChaserSide(cruiseChaser);
        var baitPosition = GetBaitPosition(baitType, cruiseChaserSide);
        if(Controller.TryGetElementByName("TEA_P2_Temporal_Stasis_Bait", out var element))
        {
            element.Enabled = _shouldDisplayElement;
            element.refX = baitPosition.X;
            element.refY = baitPosition.Y;
        }
    }

    public override void OnReset()
    {
        _isStartTemporalStasis = false;
        _shouldDisplayElement = false;
    }

    private BaitType GetBaitType(IEnumerable<IStatus> statuses)
    {
        foreach(var status in statuses)
            switch(status.StatusId)
            {
                case LightningDebuffId:
                    return C.LightningBaitPosition;
                case RedTetherDebuffId:
                    return C.RedTetherBaitPosition;
                case BlueTetherDebuffId:
                    return C.BlueTetherBaitPosition;
            }

        return C.NothingBaitPosition;
    }

    private CruiseChaserSide GetCruiseChaserSide(IGameObject cruiseChaser)
    {
        return cruiseChaser.Position.X > 100f ? CruiseChaserSide.East : CruiseChaserSide.West;
    }

    private Vector2 GetBaitPosition(BaitType baitType, CruiseChaserSide cruiseChaserSide)
    {
        return (baitType, cruiseChaserSide) switch
        {
            (BaitType.NorthRightBossSide, _) => new Vector2(106f, 98f),
            (BaitType.SouthRightBossSide, _) => new Vector2(106f, 102f),
            (BaitType.NorthLeftBossSide, _) => new Vector2(94f, 98f),
            (BaitType.SouthLeftBossSide, _) => new Vector2(94f, 102f),
            (BaitType.East, CruiseChaserSide.East) => new Vector2(113f, 100f),
            (BaitType.East, CruiseChaserSide.West) => new Vector2(118f, 100f),
            (BaitType.West, CruiseChaserSide.East) => new Vector2(82f, 100f),
            (BaitType.West, CruiseChaserSide.West) => new Vector2(87f, 100f),
            (BaitType.JusticeSide, CruiseChaserSide.East) => new Vector2(82f, 100f),
            (BaitType.JusticeSide, CruiseChaserSide.West) => new Vector2(118f, 100f),
            _ => Vector2.Zero
        };
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text("Blue Tether Bait Position");
        ImGuiEx.EnumCombo("##BlueTetherBaitPosition", ref C.BlueTetherBaitPosition);
        ImGuiEx.Text("Red Tether Bait Position");
        ImGuiEx.EnumCombo("##RedTetherBaitPosition", ref C.RedTetherBaitPosition);
        ImGuiEx.Text("Lightning Bait Position");
        ImGuiEx.EnumCombo("##LightningBaitPosition", ref C.LightningBaitPosition);
        ImGuiEx.Text("Nothing Bait Position");
        ImGuiEx.EnumCombo("##NothingBaitPosition", ref C.NothingBaitPosition);
    }

    private class Config : IEzConfig
    {
        public BaitType BlueTetherBaitPosition;
        public BaitType LightningBaitPosition;
        public BaitType NothingBaitPosition;
        public BaitType RedTetherBaitPosition;
    }
}
