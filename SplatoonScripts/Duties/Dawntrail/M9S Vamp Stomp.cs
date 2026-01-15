using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M9S_Vamp_Stomp : SplatoonScript
{
    /*
    * Constants and Types
    */
    #region Constants and Types
    private enum ClockWise
    {
        None,
        Clockwise,
        CounterClockwise
    }
    #endregion

    /*
     * Public Fields
     */
    #region Public Fields
    public override HashSet<uint>? ValidTerritories => [1321];
    public override Metadata? Metadata => new(2, "Redmoon");
    #endregion

    /*
     * Private Fields
     */
    #region Private Fields
    private bool _gimmickActive = false;
    private bool _setted = false;
    private int _exprosionCount = 0;
    private List<(uint, Vector3)> _sortedBaseObj = new();

    private Config C => Controller.GetConfig<Config>();

    private IPlayerCharacter BasePlayer
    {
        get
        {
            if(C.basePlayerOverride == "") return Player.Object;
            if(!Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.DutyRecorderPlayback]) return Player.Object;
            return Svc.Objects.OfType<IPlayerCharacter>()
                .FirstOrDefault(x => x.Name.ToString().EqualsIgnoreCase(C.basePlayerOverride)) ?? Player.Object;
        }
    }
    #endregion

    /*
     * Public Methods
     */
    #region Public Methods
    public override void OnSetup()
    {
        for(int i = 0; i < 10; i++)
        {
            Controller.RegisterElement($"AOE{i}", new Element(0)
            {
                radius = 8f,
            });
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 45898)
        {
            _gimmickActive = true;
            var list = Svc.Objects.Where(x => x.BaseId == 0x4C2F).OrderBy(x =>
            {
                var dx = x.Position.X - 100f;
                var dz = x.Position.Z - 100f;
                return dx * dx + dz * dz;
            }).ToList();
            _sortedBaseObj.Clear();
            for(int i = 0; i < list.Count; i++)
            {
                _sortedBaseObj.Add((list[i].EntityId, list[i].Position));
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!_gimmickActive) return;
        if(set.Action.Value.RowId == 45898)
        {
            Controller.Schedule(() =>
            {
                var objList = Svc.Objects.Where(x => x.BaseId == 0x4C2F).OrderBy(x =>
                {
                    var dx = x.Position.X - 100f;
                    var dz = x.Position.Z - 100f;
                    return dx * dx + dz * dz;
                }).ToList();
                int j = 0;
                for(int i = 0; i < objList.Count; i++)
                {
                    var baseObj = _sortedBaseObj.Where(x => x.Item1 == objList[i].EntityId).FirstOrDefault();
                    if(baseObj == default) return;
                    var rotatedPos = Vector3.Zero;
                    ClockWise rotation = CheckRotateAroundCenterXZ(baseObj.Item2, objList[i].Position);
                    PluginLog.Information($"BasePos:{baseObj.Item2} NewPos:{objList[i].Position} Rotation:{rotation}");
                    // 近い2匹 45度ずつ回転
                    if(i < 2)
                    {
                        rotatedPos = RotateAroundCenterXZ(baseObj.Item2, 86f, rotation);
                    }
                    // 更に近い3匹 90度ずつ回転
                    else if(i < 5)
                    {
                        rotatedPos = RotateAroundCenterXZ(baseObj.Item2, 90f, rotation);
                    }
                    else
                    {
                        rotatedPos = RotateAroundCenterXZ(baseObj.Item2, 89f, rotation);
                    }
                    if(Controller.TryGetElementByName($"AOE{j}", out var element)) element.SetRefPosition(rotatedPos);
                    j++;
                }

                for(int i = 0; i < 2; i++)
                {
                    if(Controller.TryGetElementByName("AOE" + i, out var element)) element.Enabled = true;
                }
            }, 1000);
        }
        else if(set.Action.Value.RowId == 45941)
        {
            if(Controller.TryGetElementByName("AOE" + _exprosionCount, out var element))
            {
                element.Enabled = false;
            }
            _exprosionCount++;
            if(_exprosionCount == 2)
            {
                for(int i = 2; i < 5; i++)
                {
                    if(Controller.TryGetElementByName("AOE" + i, out var element2)) element2.Enabled = true;
                }
            }
            else if(_exprosionCount == 5)
            {
                for(int i = 5; i < 10; i++)
                {
                    if(Controller.TryGetElementByName("AOE" + i, out var element3)) element3.Enabled = true;
                }
            }
            else if(_exprosionCount == 10)
            {
                this.OnReset();
            }
        }
    }

    public class Config : IEzConfig
    {
        public string basePlayerOverride;
    }

    public override void OnSettingsDraw()
    {
        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.SetNextItemWidth(200);
            ImGui.InputText("Player override", ref C.basePlayerOverride, 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if(ImGui.BeginCombo("Select..", "Select..."))
            {
                foreach(var x in Svc.Objects.OfType<IPlayerCharacter>())
                    if(ImGui.Selectable(x.GetNameWithWorld()))
                        C.basePlayerOverride = x.Name.ToString();
                ImGui.EndCombo();
            }

            ImGui.Text($"Gimmick Active: {_gimmickActive}");
            ImGui.Text($"Explosion Count: {_exprosionCount}");
            ImGui.Text($"sortedBaseObj Count: {_sortedBaseObj.Count}");
        }
    }

    public override void OnReset()
    {
        _gimmickActive = false;
        _exprosionCount = 0;
        _setted = false;
        _sortedBaseObj.Clear();
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }
    #endregion

    /*
     * Private Methods
     */
    #region Private Methods
    static bool IsNearZeroAngle(float rad, float toleranceRad)
    {
        // 角度を -π..π に正規化（0付近なら値が小さくなる）
        float norm = MathF.Atan2(MathF.Sin(rad), MathF.Cos(rad));
        return MathF.Abs(norm) <= toleranceRad;
    }

    static Vector3 RotateAroundCenterXZ(
    Vector3 position,
    float deg,
    ClockWise rotation)
    {
        if(rotation == ClockWise.None || deg == 0f)
            return position;

        const float centerX = 100f;
        const float centerZ = 100f;

        float signedDeg = (rotation != ClockWise.Clockwise)
            ? -deg
            : deg;

        float rad = MathHelper.DegToRad(signedDeg);

        // 中心からの相対座標
        float relX = position.X - centerX;
        float relZ = position.Z - centerZ;

        // XZ平面回転
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);

        float rotX = relX * cos - relZ * sin;
        float rotZ = relX * sin + relZ * cos;

        return new Vector3(
            centerX + rotX,
            position.Y,     // Yは保持
            centerZ + rotZ
        );
    }

    static ClockWise CheckRotateAroundCenterXZ(Vector3 basePos, Vector3 newPos)
    {
        const float centerX = 100f;
        const float centerZ = 100f;

        float ax = basePos.X - centerX;
        float az = basePos.Z - centerZ;
        float bx = newPos.X - centerX;
        float bz = newPos.Z - centerZ;

        // 両方ゼロベクトルっぽいなら回転方向は定義できない
        if((ax * ax + az * az) < 1e-6f || (bx * bx + bz * bz) < 1e-6f)
            return ClockWise.None;

        float cross = ax * bz - az * bx;
        float dot = ax * bx + az * bz;

        // 同一直線（cross≈0）なら、回転方向は決めにくいので None にする
        if(MathF.Abs(cross) < 1e-5f)
        {
            // dot>0 ならほぼ同方向（角度差0付近）、dot<0なら反対（180°付近）
            return ClockWise.None;
        }

        // ※ここは「直した」前提の対応にしてね
        // cross>0 は CCW、cross<0 は CW
        return (cross > 0f) ? ClockWise.Clockwise : ClockWise.CounterClockwise;
    }


    #endregion
}

