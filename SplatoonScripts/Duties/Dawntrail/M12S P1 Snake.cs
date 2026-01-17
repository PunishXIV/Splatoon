using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Element = Splatoon.Element;
using static Splatoon.Splatoon;
using System;
using ECommons.ImGuiMethods;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M12S_P1_Snake : SplatoonScript
{
    public override Metadata Metadata { get; } = new(5, "NightmareXIV, Garume");
    public override HashSet<uint>? ValidTerritories { get; } = [1327];

    public enum Debuff
    {
        Pos1 = 3004,
        Pos2 = 3005,
        Pos3 = 3006,
        Pos4 = 3451,
        Alpha = 4752,
        Beta = 4754,
        AfterBeta = 4755,
    }

    public enum DebuffGroup
    {
        None,
        Alpha,
        Beta,
    }

    public enum CutSide
    {
        Outward,
        Inward,
    }

    public enum TowerType
    {
        Red,
        Black,
    }

    public sealed class PlayerData
    {
        public uint EntityId { get; }
        public string Name { get; private set; } = "";
        public DebuffGroup Group { get; private set; } = DebuffGroup.None;
        public int PosNumber { get; private set; }
        public int CutRotation { get; private set; }
        public CutSide CutSide { get; private set; }
        public TowerType? SoakTowerType { get; private set; }
        public int? TowerOrder { get; private set; }
        public int? SoakRotation { get; private set; }

        int LastPosNumber;
        DebuffGroup LastGroup = DebuffGroup.None;

        public PlayerData(uint entityId)
        {
            EntityId = entityId;
        }

        public void UpdateFrom(IBattleChara b, Func<IBattleChara, Debuff, bool> hasStatus)
        {
            Name = b.Name.ToString();
            var group = hasStatus(b, Debuff.Alpha) ? DebuffGroup.Alpha : hasStatus(b, Debuff.Beta) ? DebuffGroup.Beta : DebuffGroup.None;
            var pos = GetPosNumber(b, hasStatus);
            if(pos != 0)
            {
                LastPosNumber = pos;
                PosNumber = pos;
            }
            else
            {
                PosNumber = LastPosNumber;
            }

            if(group != DebuffGroup.None)
            {
                LastGroup = group;
                Group = group;
            }
            else
            {
                Group = LastGroup;
            }

            ComputeAssignments();
        }

        void ComputeAssignments()
        {
            if(PosNumber == 0 || Group == DebuffGroup.None)
            {
                CutRotation = 0;
                CutSide = CutSide.Outward;
                SoakTowerType = null;
                TowerOrder = null;
                SoakRotation = null;
                return;
            }

            CutRotation = PosNumber + 1;
            CutSide = Group == DebuffGroup.Alpha ? CutSide.Outward : CutSide.Inward;
            SoakTowerType = Group == DebuffGroup.Alpha ? TowerType.Red : TowerType.Black;
            TowerOrder = PosNumber switch
            {
                1 => 3,
                2 => 4,
                3 => 1,
                4 => 2,
                _ => null,
            };
            SoakRotation = TowerOrder != null ? TowerOrder + 2 : null;
        }

        static int GetPosNumber(IBattleChara b, Func<IBattleChara, Debuff, bool> hasStatus)
        {
            if(hasStatus(b, Debuff.Pos1)) return 1;
            if(hasStatus(b, Debuff.Pos2)) return 2;
            if(hasStatus(b, Debuff.Pos3)) return 3;
            if(hasStatus(b, Debuff.Pos4)) return 4;
            return 0;
        }
    }

    public sealed class BlackTowerInfo
    {
        public uint EntityId;
        public Vector3 Position;
        public int SpawnRotation;
        public bool Removed;
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("TowerWaiting", """{"Name":"","refX":96.0,"refY":96.0,"radius":2,"Donut":0.5,"color":3355508719,"fillIntensity":0.281}""");
        Controller.RegisterElementFromCode("TowerGet", """
            {"Name":"","refX":96.0,"refY":96.0,"radius":2,"Donut":0.5,"color":3357277952,"fillIntensity":1.0,"thicc":4.0,"tether":true}
            """);
        Controller.RegisterElementFromCode("CutGuide", """{"Name":"","type":0,"radius":2.0,"thicc":10.0,"tether":true}""");
        Controller.RegisterElementFromCode("ExitDoor", """{"Name":"","type":1,"offY":10.0,"radius":2.35,"refActorDataID":19195,"refActorComparisonType":3,"includeRotation":true,"color":4294967040,"fillIntensity":0.25,"thicc":3.0}""");
        Controller.RegisterElementFromCode("CutCountdown", """{"Name":"","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"3","refActorType":1}""");
    }

    bool MechanicActive => Controller.GetPartyMembers().Any(x => x.StatusList.Any(s => HasStatus(x, Debuff.Pos4)));
    List<Vector3> Towers = [];
    List<BlackTowerInfo> BlackTowers = [];
    Debuff? MyDebuff = null;
    float LastExitRot = 0f;
    int RotationCount = 0;
    bool CoilSeen = false;
    int LastExitSector = -1;
    bool RotationStarted = false;
    Dictionary<uint, PlayerData> PlayerDatas = new();
    const float CenterX = 100f;
    const float CenterZ = 100f;
    const float EmergencyExitRadius = 12f;
    const float ExitCenterRadius = 4f;

    public override void OnReset()
    {
        Towers.Clear(); BlackTowers.Clear(); MyDebuff = null; LastExitRot = 0f; RotationCount = 0; CoilSeen = false;
        RotationStarted = false;
        LastExitSector = -1; PlayerDatas.Clear();
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        UpdateCutCountdown();
        var exitActor = GetExitActor();
        if(exitActor != null )
        {
            if (!RotationStarted)
                RotationStarted = FakeParty.Get().All(x =>
                    x.StatusList.FirstOrDefault(s => s.StatusId == (uint)Debuff.Alpha) is { RemainingTime: < 22 } ||
                    x.StatusList.FirstOrDefault(s => s.StatusId == (uint)Debuff.Beta) is { RemainingTime: < 22 });
            else
            {
                if (!CoilSeen)
                {
                    CoilSeen = true;
                    LastExitRot = exitActor.Rotation;
                    LastExitSector = GetExitSector(exitActor.Rotation);
                    RotationCount = 0;
                }
                else
                {
                    var sector = GetExitSector(exitActor.Rotation);
                    if (sector != LastExitSector)
                    {
                        RotationCount = Math.Min(RotationCount + 1, 7);
                        LastExitRot = exitActor.Rotation;
                        LastExitSector = sector;
                    }
                }
            }
        }
        else
        {
            CoilSeen = false;
            LastExitSector = -1;
        }

        var activeBlackCastingIds = new HashSet<uint>();
        foreach(var x in Svc.Objects.OfType<IBattleNpc>())
        {
            if(x.NameId == 14378 && x.IsCasting(46262) && !Towers.Any(a => a.ApproximatelyEquals(x.Position, 0.5f)))
            {
                Towers.Add(x.Position);
            }
            if(x.NameId == 14381 && x.IsCasting(46263) && x.CurrentCastTime >= 2.5f)
            {
                for(int i = 0; i < Towers.Count; i++)
                {
                    Vector3 t = Towers[i];
                    if(t.ApproximatelyEquals(x.Position, 0.5f))
                    {
                        Towers[i] = default;
                    }
                }
            }
            if(x.IsCasting(46259))
            {
                activeBlackCastingIds.Add(x.EntityId);
                if(BlackTowers.All(a => a.EntityId != x.EntityId))
                {
                    BlackTowers.Add(new BlackTowerInfo
                    {
                        EntityId = x.EntityId,
                        Position = x.Position,
                        SpawnRotation = RotationCount,
                        Removed = false,
                    });
                }
            }
        }
        UpdateBlackTowerRemovals(activeBlackCastingIds);

        UpdatePlayerDatas();
        var me = GetMyPlayerData();

        if(RotationCount >= 7 && IsInsideCenter(EmergencyExitRadius) && exitActor != null)
        {
            var dx = exitActor.Position.X - CenterX;
            var dz = exitActor.Position.Z - CenterZ;
            if(dx * dx + dz * dz <= ExitCenterRadius * ExitCenterRadius)
            {
                var cutGuide = Controller.GetElementByName("CutGuide");
                if(cutGuide != null)
                {
                    cutGuide.color = GradientColor.Get(C.NowColorA, C.NowColorB).ToUint();
                    cutGuide.SetRefPosition(GetCutPoint(exitActor, true));
                    cutGuide.Enabled = true;
                }
                return;
            }
        }

        if(me != null && me.Group != DebuffGroup.None)
        {
            var close = Controller.GetElementByName("Close");
            var cutGuide = Controller.GetElementByName("CutGuide");
            var blackGet = Controller.GetElementByName("TowerGet");
            var nextColor = C.NextColor.ToUint();
            var nowColor = GradientColor.Get(C.NowColorA, C.NowColorB).ToUint();
            MyDebuff = me.PosNumber switch { 1 => Debuff.Pos1, 2 => Debuff.Pos2, 3 => Debuff.Pos3, 4 => Debuff.Pos4, _ => MyDebuff };
            if(me.Group == DebuffGroup.Alpha)
            {
                ShowCutIfDue(me, cutGuide, nextColor, nowColor);
                if(me.TowerOrder != null && me.SoakRotation != null)
                {
                    int idx = me.TowerOrder.Value - 1;
                    if(Towers.SafeSelect(idx) != default)
                    {
                        var forceNow = me.PosNumber is 3 or 4;
                        var ready = RotationCount >= me.SoakRotation;
                        var forceGet = forceNow && RotationStarted;
                        var afterCut = (me.PosNumber is 1 or 2) && RotationCount >= me.CutRotation + 1;
                        var useGet = ready || forceGet || afterCut;
                        var te = useGet ? Controller.GetElementByName("TowerGet") : Controller.GetElementByName("TowerWaiting");
                        if(te != null)
                        {
                            te.color = useGet ? nowColor : nextColor;
                            te.SetRefPosition(Towers.SafeSelect(idx));
                            te.Enabled = true;
                        }
                    }
                    else if(me.PosNumber == 3 && GetRemainingTime(Debuff.Alpha) < 10f)
                    {
                        close.refActorObjectID = Controller.GetPartyMembers().FirstOrDefault(x => HasStatus(x, Debuff.Pos3) && HasStatus(x, Debuff.Beta))?.EntityId ?? 0;
                    }
                }
            }
            else if(me.Group == DebuffGroup.Beta)
            {
                var betaRemaining = GetRemainingTime(Debuff.Beta);
                if ((betaRemaining < 3f && betaRemaining != 0f) || HasStatus(Debuff.AfterBeta))
                {
                    ShowCutIfDue(me, cutGuide, nextColor, nowColor);
                }
                if(me.TowerOrder != null)
                {
                    int idx = me.TowerOrder.Value - 1;
                    if(me.SoakRotation != null && RotationCount > me.SoakRotation)
                    {
                        // done: don't show after soak rotation
                    }
                    else if(IsBlackTowerActive(idx))
                    {
                        if(blackGet != null)
                        {
                            blackGet.color = nowColor;
                            blackGet.SetRefPosition(GetBlackTowerPos(idx));
                            blackGet.Enabled = true;
                        }
                    }
                }
            }
        }
    }

    void UpdatePlayerDatas()
    {
        foreach(var p in Controller.GetPartyMembers())
        {
            if(!PlayerDatas.TryGetValue(p.EntityId, out var pd))
            {
                pd = new PlayerData(p.EntityId);
                PlayerDatas[p.EntityId] = pd;
            }
            pd.UpdateFrom(p, HasStatus);
        }
    }

    PlayerData? GetMyPlayerData()
    {
        return BasePlayer != null && PlayerDatas.TryGetValue(BasePlayer.EntityId, out var pd) ? pd : null;
    }

    Vector3 GetBlackTowerPos(int idx)
    {
        if(idx < 0 || idx >= BlackTowers.Count) return default;
        return BlackTowers[idx].Position;
    }

    bool IsBlackTowerActive(int idx)
    {
        if(idx < 0 || idx >= BlackTowers.Count) return false;
        var bt = BlackTowers[idx];
        return !bt.Removed && bt.Position != default;
    }

    void UpdateBlackTowerRemovals(HashSet<uint> activeBlackCastingIds)
    {
        for(int i = 0; i < BlackTowers.Count; i++)
        {
            var bt = BlackTowers[i];
            if(!bt.Removed && !activeBlackCastingIds.Contains(bt.EntityId))
            {
                bt.Removed = true;
                BlackTowers[i] = bt;
            }
        }
    }

    void ShowCutIfDue(PlayerData pd, Element? guide, uint nextColor, uint nowColor)
    {
        if(guide == null) return;
        var exit = GetExitActor();
        if(exit == null) return;
        if(pd.CutRotation <= 0) return;
        int targetRot = pd.CutRotation;
        if(RotationCount > targetRot) return;
        if(RotationCount == targetRot - 1)
        {
            guide.color = nextColor; guide.SetRefPosition(GetCenter()); guide.Enabled = true; return;
        }
        if(RotationCount == targetRot)
        {
            bool outward = pd.CutSide == CutSide.Outward;
            guide.color = nowColor; guide.SetRefPosition(GetCutPoint(exit, outward)); guide.Enabled = true;
        }
    }

    IBattleNpc? GetExitActor() => Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.DataId == 19195);

    Vector3 GetCutPoint(IBattleNpc exit, bool outward)
    {
        float yaw = exit.Rotation + (outward ? 0 : MathF.PI);
        var dir = new Vector3(MathF.Sin(yaw), 0, MathF.Cos(yaw));
        return GetCenter() + dir * (outward ? 12f : 8f);
    }

    Vector3 GetCenter() => new(CenterX, BasePlayer?.Position.Y ?? 0f, CenterZ);

    bool IsInsideCenter(float radius)
    {
        if(BasePlayer == null) return false;
        var dx = BasePlayer.Position.X - CenterX;
        var dz = BasePlayer.Position.Z - CenterZ;
        return dx * dx + dz * dz <= radius * radius;
    }

    int GetExitSector(float rot) => ((int)MathF.Floor((NormalizeAngle(rot) + MathF.PI / 4f) / (MathF.PI / 2f))) % 4;

    float NormalizeAngle(float a) { a %= MathF.Tau; return a < 0 ? a + MathF.Tau : a; }

    bool HasStatus(Debuff d)
    {
        return BasePlayer.StatusList.Any(x => x.StatusId == (uint)d);
    }

    float GetRemainingTime(Debuff d)
    {
        return BasePlayer.StatusList.TryGetFirst(x => x.StatusId == (uint)d, out var status)?status.RemainingTime:0f;
    }

    bool HasStatus(IBattleChara b, Debuff d)
    {
        return b.StatusList.Any(x => x.StatusId == (uint)d);
    }

    void UpdateCutCountdown()
    {
        var countdown = Controller.GetElementByName("CutCountdown");
        if(countdown == null || BasePlayer == null) return;
        if(C.CutCountdownSeconds <= 0f)
        {
            countdown.Enabled = false;
            return;
        }
        float remaining = 0f;
        if(HasStatus(Debuff.Alpha))
        {
            remaining = GetRemainingTime(Debuff.Alpha);
        }
        else if(HasStatus(Debuff.Beta))
        {
            remaining = GetRemainingTime(Debuff.Beta);
        }

        if(remaining > 0f && remaining <= C.CutCountdownSeconds)
        {
            countdown.overlayText = MathF.Ceiling(remaining).ToString();
            countdown.Enabled = true;
        }
        else
        {
            countdown.Enabled = false;
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Separator();
        if(ImGui.CollapsingHeader("Guide (JP)"))
        {
            ImGui.TextWrapped("3→4→1→2の順に塔を踏む処理です。ほとんどナビしますが、βはαより猶予が少ないため事前理解を強く推奨します。また、中にいる際にナビが出ない場合は中央に立ってください。");
        }
        if (ImGui.CollapsingHeader("Guide (EN)"))
        {
            ImGui.TextWrapped("Tower order is 3 -> 4 -> 1 -> 2. This script navigates most actions, but Beta has less margin than Alpha, so understanding the mechanic beforehand is strongly recommended. If you are inside and no guide appears, stand in the center.");
        }
        ImGui.Text("Colors");
        ImGui.ColorEdit4("Next (Waiting)", ref C.NextColor, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Now Color A", ref C.NowColorA, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Now Color B", ref C.NowColorB, ImGuiColorEditFlags.NoInputs);
        ImGui.SetNextItemWidth(150);
        ImGui.DragFloat("Cut countdown seconds", ref C.CutCountdownSeconds, 0.1f, 0f, 10f);

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text($"MechanicActive: {MechanicActive}");
            ImGui.Text($"CoilSeen: {CoilSeen}, Rotations: {RotationCount}, ExitFound: {GetExitActor()!=null}, ExitSector: {LastExitSector}");
            ImGui.Text($"LastExitRot rad: {LastExitRot:F2}  deg: {LastExitRot * 180f / MathF.PI:F1}");
            ImGui.Text($"MyDebuff: {MyDebuff?.ToString() ?? "none"}");
            ImGui.Text($"Alpha rem: {GetRemainingTime(Debuff.Alpha):F1}s  Beta rem: {GetRemainingTime(Debuff.Beta):F1}s");
            ImGui.Text($"Pos1:{HasStatus(Debuff.Pos1)} Pos2:{HasStatus(Debuff.Pos2)} Pos3:{HasStatus(Debuff.Pos3)} Pos4:{HasStatus(Debuff.Pos4)}");
            var me = GetMyPlayerData();
            if(me != null)
            {
                ImGui.Text($"Me: {me.Name} {me.Group} Pos{me.PosNumber} Cut{me.CutRotation} {me.CutSide} Tower{me.TowerOrder} SoakRot{me.SoakRotation}");
            }
            if(PlayerDatas.Count > 0)
            {
                ImGui.Text($"Players: {PlayerDatas.Count}");
                foreach(var pd in PlayerDatas.Values)
                {
                    ImGui.Text($" {pd.Name}: {pd.Group} Pos{pd.PosNumber} Cut{pd.CutRotation} {pd.CutSide} Tower{pd.TowerOrder} SoakRot{pd.SoakRotation}");
                }
            }
        }
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public Vector4 NextColor = ImGuiColors.DalamudYellow;
        public Vector4 NowColorA = 0xFF00FF00.ToVector4();
        public Vector4 NowColorB = 0xFF0000FF.ToVector4();
        public float CutCountdownSeconds = 3f;
    }
}
