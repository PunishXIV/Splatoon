using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public unsafe class P5_Death_of_the_Heavens : SplatoonScript
{
    private readonly Dictionary<BaitType, List<Vector2>> _baitPositions = new()
    {
        [BaitType.None] =
        [
            Vector2.Zero,
            Vector2.Zero,
            Vector2.Zero
        ],
        [BaitType.Red1] =
        [
            new Vector2(12.49f, 8.5f),
            new Vector2(8f, 9f),
            new Vector2(2f, 9f)
        ],
        [BaitType.Red2] =
        [
            new Vector2(12.49f, 24.76f),
            new Vector2(1.4f, 7.6f),
            new Vector2(1.4f, 7.6f)
        ],
        [BaitType.Red3] =
        [
            new Vector2(-12.49f, 24.76f),
            new Vector2(-1.4f, 7.6f),
            new Vector2(-1.4f, 7.6f)
        ],
        [BaitType.Red4] =
        [
            new Vector2(-12.49f, 8.5f),
            new Vector2(-9f, 9f),
            new Vector2(-2f, 9f)
        ],
        [BaitType.Blue1] =
        [
            new Vector2(20.5f, 8.5f),
            new Vector2(0f, 9f),
            Vector2.Zero
        ],
        [BaitType.Blue2] =
        [
            new Vector2(12.49f, -7.76f),
            new Vector2(0f, 11.5f),
            Vector2.Zero
        ],
        [BaitType.Blue3] =
        [
            new Vector2(-12.49f, -7.76f),
            new Vector2(0f, 14f),
            Vector2.Zero
        ],
        [BaitType.Blue4] =
        [
            new Vector2(-20.5f, 8.5f),
            new Vector2(0f, 16.5f),
            Vector2.Zero
        ]
    };

    private readonly Dictionary<uint, Vector2> _eyesPositions = new()
    {
        { 0, new Vector2(100.00f, 60.00f) },
        { 1, new Vector2(128.28f, 71.72f) },
        { 2, new Vector2(140.00f, 100.00f) },
        { 3, new Vector2(128.28f, 128.28f) },
        { 4, new Vector2(100.00f, 140.00f) },
        { 5, new Vector2(71.72f, 128.28f) },
        { 6, new Vector2(60.00f, 100.00f) },
        { 7, new Vector2(71.72f, 71.72f) }
    };

    private State _currentState = State.None;
    private Vector2 _eyesPosition;

    private Vector3 _lastPlayerPosition = Vector3.Zero;


    private BaitType _myBait = BaitType.None;

    private PlaystationMarker _myMarker = PlaystationMarker.Circle;
    public override HashSet<uint>? ValidTerritories => [968];


    private Config C => Controller.GetConfig<Config>();

    public override Metadata? Metadata => new(1, "Garume");

    private IBattleChara? Thordan => Svc.Objects.OfType<IBattleChara>()
        .FirstOrDefault(x => x.NameId == 0xE30 && x.IsCharacterVisible());

    private bool DrawPriorityList()
    {
        if (C.Priority.Length != 8)
            C.Priority = ["", "", "", "", "", "", "", ""];

        ImGuiEx.Text("Priority list");
        ImGui.PushID("prio");
        for (var i = 0; i < C.Priority.Length; i++)
        {
            ImGui.PushID($"prioelement{i}");
            ImGui.SetNextItemWidth(200);
            ImGui.InputText($"Player {i + 1}", ref C.Priority[i], 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.BeginCombo("##partysel", "Select from party"))
            {
                foreach (var x in FakeParty.Get())
                    if (ImGui.Selectable(x.Name.ToString()))
                        C.Priority[i] = x.Name.ToString();
                ImGui.EndCombo();
            }

            ImGui.PopID();
        }

        ImGui.PopID();
        return false;
    }

    public override void OnReset()
    {
        _currentState = State.None;
        _myBait = BaitType.None;
        _myMarker = PlaystationMarker.None;
    }

    public override void OnSettingsDraw()
    {
        DrawPriorityList();

        ImGui.Checkbox("Look Face", ref C.LockFace);
        ImGui.SameLine();
        ImGuiEx.HelpMarker(
            "This feature might be dangerous. Do NOT use when streaming. Make sure no other software implements similar option.\n\nThis will lock your face to the monitor, use with caution.\n\n自動で視線を調整します。ストリーミング中は使用しないでください。他のソフトウェアが同様の機能を実装していないことを確認してください。",
            EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());

        if (C.LockFace)
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
            ImGui.Text($"Current State: {_currentState}");
            ImGui.Text($"My Bait: {_myBait}");
            ImGui.Text($"My Marker: {_myMarker}");
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 27538) _currentState = State.Start;
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (_currentState != State.SecondSplit)
            return;
        if (target == Player.Object.EntityId && vfxPath.StartsWith("vfx/lockon/eff/r1fz_firechain"))
        {
            _currentState = State.PlayStationSplit;
            _myMarker = vfxPath switch
            {
                "vfx/lockon/eff/r1fz_firechain_01x.avfx" => PlaystationMarker.Circle,
                "vfx/lockon/eff/r1fz_firechain_02x.avfx" => PlaystationMarker.Triangle,
                "vfx/lockon/eff/r1fz_firechain_03x.avfx" => PlaystationMarker.Square,
                "vfx/lockon/eff/r1fz_firechain_04x.avfx" => PlaystationMarker.Cross,
                _ => PlaystationMarker.None
            };
        }
    }

    public override void OnTetherRemoval(uint source, uint data2, uint data3, uint data5)
    {
        if (_currentState == State.PlayStationSplit)
            _currentState = State.End;
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Bait1",
            "{\"Name\":\"Bait1\",\"type\":1,\"offX\":12.49,\"offY\":8.5,\"radius\":0.5,\"color\":4278190335,\"Filled\":false,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"overlayFScale\":1.7,\"thicc\":6.0,\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}",
            true);
        Controller.RegisterElementFromCode("Bait2",
            "{\"Name\":\"Bait2\",\"type\":1,\"offX\":12.49,\"offY\":8.5,\"radius\":0.5,\"color\":4278190335,\"Filled\":false,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"overlayFScale\":1.7,\"thicc\":6.0,\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}",
            true);
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if (_currentState == State.None) return;
        switch (data1)
        {
            case 1:
            {
                if (_eyesPositions.TryGetValue(position, out var eyesPosition))
                    _eyesPosition = eyesPosition;
                break;
            }
            case 32:
                _eyesPosition = Vector2.Zero;
                break;
        }
    }

    public override void OnUpdate()
    {
        switch (_currentState)
        {
            case State.None or State.End:
                Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
                return;
            case State.FirstSplit:
            {
                var pos = _baitPositions[_myBait][0];
                if (pos == Vector2.Zero) return;
                if (Controller.TryGetElementByName("Bait1", out var bait))
                {
                    bait.Enabled = true;
                    bait.tether = true;
                    bait.SetOffPosition(pos.ToVector3(0));
                }

                break;
            }
            case State.SecondSplit:
            {
                var pos = _baitPositions[_myBait][1];
                if (Controller.TryGetElementByName("Bait1", out var bait))
                {
                    bait.Enabled = true;
                    bait.tether = true;
                    bait.SetOffPosition(pos.ToVector3(0));
                }

                break;
            }
            case State.PlayStationSplit:
            {
                var pos = _baitPositions[_myBait][2];
                if (pos != Vector2.Zero)
                {
                    if (Controller.TryGetElementByName("Bait1", out var bait))
                    {
                        bait.Enabled = true;
                        bait.tether = true;
                        bait.SetOffPosition(pos.ToVector3(0));
                    }
                }

                else
                {
                    var baits = PlaystationMarkerBaitPositions(_myMarker);
                    for (var i = 0; i < baits.Count(); i++)
                        if (Controller.TryGetElementByName($"Bait{i + 1}", out var bait))
                        {
                            bait.Enabled = true;
                            bait.tether = true;
                            bait.SetOffPosition(baits[i].ToVector3(0));
                        }
                }

                if (C.LockFace)
                {
                    ApplyLockFace();
                    _lastPlayerPosition = Player.Position;
                }

                break;
            }
        }


        Controller.GetRegisteredElements()
            .Each(x => x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
    }

    private static Vector2 CalculateExtendedBisectorPoint(Vector2 point1, Vector2 point2, Vector2? center = null,
        float? radius = null)
    {
        center ??= new Vector2(100f, 100f);
        radius ??= 20f;

        var dir1 = point1 - center.Value;
        var dir2 = point2 - center.Value;

        var angle1 = MathF.Atan2(dir1.Y, dir1.X);
        var angle2 = MathF.Atan2(dir2.Y, dir2.X);

        var bisectorAngle = (angle1 + angle2) / 2f;

        var bisectorDir = new Vector2(MathF.Cos(bisectorAngle), MathF.Sin(bisectorAngle));

        var intersectionPoint1 = center.Value + bisectorDir * radius.Value;
        var intersectionPoint2 = center.Value - bisectorDir * radius.Value;

        return Vector2.Distance(intersectionPoint1, point1) > Vector2.Distance(intersectionPoint2, point1)
            ? intersectionPoint1
            : intersectionPoint2;
    }

    private void ApplyLockFace()
    {
        if (Player.Position != _lastPlayerPosition && C.LockFaceEnableWhenNotMoving) return;
        var targetPosition = Vector3.Zero;

        var thordan = Thordan;
        if (thordan != null && _eyesPosition != Vector2.Zero)
            targetPosition = CalculateExtendedBisectorPoint(thordan.Position.ToVector2(), _eyesPosition).ToVector3(0f);

        if (targetPosition == Vector3.Zero) return;

        DuoLog.Warning($"Facing target at {targetPosition}");

        FaceTarget(targetPosition);
        return;

        void FaceTarget(Vector3 position, ulong unkObjId = 0xE0000000)
        {
            ActionManager.Instance()->AutoFaceTargetPosition(&position, unkObjId);
        }
    }

    private Vector2[] PlaystationMarkerBaitPositions(PlaystationMarker marker)
    {
        return marker switch
        {
            PlaystationMarker.Circle => new[]
            {
                Vector2.Zero
            },
            PlaystationMarker.Triangle => new[]
            {
                new Vector2(1.4f, 10.4f),
                new Vector2(-1.4f, 10.4f)
            },
            PlaystationMarker.Square => new[]
            {
                new Vector2(1.4f, 10.4f),
                new Vector2(-1.4f, 10.4f)
            },
            PlaystationMarker.Cross => new[]
            {
                new Vector2(0f, 7f),
                new Vector2(0f, 11f)
            },
            _ => Array.Empty<Vector2>()
        };
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_currentState != State.Start)
            return;

        if (set.Action.RowId == 27540)
        {
            _currentState = State.FirstSplit;
            Controller.Schedule(() =>
            {
                var players = FakeParty.Get().ToArray();
                if (players.Length == 0)
                    return;

                var red = 0;
                var blue = 0;
                foreach (var player in C.Priority)
                {
                    var p = players.FirstOrDefault(x => x.Name.ToString() == player);
                    if (p == null)
                    {
                        DuoLog.Error($"Player {player} not found in party.");
                        return;
                    }

                    if (p.HasDoom())
                        red++;
                    else
                        blue++;

                    if (p.Name.ToString() == Player.Object.Name.ToString())
                    {
                        if (p.HasDoom())
                            _myBait = red switch
                            {
                                1 => BaitType.Red1,
                                2 => BaitType.Red2,
                                3 => BaitType.Red3,
                                4 => BaitType.Red4,
                                _ => _myBait
                            };
                        else
                            _myBait = blue switch
                            {
                                1 => BaitType.Blue1,
                                2 => BaitType.Blue2,
                                3 => BaitType.Blue3,
                                4 => BaitType.Blue4,
                                _ => _myBait
                            };
                    }
                }

                Controller.Schedule(() => _currentState = State.SecondSplit, 1000 * 8);
            }, 1000 * 2);
        }
    }

    private enum BaitType
    {
        Red1,
        Red2,
        Red3,
        Red4,
        Blue1,
        Blue2,
        Blue3,
        Blue4,
        None
    }

    private enum PlaystationMarker
    {
        Circle,
        Triangle,
        Square,
        Cross,
        None
    }

    private enum State
    {
        Start,
        FirstSplit,
        SecondSplit,
        PlayStationSplit,
        End,
        None
    }

    private class Config : IEzConfig
    {
        public readonly Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public readonly Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public bool LockFace = true;
        public bool LockFaceEnableWhenNotMoving = true;
        public string[] Priority = ["", "", "", "", "", "", "", ""];
    }
}

public static class PlayerExtensions
{
    public static bool HasDoom(this IPlayerCharacter p)
    {
        return p.StatusList.Any(x => x.StatusId == 2976);
    }
}