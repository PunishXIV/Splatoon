using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Configuration;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon;
using Splatoon.Serializables;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public unsafe class P3_Dive_from_Grace : SplatoonScript
{
    private readonly Dictionary<string, Dictionary<int, Dictionary<DebuffType, List<PositionDelegate>>>>
        _baitPositions = new()
        {
            ["First"] = new Dictionary<int, Dictionary<DebuffType, List<PositionDelegate>>>
            {
                [1] =
                    new()
                    {
                        [DebuffType.HighJump] =
                        [
                            EastTowerPosition,
                            SouthTowerPosition,
                            WestTowerPosition
                        ],
                        [DebuffType.Spine] =
                        [
                            EastTowerPosition
                        ],
                        [DebuffType.Illusive] =
                        [
                            WestTowerPosition
                        ]
                    },
                [2] = new()
                {
                    [DebuffType.HighJump] =
                    [
                        NorthSafePosition
                    ],
                    [DebuffType.Spine] =
                    [
                        NorthSafePosition
                    ],
                    [DebuffType.Illusive] =
                    [
                        NorthSafePosition
                    ]
                },
                [3] = new()
                {
                    [DebuffType.HighJump] =
                    [
                        NorthSafePosition
                    ],
                    [DebuffType.Spine] =
                    [
                        NorthSafePosition
                    ],
                    [DebuffType.Illusive] =
                    [
                        NorthSafePosition
                    ]
                }
            },
            ["Second"] = new Dictionary<int, Dictionary<DebuffType, List<PositionDelegate>>>
            {
                [1] = new()
                {
                    [DebuffType.HighJump] =
                    [
                        NorthSafePosition
                    ],
                    [DebuffType.Spine] =
                    [
                        NorthSafePosition
                    ],
                    [DebuffType.Illusive] =
                    [
                        NorthSafePosition
                    ]
                },
                [2] = new()
                {
                    [DebuffType.HighJump] =
                    [
                        NorthWestSafePosition,
                        NorthEastSafePosition
                    ],
                    [DebuffType.Spine] =
                    [
                        NorthEastSafePosition
                    ],
                    [DebuffType.Illusive] =
                    [
                        NorthWestSafePosition
                    ]
                },
                [3] = new()
                {
                    [DebuffType.HighJump] =
                    [
                        EastTowerPosition,
                        SouthTowerPosition,
                        WestTowerPosition
                    ],
                    [DebuffType.Spine] =
                    [
                        EastTowerPosition
                    ],
                    [DebuffType.Illusive] =
                    [
                        WestTowerPosition
                    ]
                }
            },
            ["Third"] = new Dictionary<int, Dictionary<DebuffType, List<PositionDelegate>>>
            {
                [1] = new()
                {
                    [DebuffType.HighJump] =
                    [
                        NorthSafePosition,
                        NorthEastSafePosition,
                        NorthWestSafePosition
                    ],
                    [DebuffType.Spine] =
                    [
                        NorthEastSafePosition
                    ],
                    [DebuffType.Illusive] =
                    [
                        NorthWestSafePosition
                    ]
                },
                [2] = new()
                {
                    [DebuffType.HighJump] =
                    [
                        NorthSafePosition
                    ],
                    [DebuffType.Spine] =
                    [
                        NorthSafePosition
                    ],
                    [DebuffType.Illusive] =
                    [
                        NorthSafePosition
                    ]
                },
                [3] = new()
                {
                    [DebuffType.HighJump] =
                    [
                        EastTowerPosition,
                        SouthTowerPosition,
                        WestTowerPosition
                    ],
                    [DebuffType.Spine] =
                    [
                        EastTowerPosition
                    ],
                    [DebuffType.Illusive] =
                    [
                        WestTowerPosition
                    ]
                }
            },
            ["Fourth"] = new Dictionary<int, Dictionary<DebuffType, List<PositionDelegate>>>
            {
                [1] = new()
                {
                    [DebuffType.HighJump] =
                    [
                        NorthSafePosition,
                        NorthEastSafePosition,
                        NorthWestSafePosition
                    ],
                    [DebuffType.Spine] =
                    [
                        NorthEastSafePosition
                    ],
                    [DebuffType.Illusive] =
                    [
                        NorthWestSafePosition
                    ]
                },
                [2] = new()
                {
                    [DebuffType.HighJump] =
                    [
                        NorthSafePosition
                    ],
                    [DebuffType.Spine] =
                    [
                        NorthSafePosition
                    ],
                    [DebuffType.Illusive] =
                    [
                        NorthSafePosition
                    ]
                },
                [3] = new()
                {
                    [DebuffType.HighJump] =
                    [
                        EastTowerPosition,
                        SouthTowerPosition,
                        WestTowerPosition
                    ],
                    [DebuffType.Spine] =
                    [
                        EastTowerPosition
                    ],
                    [DebuffType.Illusive] =
                    [
                        WestTowerPosition
                    ]
                }
            },
            ["Fifth"] = new Dictionary<int, Dictionary<DebuffType, List<PositionDelegate>>>
            {
                [1] = new()
                {
                    [DebuffType.HighJump] =
                    [
                        NorthSafePosition,
                        SouthTowerPosition
                    ],
                    [DebuffType.Spine] =
                    [
                        NorthSafePosition
                    ],
                    [DebuffType.Illusive] =
                    [
                        NorthSafePosition
                    ]
                },
                [2] = new()
                {
                    [DebuffType.HighJump] =
                    [
                        EastTowerPosition,
                        WestTowerPosition
                    ],
                    [DebuffType.Spine] =
                    [
                        EastTowerPosition
                    ],
                    [DebuffType.Illusive] =
                    [
                        WestTowerPosition
                    ]
                },
                [3] = new()
                {
                    [DebuffType.HighJump] =
                    [
                        NorthSafePosition
                    ],
                    [DebuffType.Spine] =
                    [
                        NorthSafePosition
                    ],
                    [DebuffType.Illusive] =
                    [
                        NorthSafePosition
                    ]
                }
            }
        };

    private int _darkCount;

    private GeneralSafe _generalSafe = GeneralSafe.None;

    private Vector3 _lastPosition;

    private DebuffType _myDebuff = DebuffType.None;
    private int _myNumber = -1;

    private int _phaseCount;

    public override HashSet<uint>? ValidTerritories => [968];

    private Config C => Controller.GetConfig<Config>();

    public override Metadata? Metadata => new(4, "Garume");

    private static Vector2 EastTowerPosition(float offset)
    {
        return new Vector2(107f + offset, 100f);
    }

    private static Vector2 NorthSafePosition(float offset)
    {
        return new Vector2(100f, 93f - offset);
    }

    private static Vector2 SouthTowerPosition(float offset)
    {
        return new Vector2(100f, 107f + offset);
    }

    private static Vector2 WestTowerPosition(float offset)
    {
        return new Vector2(93f - offset, 100f);
    }

    private static Vector2 NorthEastSafePosition(float offset)
    {
        return new Vector2(106.5f, 86f);
    }

    private static Vector2 NorthWestSafePosition(float offset)
    {
        return new Vector2(93.5f, 86f);
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (target != Player.Object.EntityId) return;

        _myNumber = vfxPath switch
        {
            "vfx/lockon/eff/r1fz_lockon_num03_s5x.avfx" => 3,
            "vfx/lockon/eff/r1fz_lockon_num02_s5x.avfx" => 2,
            "vfx/lockon/eff/r1fz_lockon_num01_s5x.avfx" => 1,
            _ => _myNumber
        };
    }


    public override void OnReset()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        _myDebuff = DebuffType.None;
        _myNumber = -1;
        _phaseCount = 0;
        _darkCount = 0;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_myNumber == -1 || _myDebuff == DebuffType.None) return;

        if (set.Action?.RowId is 26382 or 26383 or 26384)
        {
            _darkCount++;
            if (_darkCount is 3 or 5 or 8)
            {
                _phaseCount++;
                var positions = GetBaitPositions(_phaseCount, _myNumber, _myDebuff);
                SetOffPositionBaitElements(positions);
            }
        }
    }


    public override void OnStartingCast(uint source, uint castId)
    {
        switch (castId)
        {
            case 26386 or 26387:
            {
                _generalSafe = castId == 26386 ? GeneralSafe.Out : GeneralSafe.In;
                Controller.Schedule(() =>
                {
                    _generalSafe = _generalSafe == GeneralSafe.In ? GeneralSafe.Out : GeneralSafe.In;
                    var positions = GetBaitPositions(_phaseCount, _myNumber, _myDebuff);
                    SetOffPositionBaitElements(positions);
                }, 1000 * 12);

                _phaseCount++;
                if (_phaseCount == 1)
                {
                    var statuses = Player.Status;
                    if (statuses.Any(x => x.StatusId == (uint)DebuffType.HighJump))
                        _myDebuff = DebuffType.HighJump;
                    else if (statuses.Any(x => x.StatusId == (uint)DebuffType.Spine))
                        _myDebuff = DebuffType.Spine;
                    else if (statuses.Any(x => x.StatusId == (uint)DebuffType.Illusive))
                        _myDebuff = DebuffType.Illusive;
                }

                if (_myNumber == -1 || _myDebuff == DebuffType.None) return;
                var positions = GetBaitPositions(_phaseCount, _myNumber, _myDebuff);
                SetOffPositionBaitElements(positions);
                break;
            }
            case 26380:
                Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
                _myNumber = -1;
                _myDebuff = DebuffType.None;
                break;
        }
    }

    private void ApplyLockFace()
    {
        if (Player.Position != _lastPosition && C.LockFaceEnableWhenNotMoving) return;

        var isEast = Player.Position.X > 100f;
        var targetPosition = (_myDebuff, isEast) switch
        {
            (DebuffType.Spine, _) => new Vector3(100f, 0f, Player.Position.Z),
            (DebuffType.Illusive, true) => new Vector3(200f, 0f, Player.Position.Z),
            (DebuffType.Illusive, false) => new Vector3(0f, 0f, Player.Position.Z),
            _ => Vector3.Zero
        };

        if (targetPosition == Vector3.Zero) return;

        FaceTarget(targetPosition);
        return;

        void FaceTarget(Vector3 position, ulong unkObjId = 0xE0000000)
        {
            ActionManager.Instance()->AutoFaceTargetPosition(&position, unkObjId);
        }
    }

    private List<Vector2>? GetBaitPositions(int phase, int number, DebuffType debuff)
    {
        string key;
        switch (phase)
        {
            case 1:
                key = "First";
                break;
            case 2:
                key = "Second";
                break;
            case 3:
                key = "Third";
                break;
            case 4:
                key = "Fourth";
                break;
            case 5:
                key = "Fifth";
                break;
            default:
                return null;
        }

        var offset = 0f;
        if (C.ShouldConsiderCircleDonut)
            offset = _generalSafe switch
            {
                GeneralSafe.In => -1.5f,
                GeneralSafe.Out => 2.5f,
                _ => 0f
            };

        if (number == 2 && phase == 2 && _generalSafe == GeneralSafe.In)
        {
            Controller.Schedule(() =>
            {
                _generalSafe = GeneralSafe.None;
                var positions = GetBaitPositions(_phaseCount, _myNumber, _myDebuff);
                SetOffPositionBaitElements(positions);
            }, 1000 * 3);
            return new List<Vector2> { NorthSafePosition(offset) };
        }

        if (_baitPositions.TryGetValue(key, out var phaseDict))
            if (phaseDict.TryGetValue(number, out var numberDict))
                if (numberDict.TryGetValue(debuff, out var positions))
                    return positions.Select(x => x(offset)).ToList();
        return null;
    }

    private void SetOffPositionBaitElements(List<Vector2>? positions)
    {
        if (positions == null) return;

        for (var i = 0; i < 3; i++)
        {
            var elementName = $"Bait{i}";
            if (Controller.TryGetElementByName(elementName, out var element)) element.Enabled = false;
        }

        for (var i = 0; i < positions.Count; i++)
        {
            var elementName = $"Bait{i}";
            if (Controller.TryGetElementByName(elementName, out var element))
            {
                element.Enabled = true;
                element.SetOffPosition(positions[i].ToVector3());
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Navigation Settings");
        ImGui.Indent();

        ImGui.Text("Bait Color");
        ImGuiComponents.HelpMarker(
            "Change the color of the bait and the text that will be displayed on your bait.\nSetting different values makes it rainbow.");
        ImGui.Indent();
        ImGui.ColorEdit4("Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.SameLine();
        ImGui.ColorEdit4("Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
        ImGui.Unindent();

        ImGui.Checkbox("Consider Circle Donut", ref C.ShouldConsiderCircleDonut);

        ImGui.Unindent();

        ImGui.Checkbox("Look Face", ref C.LookFace);
        ImGui.SameLine();
        ImGuiEx.HelpMarker(
            "This feature might be dangerous. Do NOT use when streaming. Make sure no other software implements similar option.\n\nThis will lock your face to the monitor, use with caution.\n\n自動で視線を調整します。ストリーミング中は使用しないでください。他のソフトウェアが同様の機能を実装していないことを確認してください。",
            EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());

        if (C.LookFace)
        {
            ImGui.Indent();
            ImGui.Checkbox("Lock Face Enable When Not Moving", ref C.LockFaceEnableWhenNotMoving);
            ImGui.SameLine();
            ImGuiEx.HelpMarker(
                "This will enable lock face when you are not moving. Be sure to enable it..\n\n動いていないときに視線をロックします。必ず有効にしてください。",
                EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
            ImGui.Unindent();
        }


        if (ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text("Debuff: " + _myDebuff);
            ImGui.Text("Number: " + _myNumber);
            ImGui.Text("Phase: " + _phaseCount);
            ImGui.Text("Dark Count: " + _darkCount);
        }
    }

    public override void OnUpdate()
    {
        if (_myNumber == -1 || _myDebuff == DebuffType.None) return;
        Controller.GetRegisteredElements().Each(x =>
            x.Value.color = GradientColor.Get(0xFFFF00FF.ToVector4(), 0xFFFFFF00.ToVector4()).ToUint());

        if (C.LookFace)
        {
            if (_myNumber == 1 && _phaseCount == 1) ApplyLockFace();

            if (_myNumber == 2 && _phaseCount == 2) ApplyLockFace();

            if (_myNumber == 3 && _phaseCount == 4) ApplyLockFace();

            _lastPosition = Player.Position;
        }
    }

    public override void OnSetup()
    {
        for (var i = 0; i < 3; i++)
        {
            var elementName = $"Bait{i}";
            var element = new Element(0)
            {
                radius = 1f,
                tether = true,
                thicc = 2f,
                LineEndA = LineEnd.Arrow
            };
            Controller.RegisterElement(elementName, element);
        }
    }

    private delegate Vector2 PositionDelegate(float offset);

    private enum DebuffType : uint
    {
        HighJump = 2755u,
        Spine = 2756u,
        Illusive = 2757u,
        None = 0u
    }

    private enum GeneralSafe
    {
        In,
        Out,
        None
    }

    private class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public bool LockFaceEnableWhenNotMoving = true;
        public bool LookFace = true;
        public bool ShouldConsiderCircleDonut;
    }
}