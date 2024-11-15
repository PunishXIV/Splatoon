using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Schedulers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;
internal unsafe class P5_Delta_Hello_Guide :SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = new HashSet<uint> { 1122 };
    public override Metadata? Metadata => new Metadata(4, "Redmoon");

    #region Types
    private class PartyData
    {
        public int index = 0;
        public IPlayerCharacter player;
        public Job job;
        public IPlayerCharacter? pair = null;
        public bool isClosePosition = false;
        public bool isFar = false;
        public bool isLeft = false;
        public bool isBashed = false;
        public bool isSampled = false;
        public string SampledLeftRight = "";
        public string position = "";

        public PartyData(IPlayerCharacter player, Job job)
        {
            this.player = player;
            this.job = job;
        }
    }

    private enum GimmickPhase
    {
        None = 0,
        DeltaFirstHalfStart,
        DeltaFirstHalfHandSpawn,
        DeltaFirstHalfStackTiming,
        DeltaSecondHalf,
    }

    private class ElementPos
    {
        public string ElementName;
        public uint Color;
        public uint OverlayBGColor;
        public uint OverlayTextColor;
        public bool includeRotation;
        public float AdditionalRotation;
        public Vector3 Position;
    }
    #endregion

    #region ReadOnly
    const uint beetleModelId = 0xEBB;
    const uint finalModelId = 0xEBF;

    private class CastID
    {
        public const uint RunMiDelta = 31624;
        public const uint RunMiSigma = 32788;
        public const uint RunMiOmega = 0;
        public const uint HelloFar = 33040;
        public const uint ArmWaveCannon = 31600;
        public const uint ShieldCombo = 31528;
        public const uint OverSampledCannonRight = 31638;
        public const uint OverSampledCannonLeft = 31639;
        public const uint OverSampledCannonHit = 31597;
        public const uint SwivelCannonRight = 31636;
        public const uint SwivelCannonLeft = 31637;
    };

    private class BuffID
    {
        public const uint SampledBuffRight = 3452u;
        public const uint SampledBuffLeft = 3453u;
        public const uint BashDebuff = 2534;
        public const uint HelloNear = 3442;
        public const uint HelloFar = 3443;
    }

    List<Job> PriorityJobList = new List<Job>()
    {
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
    };

    IReadOnlyList<ElementPos> Delta = new List<ElementPos>()
    {
        new ElementPos { ElementName = "GreenNearOmega", Position = new Vector3(0, 1.78f, 0), Color = 3355508503, OverlayBGColor = 2617245696, OverlayTextColor = 4278255360, includeRotation = true, AdditionalRotation = 309.2f.DegreesToRadians() },
        new ElementPos { ElementName = "GreenFarFromOmega", Position = new Vector3(0, 37.1f, 0), Color = 3355508503, OverlayBGColor = 2617245696, OverlayTextColor = 4278255360, includeRotation = true, AdditionalRotation = 343.2f.DegreesToRadians() },
        new ElementPos { ElementName = "NearSource", Position = new Vector3(0, 20.8f, 0), Color = 4278225677, OverlayBGColor = 4278220288, OverlayTextColor = 4294967295, includeRotation = true, AdditionalRotation = 340.9f.DegreesToRadians() },
        new ElementPos { ElementName = "NearTakerInner", Position = new Vector3(0, 24.48f, 0), Color = 4278237622, OverlayBGColor = 4278236333, OverlayTextColor = 4278190080, includeRotation = true, AdditionalRotation = 321.9f.DegreesToRadians() },
        new ElementPos { ElementName = "NearTakerOuter", Position = new Vector3(0, 30.84f, 0), Color = 4278237622, OverlayBGColor = 4278236333, OverlayTextColor = 4278190080, includeRotation = true, AdditionalRotation = 321.6f.DegreesToRadians() },
        new ElementPos { ElementName = "BrokenTetherChillSpot", Position = new Vector3(0, 34.54f, 0), Color = 4294967295, OverlayBGColor = 4294967295, OverlayTextColor = 4278190080, includeRotation = true, AdditionalRotation = 332.4f.DegreesToRadians() },
        new ElementPos { ElementName = "FarSource", Position = new Vector3(0, 14.36f, 0), Color = 4288326400, OverlayBGColor = 4285363712, OverlayTextColor = 4294967295, includeRotation = true, AdditionalRotation = 293.2f.DegreesToRadians() }
    };
    #endregion

    #region PrivateDefinitions
    private GimmickPhase _gimmickPhase = GimmickPhase.None;
    private List<PartyData> _partyData = new List<PartyData>();
    private IBattleNpc? _beetle = null;
    private IBattleNpc? _final = null;
    private bool _isOverSampledRightCasted = false;
    private bool _isOverSampledLeftCasted = false;
    private bool _isSwivelCannonRightCasted = false;
    private bool _isSwivelCannonLeftCasted = false;
    private int _handCount = 0;
    private Job _debugJob = Job.WAR;
    private Config C => Controller.GetConfig<Config>();
    #endregion

    #region public
    public override void OnSetup()
    {
        for(int i = 0; i < 4; i++)
        {
            Controller.RegisterElement($"SampledCannonRange{i}", new Element(1) { refActorComparisonType = 2, radius = 7f, Filled = true, color = 3372166400 });
        }

        Controller.RegisterElement("FinalCloseLeft", new Element(1) { refActorComparisonType = 2, offY = 19f, thicc = 5f, includeRotation = true, AdditionalRotation = 300f.DegreesToRadians(), color = 3355508503, overlayBGColor = 2617245696, overlayTextColor = 4278255360, });
        Controller.RegisterElement("FinalFarLeft", new Element(1) { refActorComparisonType = 2, offY = 34f, thicc = 5f, includeRotation = true, AdditionalRotation = 330f.DegreesToRadians(), color = 3355508503, overlayBGColor = 2617245696, overlayTextColor = 4278255360, });
        Controller.RegisterElement("FinalNoSampledBashLeft", new Element(1) { refActorComparisonType = 2, offY = 4f, thicc = 5f, includeRotation = true, AdditionalRotation = 345f.DegreesToRadians(), color = 3355508503, overlayBGColor = 2617245696, overlayTextColor = 4278255360, });
        Controller.RegisterElement("FinalSampledBashLeft", new Element(1) { refActorComparisonType = 2, offY = 8f, thicc = 5f, includeRotation = true, AdditionalRotation = 329f.DegreesToRadians(), color = 3355508503, overlayBGColor = 2617245696, overlayTextColor = 4278255360, });
        Controller.RegisterElement("FinalNoSampledStackLeft", new Element(1) { refActorComparisonType = 2, offY = 20f, thicc = 5f, includeRotation = true, AdditionalRotation = 358f.DegreesToRadians(), color = 3355508503, overlayBGColor = 2617245696, overlayTextColor = 4278255360, });
        Controller.RegisterElement("FinalSampledStackLeft", new Element(1) { refActorComparisonType = 2, offY = 21f, thicc = 5f, includeRotation = true, AdditionalRotation = 347f.DegreesToRadians(), color = 3355508503, overlayBGColor = 2617245696, overlayTextColor = 4278255360, });

        foreach(var element in Delta)
        {
            Controller.RegisterElement(element.ElementName, new Element(1)
            {
                refActorComparisonType = 2,
                thicc = 5f,
                offY = element.Position.Y,
                color = element.Color,
                overlayBGColor = element.OverlayBGColor,
                overlayTextColor = element.OverlayTextColor,
                includeRotation = element.includeRotation,
                AdditionalRotation = element.AdditionalRotation,
            });
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        switch(castId)
        {
            case CastID.RunMiDelta:
            // Set Party Data
            var party = FakeParty.Get();
            List<PartyData> unsortedList = new List<PartyData>();
            foreach(var player in party)
            {
                unsortedList.Add(new PartyData(player, player.GetJob()));
            }

            foreach(var item in unsortedList)
            {
                var index = PriorityJobList.IndexOf(item.job);
                if(index == -1)
                {
                    DuoLog.Error($"Job {item.job} not found in PriorityJobList\nPlease Report Discord Server");
                    return;
                }
                item.index = index;
            }

            _partyData = unsortedList.OrderBy(x => x.index).ToList();
            _gimmickPhase = GimmickPhase.DeltaFirstHalfStart;
            break;

            case CastID.SwivelCannonLeft:
            _isSwivelCannonLeftCasted = true;
            _gimmickPhase = GimmickPhase.DeltaSecondHalf;
            break;
            case CastID.SwivelCannonRight:
            _isSwivelCannonRightCasted = true;
            _gimmickPhase = GimmickPhase.DeltaSecondHalf;
            break;
            case CastID.OverSampledCannonLeft:
            _isOverSampledLeftCasted = true;
            break;
            case CastID.OverSampledCannonRight:
            _isOverSampledRightCasted = true;
            break;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null || _gimmickPhase == GimmickPhase.None) return;

        switch(set.Action.Value.RowId)
        {
            case CastID.ShieldCombo:
            _gimmickPhase = GimmickPhase.DeltaFirstHalfStackTiming;
            break;
            case CastID.SwivelCannonLeft:
            case CastID.SwivelCannonRight:
            _gimmickPhase = GimmickPhase.None;
            break;
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(!(_gimmickPhase == GimmickPhase.DeltaFirstHalfStart && _partyData.Count == 8)) return;

        PartyData? sourcePlayer = _partyData.FirstOrDefault(x => x.player.EntityId == source);
        PartyData? targetPlayer = _partyData.FirstOrDefault(x => x.player.EntityId == target);
        if(sourcePlayer == null || targetPlayer == null) return;

        // Near
        if(data3 == 200 && data5 == 15)
        {
            sourcePlayer.pair = targetPlayer.player;
            sourcePlayer.isFar = false;
            targetPlayer.pair = sourcePlayer.player;
            targetPlayer.isFar = false;
        }

        // Far
        if(data3 == 201 && data5 == 15)
        {
            sourcePlayer.pair = targetPlayer.player;
            sourcePlayer.isFar = true;
            targetPlayer.pair = sourcePlayer.player;
            targetPlayer.isFar = true;
        }
    }

    public override void OnObjectCreation(nint newObjectPtr)
    {
        if(_gimmickPhase == GimmickPhase.None || _partyData.Count != 8) return;

        _ = new TickScheduler(delegate
        {
            if(Svc.Objects.FirstOrDefault(x => x.Address == newObjectPtr)?.DataId != 0x3D5D &&
               Svc.Objects.FirstOrDefault(x => x.Address == newObjectPtr)?.DataId != 0x3D5E) return;

            ++_handCount;
            if(_handCount < 8)
            {
                return;
            }

            // Find beetle and final
            if(_beetle == null || _final == null)
            {
                _beetle = Svc.Objects.FirstOrDefault(x => x is IBattleNpc c && c.Struct()->Character.ModelCharaId == beetleModelId) as IBattleNpc;
                _final = Svc.Objects.FirstOrDefault(x => x is IBattleNpc c && c.Struct()->Character.ModelCharaId == finalModelId) as IBattleNpc;

                if(_beetle == null || _final == null)
                {
                    _gimmickPhase = GimmickPhase.None;
                    return;
                }
            }

            // Find Closest 2 Players
            // Far Players
            var farPlayers = _partyData.Where(x => x.isFar).ToList();
            var closest = farPlayers.OrderBy(x => Vector3.Distance(new Vector3(100, 0, 100), x.player.Position)).FirstOrDefault();
            PartyData pm = farPlayers.FirstOrDefault(x => x.player.EntityId == closest.player.EntityId);
            PartyData pmPartner = farPlayers.FirstOrDefault(x => x.player.EntityId == closest.pair?.EntityId);
            if(pm != null && pmPartner != null)
            {
                pm.isClosePosition = true;
                pmPartner.isClosePosition = true;
                // Left or Right
                (pm.isLeft, pmPartner.isLeft) = IsLeft(new Vector3(100, 0, 100), _beetle.Position, pm.pair.Position, pmPartner.pair.Position);
            }

            // Near Players
            var nearPlayers = _partyData.Where(x => !x.isFar).ToList();
            var closestNear = nearPlayers.OrderBy(x => Vector3.Distance(new Vector3(100, 0, 100), x.player.Position)).FirstOrDefault();
            PartyData pmNear = nearPlayers.FirstOrDefault(x => x.player.EntityId == closestNear.player.EntityId);
            PartyData pmPartnerNear = nearPlayers.FirstOrDefault(x => x.player.EntityId == closestNear.pair?.EntityId);
            if(pmNear != null && pmPartnerNear != null)
            {
                pmNear.isClosePosition = true;
                pmPartnerNear.isClosePosition = true;
                // Left or Right
                (pmNear.isLeft, pmPartnerNear.isLeft) = IsLeft(new Vector3(100, 0, 100), _final.Position, pmNear.pair.Position, pmPartnerNear.pair.Position);
            }

            var Outers = _partyData.Where(x => x.isClosePosition == false).ToList();
            // Left or Right
            (Outers[0].isLeft, Outers[1].isLeft) = IsLeft(new Vector3(100, 0, 100), _final.Position, Outers[0].pair.Position, Outers[1].pair.Position);
            (Outers[2].isLeft, Outers[3].isLeft) = IsLeft(new Vector3(100, 0, 100), _final.Position, Outers[2].pair.Position, Outers[3].pair.Position);

            // Check Party Data
            if(!_partyData.All(x => x.pair != null) ||
                _partyData.Where(x => x.isFar == true).Count() != 4 ||
                _partyData.Where(x => x.isLeft == true).Count() != 4)
            {
                DuoLog.Error("Party Data Error");
                _gimmickPhase = GimmickPhase.None;
                return;
            }
            else
            {
                _gimmickPhase = GimmickPhase.DeltaFirstHalfHandSpawn;
            }

            // Set Delta Position
            bool innerUsed = false;
            foreach(var player in _partyData)
            {
                if(player.isFar)
                {
                    if(player.player.StatusList.Any(x => x.StatusId == BuffID.HelloNear))
                    {
                        player.position = "NearSource";
                    }
                    else if(player.player.StatusList.Any(x => x.StatusId == BuffID.HelloFar))
                    {
                        player.position = "FarSource";
                    }
                    else
                    {
                        if(!innerUsed)
                        {
                            player.position = "NearTakerInner";
                            innerUsed = true;
                        }
                        else
                        {
                            player.position = "NearTakerOuter";
                        }
                    }
                }
                else
                {
                    if((player.isClosePosition && !C.invertInOutPosition) ||
                       (!player.isClosePosition && C.invertInOutPosition))
                    {
                        if(player.isLeft)
                        {
                            player.position = "GreenFarFromOmega";
                        }
                        else
                        {
                            player.position = "GreenNearOmega";
                        }
                    }
                    else
                    {
                        player.position = "BrokenTetherChillSpot";
                    }
                }
            }
        });
    }

    public override void OnUpdate()
    {
        ActorEffectCheck();

        if(_gimmickPhase == GimmickPhase.DeltaFirstHalfStackTiming)
        {
            ShowDeltaStackPoint(Svc.ClientState.LocalPlayer.EntityId);
        }
        else if(_gimmickPhase == GimmickPhase.DeltaSecondHalf)
        {
            ShowDeltaHelloPosition(Svc.ClientState.LocalPlayer.EntityId);
        }
    }

    public override void OnReset()
    {
        _gimmickPhase = GimmickPhase.None;
        _partyData.Clear();
        _beetle = null;
        _final = null;
        _isOverSampledRightCasted = false;
        _isOverSampledLeftCasted = false;
        _handCount = 0;
        _isSwivelCannonRightCasted = false;
        _isSwivelCannonLeftCasted = false;
        ElementOff();
    }

    public class Config :IEzConfig
    {
        public bool invertInOutPosition = false;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Hello Near The default setting for buff cutting order is to give priority to the outside. If you want to prioritize the inside, please set the following.");
        ImGui.Checkbox("Invert In/Out Position", ref C.invertInOutPosition);
        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"GimmickPhase: {_gimmickPhase}");
            ImGui.Text($"OverSampledRightCasted: {_isOverSampledRightCasted}");
            ImGui.Text($"OverSampledLeftCasted: {_isOverSampledLeftCasted}");
            ImGui.Text($"SwivelCannonRightCasted: {_isSwivelCannonRightCasted}");
            ImGui.Text($"SwivelCannonLeftCasted: {_isSwivelCannonLeftCasted}");
            ImGui.Text($"PartyData Count: {_partyData.Count}");
            ImGui.Text($"Beetle: {_beetle?.EntityId ?? 0}");
            ImGui.Text($"Final: {_final?.EntityId ?? 0}");
            ImGui.Text($"PartyData: ");
            List<ImGuiEx.EzTableEntry> ezTableEntry = new List<ImGuiEx.EzTableEntry>();
            if(_partyData.Count > 0)
            {
                foreach(var player in _partyData)
                {
                    ezTableEntry.Add(new ImGuiEx.EzTableEntry("Name", () => ImGui.Text(player.player.Name.ToString())));
                    ezTableEntry.Add(new ImGuiEx.EzTableEntry("Job", () => ImGui.Text(player.job.ToString())));
                    ezTableEntry.Add(new ImGuiEx.EzTableEntry("Pair", () => ImGui.Text(player.pair?.Name.ToString() ?? "")));
                    ezTableEntry.Add(new ImGuiEx.EzTableEntry("isClosePosition", () => ImGui.Text(player.isClosePosition.ToString())));
                    ezTableEntry.Add(new ImGuiEx.EzTableEntry("isFar", () => ImGui.Text(player.isFar.ToString())));
                    ezTableEntry.Add(new ImGuiEx.EzTableEntry("isLeft", () => ImGui.Text(player.isLeft.ToString())));
                    ezTableEntry.Add(new ImGuiEx.EzTableEntry("isBashed", () => ImGui.Text(player.isBashed.ToString())));
                    ezTableEntry.Add(new ImGuiEx.EzTableEntry("isSampled", () => ImGui.Text(player.isSampled.ToString())));
                    ezTableEntry.Add(new ImGuiEx.EzTableEntry("Position", () => ImGui.Text(player.position)));
                }
                ImGuiEx.EzTable(ezTableEntry);
            }
            else
            {
                ImGui.Text("No Party Data");
            }
        }
    }
    #endregion

    #region private
    private void ActorEffectCheck()
    {
        if(_gimmickPhase == GimmickPhase.None) return;

        if(_partyData.All(x => x.isBashed == false))
        {
            var p = _partyData.FirstOrDefault(x => x.player.StatusList.Any(y => y.StatusId == BuffID.BashDebuff));
            if(p != null)
            {
                p.isBashed = true;
            }
        }

        if(_partyData.All(x => x.isSampled == false))
        {
            var p = _partyData.FirstOrDefault(x => x.player.StatusList
                        .Any(y => y.StatusId == BuffID.SampledBuffLeft) || x.player.StatusList.Any(y => y.StatusId == BuffID.SampledBuffRight));
            if(p != null)
            {
                p.isSampled = true;
                p.SampledLeftRight = p.player.StatusList.Any(y => y.StatusId == BuffID.SampledBuffLeft) ? "Left" : "Right";
            }
        }
    }

    private void ShowDeltaStackPoint(uint ObjectId)
    {
        ElementOff();

        if((_isOverSampledLeftCasted && _isOverSampledRightCasted) || (!_isOverSampledLeftCasted && !_isOverSampledRightCasted))
        {
            _gimmickPhase = GimmickPhase.None;
            return;
        }

        List<PartyData> helloNears = _partyData.Where(x => x.isFar == false).ToList();
        int i = 0;
        foreach(PartyData nears in helloNears)
        {
            Controller.GetElementByName($"SampledCannonRange{i}").refActorObjectID = nears.player.EntityId;
            Controller.GetElementByName($"SampledCannonRange{i}").Enabled = true;
            ++i;
        }

        PartyData? player = _partyData.FirstOrDefault(x => x.player.EntityId == ObjectId);

        if(player.isFar)
        {
            // Far
            // Sampled + Bash
            if(player.isSampled && player.isBashed)
            {
                ShowStackElement("FinalSampledBashLeft", player);
            }
            // Bash
            else if(player.isBashed && !player.isSampled)
            {
                ShowStackElement("FinalNoSampledBashLeft", player);
            }
            // Sampled
            else if(player.isSampled && !player.isBashed)
            {
                ShowStackElement("FinalSampledStackLeft", player);
            }
            else
            {
                // Bash + Sampled
                if(_partyData.Any(x => x.isSampled && x.isBashed))
                {
                    ShowStackElement("FinalNoSampledStackLeft", player);
                }
                // Bash
                else if(_partyData.Any(x => x.isBashed && !x.isSampled))
                {
                    ShowStackElement("FinalSampledStackLeft", player);
                }
            }
        }
        else
        {
            // Near
            if(player.isClosePosition)
            {
                ShowStackElement("FinalFarLeft", player);
            }
            else
            {
                ShowStackElement("FinalCloseLeft", player);
            }
        }
    }

    private void ShowStackElement(string elementName, PartyData player)
    {
        Controller.GetElementByName(elementName).AdditionalRotation = InvertRotationCheck(Controller.GetElementByName(elementName), player);
        Controller.GetElementByName(elementName).tether = true;
        Controller.GetElementByName(elementName).refActorObjectID = _final.EntityId;
        Controller.GetElementByName(elementName).Enabled = true;
    }

    private void ShowDeltaHelloPosition(uint ObjectId)
    {
        PartyData? player = _partyData.FirstOrDefault(x => x.player.EntityId == ObjectId);
        ElementOff();

        Controller.GetElementByName(player.position).AdditionalRotation = InvertRotationCheck(Controller.GetElementByName(player.position), player);
        Controller.GetElementByName(player.position).refActorObjectID = _beetle.EntityId;
        Controller.GetElementByName(player.position).tether = true;
        Controller.GetElementByName(player.position).Enabled = true;
    }

    private static (bool isTargetLeft, bool isPairLeft) IsLeft(Vector3 center, Vector3 reference, Vector3 target, Vector3 pair)
    {
        Vector2 AB = new Vector2(center.X - reference.X, center.Z - reference.Z);
        Vector2 AC = new Vector2(target.X - reference.X, target.Z - reference.Z);
        Vector2 AD = new Vector2(pair.X - reference.X, pair.Z - reference.Z);

        float crossProductTarget = (AB.X * AC.Y) - (AB.Y * AC.X);
        float crossProductPair = (AB.X * AD.Y) - (AB.Y * AD.X);

        bool isTargetLeft = crossProductTarget < 0;
        bool isPairLeft = crossProductPair < 0;

        return (isTargetLeft, isPairLeft);
    }

    private float InvertRotationCheck(Element element, PartyData pd)
    {
        if(!element.includeRotation) return 0;

        if(_gimmickPhase == GimmickPhase.DeltaFirstHalfStackTiming)
        {
            if(pd.isFar)
            {
                if(element.AdditionalRotation > 200f.DegreesToRadians() && _isOverSampledRightCasted) return element.AdditionalRotation;
                if(element.AdditionalRotation <= 200f.DegreesToRadians() && _isOverSampledLeftCasted) return element.AdditionalRotation;
            }
            else
            {
                if(element.AdditionalRotation > 200f.DegreesToRadians() && !pd.isLeft) return element.AdditionalRotation;
                if(element.AdditionalRotation <= 200f.DegreesToRadians() && pd.isLeft) return element.AdditionalRotation;
            }
        }
        else
        {
            if(element.AdditionalRotation >= 200f.DegreesToRadians() && _isSwivelCannonRightCasted) return element.AdditionalRotation;
            if(element.AdditionalRotation < 200f.DegreesToRadians() && _isSwivelCannonLeftCasted) return element.AdditionalRotation;
        }

        DuoLog.Information($"Invert Rotation: {element.AdditionalRotation}");
        float rotation = element.AdditionalRotation;
        return (float)360f.DegreesToRadians() - rotation;
    }

    private void ElementOff() => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    #endregion
}
