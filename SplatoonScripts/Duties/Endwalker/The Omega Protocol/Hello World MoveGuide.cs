// Ignore Spelling: Metadata

using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.SplatoonAPI;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using Lumina.Data;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;
internal unsafe class Hello_World_MoveGuide :SplatoonScript
{
    public override Metadata? Metadata => new(1, "Redmoon");
    public override HashSet<uint> ValidTerritories => new() { 1122 };

    private enum State
    {
        None = 0,
        HelloWorldCasted,
        PartyInitDone,
        PartySetupDone,
        LatentDefectCasting,
        BreakPatchNear,
        BreakPatchFar,
        WaitingRot,
        Waiting
    }

    private enum GimmickRole
    {
        None = 0,
        ReceiveCircle,
        Circle,
        ReceiveShare,
        Share
    }

    private enum HasColorRot
    {
        None = 0,
        RedRot,
        BlueRot
    }

    private class PartyListData
    {
        public string Name = "";
        public IPlayerCharacter? pcPtr = null;
        public GimmickRole gimmickRole = GimmickRole.None;
        public HasColorRot hasColorRot = HasColorRot.None;
    }

    private class CastIDs
    {
        public const uint HelloWorld = 31573u;
        public const uint RedRot = 31583u;
        public const uint BlueRot = 31584u;
        public const uint CriticalRedRot = 31578u;
        public const uint CriticalBlueRot = 31579u;
        public const uint Patch = 31587u;
        public const uint CriticalError = 31588u;
        public const uint LatentDefect = 31599u;
    }

    private class Effects
    {
        public const uint CodeSmellShare = 3436u;
        public const uint CodeSmellCircle = 3437u;
        public const uint BlueRot = 3429u;
        public const uint CodeSmellRedRot = 3438u;
        public const uint CodeSmellBlueRot = 3439u;
        public const uint UpcomingFarTether = 3441u;
        public const uint LatentBug = 3527u;
        public const uint Share = 3524u;
        public const uint Circle = 3525u;
        public const uint RedRot = 3526u;
    }

    private readonly bool isDebug = true;
    private readonly Vector3 CenterPos = new(100f, 0f, 100f);

    private bool isLock = false;
    private MarkingController* mkc;
    private State prevState = State.None;
    private State state = State.None;
    private List<PartyListData> partyList = new List<PartyListData>();
    private PartyListData? myData = null;
    private int latentCount = 0;
    private Vector3 firstTowerPos = Vector3.Zero;
    private Vector3 secondTowerPos = Vector3.Zero;
    private Config Conf => Controller.GetConfig<Config>();

    public override void OnSetup()
    {
        mkc = MarkingController.Instance();
        Controller.RegisterElementFromCode("GuidePoint", "{\"Name\":\"\",\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":5.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == CastIDs.LatentDefect)
        {
            HideAll();
            if (state == State.PartySetupDone || state == State.Waiting)
            {
                ++latentCount;
                state = State.LatentDefectCasting;
                LocalUpdate();
            }
            else
            {
                DuoLog.Information("Latent Defect casted at wrong time");
                this.OnReset();
            }
        }
        if (castId == CastIDs.CriticalError)
        {
            this.OnReset();
            state = State.None;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action == null)
            return;

        if (set.Action.RowId == CastIDs.HelloWorld)
        {
            HideAll();
            state = State.HelloWorldCasted;
            LocalUpdate();
        }
        if (state == State.LatentDefectCasting && set.Action.RowId == CastIDs.LatentDefect)
        {
            HideAll();
            state = State.BreakPatchNear;
            LocalUpdate();
        }
        if (state == State.WaitingRot && set.Action.RowId == CastIDs.CriticalRedRot)
        {
            HideAll();
            state = State.Waiting;
            LocalUpdate();
        }
    }

    public override void OnTetherRemoval(uint source, uint data2, uint data3, uint data5)
    {
        if (state == State.BreakPatchNear)
        {
            HideAll();
            foreach (var x in partyList.Where(x => x.gimmickRole == GimmickRole.ReceiveShare))
            {
                if (x.pcPtr.EntityId == source)
                {
                    state = State.BreakPatchFar;
                }
            }
            LocalUpdate();
        }
        else if (state == State.BreakPatchFar)
        {
            HideAll();
            foreach (var x in partyList.Where(x => x.gimmickRole == GimmickRole.ReceiveCircle))
            {
                if (x.pcPtr.EntityId == source)
                {
                    state = State.WaitingRot;
                }
            }
            LocalUpdate();
        }
    }

    public override void OnUpdate()
    {
        if (state == State.HelloWorldCasted || state == State.PartyInitDone)
        {
            LocalUpdate();
        }
    }

    public override void OnReset()
    {
        isLock = false;
        HideAll();
        state = State.None;
        partyList.Clear();
        myData = null;
        latentCount = 0;
    }

    public class Config :IEzConfig
    {
        public bool Debug = false;
        public string ObjectID = "";
        public float deg = 0;
        public Vector3 position = new Vector3(0, 0, 0);
        public Vector3 refOffset = new Vector3(0, 0, 0);
    }

    public override void OnSettingsDraw()
    {
        if (ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text("State: " + state.ToString());
            ImGui.Text("Latent Count: " + latentCount.ToString());
            ImGui.Text("IsLock: " + isLock.ToString());

            ImGui.Text("Method Check");
            ImGui.Text("ShowGuidePointFixPos()");
            ImGui.InputFloat3("Position", ref Conf.position);
            if (ImGui.Button("ShowGuidePointFixPos Show"))
            {
                ShowGuidePointFixPos(Conf.position);
            }

            ImGui.Text("ShowGuidePointRef()");
            ImGui.InputText($"ObjectID", ref Conf.ObjectID, 11U);
            ImGui.InputFloat3("refOffset", ref Conf.refOffset);
            ImGui.InputFloat("Degree", ref Conf.deg);
            if (ImGui.Button("ShowGuidePointRef Show"))
            {
                uint ObjectID = 0u;
                try
                {
                    if (Convert.ToUInt32(Conf.ObjectID, 16) == 0)
                        return;
                }
                catch (Exception)
                {
                    return;
                }
                ShowGuidePointRef(Convert.ToUInt32(Conf.ObjectID, 16), Conf.deg, Conf.refOffset);
                isLock = false;
            }

            List<ImGuiEx.EzTableEntry> Entries = [];
            foreach (var x in partyList)
            {
                var ptr = x.pcPtr;
                if (ptr == null)
                    continue;
                Entries.Add(new("Name", () => ImGui.Text(x.Name.ToString())));
                Entries.Add(new("ObjectId", () => ImGui.Text(ptr.EntityId.ToString())));
                Entries.Add(new("gimmickRole", () => ImGui.Text(x.gimmickRole.ToString())));
                Entries.Add(new("hasColorRot", () => ImGui.Text(x.hasColorRot.ToString())));
            }
            ImGuiEx.EzTable(Entries);
        }
    }

    private void LocalUpdate()
    {
        try
        {
            Proc();
        }
        catch (Exception e)
        {
            DuoLog.Error(e.ToString());
        }
    }

    private void Proc()
    {
        if (prevState != state)
        {
            if (prevState == State.Waiting && state == State.LatentDefectCasting)
            {
                NextGimmickRole();
            }
            isLock = false;
            prevState = state;
        }

        if (isLock)
            return;

        if (state == State.HelloWorldCasted)
        {
            foreach (var x in FakeParty.Get().ToList())
            {
                partyList.Add(new PartyListData() { Name = x.Name.ToString(), pcPtr = x });

                if (x.Address == Svc.ClientState.LocalPlayer.Address)
                {
                    myData = partyList.Last();
                }
            }
            state = State.PartyInitDone;
        }

        if (state == State.PartyInitDone)
        {
            if (partyList.Count(x => x.pcPtr.StatusList.Any(x => x.StatusId == Effects.UpcomingFarTether)) != 8)
                return;

            HasColorRot CircleRot = HasColorRot.None;
            foreach (var x in partyList)
            {
                if (HasEffect(Effects.CodeSmellRedRot, null, x.pcPtr) || HasEffect(Effects.RedRot, 27f, x.pcPtr))
                {
                    x.hasColorRot = HasColorRot.RedRot;
                }
                else if (HasEffect(Effects.CodeSmellBlueRot, null, x.pcPtr) || HasEffect(Effects.BlueRot, 27f, x.pcPtr))
                {
                    x.hasColorRot = HasColorRot.BlueRot;
                }
                if (HasEffect(Effects.CodeSmellCircle, null, x.pcPtr) || HasEffect(Effects.Circle, 21f, x.pcPtr))
                {
                    x.gimmickRole = GimmickRole.Circle;
                    CircleRot = x.hasColorRot;
                }
                else if (HasEffect(Effects.CodeSmellShare, null, x.pcPtr) || HasEffect(Effects.Share, 21f, x.pcPtr))
                {
                    x.gimmickRole = GimmickRole.Share;
                }
                else if (HasEffect(Effects.LatentBug, null, x.pcPtr))
                {
                    x.gimmickRole = GimmickRole.ReceiveCircle;
                }
                else
                {
                    x.gimmickRole = GimmickRole.ReceiveShare;
                }
            }

            foreach (var x in partyList.Where(x => (x.gimmickRole == GimmickRole.ReceiveShare) || (x.gimmickRole == GimmickRole.ReceiveCircle)))
            {
                if (x.gimmickRole == GimmickRole.ReceiveCircle)
                {
                    x.hasColorRot = (CircleRot == HasColorRot.RedRot) ? HasColorRot.RedRot : HasColorRot.BlueRot;
                }
                else if (x.gimmickRole == GimmickRole.ReceiveShare)
                {
                    x.hasColorRot = (CircleRot == HasColorRot.RedRot) ? HasColorRot.BlueRot : HasColorRot.RedRot;
                }
            }
            state = State.PartySetupDone;

            return;
        }

        switch (myData.gimmickRole)
        {
            case GimmickRole.Circle:
            GuideCircle();
            break;
            case GimmickRole.Share:
            GuideShare();
            break;
            case GimmickRole.ReceiveCircle:
            GuideReceiveCircle();
            break;
            case GimmickRole.ReceiveShare:
            GuideReceiveShare();
            break;
        }
    }

    private void NextGimmickRole()
    {
        foreach (var x in partyList)
        {
            switch (x.gimmickRole)
            {
                case GimmickRole.Circle:
                x.gimmickRole = GimmickRole.ReceiveShare;
                break;
                case GimmickRole.Share:
                x.gimmickRole = GimmickRole.ReceiveCircle;
                break;
                case GimmickRole.ReceiveCircle:
                x.gimmickRole = GimmickRole.Circle;
                break;
                case GimmickRole.ReceiveShare:
                x.gimmickRole = GimmickRole.Share;
                break;
            }

            if (x.gimmickRole == GimmickRole.ReceiveShare || x.gimmickRole == GimmickRole.ReceiveCircle)
            {
                x.hasColorRot = (x.hasColorRot == HasColorRot.RedRot) ? HasColorRot.BlueRot : HasColorRot.RedRot;
            }
        }
    }

    private void GuideCircle()
    {
        switch (state)
        {
            case State.LatentDefectCasting:
            uint searchID = (myData.hasColorRot == HasColorRot.RedRot) ? CastIDs.RedRot : CastIDs.BlueRot;
            List<IBattleChara> towerList = Svc.Objects.OfType<IBattleChara>().Where(x => x.CastActionId == searchID).ToList();

            if (Vector3.Distance(myData.pcPtr.Position, towerList.First().Position) < Vector3.Distance(myData.pcPtr.Position, towerList.Last().Position))
            {
                ShowGuidePointFixPos(OffsetTowardsNorth(towerList.First().Position, CenterPos, new(0, 0, -2f)));
            }
            else
            {
                ShowGuidePointFixPos(OffsetTowardsNorth(towerList.Last().Position, CenterPos, new(0, 0, -2f)));
            }
            break;

            case State.BreakPatchNear:
            case State.BreakPatchFar:
            case State.WaitingRot:
            Vector3 nearestWaymark = GetNearestWaymark();
            ShowGuidePointFixPos(nearestWaymark);
            break;

            case State.Waiting:
            ShowGuidePointFixPos(CenterPos);
            break;
        }

    }

    private void GuideShare()
    {
        DebugLog("GuideShare");
        uint searchID = (myData.hasColorRot == HasColorRot.RedRot) ? CastIDs.RedRot : CastIDs.BlueRot;
        List<IBattleChara> towerList = Svc.Objects.OfType<IBattleChara>().Where(x => x.CastActionId == searchID).ToList();

        switch (state)
        {
            case State.LatentDefectCasting:
            IBattleChara myTower = DecideTower(towerList, partyList.Where(x => x.gimmickRole == GimmickRole.Share).Select(x => x.pcPtr).ToList());
            IBattleChara otherTower = towerList.Where(x => x.EntityId != myTower.EntityId).First();
            string firstLeftRight = GetRelativePosition(myTower.Position, CenterPos, otherTower.Position);
            if (firstLeftRight == "right")
            {
                ShowGuidePointRef(myTower.EntityId, 295, new(0, 0, 5));
            }
            else
            {
                ShowGuidePointRef(myTower.EntityId, 65, new(0, 0, 5));
            }
            break;

            case State.BreakPatchNear:
            DebugLog(myData.pcPtr.Position.ToString());
            ShowGuidePointFixPos(myData.pcPtr.Position);
            break;

            case State.BreakPatchFar:
            case State.WaitingRot:
            Vector3 nearestWaymark = GetNearestWaymark();
            ShowGuidePointFixPos(nearestWaymark);
            break;

            case State.Waiting:
            ShowGuidePointFixPos(CenterPos);
            break;
        }
    }

    private void GuideReceiveCircle() // TODO : Implement
    {
        DebugLog("GuideReceiveCircle");
        uint searchID = (myData.hasColorRot == HasColorRot.RedRot) ? CastIDs.RedRot : CastIDs.BlueRot;
        List<IBattleChara> towerList = Svc.Objects.OfType<IBattleChara>().Where(x => x.CastActionId == searchID).ToList();
        Vector3 midPoint = Vector3.Zero;

        if (towerList.Count != 4)
        {
            switch (state)
            {
                case State.LatentDefectCasting:
                firstTowerPos = towerList.First().Position;
                secondTowerPos = towerList.Last().Position;
                IBattleChara myTower = DecideTower(towerList, partyList.Where(x => x.gimmickRole == GimmickRole.Share).Select(x => x.pcPtr).ToList());
                string firstLeftRight = GetRelativePosition(towerList.First().Position, CenterPos, towerList.Last().Position);
                if (firstLeftRight == "right")
                {
                    ShowGuidePointRef(myTower.EntityId, 90, new(0, 0, 7));
                }
                else
                {
                    ShowGuidePointRef(myTower.EntityId, 270, new(0, 0, 7));
                }
                break;

                case State.BreakPatchNear:
                var Shares = partyList.Where(x => x.gimmickRole == GimmickRole.Circle);
                IBattleChara? nearestShare = null;
                if (Vector3.Distance(myData.pcPtr.Position, Shares.First().pcPtr.Position) > Vector3.Distance(myData.pcPtr.Position, Shares.Last().pcPtr.Position))
                {
                    nearestShare = Shares.Last().pcPtr;
                }
                else
                {
                    nearestShare = Shares.First().pcPtr;
                }
                ShowGuidePointRef(nearestShare.EntityId);
                break;

                case State.BreakPatchFar:
                midPoint = CalculateMidpoint(firstTowerPos, secondTowerPos);
                ShowGuidePointFixPos(OffsetTowardsNorth(midPoint, CenterPos, new(0, 0, 0)));
                break;

                case State.WaitingRot:
                case State.Waiting:
                midPoint = CalculateMidpoint(firstTowerPos, secondTowerPos);
                ShowGuidePointFixPos(OffsetTowardsNorth(midPoint, CenterPos, new(0, 0, 4)));
                break;
            }
        }
        else
        {
            searchID = (myData.hasColorRot == HasColorRot.RedRot) ? CastIDs.BlueRot : CastIDs.RedRot;
            DebugLog("TowerList is 4");
            switch (state)
            {
                case State.LatentDefectCasting:
                firstTowerPos = towerList.First().Position;
                secondTowerPos = towerList.Last().Position;
                IBattleChara myTower = DecideTower(towerList, partyList.Where(x => x.gimmickRole == GimmickRole.Share).Select(x => x.pcPtr).ToList());
                IBattleChara otherTower = towerList.Where(x => x.EntityId != myTower.EntityId).First();
                string firstLeftRight = GetRelativePosition(myTower.Position, CenterPos, otherTower.Position);
                if (firstLeftRight == "right")
                {
                    ShowGuidePointRef(myTower.EntityId, 295, new(0, 0, 7));
                }
                else
                {
                    ShowGuidePointRef(myTower.EntityId, 65, new(0, 0, 7));
                }
                break;

                case State.BreakPatchNear:
                case State.BreakPatchFar:
                case State.WaitingRot:
                case State.Waiting:
                ShowGuidePointFixPos(CenterPos);
                break;
            }
        }
    }

    private void GuideReceiveShare()
    {
        DebugLog("GuideReceiveShare");
        uint searchID = (myData.hasColorRot == HasColorRot.RedRot) ? CastIDs.RedRot : CastIDs.BlueRot;
        List<IBattleChara> towerList = Svc.Objects.OfType<IBattleChara>().Where(x => x.CastActionId == searchID).ToList();

        if (latentCount != 4)
        {
            switch (state)
            {
                case State.LatentDefectCasting:
                firstTowerPos = towerList.First().Position;
                secondTowerPos = towerList.Last().Position;
                IBattleChara myTower = DecideTower(towerList, partyList.Where(x => x.gimmickRole == GimmickRole.Share).Select(x => x.pcPtr).ToList());
                IBattleChara otherTower = towerList.Where(x => x.EntityId != myTower.EntityId).First();
                string firstLeftRight = GetRelativePosition(myTower.Position, CenterPos, otherTower.Position);
                if (firstLeftRight == "right")
                {
                    ShowGuidePointRef(myTower.EntityId, 295, new(0, 0, 7));
                }
                else
                {
                    ShowGuidePointRef(myTower.EntityId, 65, new(0, 0, 7));
                }
                break;

                case State.BreakPatchNear:
                var Shares = partyList.Where(x => x.gimmickRole == GimmickRole.Share);
                IBattleChara? nearestShare = null;
                if (Vector3.Distance(myData.pcPtr.Position, Shares.First().pcPtr.Position) > Vector3.Distance(myData.pcPtr.Position, Shares.Last().pcPtr.Position))
                {
                    nearestShare = Shares.Last().pcPtr;
                }
                else
                {
                    nearestShare = Shares.First().pcPtr;
                }
                ShowGuidePointRef(nearestShare.EntityId);
                break;

                case State.BreakPatchFar:
                case State.WaitingRot:
                case State.Waiting:
                Vector3 midPoint = CalculateMidpoint(firstTowerPos, secondTowerPos);
                ShowGuidePointFixPos(OffsetTowardsNorth(midPoint, CenterPos, new(0, 0, 4)));
                break;
            }
        }
        else
        {
            switch (state)
            {
                case State.LatentDefectCasting:
                firstTowerPos = towerList.First().Position;
                secondTowerPos = towerList.Last().Position;
                IBattleChara myTower = DecideTower(towerList, partyList.Where(x => x.gimmickRole == GimmickRole.Share).Select(x => x.pcPtr).ToList());
                IBattleChara otherTower = towerList.Where(x => x.EntityId != myTower.EntityId).First();
                string firstLeftRight = GetRelativePosition(myTower.Position, CenterPos, otherTower.Position);
                if (firstLeftRight == "right")
                {
                    ShowGuidePointRef(myTower.EntityId, 295, new(0, 0, 7));
                }
                else
                {
                    ShowGuidePointRef(myTower.EntityId, 65, new(0, 0, 7));
                }
                break;

                case State.BreakPatchNear:
                case State.BreakPatchFar:
                case State.WaitingRot:
                case State.Waiting:
                ShowGuidePointFixPos(CenterPos);
                break;
            }
        }
    }


    private void ShowGuidePointFixPos(Vector3 position)
    {
        var element = Controller.GetElementByName("GuidePoint");
        element.type = 0;
        element.tether = true;
        element.SetRefPosition(position);
        element.SetOffPosition(Vector3.Zero);
        if (element.Enabled == false)
            element.Enabled = true;

        isLock = true;
    }

    private void ShowGuidePointRef(uint ObjectID, float? deg = null, Vector3? offset = null)
    {
        DebugLog($"ShowGuidePointRef: {Convert.ToString(ObjectID, 16)}, {deg}, {offset}");
        var element = Controller.GetElementByName("GuidePoint");
        element.type = 1;
        element.refActorType = 0;
        element.refActorObjectID = ObjectID;
        element.refActorComparisonType = 2;
        element.SetOffPosition((offset == null) ? Vector3.Zero : offset.Value);
        element.includeRotation = (deg == null) ? false : true;
        element.AdditionalRotation = (deg == null || deg > 360) ? 0 : (float)(Math.PI / 180 * deg);
        element.tether = true;
        if (element.Enabled == false)
            element.Enabled = true;

        isLock = true;
    }

    private IBattleChara DecideTower(List<IBattleChara> towerList, List<IPlayerCharacter> playerRoleList)
    {
        IBattleChara? pair = null;
        foreach (var x in playerRoleList)
        {
            if (x.Address != myData.pcPtr.Address)
            {
                pair = x;
                break; // Break the loop once we find the pair
            }
        }

        // Calculate distances
        float myDistanceToTower1 = Vector3.Distance(myData.pcPtr.Position, towerList.First().Position);
        float myDistanceToTower2 = Vector3.Distance(myData.pcPtr.Position, towerList.Last().Position);
        float pairDistanceToTower1 = Vector3.Distance(pair.Position, towerList.First().Position);
        float pairDistanceToTower2 = Vector3.Distance(pair.Position, towerList.Last().Position);

        DebugLog($"MyPos: {myData.pcPtr.Position.ToString()} Tower1 Distance: {myDistanceToTower1} Tower2 Distance: {myDistanceToTower2}");
        DebugLog($"PairPos: {pair.Position.ToString()} Tower1 Distance: {pairDistanceToTower1} Tower2 Distance: {pairDistanceToTower2}");

        // Convert EntityID to hex for logging
        string tower1IDHex = Convert.ToString(towerList.First().EntityId, 16);
        string tower2IDHex = Convert.ToString(towerList.Last().EntityId, 16);

        DebugLog($"Tower1 ID (hex): {tower1IDHex} Tower2 ID (hex): {tower2IDHex}");

        // If both are closer to Tower1, assign the one who is closer to Tower2 to Tower2
        if (myDistanceToTower1 < myDistanceToTower2 && pairDistanceToTower1 < pairDistanceToTower2)
        {
            if (myDistanceToTower2 < pairDistanceToTower2)
            {
                DebugLog("MyPos is closer to Tower2, assigning MyPos to Tower2.");
                return towerList.Last(); // Assign MyPos to Tower2
            }
            else
            {
                DebugLog("PairPos is closer to Tower2, assigning PairPos to Tower2.");
                return towerList.First(); // Keep MyPos at Tower1 and assume Pair goes to Tower2
            }
        }

        // If both are closer to Tower2, assign the one who is closer to Tower1 to Tower1
        if (myDistanceToTower2 < myDistanceToTower1 && pairDistanceToTower2 < pairDistanceToTower1)
        {
            if (myDistanceToTower1 < pairDistanceToTower1)
            {
                DebugLog("MyPos is closer to Tower1, assigning MyPos to Tower1.");
                return towerList.First(); // Assign MyPos to Tower1
            }
            else
            {
                DebugLog("PairPos is closer to Tower1, assigning PairPos to Tower1.");
                return towerList.Last(); // Keep MyPos at Tower2 and assume Pair goes to Tower1
            }
        }

        // If only MyPos is closer to Tower1, return Tower1
        if (myDistanceToTower1 <= myDistanceToTower2)
        {
            DebugLog("MyPos is closer to Tower1, assigning MyPos to Tower1.");
            return towerList.First();
        }

        // Otherwise, assign MyPos to Tower2
        DebugLog("MyPos is closer to Tower2, assigning MyPos to Tower2.");
        return towerList.Last();
    }


    private void HideAll() => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

    private void DebugLog(string message)
    {
        if (isDebug)
            DuoLog.Information(message);
    }

    /// <summary>
    /// Calculates an offset from a specific position towards a target position, treating the target as "north".
    /// </summary>
    /// <param name="pos">The base current position.</param>
    /// <param name="targetPos">The target position considered as "north".</param>
    /// <param name="offset">The offset vector. X represents left/right (right is positive), Z represents forward/backward (forward is positive).</param>
    /// <returns>Returns the new <see cref="Vector3"/> position after applying the offset.</returns>
    public static Vector3 OffsetTowardsNorth(Vector3 pos, Vector3 targetPos, Vector3 offset)
    {
        // Calculate the direction towards the target (treated as "north")
        Vector3 northDirection = Vector3.Normalize(targetPos - pos);

        // Offset based on the "north" direction
        // Calculate the right direction relative to the north direction
        Vector3 rightDirection = Vector3.Cross(Vector3.UnitY, northDirection);
        Vector3 forwardDirection = northDirection;

        // Calculate the offset position
        Vector3 offsetPosition = pos
                                 + (rightDirection * offset.X)  // Offset in the right direction
                                 + (forwardDirection * offset.Z); // Offset in the forward ("north") direction

        return offsetPosition;
    }


    public Vector3 GetNearestWaymark(Vector3? referencePos = null)
    {
        Vector3 basePos = referencePos ?? Svc.ClientState.LocalPlayer?.Position ?? Vector3.Zero;
        Vector3 nearestWaymark = Vector3.Zero;
        float shortestDistance = float.MaxValue;

        string[] MkNum = { "A", "B", "C", "D", "1", "2", "3", "4" };
        string MK = "";
        for (int i = 0; i < MkNum.Length; i++)
        {
            Vector3 markerPos = mkc->FieldMarkers[i].Position;

            float distance = Vector3.Distance(basePos, markerPos);
            if (distance < shortestDistance)
            {
                MK = MkNum[i];
                shortestDistance = distance;
                nearestWaymark = markerPos;
            }
        }
        return nearestWaymark;
    }

    /// <summary>
    /// Returns whether the target position is to the left or right of the current position,
    /// using the reference position as the perspective for the calculation.
    /// </summary>
    /// <param name="currentPos">The current position (Vector3).</param>
    /// <param name="referencePos">The reference position (Vector3), considered as the perspective.</param>
    /// <param name="targetPos">The target position (Vector3) to check its relative position.</param>
    /// <returns>A string indicating whether the target is "left" or "right".</returns>
    public string GetRelativePosition(Vector3 currentPos, Vector3 referencePos, Vector3 targetPos)
    {
        DebugLog($"Current: {currentPos.ToString()} Reference: {referencePos.ToString()} Target: {targetPos.ToString()}");
        // Calculate the vector from the reference to the current position (this is the perspective)
        Vector3 perspectiveDirection = currentPos - referencePos;
        perspectiveDirection.Y = 0; // Ignore Y axis (height)

        // Normalize the perspective direction
        perspectiveDirection = Vector3.Normalize(perspectiveDirection);

        // Calculate the vector from the reference position to the target position
        Vector3 targetDirection = targetPos - referencePos;
        targetDirection.Y = 0; // Ignore Y axis (height)

        // Calculate the cross product to determine if the target is to the left or right from referencePos
        float crossProduct = (perspectiveDirection.X * targetDirection.Z) - (perspectiveDirection.Z * targetDirection.X);

        var result = crossProduct > 0 ? "right" : "left";
        DebugLog($"Result: {result}");

        // If crossProduct is positive, target is to the right, if negative, it's to the left
        return crossProduct > 0 ? "right" : "left";
    }

    /// <summary>
    /// Calculates the midpoint between two 3D points.
    /// </summary>
    /// <param name="point1">First point (Vector3).</param>
    /// <param name="point2">Second point (Vector3).</param>
    /// <returns>The midpoint as a Vector3.</returns>
    public static Vector3 CalculateMidpoint(Vector3 point1, Vector3 point2)
    {
        return new Vector3(
            (point1.X + point2.X) / 2,
            (point1.Y + point2.Y) / 2,
            (point1.Z + point2.Z) / 2
        );
    }

    private static bool HasEffect(uint effect, float? remainingTile = null, IBattleChara? obj = null)
    {
        return (obj ?? Svc.ClientState.LocalPlayer).StatusList.Any(x => x.StatusId == effect && (remainingTile == null || x.RemainingTime < remainingTile));
    }
}
