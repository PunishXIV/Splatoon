using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Schedulers;
using Splatoon;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Forked_Tower_Magic
{
    internal class Boss_2_KB : SplatoonScript
    {
        public override HashSet<uint>? ValidTerritories { get; } = [1346];
        public override Metadata Metadata => new(1, "damolitionn");

        private Config Conf => Controller.GetConfig<Config>();

        private IBattleNpc? SwordDancer => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.BaseId == 19838 && b.IsCharacterVisible()) as IBattleNpc;

        private IBattleNpc? DancingSword => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.BaseId == 19842 && b.IsTargetable) as IBattleNpc;
        private List<IGameObject> SwordJumpOrder = new();
        private List<IGameObject> DancingSwords => Svc.Objects.Where(x => x.BaseId == 19842).ToList();

        private IGameObject? CurrentSword;
        private bool IsTrackingJumps = false;
        private float ProximityThreshold = 5f;

        private List<Element?> SwordElements = new();
        private TickScheduler? sched = null;


        public override void OnSetup()
        {
            var circleTemplate = "{\"refX\":0,\"refY\":0,\"refZ\":0,\"radius\":3.0, \"color\":255, \"fillIntensity\":0.5}";

            for(int i = 0; i < 5; i++)
            {
                var element = Controller.RegisterElementFromCode($"sword_{i}", circleTemplate);
                element.Enabled = false;
                SwordElements.Add(element);
            }
        }

        public override void OnStartingCast(uint source, uint castId)
        {
            if(castId == 49654)
            {
                IsTrackingJumps = true;
                SwordJumpOrder.Clear();
                CurrentSword = null;

                for(int i = 0; i < DancingSwords.Count && i < SwordElements.Count; i++)
                {
                    var sword = DancingSwords[i];
                    var element = SwordElements[i];

                    if(element != null)
                    {
                        element.refX = sword.Position.X;
                        element.refY = sword.Position.Z;
                        element.refZ = sword.Position.Y;
                        element.Enabled = true;
                    }
                }

                sched = new TickScheduler(() =>
                {
                    for(int i = 0; i < SwordElements.Count; i++)
                    {
                        if(SwordElements[i] != null)
                        {
                            SwordElements[i].overlayText = "";
                            SwordElements[i].Enabled = false;
                        }
                    }
                    IsTrackingJumps = false;
                }, 30000);
            }
        }

        public override void OnUpdate()
        {
            if(!IsTrackingJumps || SwordDancer == null) return;

            var closestSword = DancingSwords
                .OrderBy(s => Vector3.Distance(s.Position, SwordDancer.Position))
                .FirstOrDefault();

            if(closestSword == null) return;

            var distanceToCurrent = Vector3.Distance(closestSword.Position, SwordDancer.Position);

            if(distanceToCurrent < ProximityThreshold && closestSword != CurrentSword)
            {
                CurrentSword = closestSword;
                RecordJump();
            }
        }

        private void RecordJump()
        {
            if(!SwordJumpOrder.Contains(CurrentSword))
            {
                SwordJumpOrder.Add(CurrentSword);
                var jumpNumber = SwordJumpOrder.Count;
                var swordIndex = DancingSwords.IndexOf(CurrentSword);

                if(swordIndex < SwordElements.Count && SwordElements[swordIndex] != null)
                {
                    SwordElements[swordIndex]!.overlayText = jumpNumber.ToString();
                }
            }
        }

        public override void OnDisable()
        {
            sched?.Dispose();
        }

        public override void OnReset()
        {
            sched?.Dispose();
            IsTrackingJumps = false;
            SwordJumpOrder.Clear();
            CurrentSword = null;
        }

        public class Config : IEzConfig
        {
            public bool Enabled { get; set; } = true;
        }
    }
}