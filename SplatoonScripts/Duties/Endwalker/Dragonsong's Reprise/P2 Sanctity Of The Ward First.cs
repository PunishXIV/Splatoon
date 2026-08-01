using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using ECommons;
using ECommons.ChatMethods;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public unsafe class P2_Sanctity_Of_The_Ward_First : SplatoonScript
{
    private readonly Vector2 _center = new(100, 100);

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

    private ClockwiseDirection _clockwiseDirection;

    private Vector2 _eyesPosition;

    private Vector3 _lastPlayerPosition = Vector3.Zero;

    private IGameObject? _sword1;
    private IGameObject? _sword2;

    private ZephiranDirection _zephiranDirection;
    public override HashSet<uint>? ValidTerritories => [968];
    public override Metadata Metadata => new(9, "Garume, damolitionn, NightmareXIV");
    private bool IsStart => _sword1 != null && _sword2 != null;
    private Config C => Controller.GetConfig<Config>();
    private IBattleChara? Zephiran => Svc.Objects.OfType<IBattleChara>().FirstOrDefault(x => x.NameId == 0xE31);

    private IBattleChara? Adelphel => Svc.Objects.OfType<IBattleChara>()
        .FirstOrDefault(x => x.NameId == 0xE32 && x.IsCharacterVisible());

    private IBattleChara? Thordan => Svc.Objects.OfType<IBattleChara>()
        .FirstOrDefault(x => x.NameId == 0xE30 && x.IsCharacterVisible());

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(!IsStart)
        {
            return;
        }

        switch(data1)
        {
            case 1:
                {
                    if(_eyesPositions.TryGetValue(position, out var eyesPosition))
                    {
                        _eyesPosition = eyesPosition;
                    }

                    break;
                }
            case 32:
                _eyesPosition = Vector2.Zero;
                break;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(IsStart)
        {
            return;
        }

        switch(vfxPath)
        {
            // 1 sword
            case "vfx/lockon/eff/m0244trg_a1t.avfx":
                _sword1 = target.GetObject();
                break;
            // 2 sword
            case "vfx/lockon/eff/m0244trg_a2t.avfx":
                _sword2 = target.GetObject();
                break;
        }

        if(IsStart)
        {
            var zephiran = Zephiran;
            var adelphel = Adelphel;

            if(zephiran == null || adelphel == null)
            {
                return;
            }

            _zephiranDirection = GetZephiranDirection(zephiran);
            _clockwiseDirection = adelphel.Position.X > _center.X
                ? ClockwiseDirection.Clockwise
                : ClockwiseDirection.CounterClockwise;
        }
    }

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            tether = true
        };
        Controller.TryRegisterElement("bait", element, true);

        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"G1CCW","ZoneLockH":[968],"ElementsL":[{"Name":"Point","type":1,"offX":3.52,"offY":-5.0,"radius":0.5,"color":3372158208,"Filled":false,"thicc":5.0,"refActorNPCNameID":3633,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"tether":true,"IsCapturing":true,"Nodraw":true},{"Name":"Second","type":1,"offX":11.74,"offY":-1.44,"radius":0.5,"color":3372158208,"Filled":false,"thicc":5.0,"refActorNPCNameID":3633,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"tether":true,"IsCapturing":true,"Nodraw":true}]}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"G1CW","ZoneLockH":[968],"ElementsL":[{"Name":"Point","type":1,"offX":-3.24,"offY":-5.0,"radius":0.5,"color":3372158208,"Filled":false,"thicc":5.0,"refActorNPCNameID":3633,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"tether":true,"IsCapturing":true,"Nodraw":true},{"Name":"Second","type":1,"offX":-11.82,"offY":-1.54,"radius":0.5,"color":3372158208,"Filled":false,"thicc":5.0,"refActorNPCNameID":3633,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"tether":true,"IsCapturing":true,"Nodraw":true}]}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"G2CCW","ZoneLockH":[968],"ElementsL":[{"Name":"Point","type":1,"offX":-3.44,"offY":35.0,"radius":0.5,"color":3372158208,"Filled":false,"thicc":5.0,"refActorNPCNameID":3633,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"tether":true,"IsCapturing":true,"Nodraw":true},{"Name":"Second","type":1,"offX":-11.68,"offY":31.4,"radius":0.5,"color":3372158208,"Filled":false,"thicc":5.0,"refActorNPCNameID":3633,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"tether":true,"IsCapturing":true,"Nodraw":true}]}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Enabled":false,"Name":"G2CW","ZoneLockH":[968],"ElementsL":[{"Name":"Point","type":1,"offX":3.24,"offY":35.0,"radius":0.5,"color":3372158208,"Filled":false,"thicc":5.0,"refActorNPCNameID":3633,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"tether":true,"IsCapturing":true,"Nodraw":true},{"Name":"Second","type":1,"offX":11.84,"offY":31.5,"radius":0.5,"color":3372158208,"Filled":false,"thicc":5.0,"refActorNPCNameID":3633,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"tether":true,"IsCapturing":true,"Nodraw":true}]}
            """);
        Controller.RegisterElementsFromMultilineCode("""
            {"Name":"Hint","refX":86.5,"refY":108.5,"radius":0.5,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"tether":true}
            {"Name":"LineNext","type":2,"refX":89.94933,"refY":112.289276,"refZ":-7.6293945E-06,"offX":84.5,"offY":102.0,"radius":0.0,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"tether":true,"EnablePointerLine":true,"PointerLineStyle":{"ChunkLength":0.0,"IntervalLength":2.1,"Width":0.5,"AnimationDuration":2000,"TipLength":0.3,"Thickness":2.0,"Accent":4278190335,"Background":4278255605}}
            {"Name":"StaticWest","refX":80.0,"refY":100.0,"radius":0.5,"Filled":false,"fillIntensity":0.5,"thicc":5.0}
            {"Name":"StaticEast","refX":120.0,"refY":100.0,"radius":0.5,"Filled":false,"fillIntensity":0.5,"thicc":5.0}
            """);
        Controller.RegisterLayoutFromCode("""
            ~Lv2~{"Name":"SCwStaticDodgePositions","ZoneLockH":[968],"ElementsL":[{"Name":"WestCCW1","Enabled":false,"refX":83.52723,"refY":88.31066,"refZ":-1.9073486E-05},{"Name":"WestCCW2","Enabled":false,"refX":80.0,"refY":100.0},{"Name":"WestCW1","Enabled":false,"refX":83.620476,"refY":111.82414,"refZ":2.2737368E-13},{"Name":"WestCW2","Enabled":false,"refX":80.0,"refY":100.0},{"Name":"EastCCW1","Enabled":false,"refX":116.48181,"refY":111.62211,"refZ":2.2737368E-13},{"Name":"EastCCW2","Enabled":false,"refX":120.0,"refY":100.0},{"Name":"EastCW1","Enabled":false,"refX":116.425446,"refY":88.14759,"refZ":2.2737368E-13},{"Name":"EastCW2","Enabled":false,"refX":120.0,"refY":100.0}]}
            """);

        var clockwiseTextElement = new Element(0)
        {
            overlayText = "←←←",
            overlayFScale = 5f,
            overlayVOffset = 5f,
            offX = 100f,
            offY = 100f
        };
        Controller.TryRegisterElement("clockwise", clockwiseTextElement, true);

        var counterClockwiseTextElement = new Element(0)
        {
            overlayText = "→→→",
            overlayFScale = 5f,
            overlayVOffset = 5f,
            offX = 100f,
            offY = 100f
        };

        Controller.TryRegisterElement("counterClockwise", counterClockwiseTextElement, true);

        var eyesElement = new Element(0)
        {
            radius = 2f,
            color = 0xFFFF00FF,
            thicc = 5f
        };

        Controller.TryRegisterElement("eyes", eyesElement, true);
    }

    public override void OnUpdate()
    {
        Controller.Hide();

        
        if(BrightsphereCasts >= 16)
        {
            Controller.Reset();
        }
        else if(BrightsphereCasts >= 8)
        {
            this.Phase = PhaseDef.SecondMove;
        }
        if(!IsStart)
        {
            return;
        }
        if(Phase == PhaseDef.Start && !C.UsePseudostaticGroups)
        {
            Phase = PhaseDef.FirstMove;
        }

        if(_zephiranDirection != ZephiranDirection.None)
        {
            var resolvePosition = C.ResolvePosition;
            var pairCharacterName = C.UsePseudostaticGroups?null:ResolvePairCharacterNameFromParty();

            if(string.IsNullOrEmpty(pairCharacterName) && !C.UsePseudostaticGroups)
            {
                Svc.Chat.Print("No pair character defined.");
                return;
            }

            if(_sword1?.Name.ToString() == BasePlayer.Name.ToString())
            {
                resolvePosition = ResolvePosition.ZephiranFaceToFace;
            }
            else if(_sword2?.Name.ToString() == BasePlayer.Name.ToString())
            {
                resolvePosition = ResolvePosition.ZephiranBack;
            }
            else
            {
                if(!C.UsePseudostaticGroups)
                {
                    if(_sword1?.Name.ToString() == pairCharacterName)
                    {
                        resolvePosition = ResolvePosition.ZephiranBack;
                    }
                    else if(_sword2?.Name.ToString() == pairCharacterName)
                    {
                        resolvePosition = ResolvePosition.ZephiranFaceToFace;
                    }
                }
                else
                {
                    resolvePosition = _zephiranDirection switch
                    {
                        ZephiranDirection.NorthEast => ResolvePosition.ZephiranBack,
                        ZephiranDirection.SouthEast => ResolvePosition.ZephiranBack,
                        _ => ResolvePosition.ZephiranFaceToFace
                    };
                    if(C.PseudostaticWest)
                    {
                        resolvePosition = resolvePosition == ResolvePosition.ZephiranFaceToFace ? ResolvePosition.ZephiranBack : ResolvePosition.ZephiranFaceToFace;
                    }
                }
            }
            var layout = ResolveElement(resolvePosition, _clockwiseDirection);
            if(layout != null && Positions.Any(x => x == null))
            {
                layout.Enabled = true;
                var position = layout.GetCapturedPositions();
                if(position != null && position.SafeSelect("Point") != null && position.SafeSelect("Second") != null)
                {
                    if(C.UsePseudostaticGroups && Controller.TryGetLayoutByName("SCwStaticDodgePositions", out var l))
                    {
                        bool isWest;
                        if(resolvePosition == ResolvePosition.ZephiranFaceToFace)
                        {
                            isWest = !_zephiranDirection.EqualsAny(ZephiranDirection.NorthWest, ZephiranDirection.SouthWest);
                        }
                        else
                        {
                            isWest = _zephiranDirection.EqualsAny(ZephiranDirection.NorthWest, ZephiranDirection.SouthWest);
                        }
                        this.Positions = [
                            Controller.GetElementByName($"Static{(isWest ? "West" : "East")}")!.RefPosition,
                            l.GetElement($"{(isWest?"West":"East")}{(_clockwiseDirection == ClockwiseDirection.Clockwise?"CW":"CCW")}1")!.RefPosition,
                            l.GetElement($"{(isWest?"West":"East")}{(_clockwiseDirection == ClockwiseDirection.Clockwise?"CW":"CCW")}2")!.RefPosition,
                            ];
                    }
                    if(!C.UsePseudostaticGroups)
                    {
                        this.Positions = [position["Point"][0], position["Point"][0], position["Second"][0]];
                    }
                    
                }
            }

            if(Positions.All(x => x != null))
            {
                Element hint = Controller.GetElementByName("Hint")!;
                hint.color = Controller.AttentionColor;
                Element line = Controller.GetElementByName("LineNext")!;
                if(this.Phase == PhaseDef.Start)
                {
                    hint.Enabled = true;
                    hint.SetRefPosition(Positions[0].Value);
                    line.RefPosition = Positions[0].Value;
                    line.OffPosition = Positions[1].Value;
                    line.Enabled = true;
                }
                if(this.Phase == PhaseDef.FirstMove)
                {
                    hint.Enabled = true;
                    hint.SetRefPosition(Positions[1].Value);
                    line.RefPosition = Positions[1].Value;
                    line.OffPosition = Positions[2].Value;
                    // line.Enabled = true;
                }
                if(this.Phase == PhaseDef.SecondMove)
                {
                    hint.Enabled = true;
                    hint.SetRefPosition(Positions[2].Value);
                }
            }

            var thordan = Thordan;
            if(thordan != null && _eyesPosition != Vector2.Zero && C.LockFace)
            {
                if(BasePlayer.Position != _lastPlayerPosition && C.LockFaceEnableWhenNotMoving)
                {
                    return;
                }

                var resolveFacePosition = CalculateExtendedBisectorPoint(thordan.Position.ToVector2(), _eyesPosition);
                FaceTarget(resolveFacePosition.ToVector3(0f));
            }
        }

        if(_clockwiseDirection != ClockwiseDirection.None)
        {
            var elementName = _clockwiseDirection == ClockwiseDirection.Clockwise ? "clockwise" : "counterClockwise";
            if(Controller.TryGetElementByName(elementName, out var element))
            {
                element.Enabled = true;
            }
        }

        if(_eyesPosition != Vector2.Zero)
        {
            if(Controller.TryGetElementByName("eyes", out var element))
            {
                element.Enabled = true;
                element.offX = _eyesPosition.X;
                element.offY = _eyesPosition.Y;
            }
        }

        _lastPlayerPosition = BasePlayer.Position;
        return;

        static void FaceTarget(Vector3 position, ulong unkObjId = 0xE0000000)
        {
            ActionManager.Instance()->AutoFaceTargetPosition(&position, unkObjId);
        }
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

        var intersectionPoint1 = center.Value + (bisectorDir * radius.Value);
        var intersectionPoint2 = center.Value - (bisectorDir * radius.Value);

        return Vector2.Distance(intersectionPoint1, point1) > Vector2.Distance(intersectionPoint2, point1)
            ? intersectionPoint1
            : intersectionPoint2;
    }

    public override void OnReset()
    {
        Positions = [null, null, null];
        _sword1 = null;
        _sword2 = null;
        Phase = PhaseDef.Start;
        BrightsphereCasts = 0;
    }

    private Layout? ResolveElement(ResolvePosition resolvePosition, ClockwiseDirection clockwiseDirection)
    {
        var elementName = (resolvePosition, clockwiseDirection) switch
        {
            (ResolvePosition.ZephiranFaceToFace, ClockwiseDirection.Clockwise) => "G2CW",
            (ResolvePosition.ZephiranFaceToFace, ClockwiseDirection.CounterClockwise) => "G2CCW",
            (ResolvePosition.ZephiranBack, ClockwiseDirection.Clockwise) => "G1CW",
            (ResolvePosition.ZephiranBack, ClockwiseDirection.CounterClockwise) => "G1CCW",
            _ => ""
        };

        if(Controller.TryGetLayoutByName(elementName, out var l)) return l;
        return null;
    }

    private ZephiranDirection GetZephiranDirection(IBattleChara target)
    {
        if(target.NameId != 0xE31)
        {
            return ZephiranDirection.None;
        }

        var isEast = target.Position.X > _center.X;
        var isNorth = target.Position.Z < _center.Y;
        return (isEast, isNorth) switch
        {
            (true, true) => ZephiranDirection.NorthEast,
            (true, false) => ZephiranDirection.SouthEast,
            (false, false) => ZephiranDirection.SouthWest,
            (false, true) => ZephiranDirection.NorthWest
        };
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("General Settings");
        ImGui.Checkbox("Enable pseudostatic group mode", ref C.UsePseudostaticGroups);
        ImGui.Indent();

        if(!C.UsePseudostaticGroups)
        {
            ImGui.Text("Pair Character Name");
            ImGui.SameLine();
            ImGuiEx.Spacing();
            if(ImGui.Button("Perform test"))
            {
                SelfTest();
            }

            ImGui.Text("Pair Character Name");
            C.PriorityData.Draw();

            ImGui.Text("Resolve Position");
            ImGuiEx.EnumCombo("##Resolve Position", ref C.ResolvePosition);
        }
        else
        {
            ImGuiEx.TextV($"Direction: ");
            ImGui.SameLine();
            ImGuiEx.RadioButtonBool("West", "East", ref C.PseudostaticWest, true);
            ImGuiEx.Checkbox("I'm adjusting", ref C.PseudostaticAdjust, enabled: false);
            ImGuiEx.HelpMarker("Not implemented. Contact NightmareXIV in discord if you need this option.");
        }
        ImGui.Unindent();

        ImGui.Text("Other Settings");
        ImGui.Indent();
        ImGui.Checkbox("Look Face", ref C.LockFace);
        ImGui.SameLine();
        ImGuiEx.HelpMarker(
            "This feature might be dangerous. Do NOT use when streaming. Make sure no other software implements similar option.\n\nThis will lock your face to the monitor, use with caution.\n\n自動で視線を調整します。ストリーミング中は使用しないでください。他のソフトウェアが同様の機能を実装していないことを確認してください。",
            EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());

        if(C.LockFace)
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

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"""
                Phase={this.Phase}
                BrightsphereCasts={this.BrightsphereCasts}
                """);
        }
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(!C.ShouldCheckOnStart || C.UsePseudostaticGroups)
        {
            return;
        }

        if(category == DirectorUpdateCategory.Commence ||
            (category == DirectorUpdateCategory.Recommence && Controller.Phase == 2))
        {
            SelfTest();
        }
    }

    private void SelfTest()
    {
        Svc.Chat.PrintChat(new XivChatEntry
        {
            Message = new SeStringBuilder()
                .AddUiForeground("= P2 Sancity of The Ward First self-test =", (ushort)UIColor.LightBlue).Build()
        });
        var party = FakeParty.Get();
        var pairCharacter = ResolvePairCharacterNameFromParty();
        if(pairCharacter != null)
        {
            Svc.Chat.PrintChat(new XivChatEntry
            { Message = new SeStringBuilder().AddUiForeground("Test Success! Partner: " + pairCharacter, (ushort)UIColor.Green).Build() });
        }
        else
        {
            Svc.Chat.PrintChat(new XivChatEntry
            {
                Message = new SeStringBuilder()
                    .AddUiForeground($"Could not find player {C.PriorityData.GetFirstValidList()?.List.First().Name}\n",
                        (ushort)UIColor.Red)
                    .AddUiForeground("!!! Test failed !!!", (ushort)UIColor.Red).Build()
            });
        }
    }

    private enum ClockwiseDirection
    {
        None,
        Clockwise,
        CounterClockwise
    }

    private enum ResolvePosition
    {
        ZephiranFaceToFace,
        ZephiranBack
    }

    private enum ZephiranDirection
    {
        None,
        NorthEast,
        SouthEast,
        SouthWest,
        NorthWest
    }

    Vector3?[] Positions = [null, null, null];
    private class Config
    {
        public bool LockFace = true;
        public bool LockFaceEnableWhenNotMoving = true;
        public OnePriorityData PriorityData = new();
        public ResolvePosition ResolvePosition = ResolvePosition.ZephiranFaceToFace;
        public bool ShouldCheckOnStart = true;
        public bool UsePseudostaticGroups = false;
        public bool PseudostaticWest = false;
        public bool PseudostaticAdjust = false;
    }

    public class OnePriorityData : PriorityData
    {
        public override int GetNumPlayers()
        {
            return 1;
        }
    }
    private string? ResolvePairCharacterNameFromParty()
    {
        var party = FakeParty.Get();
        var priorityList = C.PriorityData.GetFirstValidList()?.List;
        if(priorityList == null || priorityList.Count == 0)
        {
            Svc.Chat.PrintChat(new XivChatEntry
            {
                Message = new SeStringBuilder()
                    .AddUiForeground("Priority list is empty or null.", (ushort)UIColor.Red).Build()
            });
            return null;
        }

        foreach(var member in party)
        {
            if(C.PriorityData.GetPlayer(x => x.Name == member.Name.TextValue) is not null)
            {
                return member.Name.TextValue;
            }
        }

        Svc.Chat.PrintChat(new XivChatEntry
        {
            Message = new SeStringBuilder()
                .AddUiForeground("No matching pair character found in party.", (ushort)UIColor.Red).Build()
        });

        return null;
    }

    public PhaseDef Phase = PhaseDef.Start;
    public int BrightsphereCasts = 0;
    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action?.RowId == 25554)
        {
            Phase = PhaseDef.FirstMove;
        }
        if(set.Action?.RowId == 25295)
        {
            BrightsphereCasts++;
        }
    }

    public enum PhaseDef { Start, FirstMove, SecondMove,}
}