using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using ECommons;
using ECommons.ChatMethods;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon;
using Splatoon.Serializables;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public class P6_Wyrmsbreath_First : SplatoonScript
{
    private readonly Vector2 _centerPosition = new(100f, 100f);

    private readonly Dictionary<string, EnchantmentType> _enchantments = new();
    private readonly Vector2 _lowerLeftPosition = new(95f, 118.5f);
    private readonly Vector2 _lowerRightPosition = new(105f, 118.5f);
    private readonly Vector2 _upperLeftPosition = new(85f, 88f);
    private readonly Vector2 _upperPosition = new(100f, 108f);
    private readonly Vector2 _upperRightPosition = new(115f, 88f);
    private bool _mayLeftTankStack;

    private bool _mayRightTankStack;
    private BaitPosition _myBaitPosition = BaitPosition.None;
    private State _state = State.None;
    public override HashSet<uint>? ValidTerritories => [968];
    public override Metadata? Metadata => new(5, "Garume");


    private Config C => Controller.GetConfig<Config>();

    public override void OnReset()
    {
        _enchantments.Clear();
        _state = State.None;
        _mayLeftTankStack = false;
        _mayRightTankStack = false;
        _myBaitPosition = BaitPosition.None;
    }


    private BaitPosition GetBaitPosition(string characterName)
    {
        Alert(characterName);

        var index = Array.IndexOf(C.Priority.GetFirstValidList()?.List.ToArray() ?? [], characterName);
        return C.BaitPositions[index];
    }

    private string GetPairCharacterName(string characterName)
    {
        Alert($"GetPairCharacterName {characterName}");
        var index = Array.IndexOf(C.Priority.GetFirstValidList()?.List.ToArray() ?? [], characterName);
        var baitPosition = C.BaitPositions[index];
        var indexs = C.BaitPositions.Select((x, i) => (x, i)).Where(x => x.x == baitPosition).Select(x => x.i).ToList();
        Alert($"Indexs: {string.Join(", ", indexs)}");

        indexs.Remove(index);
        return C.Priority.GetFirstValidList()?.List[indexs.First()].Name ?? "";
    }

    public override void OnGainBuffEffect(uint sourceId, Status Status)
    {
        if (_state != State.Start) return;
        if (Status.StatusId is 2902 or 2903)
            _state = State.End;
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (_state != State.None) return;
        if (target.GetObject() is not IBattleChara targetObject) return;
        var targetDataId = targetObject.DataId;
        if (targetDataId != 0x3144 && targetDataId != 0x3145) return;
        if (source.GetObject() is not ICharacter sourceCharacter) return;
        _enchantments[sourceCharacter.Name.ToString()] = targetDataId switch
        {
            0x3144 => EnchantmentType.Fire,
            0x3145 => EnchantmentType.Ice,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (_enchantments.Count == 6)
        {
            _state = State.Start;
            _myBaitPosition = GetBaitPosition(Player.Name);
            var party = _enchantments.Keys.ToList();
            if (C.SwapIfNeeded)
            {
                var myEnchantment = _enchantments[Player.Name];
                var pairCharacterName = GetPairCharacterName(Player.Name);
                var pairEnchantment = _enchantments[pairCharacterName];

                Alert($"Pair: {pairCharacterName} {pairEnchantment}");

                if (myEnchantment == pairEnchantment)
                {
                    party.Remove(Player.Object.Name.ToString());
                    party.Remove(pairCharacterName);

                    // other 1
                    var otherPartyMember1 = party[0];
                    var otherPartyMember1Enchantment = _enchantments[otherPartyMember1];
                    var otherPartyMember1Pair = GetPairCharacterName(otherPartyMember1);

                    Alert($"{otherPartyMember1} {otherPartyMember1Pair}");

                    var otherPartyMember1PairEnchantment = _enchantments[otherPartyMember1Pair];

                    Alert($"{otherPartyMember1Enchantment} {otherPartyMember1PairEnchantment}");

                    if (otherPartyMember1Enchantment != otherPartyMember1PairEnchantment)
                    {
                        party.Remove(otherPartyMember1);
                        party.Remove(otherPartyMember1Pair);

                        // other 2
                        var otherPartyMember2 = party[0];
                        _myBaitPosition = GetBaitPosition(otherPartyMember2);
                    }
                    else
                    {
                        _myBaitPosition = GetBaitPosition(otherPartyMember1);
                    }
                }
            }
        }
    }


    public override void OnStartingCast(uint source, uint castId)
    {
        if (_state == State.End) return;

        if (castId is 27954 or 27955 or 27956 or 27957)
        {
            switch (castId)
            {
                case 27955:
                    _mayRightTankStack = true;
                    break;
                case 27957:
                    _mayLeftTankStack = true;
                    break;
            }

            if (_myBaitPosition != BaitPosition.None)
            {
                var position = _myBaitPosition switch
                {
                    BaitPosition.TriangleLowerLeft => _lowerLeftPosition,
                    BaitPosition.TriangleLowerRight => _lowerRightPosition,
                    BaitPosition.TriangleUpper => _upperPosition,
                    BaitPosition.UpperLeft => _upperLeftPosition,
                    BaitPosition.UpperRight => _upperRightPosition,
                    _ => Vector2.Zero
                };

                if (_myBaitPosition is BaitPosition.UpperLeft or BaitPosition.UpperRight)
                    if (_mayLeftTankStack && _mayRightTankStack)
                        position = _centerPosition;

                if (Controller.TryGetElementByName("Bait", out var element))
                {
                    element.Enabled = true;
                    element.SetOffPosition(position.ToVector3());
                }
            }
        }
    }

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            radius = 1f,
            thicc = 6f,
            Donut = 0.35f,
            LineEndA = LineEnd.Arrow,
            tether = true
        };
        Controller.TryRegisterElement("Bait", element);
    }

    private void Alert(string message)
    {
        if (C.ShouldShowDebugMessage)
            DuoLog.Information(message);
    }

    public override void OnUpdate()
    {
        if (_state is State.None or State.End)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            return;
        }

        Controller.GetRegisteredElements().Each(x =>
            x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Character List");

        ImGui.SameLine();
        ImGuiEx.Spacing();
        if (ImGui.Button("Perform test")) SelfTest();

        ImGuiEx.Tooltip("The list is populated based on the job.\nYou can adjust the priority from the option header.");

        C.Priority.Draw();
        for (var i = 0; i < 8; i++)
        {
            ImGui.PushID("Character" + i);
            ImGui.Text($"Character {i + 1}");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            ImGuiEx.EnumCombo($"##BaitPosition{i}", ref C.BaitPositions[i]);
            ImGui.PopID();
        }

        ImGui.Checkbox("Swap if needed", ref C.SwapIfNeeded);
        ImGui.ColorEdit4("Bait Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Bait Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
        ImGui.Checkbox("Check on Start", ref C.ShouldCheckOnStart);


        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Checkbox("Show Debug Message", ref C.ShouldShowDebugMessage);
            ImGui.Text($"State: {_state}");
            ImGui.Text($"My Bait Position: {_myBaitPosition}");
            ImGui.Text("Enchantments");
            foreach (var enchantment in _enchantments)
                ImGui.Text($"{enchantment.Key}: {enchantment.Value}");
        }
    }

    private void SelfTest()
    {
        Svc.Chat.PrintChat(new XivChatEntry
        {
            Message = new SeStringBuilder()
                .AddUiForeground("= P5 Death of the Heavens self-test =", (ushort)UIColor.LightBlue).Build()
        });
        var isCorrect = C.Priority.GetFirstValidList() != null;

        if (!isCorrect)
        {
            Svc.Chat.PrintChat(new XivChatEntry
            {
                Message = new SeStringBuilder()
                    .AddUiForeground("Priority list is not filled correctly.", (ushort)UIColor.Red)
                    .AddUiForeground("!!! Test failed !!!", (ushort)UIColor.Red).Build()
            });
            return;
        }

        Svc.Chat.PrintChat(new XivChatEntry
        {
            Message = new SeStringBuilder()
                .AddUiForeground("Test Success!", (ushort)UIColor.Green).Build()
        });
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (!C.ShouldCheckOnStart)
            return;
        if (category == DirectorUpdateCategory.Commence ||
            (category == DirectorUpdateCategory.Recommence && Controller.Phase == 2))
            SelfTest();
    }

    private enum BaitPosition
    {
        None,
        TriangleLowerLeft,
        TriangleLowerRight,
        TriangleUpper,
        UpperLeft,
        UpperRight
    }

    private enum EnchantmentType
    {
        Ice,
        Fire
    }

    private enum State
    {
        None,
        Start,
        End
    }

    private class Config : IEzConfig
    {
        public readonly BaitPosition[] BaitPositions = new BaitPosition[8];
        public PriorityData Priority = new();
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();

        public bool ShouldCheckOnStart = true;
        public bool ShouldShowDebugMessage;
        public bool SwapIfNeeded;
    }
}
