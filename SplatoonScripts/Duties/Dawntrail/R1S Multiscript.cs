using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ECommons.Schedulers;


namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public class R1S_Multiscript : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = new HashSet<uint> { 1226 };

    private List<Vector3> clonePositions = new List<Vector3>();

    public override Metadata? Metadata => new(2, "damolitionn");

    private bool IsLeapingCleave = false;
    private bool LeftFirst = false;
    private bool RightFirst = false;
    private string movement = "";
    private string attack = "";

    private TickScheduler? sched = null;
    private Vector3 jumpTargetPosition;

    IBattleNpc? BlackCat => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == 17193 && b.IsTargetable) as IBattleNpc;

    public override void OnSetup()
    {
        //Clone Position
        Controller.RegisterElementFromCode("SArrowLeft", "{\"Name\":\"SArrowLeft\",\"enabled\": false,\"type\":2,\"refX\":90.0,\"refY\":105,\"refZ\":0,\"offX\":100.0,\"offY\":105,\"radius\":0.0,\"color\":3355507455,\"thicc\":3.0,\"LineEndA\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("SArrowRight", "{\"Name\":\"SArrowRight\",\"enabled\": false,\"type\":2,\"refX\":110.0,\"refY\":105,\"refZ\":0,\"offX\":100.0,\"offY\":105,\"radius\":0.0,\"color\":3355507455,\"thicc\":3.0,\"LineEndA\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("NArrowRight", "{\"Name\":\"NArrowRight\",\"enabled\": false,\"type\":2,\"refX\":90.0,\"refY\":95,\"refZ\":0,\"offX\":100.0,\"offY\":95,\"radius\":0.0,\"color\":3355507455,\"thicc\":3.0,\"LineEndA\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("NArrowLeft", "{\"Name\":\"NArrowLeft\",\"enabled\": false,\"type\":2,\"refX\":110.0,\"refY\":95,\"refZ\":0,\"offX\":100.0,\"offY\":95,\"radius\":0.0,\"color\":3355507455,\"thicc\":3.0,\"LineEndA\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        //Cleaves
        if (!Controller.TryRegisterElement("Cleave", new(0)
        {
            Name = "Cleave",
            Enabled = false,
            type = 3,
            refY = 30.0f,
            radius = 30.0f,
            refActorComparisonType = 3,
            includeRotation = true,
            AdditionalRotation = 4.712389f
        }))
        {
            DuoLog.Error("Could not register layout");
        }

        //Clone Cleaves
        if (!Controller.TryRegisterElement("CloneCleave", new(0)
        {
            Name = "CloneCleave",
            Enabled = false,
            type = 2,
            radius = 30.0f,
        }))
        {
            DuoLog.Error("Could not register layout");
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        var obj = target.GetObject();

        if (obj?.DataId == 17193)
        {
            if (clonePositions.Count >= 6)
            {
                IsLeapingCleave = true;
            }
            //Left Cleave First
            if (vfxPath.Contains("vfx/common/eff/m0884_cast_twin02p1.avfx"))
            {
                RightFirst = false;
                LeftFirst = true;
                HandleCleaveSequence();
            }
            //Right Cleave First
            else if (vfxPath.Contains("vfx/common/eff/m0884_cast_twin01p1.avfx"))
            {
                LeftFirst = false;
                RightFirst = true;
                HandleCleaveSequence();
            }
        }

        if (obj?.DataId == 17196 && clonePositions.Count == 9)
        {
            if (vfxPath.Contains("vfx/common/eff/m0884_cast_dbl01p1.avfx"))
            {
                GetJumpPositions(8);
                HandleCloneCleaves();
            }
        }

        if (obj?.DataId == 17196 && clonePositions.Count == 10)
        {
            if (vfxPath.Contains("vfx/common/eff/m0884_cast_dbl01p1.avfx"))
            {
                GetJumpPositions(9);
                HandleCloneCleaves();
            }
        }
    }

    private void GetJumpPositions(int cloneNumber)
    {
        if (clonePositions[cloneNumber].Z > 100)
        {
            if (Controller.TryGetElementByName($"SArrowLeft", out var arrow1) && arrow1.Enabled)
            {
                jumpTargetPosition = new Vector3(arrow1.refX, arrow1.refZ, arrow1.refY);
            }
            if (Controller.TryGetElementByName($"SArrowRight", out var arrow2) && arrow2.Enabled)
            {
                jumpTargetPosition = new Vector3(arrow2.refX, arrow2.refZ, arrow2.refY);
            }
        }
        else if (clonePositions[cloneNumber].Z < 100)
        {
            if (Controller.TryGetElementByName($"NArrowLeft", out var arrow1) && arrow1.Enabled)
            {
                jumpTargetPosition = new Vector3(arrow1.refX, arrow1.refZ, arrow1.refY);
            }
            if (Controller.TryGetElementByName($"NArrowRight", out var arrow2) && arrow2.Enabled)
            {
                jumpTargetPosition = new Vector3(arrow2.refX, arrow2.refZ, arrow2.refY);
            }
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (source.GetObject().DataId == 17196 && target.GetObject().DataId == 17193)
        {
            var position = source.GetObject().Position;
            clonePositions.Add(position);
            CheckBossBuffs();
        }
    }

    private void HandleCloneAttacks()
    {
        if (clonePositions.Count <= 2)
        {
            if (LeftFirst)
            {
                LeftCleaveFirst(20000, 4000, 17196, 17196);
            }
            else
            {
                RightCleaveFirst(20000, 4000, 17196, 17196);
            }
            var flagResetScheduler = new TickScheduler(() =>
            {
                LeftFirst = false;
                RightFirst = false;
            }, 3000);
        }
    }

    private void HandleCleaveSequence()
    {
        if (IsLeapingCleave == true)
        {
            if (LeftFirst)
            {
                LeftCleaveFirst(7000, 3000, 17195, 17193);
            }
            else
            {
                RightCleaveFirst(7000, 3000, 17195, 17193);
            }

        }
        else
        {
            if (LeftFirst)
            {
                LeftCleaveFirst(6000, 3000, 17193, 17193);
            }
            else
            {
                RightCleaveFirst(6000, 3000, 17193, 17193);
            }
        }
    }

    private void HandleCloneCleaves()
    {
        if (jumpTargetPosition.Z > 100)
        {
            if (LeftFirst)
            {
                LeftCloneCleave();
            }
            else
            {
                RightCloneCleave();
            }
        }
        else
        {
            if (LeftFirst)
            {
                RightCloneCleave();
            }
            else
            {
                LeftCloneCleave();
            }
        }
    }

    private void LeftCleaveFirst(uint t1, uint t2, uint dataID1, uint dataID2)
    {
        if (Controller.TryGetElementByName("Cleave", out var leftCleave))
        {
            leftCleave.Enabled = true;
            leftCleave.refActorDataID = dataID1;
            leftCleave.onlyVisible = true;
            leftCleave.AdditionalRotation = 4.712389f;
        }

        sched?.Dispose();
        sched = new TickScheduler(() =>
        {
            if (Controller.TryGetElementByName("Cleave", out var leftCleave))
            {
                leftCleave.Enabled = false;
            }
            if (Controller.TryGetElementByName("Cleave", out var rightCleave))
            {
                rightCleave.Enabled = true;
                rightCleave.refActorDataID = dataID2;
                rightCleave.onlyVisible = true;
                rightCleave.AdditionalRotation = 1.5707964f;
            }

            sched = new TickScheduler(() =>
            {
                if (Controller.TryGetElementByName("Cleave", out var rightCleave))
                {
                    rightCleave.Enabled = false;
                }
            }, t2);
        }, t1);
    }

    private void RightCleaveFirst(uint t1, uint t2, uint dataID1, uint dataID2)
    {
        if (Controller.TryGetElementByName("Cleave", out var rightCleave))
        {
            rightCleave.Enabled = true;
            rightCleave.refActorDataID = dataID1;
            rightCleave.onlyVisible = true;
            rightCleave.AdditionalRotation = 1.5707964f;
        }

        sched?.Dispose();
        sched = new TickScheduler(() =>
        {
            if (Controller.TryGetElementByName("Cleave", out var rightCleave))
            {
                rightCleave.Enabled = false;
            }
            if (Controller.TryGetElementByName("Cleave", out var leftCleave))
            {
                leftCleave.Enabled = true;
                leftCleave.refActorDataID = dataID2;
                leftCleave.onlyVisible = true;
                leftCleave.AdditionalRotation = 4.712389f;
            }

            sched = new TickScheduler(() =>
            {
                if (Controller.TryGetElementByName("Cleave", out var leftCleave))
                {
                    leftCleave.Enabled = false;
                }
            }, t2);
        }, t1);
    }

    private void LeftCloneCleave()
    {
        if (Controller.TryGetElementByName("CloneCleave", out var leftCleave))
        {
            leftCleave.Enabled = true;
            leftCleave.refX = jumpTargetPosition.X;
            leftCleave.refY = jumpTargetPosition.Z;
            leftCleave.offX = jumpTargetPosition.X - 30f;
            leftCleave.offY = jumpTargetPosition.Z;
        }

        sched?.Dispose();
        sched = new TickScheduler(() =>
        {
            if (Controller.TryGetElementByName("CloneCleave", out var leftCleave))
            {
                leftCleave.Enabled = false;
            }
            if (Controller.TryGetElementByName("CloneCleave", out var rightCleave))
            {
                rightCleave.Enabled = true;
                rightCleave.refX = jumpTargetPosition.X;
                rightCleave.refY = jumpTargetPosition.Z;
                rightCleave.offX = jumpTargetPosition.X + 30f;
                rightCleave.offY = jumpTargetPosition.Z;
            }

            sched = new TickScheduler(() =>
            {
                if (Controller.TryGetElementByName("CloneCleave", out var rightCleave))
                {
                    rightCleave.Enabled = false;
                }
            }, 2000);
        }, 7000);
    }

    private void RightCloneCleave()
    {
        if (Controller.TryGetElementByName("CloneCleave", out var rightCleave))
        {
            rightCleave.Enabled = true;
            rightCleave.refX = jumpTargetPosition.X;
            rightCleave.refY = jumpTargetPosition.Z;
            rightCleave.offX = jumpTargetPosition.X + 30f;
            rightCleave.offY = jumpTargetPosition.Z;
        }

        sched?.Dispose();
        sched = new TickScheduler(() =>
        {
            if (Controller.TryGetElementByName("CloneCleave", out var rightCleave))
            {
                rightCleave.Enabled = false;
            }
            if (Controller.TryGetElementByName("CloneCleave", out var leftCleave))
            {
                leftCleave.Enabled = true;
                leftCleave.refX = jumpTargetPosition.X;
                leftCleave.refY = jumpTargetPosition.Z;
                leftCleave.offX = jumpTargetPosition.X - 30f;
                leftCleave.offY = jumpTargetPosition.Z;
            }

            sched = new TickScheduler(() =>
            {
                if (Controller.TryGetElementByName("CloneCleave", out var leftCleave))
                {
                    leftCleave.Enabled = false;
                }
            }, 2000);
        }, 7000);
    }

    private void CheckBossBuffs()
    {
        var boss = BlackCat;
        if (boss == null) { return; }

        if (boss.StatusList.Any(status => status.StatusId == 4050))
        {
            movement = "Right";
        }
        else if (boss.StatusList.Any(status => status.StatusId == 4051))
        {
            movement = "Left";
        }
        if (boss.StatusList.Any(status => status.StatusId == 4048))
        {
            attack = "Cleave";
        }
        else if (boss.StatusList.Any(status => status.StatusId == 4049))
        {
            attack = "Claw";
        }
        if (!string.IsNullOrEmpty(movement) && !string.IsNullOrEmpty(attack))
        {
            if (clonePositions.Count == 7)
            {
                ShowArrows(6);
            }
            else if (clonePositions.Count == 8)
            {
                ShowArrows(7);
            }
        }
        if (string.IsNullOrEmpty(movement) && !string.IsNullOrEmpty(attack))
        {
            if (movement == "" && attack == "Cleave")
            {
                HandleCloneAttacks();
            }
        }
    }

    private void ShowArrows(int cloneNumber)
    {
        if (clonePositions[cloneNumber].Z > 100)
        {
            if (movement == "Left")
            {
                if (Controller.TryGetElementByName($"SArrowLeft", out var arrow))
                {
                    arrow.Enabled = true;
                }
            }
            else if (movement == "Right")
            {
                if (Controller.TryGetElementByName($"SArrowRight", out var arrow))
                {
                    arrow.Enabled = true;
                }
            }
        }
        else
        {
            if (movement == "Left")
            {
                if (Controller.TryGetElementByName($"NArrowLeft", out var arrow))
                {
                    arrow.Enabled = true;
                }
            }
            else if (movement == "Right")
            {
                if (Controller.TryGetElementByName($"NArrowRight", out var arrow))
                {
                    arrow.Enabled = true;
                }
            }
        }
    }

    private void HideArrows(int cloneNumber)
    {
        if (clonePositions[cloneNumber].Z > 100)
        {
            if (Controller.TryGetElementByName($"SArrowLeft", out var arrow1))
            {
                arrow1.Enabled = false;
            }
            if (Controller.TryGetElementByName($"SArrowRight", out var arrow2))
            {
                arrow2.Enabled = false;
            }
        }
        else
        {
            if (Controller.TryGetElementByName($"NArrowLeft", out var arrow1))
            {
                arrow1.Enabled = false;
            }
            if (Controller.TryGetElementByName($"NArrowRight", out var arrow2))
            {
                arrow2.Enabled = false;
            }
        }
    }

    private void ShowCleaves()
    {
        if (LeftFirst)
        {
            if (Controller.TryGetElementByName("Cleave", out var e))
            {
                e.Enabled = true;
                e.refActorDataID = 17193;
                e.AdditionalRotation = 4.712389f;
            }
        }
        else
        {
            if (Controller.TryGetElementByName("Cleave", out var e))
            {
                e.Enabled = true;
                e.refActorDataID = 17193;
                e.AdditionalRotation = 1.5707964f;
            }
        }

    }

    public override void OnUpdate()
    {
        if (clonePositions.Count == 9)
        {
            sched = new TickScheduler(() =>
            {
                HideArrows(8);
            }, 16000);
        }
        if (clonePositions.Count == 10)
        {
            sched = new TickScheduler(() =>
            {
                HideArrows(9);
            }, 16000);
        }
        if (clonePositions.Count == 0)
        {
            clonePositions.Clear();
            jumpTargetPosition = new Vector3(0, 0, 0);
            HideArrows(8);
            HideArrows(9);
        }
    }

    public override void OnReset()
    {
        IsLeapingCleave = false;
        LeftFirst = false;
        RightFirst = false;
        sched?.Dispose();
        clonePositions.Clear();
        jumpTargetPosition = new Vector3(0, 0, 0);
        HideArrows(8);
        HideArrows(9);
        if (Controller.TryGetElementByName($"Cleaves", out var e1))
        {
            e1.Enabled = false;
        }
        if (Controller.TryGetElementByName($"CloneCleave", out var e2))
        {
            e2.Enabled = false;
        }
    }
}