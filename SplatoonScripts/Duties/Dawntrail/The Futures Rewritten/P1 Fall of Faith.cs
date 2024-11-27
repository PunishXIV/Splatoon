using ECommons;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;
internal class P1_Fall_of_Faith :SplatoonScript
{
    #region Enums
    public enum Direction
    {
        North,
        East,
        South,
        West
    }

    private enum Debuff
    {
        None,
        Red,
        Blue
    }

    private enum State
    {
        None,
        Start,
        Split,
        End
    }

    private enum LR
    {
        None,
        Left,
        Right
    }
    #endregion

    #region Structs
    record class PlayerData
    {
        int tetherNum = 0;
        LR lR = LR.None;
    }
    #endregion

    public class Config :IEzConfig
    {
        public readonly Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public readonly Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();

        public readonly List<Job> Jobs =
        [
            Job.PLD,
            Job.WAR,
            Job.DRK,
            Job.GNB,
            Job.WHM,
            Job.SCH,
            Job.AST,
            Job.SGE,
            Job.VPR,
            Job.DRG,
            Job.MNK,
            Job.SAM,
            Job.RPR,
            Job.NIN,
            Job.BRD,
            Job.MCH,
            Job.DNC,
            Job.BLM,
            Job.SMN,
            Job.RDM,
            Job.PCT
        ];

        public InternationalString BlueTetherText = new() { Jp = "雷 散開" };

        public Direction NoTether12Direction = Direction.North;
        public Direction NoTether34Direction = Direction.South;

        public string[] Priority = ["", "", "", "", "", "", "", ""];

        public InternationalString RedTetherText = new() { Jp = "炎 ペア割" };

        public Direction Tether1Direction = Direction.North;
        public Direction Tether2Direction = Direction.South;
        public Direction Tether3Direction = Direction.North;
        public Direction Tether4Direction = Direction.South;
    }


    private readonly ImGuiEx.RealtimeDragDrop<Job> DragDrop = new("DragDropJob", x => x.ToString());

    private Dictionary<string, PlayerData> _partyDatas = new();

    private State _state = State.None;

    private int _tetherCount = 1;
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(1, "Redmoon");
    private Config C => Controller.GetConfig<Config>();

    public override void OnSettingsDraw()
    {
        if (ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"""
            List:
            {C.Priority.GetPlayers(x => x.Name.Length <= n).Select(x => x.Name).Print("\n")}
            
            Your index: {C.Priority.GetOwnIndex(x => x.Name.Length <= n)}
            Your index backwards: {C.Priority.GetOwnIndex(x => x.Name.Length <= n, true)}
            """);
        }
    }




}
