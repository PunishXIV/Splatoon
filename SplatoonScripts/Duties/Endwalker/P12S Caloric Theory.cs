using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PluginLog = ECommons.Logging.PluginLog;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P12S_Caloric_Theory : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => [1154];
        public override Metadata? Metadata => new(2, "tatad2");

        private string ElementNamePrefix = "P12SCaloricTheory123";

        private int closeCaloricStatusId = 3589;
        private int spreadStatusId = 3591; // atmosfaction
        private int stackStatusId = 3590;  // entropifaction

        private bool lastHasBuff = false;

        // the distance player can move in one layer of close caloric buff. 
        // may not very accurate. 9.2 meters has been tested to be safe and 9.4 meters are unsafe, but the data may affected by server latency. 
        private float maxDistancePerBuff = 9.2f;

        // change the color of Indicator when the remaining distance is less than this value.
        // better stop moving when the color is changed. 
        private float changeColorDistance = 0.5f;

        private float spreadRadius = 7.0f;
        private float stackRadius = 4.0f;
        private float distancePassed = 0;

        private Vector2 lastPosition;
        private int caloricTheoryCount = 0;

        private Element? Indicator1;
        private Element? Indicator2;
        private HashSet<uint> StackSpreadRecord = [];

        private List<TickScheduler> Sch = [];

        private bool debug = false;
        private Dictionary<uint, float> distanceDebug = [];
        private Dictionary<uint, Vector2> positionDebug = [];

        private void AddStackSpreadElement(uint objectId, bool isStack)
        {
            // {"Name":"","type":1,"Enabled":false,"radius":5.0,"refActorObjectID":291,"refActorComparisonType":2}

            StackSpreadRecord.Add(objectId);
            var e = new Element(1)
            {
                refActorObjectID = objectId,
                refActorComparisonType = 2,
                radius = isStack ? stackRadius : spreadRadius,
                color = isStack ? ImGuiColors.DPSRed.ToUint() : ImGuiColors.TankBlue.ToUint()
            };
            Controller.RegisterElement(ElementNamePrefix + objectId.ToString(), e, true);
        }

        private void ClearStackSpreadElements()
        {
            foreach(var objectId in StackSpreadRecord)
                Controller.TryUnregisterElement(ElementNamePrefix + objectId.ToString());
            StackSpreadRecord.Clear();
        }

        private void Reset()
        {
            PluginLog.Debug("caloric theory RESET");
            Sch.Each(x => x.Dispose());
            Indicator1.Enabled = false;
            Indicator2.Enabled = false;
            lastHasBuff = false;
            caloricTheoryCount = 0;
            ClearStackSpreadElements();
        }

        public override void OnEnable()
        {
            Reset();
        }

        public override void OnSetup()
        {
            // {"Name":"","type":1,"radius":5.0,"refActorType":1}

            var e1 = new Element(1)
            {
                refActorType = 1,
                Enabled = false
            };
            Controller.RegisterElement(ElementNamePrefix + "Indicator1", e1, true);
            Indicator1 = Controller.GetElementByName(ElementNamePrefix + "Indicator1");


            var e2 = new Element(1)
            {
                refActorType = 1,
                Enabled = false
            };
            Controller.RegisterElement(ElementNamePrefix + "Indicator2", e2, true);
            Indicator2 = Controller.GetElementByName(ElementNamePrefix + "Indicator2");
        }

        public override void OnMessage(string Message)
        {
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if(category == DirectorUpdateCategory.Commence || category == DirectorUpdateCategory.Recommence || category == DirectorUpdateCategory.Wipe)
                Reset();
        }

        private void Init(bool isPhaseTwo)
        {
            Indicator1.radius = maxDistancePerBuff;
            Indicator2.radius = maxDistancePerBuff * 2;
            Indicator1.Enabled = true;
            Indicator2.Enabled = isPhaseTwo;
            Indicator1.color = ImGuiColors.HealerGreen.ToUint();
            Indicator2.color = ImGuiColors.HealerGreen.ToUint();
            distancePassed = 0;
            //lastPosition = FakeParty.Get().First(x => x.EntityId == 0x1017913F).Position.ToVector2();
            lastPosition = Svc.ClientState.LocalPlayer.Position.ToVector2();

            if(debug)
            {
                foreach(var player in FakeParty.Get())
                {
                    distanceDebug[player.EntityId] = 0;
                    positionDebug[player.EntityId] = player.Position.ToVector2();
                }
            }
        }

        public override void OnUpdate()
        {
            var hasBuff = Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == closeCaloricStatusId);
            if(hasBuff)
            {
                if(!lastHasBuff)
                {
                    caloricTheoryCount++;
                    Init(caloricTheoryCount >= 2);
                }
                else
                {
                    //Vector2 Position = FakeParty.Get().First(x => x.EntityId == 0x1017913F).Position.ToVector2(); 
                    var Position = Svc.ClientState.LocalPlayer.Position.ToVector2();
                    var distance = Vector2.Distance(Position, lastPosition);
                    //PluginLog.Information($"pos:{Position}, lastPos:{lastPosition}, dis: {distance}"); 
                    lastPosition = Position;
                    distancePassed += distance;
                    Indicator1.radius -= distance;
                    Indicator2.radius -= distance;
                    if(distancePassed >= maxDistancePerBuff - changeColorDistance)
                        Indicator1.color = ImGuiColors.DalamudRed.ToUint();
                    if(distancePassed >= maxDistancePerBuff * 2 - changeColorDistance)
                        Indicator2.color = ImGuiColors.DalamudRed.ToUint();

                    if(debug)
                    {
                        foreach(var player in FakeParty.Get())
                        {
                            Position = player.Position.ToVector2();
                            distance = Vector2.Distance(Position, positionDebug[player.EntityId]);
                            positionDebug[player.EntityId] = Position;
                            distanceDebug[player.EntityId] += distance;
                        }
                    }
                }
                lastHasBuff = hasBuff;

                if(Conf.StakcSpreadShowTime > 0 && Conf.StakcSpreadShowTime < 7)
                {
                    // display stack/spread range
                    var stackers = FakeParty.Get().Where(x => x.StatusList.Any(x => x.StatusId == stackStatusId && x.RemainingTime <= Conf.StakcSpreadShowTime));
                    foreach(var stacker in stackers)
                    {
                        var objectId = stacker.EntityId;
                        if(StackSpreadRecord.Contains(objectId)) continue;
                        AddStackSpreadElement(objectId, true);
                        if(StackSpreadRecord.Count == 1)
                            Sch.Add(new TickScheduler(ClearStackSpreadElements, Conf.StakcSpreadShowTime * 1000));
                    }

                    var spreaders = FakeParty.Get().Where(x => x.StatusList.Any(x => x.StatusId == spreadStatusId && x.RemainingTime <= Conf.StakcSpreadShowTime));
                    foreach(var spreader in spreaders)
                    {
                        var objectId = spreader.EntityId;
                        if(StackSpreadRecord.Contains(objectId)) continue;
                        AddStackSpreadElement(objectId, false);
                        if(StackSpreadRecord.Count == 1)
                            Sch.Add(new TickScheduler(ClearStackSpreadElements, Conf.StakcSpreadShowTime * 1000));
                    }
                }
            }

            if(!hasBuff && lastHasBuff)
            {
                lastHasBuff = false;
                Indicator1.Enabled = false;
                Indicator2.Enabled = false;
                ClearStackSpreadElements();
            }
        }
        public class Config : IEzConfig
        {
            public int StakcSpreadShowTime = 2;
        }

        private Config Conf => Controller.GetConfig<Config>();
        public override void OnSettingsDraw()
        {
            ImGui.InputInt("the time to show stack/spread aoe(seconds, max=6)", ref Conf.StakcSpreadShowTime);

            if(ImGui.CollapsingHeader("Debug"))
            {
                ImGui.Checkbox("debug", ref debug);
                ImGui.Text($"Position: {lastPosition}, distance: {distancePassed}");
                ImGui.Text($"hasBuff: {lastHasBuff}");
                ImGui.Text($"StackSpread buff count: {StackSpreadRecord.Count()}");

                if(debug)
                {
                    foreach(var player in FakeParty.Get())
                    {
                        if(distanceDebug.ContainsKey(player.EntityId))
                        {
                            ImGui.Text($"{player.Name}, dis: {distanceDebug[player.EntityId]}, pos: {player.Position}");
                        }
                    }
                }
            }
        }
    }
}
