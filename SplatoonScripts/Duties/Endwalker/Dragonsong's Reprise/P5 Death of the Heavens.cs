using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using ECommons;
using ECommons.ChatMethods;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.PartyFunctions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public unsafe class P5_Death_of_the_Heavens : SplatoonScript
{
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
    private readonly ImGuiEx.RealtimeDragDrop<Job> DragDrop = new("DragDropJob", x => x.ToString());

    public override HashSet<uint>? ValidTerritories => [968];
    private Config C => Controller.GetConfig<Config>();
    public override Metadata? Metadata => new(5, "Garume");

    private IBattleChara? Thordan => Svc.Objects.OfType<IBattleChara>()
        .FirstOrDefault(x => x.NameId == 0xE30 && x.IsCharacterVisible());

    private IEnumerable<IGameObject> DeathSentence => Svc.Objects
        .Where(x => x.DataId == 0x1EB685);

    private Vector2 GetBaitPosition(State state, BaitType bait)
    {
        var position = (state, bait) switch
        {
            (State.FirstSplit, BaitType.Red1) => new Vector2(12.49f, 8.5f),
            (State.FirstSplit, BaitType.Red2) => new Vector2(12.49f, 24.76f),
            (State.FirstSplit, BaitType.Red3) => new Vector2(-12.49f, 24.76f),
            (State.FirstSplit, BaitType.Red4) => new Vector2(-12.49f, 8.5f),
            (State.FirstSplit, BaitType.Blue1) => new Vector2(20.5f, 8.5f),
            (State.FirstSplit, BaitType.Blue2) => new Vector2(12.49f, -7.76f),
            (State.FirstSplit, BaitType.Blue3) => new Vector2(-12.49f, -7.76f),
            (State.FirstSplit, BaitType.Blue4) => new Vector2(-20.5f, 8.5f),
            (State.SecondSplit, BaitType.Red1) => new Vector2(8f, 9f),
            (State.SecondSplit, BaitType.Red2) => new Vector2(1.4f, 7.6f),
            (State.SecondSplit, BaitType.Red3) => new Vector2(-1.4f, 7.6f),
            (State.SecondSplit, BaitType.Red4) => new Vector2(-9f, 9f),
            (State.SecondSplit, BaitType.Blue1) => C.PrePlaystationSplit switch
            {
                PrePlaystationSplit.Horizontal => new Vector2(6f, 13.5f),
                PrePlaystationSplit.Vertical => new Vector2(0f, 9f),
                _ => throw new ArgumentOutOfRangeException()
            },
            (State.SecondSplit, BaitType.Blue2) => C.PrePlaystationSplit switch
            {
                PrePlaystationSplit.Horizontal => new Vector2(2f, 13.5f),
                PrePlaystationSplit.Vertical => new Vector2(0f, 11.5f),
                _ => throw new ArgumentOutOfRangeException()
            },
            (State.SecondSplit, BaitType.Blue3) => C.PrePlaystationSplit switch
            {
                PrePlaystationSplit.Horizontal => new Vector2(-2f, 13.5f),
                PrePlaystationSplit.Vertical => new Vector2(0f, 14f),
                _ => throw new ArgumentOutOfRangeException()
            },
            (State.SecondSplit, BaitType.Blue4) => C.PrePlaystationSplit switch
            {
                PrePlaystationSplit.Horizontal => new Vector2(-6f, 13.5f),
                PrePlaystationSplit.Vertical => new Vector2(0f, 16.5f),
                _ => throw new ArgumentOutOfRangeException()
            },
            (State.PlayStationSplit, BaitType.Red1) => new Vector2(2f, 9f),
            (State.PlayStationSplit, BaitType.Red2) => new Vector2(1.4f, 7.6f),
            (State.PlayStationSplit, BaitType.Red3) => new Vector2(-1.4f, 7.6f),
            (State.PlayStationSplit, BaitType.Red4) => new Vector2(-2f, 9f),
            (State.PlayStationSplit, BaitType.Blue1) => Vector2.Zero,
            (State.PlayStationSplit, BaitType.Blue2) => Vector2.Zero,
            (State.PlayStationSplit, BaitType.Blue3) => Vector2.Zero,
            (State.PlayStationSplit, BaitType.Blue4) => Vector2.Zero,
            _ => default
        };

        if (C.OrientationBase == Direction.South)
            position = new Vector2(position.X, -position.Y);

        return position;
    }

    private bool DrawPriorityList()
    {
        if (C.Priority.Length != 8)
            C.Priority = ["", "", "", "", "", "", "", ""];

        ImGuiEx.Text("Priority list");
        ImGui.SameLine();
        ImGuiEx.Spacing();
        if (ImGui.Button("Perform test")) SelfTest();
        ImGui.SameLine();
        if (ImGui.Button("Fill by job"))
        {
            HashSet<(string, Job)> party = [];
            foreach (var x in FakeParty.Get())
                party.Add((x.Name.ToString(), x.GetJob()));

            var proxy = InfoProxyCrossRealm.Instance();
            for (var i = 0; i < proxy->GroupCount; i++)
            {
                var group = proxy->CrossRealmGroups[i];
                for (var c = 0; c < proxy->CrossRealmGroups[i].GroupMemberCount; c++)
                {
                    var x = group.GroupMembers[c];
                    party.Add((x.Name.Read(), (Job)x.ClassJobId));
                }
            }

            var index = 0;
            foreach (var job in C.Jobs.Where(job => party.Any(x => x.Item2 == job)))
            {
                C.Priority[index] = party.First(x => x.Item2 == job).Item1;
                index++;
            }

            for (var i = index; i < C.Priority.Length; i++)
                C.Priority[i] = "";
        }
        ImGuiEx.Tooltip("The list is populated based on the job.\nYou can adjust the priority from the option header.");

        ImGui.PushID("prio");
        for (var i = 0; i < C.Priority.Length; i++)
        {
            ImGui.PushID($"prioelement{i}");
            ImGui.Text($"Character {i + 1}");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            ImGui.InputText($"##Character{i}", ref C.Priority[i], 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.BeginCombo("##partysel", "Select from party"))
            {
                foreach (var x in FakeParty.Get().Select(x => x.Name.ToString())
                             .Union(UniversalParty.Members.Select(x => x.Name)).ToHashSet())
                    if (ImGui.Selectable(x))
                        C.Priority[i] = x;
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
        ImGui.Text("General");
        ImGui.Indent();
        DrawPriorityList();
        ImGui.Text("Pre Playstation Split");
        ImGuiEx.EnumCombo("##Pre Playstation Split", ref C.PrePlaystationSplit);
        ImGui.Text("Orientation Base");
        ImGuiEx.EnumRadio(ref C.OrientationBase, true);
        ImGui.Unindent();

        ImGui.Text("Other");
        ImGui.Indent();
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

        ImGui.Checkbox("Check on Start", ref C.ShouldCheckOnStart);

        ImGui.Unindent();

        if (ImGuiEx.CollapsingHeader("Option"))
        {
            DragDrop.Begin();
            foreach (var job in C.Jobs)
            {
                DragDrop.NextRow();
                ImGui.Text(job.ToString());
                ImGui.SameLine();

                if (ThreadLoadImageHandler.TryGetIconTextureWrap((uint)job.GetIcon(), false, out var texture))
                {
                    ImGui.Image(texture.ImGuiHandle, new Vector2(24f));
                    ImGui.SameLine();
                }

                ImGui.SameLine();
                DragDrop.DrawButtonDummy(job, C.Jobs, C.Jobs.IndexOf(job));
            }

            DragDrop.End();
        }
        
        if (ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Checkbox("Show Debug Message", ref C.ShowDebug);
            ImGui.Text($"Current State: {_currentState}");
            ImGui.Text($"My Bait: {_myBait}");
            ImGui.Text($"My Marker: {_myMarker}");
        }
    }

    private void SelfTest()
    {
        Print("= P5 Death of the Heavens self-test =", UIColor.LightBlue);
        var party = FakeParty.Get().ToArray();
        var isCorrect = C.Priority.All(x => !string.IsNullOrEmpty(x));

        if (!isCorrect)
        {
            Print("Priority list is not filled correctly.", UIColor.Red);
            return;
        }

        if (party.Length != 8)
        {
            isCorrect = false;
            Print("Can only be tested in content.", UIColor.Red);
        }

        foreach (var player in party)
            if (C.Priority.All(x => x != player.Name.ToString()))
            {
                isCorrect = false;
                Print($"Player {player.Name} is not in the priority list.", UIColor.Red);
            }

        if (isCorrect)
            Print("Test Success!", UIColor.Green);
        else
            Print("!!! Test failed !!!", UIColor.Red);
        return;

        void Print(string message, UIColor color)
        {
            Svc.Chat.PrintChat(new XivChatEntry
            {
                Message = new SeStringBuilder()
                    .AddUiForeground(message, (ushort)color).Build()
            });
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


    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Bait1",
            "{\"Name\":\"Bait1\",\"type\":1,\"offX\":12.49,\"offY\":8.5,\"radius\":0.5,\"color\":4278190335,\"Filled\":false,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"overlayFScale\":1.7,\"thicc\":6.0,\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}",
            true);
        Controller.RegisterElementFromCode("Bait2",
            "{\"Name\":\"Bait2\",\"type\":1,\"offX\":12.49,\"offY\":8.5,\"radius\":0.5,\"color\":4278190335,\"Filled\":false,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"overlayFScale\":1.7,\"thicc\":6.0,\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}",
            true);
        var element = new Element(0)
        {
            thicc = 6f
        };
        Controller.RegisterElement("DeathSentence", element);
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
                _currentState = State.PostPlaystationSplit;
                break;
        }
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        switch (_currentState)
        {
            case State.FirstSplit:
            case State.SecondSplit:
            {
                var pos = GetBaitPosition(_currentState, _myBait);
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
                var pos = GetBaitPosition(_currentState, _myBait);
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
                    for (var i = 0; i < baits.Length; i++)
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

            case State.PostPlaystationSplit:
            {
                if (Player.Status.All(x => x.StatusId != 2976))
                {
                    _currentState = State.End;
                    return;
                }

                var deathSentence = DeathSentence.MinBy(x => Vector3.Distance(x.Position, Player.Position));
                if (deathSentence != null)
                    if (Controller.TryGetElementByName("DeathSentence", out var bait))
                    {
                        bait.Enabled = true;
                        bait.tether = true;
                        bait.SetRefPosition(deathSentence.Position);
                    }

                break;
            }
            case State.Start:
            case State.End:
            case State.None:
            default:
                break;
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

        if (C.ShowDebug)
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
            PlaystationMarker.Circle =>
            [
                Vector2.Zero
            ],
            PlaystationMarker.Triangle =>
            [
                new Vector2(1.4f, 10.4f),
                new Vector2(-1.4f, 10.4f)
            ],
            PlaystationMarker.Square =>
            [
                new Vector2(1.4f, 10.4f),
                new Vector2(-1.4f, 10.4f)
            ],
            PlaystationMarker.Cross =>
            [
                new Vector2(0f, 7f),
                new Vector2(0f, 11f)
            ],
            _ => Array.Empty<Vector2>()
        };
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (_currentState != State.Start)
            return;
        if (set.Action is null)
            return;

        switch (set.Action.Value.RowId)
        {
            case 27540:
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

                    _currentState = State.FirstSplit;
                    Controller.Schedule(() => _currentState = State.SecondSplit, 1000 * 8);
                }, 1000 * 2);
                break;
        }
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (!C.ShouldCheckOnStart)
            return;
        if (category == DirectorUpdateCategory.Commence ||
            (category == DirectorUpdateCategory.Recommence && Controller.Phase == 2))
            SelfTest();
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
        PostPlaystationSplit,
        End,
        None
    }

    private enum PrePlaystationSplit
    {
        Horizontal,
        Vertical
    }

    private enum Direction
    {
        North,
        South
    }

    private class Config : IEzConfig
    {
        public readonly Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public readonly Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();

        public readonly List<Job> Jobs =
        [
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
        ];

        public bool LockFace = true;
        public bool LockFaceEnableWhenNotMoving = true;
        public Direction OrientationBase = Direction.North;
        public PrePlaystationSplit PrePlaystationSplit = PrePlaystationSplit.Horizontal;
        public string[] Priority = ["", "", "", "", "", "", "", ""];
        public bool ShouldCheckOnStart = true;
        public bool ShowDebug;
    }
}

public static class PlayerExtensions
{
    public static bool HasDoom(this IPlayerCharacter p)
    {
        return p.StatusList.Any(x => x.StatusId == 2976);
    }
}