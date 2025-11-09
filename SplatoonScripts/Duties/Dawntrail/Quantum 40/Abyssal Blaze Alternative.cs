using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Schedulers;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Quantum40;
internal unsafe class Abyssal_Blaze_Alternative : SplatoonScript
{
    #region types
    private enum CastAbyssalBlaze
    {
        None = 0,
        AbyssalBlazeVertical,
        AbyssalBlazeHorizontal,
    }

    private enum PositionType
    {
        None = 0,
        LeftUp,
        RightUp,
        LeftDown,
        RightDown,
    }

    private class PositionInfo
    {
        public Vector3 Position;
        public PositionType positionType;
        public PositionInfo(Vector3 position, PositionType type)
        {
            Position = position;
            positionType = type;
        }
    }
    #endregion

    #region constants
    private readonly IReadOnlyList<PositionInfo> positionInfos = new List<PositionInfo>()
    {
        new PositionInfo(new Vector3(-605.93f, 0.0f, -311.806f), PositionType.LeftUp),
        new PositionInfo(new Vector3(-594.030f, 0.0f, -311.806f), PositionType.RightUp),
        new PositionInfo(new Vector3(-605.93f, 0.0f, -288.206f), PositionType.LeftDown),
        new PositionInfo(new Vector3(-594.030f, 0.0f, -288.206f), PositionType.RightDown),
    };

    private const uint AbyssalBlazeNpcId = 0x1EBE70;
    #endregion

    #region public fields
    public override HashSet<uint>? ValidTerritories { get; } = [1311];
    public override Metadata? Metadata => new(1, "redmoon");
    #endregion

    #region private fields
    private List<IGameObject> _rightObjects = new List<IGameObject>();
    private CastAbyssalBlaze _firstCastedBlaze = CastAbyssalBlaze.None;
    private bool _isLock = false;
    private bool _isShowed = false;
    private int _explosionsCount = 0;
    private int _gimmickCount = 0;
    private int _aoeCount = 0;
    #endregion

    #region overrides
    public override void OnSetup()
    {
        Controller.RegisterElement($"guide", new Splatoon.Element(0)
        {
            radius = 1.07f,
            thicc = 10f
        });
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(_firstCastedBlaze == CastAbyssalBlaze.None) return;

        if(castId is 44118 && _gimmickCount == 2)
        {
            _isShowed = true;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!set.Action.HasValue) return;

        var blaze = set.Action.Value.RowId switch
        {
            35363 or 44798 or 44800 => CastAbyssalBlaze.AbyssalBlazeVertical,
            35364 or 44799 or 44797 => CastAbyssalBlaze.AbyssalBlazeHorizontal,
            _ => CastAbyssalBlaze.None,
        };

        if(blaze != CastAbyssalBlaze.None)
        {
            if(_firstCastedBlaze == CastAbyssalBlaze.None)
            {
                WormReset();
                _firstCastedBlaze = blaze;
            }
            else
            {
                _isLock = true;
            }
        }

        // Exprotion
        if(set.Action.Value.RowId is 44119)
        {
            _explosionsCount++;
            if(_explosionsCount >= 70)
            {
                WormReset();
                _gimmickCount++;
            }
        }

        if(set.Action.Value.RowId is 44122)
        {
            _aoeCount++;
        }

        if(_firstCastedBlaze != CastAbyssalBlaze.None)
        {
            if(set.Action.Value.RowId is 44139 && _gimmickCount == 0) _isShowed = true;
            if(_aoeCount >= 12 && _gimmickCount == 1) _isShowed = true;
            if(set.Action.Value.RowId is 44126 && _gimmickCount == 3) _isShowed = true;
        }
    }

    public override void OnObjectCreation(nint newObjectPtr)
    {
        _ = new TickScheduler(() =>
        {
            if(_firstCastedBlaze == CastAbyssalBlaze.None) return;
            var obj = Svc.Objects.Where(o => o.Address == newObjectPtr).FirstOrDefault();
            if(obj == null) return;
            if(obj.BaseId == AbyssalBlazeNpcId && !_isLock && obj.Position.X > -600.0f)
            {
                _rightObjects.Add(obj);
            }
        });
    }

    public override void OnUpdate()
    {
        if(_firstCastedBlaze == CastAbyssalBlaze.None)
        {
            WormReset();
            return;
        }

        if(_rightObjects.Count == 0) return;

        var position = (_firstCastedBlaze, _rightObjects.Count) switch
        {
            (CastAbyssalBlaze.AbyssalBlazeVertical, 2) => PositionType.RightDown,
            (CastAbyssalBlaze.AbyssalBlazeVertical, 3) => PositionType.LeftDown,
            (CastAbyssalBlaze.AbyssalBlazeHorizontal, 2) => PositionType.LeftUp,
            (CastAbyssalBlaze.AbyssalBlazeHorizontal, 3) => PositionType.RightUp,
            _ => PositionType.None,
        };

        var shouldGoToPos = positionInfos.FirstOrDefault(p => p.positionType == position);
        if(shouldGoToPos == null) return;
        if(!_isShowed) // Circle guide only
        {
            if(Controller.GetRegisteredElements().TryGetValue("guide", out var element))
            {
                element.SetRefPosition(shouldGoToPos.Position);
                element.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
                element.tether = false;
                element.Enabled = true;
            }
        }
        else // Tether guide
        {
            if(Controller.GetRegisteredElements().TryGetValue("guide", out var element))
            {
                element.SetRefPosition(shouldGoToPos.Position);
                element.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
                element.tether = true;
                element.Enabled = true;
            }
        }
    }

    public override void OnReset()
    {
        _gimmickCount = 0;
        WormReset();
    }
    #endregion

    #region private methods
    private void WormReset()
    {
        _firstCastedBlaze = CastAbyssalBlaze.None;
        _isLock = false;
        _rightObjects.Clear();
        _explosionsCount = 0;
        _aoeCount = 0;
        _isShowed = false;
        Controller.GetRegisteredElements().Each(e => e.Value.Enabled = false);
    }

    public override void OnSettingsDraw()
    {
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text($"First Casted Blaze: {_firstCastedBlaze}");
            ImGui.Text($"Exprotion Count: {_explosionsCount}");
            ImGui.Text($"Gimick Count: {_gimmickCount}");
            ImGui.Text($"AOE Count: {_aoeCount}");
            ImGui.Text($"Is Showed: {_isShowed}");
            for(int i = 0; i < _rightObjects.Count; i++)
            {
                var obj = _rightObjects[i];
                ImGui.Text($"Right Object {i}: {obj.Name} ({obj.Position.X}, {obj.Position.Y}, {obj.Position.Z})");
            }

            if(ImGui.Button("Reset"))
            {
                WormReset();
            }
        }
    }
    #endregion
}
