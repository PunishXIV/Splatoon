using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Schedulers;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Quantum40;
public unsafe class Abyssal_Blaze : SplatoonScript
{
    private enum CastAbyssalBlaze : uint
    {
        None = 0U,
        AbyssalBlazeVertical = 44075U,
        AbyssalBlazeHorizontal = 44076U,
    }

    private const uint AbyssalBlazeNpcId = 0x1EBE70;

    public override HashSet<uint>? ValidTerritories { get; } = [1311];
    public override Metadata? Metadata => new(2, "redmoon");

    private List<IGameObject> _firstObject = new List<IGameObject>();
    private List<IGameObject> _secondObject = new List<IGameObject>();
    private CastAbyssalBlaze _firstCastedBlaze = CastAbyssalBlaze.None;
    private CastAbyssalBlaze _secondCastedBlaze = CastAbyssalBlaze.None;
    private bool _isFirstCasted = false;
    private bool _isShowed = false;
    private int _exprotionCount = 0;
    private int _gimickCount = 0;
    private int _aoeCount = 0;

    public override void OnSetup()
    {
        for(int i = 0; i < (14 * 12); i++)
        {
            Controller.RegisterElement($"AOE{i}", new Splatoon.Element(0)
            {
                radius = 5.0f,
            });
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(_firstCastedBlaze == CastAbyssalBlaze.None) return;

        if(castId is 44118 && _gimickCount == 2)
        {
            _isShowed = true;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!set.Action.HasValue) return;

        if(set.Action.Value.RowId is 35363 or 35364 or 44798 or 44799 or 44800 or 44797) // Abyssal Blaze (Vertical/Horizontal) (Hard Mode)
        {
            if(!_isFirstCasted)
            {
                WormReset();
                _firstCastedBlaze = set.Action.Value.RowId switch
                {
                    35363 => CastAbyssalBlaze.AbyssalBlazeVertical,
                    44798 => CastAbyssalBlaze.AbyssalBlazeVertical,
                    44800 => CastAbyssalBlaze.AbyssalBlazeVertical, // Hard Mode
                    35364 => CastAbyssalBlaze.AbyssalBlazeHorizontal,
                    44799 => CastAbyssalBlaze.AbyssalBlazeHorizontal,
                    44797 => CastAbyssalBlaze.AbyssalBlazeHorizontal, // Hard Mode
                    _ => CastAbyssalBlaze.None,
                };
                _isFirstCasted = true;
            }
            else
            {
                _secondCastedBlaze = set.Action.Value.RowId switch
                {
                    35363 => CastAbyssalBlaze.AbyssalBlazeVertical,
                    44798 => CastAbyssalBlaze.AbyssalBlazeVertical,
                    44800 => CastAbyssalBlaze.AbyssalBlazeVertical, // Hard Mode
                    35364 => CastAbyssalBlaze.AbyssalBlazeHorizontal,
                    44799 => CastAbyssalBlaze.AbyssalBlazeHorizontal,
                    44797 => CastAbyssalBlaze.AbyssalBlazeHorizontal, // Hard Mode
                    _ => CastAbyssalBlaze.None,
                };
            }
        }

        // Exprotion
        if(set.Action.Value.RowId is 44119) //Abyssal Blaze Explosion
        {
            _exprotionCount++;
            if(_exprotionCount >= 70)
            {
                WormReset();
                _gimickCount++;
            }
        }

        if(set.Action.Value.RowId is 44122)
        {
            _aoeCount++;
        }

        if(_firstCastedBlaze != CastAbyssalBlaze.None)
        {
            if(set.Action.Value.RowId is 44139 && _gimickCount == 0) _isShowed = true;
            if(_aoeCount >= 12 && _gimickCount == 1) _isShowed = true;
        }
    }

    public override void OnObjectCreation(nint newObjectPtr)
    {
        _ = new TickScheduler(() =>
        {
            if(_firstCastedBlaze == CastAbyssalBlaze.None) return;
            var obj = Svc.Objects.Where(o => o.Address == newObjectPtr).FirstOrDefault();
            if(obj == null) return;
            if(obj.BaseId == AbyssalBlazeNpcId)
            {
                if(_secondCastedBlaze == CastAbyssalBlaze.None)
                {
                    if(!_firstObject.Contains(obj))
                        _firstObject.Add(obj);
                }
                else
                {
                    if(!_secondObject.Contains(obj))
                        _secondObject.Add(obj);
                }
            }
        });
    }

    public override void OnUpdate()
    {
        if(_firstCastedBlaze == CastAbyssalBlaze.None) return;
        if(!_isShowed) return;
        var cfg = Controller.GetConfig<Config>();


        int elementCount = 0;
        for(int i = 0; i < _firstObject.Count; i++)
        {
            var obj = _firstObject[i];
            var positions = CalculatePosition(obj, _firstCastedBlaze);
            for(int j = 0; j < positions.Count; j++)
            {
                if(Controller.TryGetElementByName($"AOE{elementCount}", out var element))
                {
                    element.SetRefPosition(positions[j]);
                    var color = element.color.ToVector4();
                    color.W = cfg.density / 255.0f;
                    element.color = color.ToUint();
                    element.Enabled = true;
                    elementCount++;
                }
            }
        }
        for(int i = 0; i < _secondObject.Count; i++)
        {
            var obj = _secondObject[i];
            var positions = CalculatePosition(obj, _secondCastedBlaze);
            for(int j = 0; j < positions.Count; j++)
            {
                if(Controller.TryGetElementByName($"AOE{elementCount}", out var element))
                {
                    element.SetRefPosition(positions[j]);
                    var color = element.color.ToVector4();
                    color.W = cfg.density / 255.0f;
                    element.color = color.ToUint();
                    element.Enabled = true;
                    elementCount++;
                }
            }
        }
    }

    public override void OnReset()
    {
        _gimickCount = 0;
        WormReset();
    }

    private void WormReset()
    {
        _firstCastedBlaze = CastAbyssalBlaze.None;
        _secondCastedBlaze = CastAbyssalBlaze.None;
        _isFirstCasted = false;
        _firstObject.Clear();
        _secondObject.Clear();
        _exprotionCount = 0;
        _aoeCount = 0;
        _isShowed = false;
        Controller.GetRegisteredElements().Each(e => e.Value.Enabled = false);
    }

    public class Config : IEzConfig
    {
        public int density = 70;
    }

    public override void OnSettingsDraw()
    {
        var cfg = Controller.GetConfig<Config>();
        ImGui.Text("AOE Density (default 100%)");
        ImGui.SliderInt("##density", ref cfg.density, 0, 255, $"{cfg.density}%");
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text($"First Casted: {_isFirstCasted}");
            ImGui.Text($"First Casted Blaze: {_firstCastedBlaze}");
            ImGui.Text($"Second Casted Blaze: {_secondCastedBlaze}");
            ImGui.Text($"First Object Count: {_firstObject.Count}");
            ImGui.Text($"Second Object Count: {_secondObject.Count}");
            ImGui.Text($"Exprotion Count: {_exprotionCount}");
            ImGui.Text($"Gimick Count: {_gimickCount}");
            ImGui.Text($"AOE Count: {_aoeCount}");
            ImGui.Text($"Is Showed: {_isShowed}");
            for(int i = 0; i < _firstObject.Count; i++)
            {
                var obj = _firstObject[i];
                ImGui.Text($"First Object {i}: {obj.Name} ({obj.Position.X}, {obj.Position.Y}, {obj.Position.Z})");
            }
            for(int i = 0; i < _secondObject.Count; i++)
            {
                var obj = _secondObject[i];
                ImGui.Text($"Second Object {i}: {obj.Name} ({obj.Position.X}, {obj.Position.Y}, {obj.Position.Z})");
            }

            if(ImGui.Button("Reset"))
            {
                _firstCastedBlaze = CastAbyssalBlaze.None;
                _secondCastedBlaze = CastAbyssalBlaze.None;
                _isFirstCasted = false;
                _firstObject.Clear();
                _secondObject.Clear();
                Controller.GetRegisteredElements().Each(e => e.Value.Enabled = false);
            }
        }
    }

    // オブジェクト位置から1つ、左右に６つずつ、計１３個の位置を計算して返す
    // Horizontalの場合は、オブジェクト位置から前後に６つずつ、計１３個の位置を計算して返す
    // Verticalの場合は、オブジェクト位置から左右に６つずつ、計１３個の位置を計算して返す
    // 中心から4m感覚で配置
    // angleではなくXZ平面での方向ベクトルを使う
    private List<Vector3> CalculatePosition(IGameObject obj, CastAbyssalBlaze blazeType)
    {
        List<Vector3> positions = new List<Vector3>();
        positions.Add(obj.Position);

        if(blazeType == CastAbyssalBlaze.AbyssalBlazeVertical)
        {
            for(int i = 1; i <= 6; i++)
            {
                positions.Add(positions[0] + new Vector3(0, 0, 4 * i));
                positions.Add(positions[0] - new Vector3(0, 0, 4 * i));
            }
        }
        else if(blazeType == CastAbyssalBlaze.AbyssalBlazeHorizontal)
        {
            for(int i = 1; i <= 6; i++)
            {
                positions.Add(positions[0] + new Vector3(4 * i, 0, 0));
                positions.Add(positions[0] - new Vector3(4 * i, 0, 0));
            }
        }

        // Xが-575 ~ -625, Zが-280 ~ -320の範囲を超えたら除外
        positions = positions.Where(p => p.X <= -575 && p.X >= -625 && p.Z <= -280 && p.Z >= -320).ToList();

        return positions;
    }
}
