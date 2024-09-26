using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public class P6_Wyrmsbreath_First : SplatoonScript
{
    public enum BaitPosition
    {
        None,
        TriangleLowerLeft,
        TriangleLowerRight,
        TriangleUpper,
        UpperLeft,
        UpperRight
    }

    public enum EnchantmentType
    {
        None,
        Ice,
        Fire
    }

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
    public override Metadata? Metadata => new(2, "Garume");


    private Config C => Controller.GetConfig<Config>();

    public override void OnReset()
    {
        _enchantments.Clear();
        _state = State.None;
        _mayLeftTankStack = false;
        _mayRightTankStack = false;
        _myBaitPosition = BaitPosition.None;
    }


    public BaitPosition GetBaitPosition(string characterName)
    {
        DuoLog.Information(characterName);

        var index = Array.IndexOf(C.CharacterNames, characterName);
        return C.BaitPositions[index];
    }

    public string GetPairCharacterName(string characterName)
    {
        DuoLog.Information($"GetPairCharacterName {characterName}");
        var index = Array.IndexOf(C.CharacterNames, characterName);
        var baitPosition = C.BaitPositions[index];
        var indexs = C.BaitPositions.Select((x, i) => (x, i)).Where(x => x.x == baitPosition).Select(x => x.i).ToList();
        DuoLog.Information($"Indexs: {string.Join(", ", indexs)}");

        indexs.Remove(index);
        return C.CharacterNames[indexs[0]];
    }

    public override void OnGainBuffEffect(uint sourceId, List<uint> gainBuffIds)
    {
        if (_state != State.Start) return;
        if (gainBuffIds.Contains(2902) || gainBuffIds.Contains(2903))
            _state = State.End;
    }

    public EnchantmentType GetEnchantment(string characterName)
    {
        var enchantment = _enchantments[characterName];
        if (enchantment == EnchantmentType.None)
        {
            if (_enchantments.Values.Count(x => x == EnchantmentType.Ice) == 2)
                return EnchantmentType.Ice;
            if (_enchantments.Values.Count(x => x == EnchantmentType.Fire) == 2)
                return EnchantmentType.Fire;
        }

        return enchantment;
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (!vfxPath.StartsWith("vfx/channeling/eff/chn")) return;
        if (_state != State.None) return;
        _enchantments[target.GetObject().Name.ToString()] = vfxPath switch
        {
            "vfx/channeling/eff/chn_ice_mouth01x.avfx" => EnchantmentType.Ice,
            "vfx/channeling/eff/chn_fire_mouth01x.avfx" => EnchantmentType.Fire,
            _ => EnchantmentType.None
        };


        if (_enchantments.Count == 6)
        {
            _state = State.Start;
            _myBaitPosition = GetBaitPosition(Player.Name);
            var party = _enchantments.Keys.ToList();
            if (C.SwapIfNeeded)
            {
                var myEnchantment = GetEnchantment(Player.Name);
                var pairCharacterName = GetPairCharacterName(Player.Name);
                var pairEnchantment = GetEnchantment(pairCharacterName);

                DuoLog.Information($"Pair: {pairCharacterName} {pairEnchantment}");

                if (myEnchantment == pairEnchantment)
                {
                    party.Remove(Player.Object.Name.ToString());
                    party.Remove(pairCharacterName);

                    // other 1
                    var otherPartyMember1 = party[0];
                    var otherPartyMember1Enchantment = GetEnchantment(otherPartyMember1);
                    var otherPartyMember1Pair = GetPairCharacterName(otherPartyMember1);

                    DuoLog.Information($"{otherPartyMember1} {otherPartyMember1Pair}");

                    var otherPartyMember1PairEnchantment = GetEnchantment(otherPartyMember1Pair);

                    DuoLog.Information($"{otherPartyMember1Enchantment} {otherPartyMember1PairEnchantment}");

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
            tether = true
        };
        Controller.TryRegisterElement("Bait", element);
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

        if (C.CharacterNames.Length != 8)
        {
            C.CharacterNames = ["", "", "", "", "", "", "", ""];
            C.BaitPositions = new BaitPosition[8];
        }

        for (var i = 0; i < 8; i++)
        {
            ImGui.PushID("Character" + i);
            ImGui.Text($"Character {i + 1}");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            ImGui.InputText($"##Character{i}", ref C.CharacterNames[i], 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            ImGuiEx.EnumCombo($"##BaitPosition{i}", ref C.BaitPositions[i]);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.BeginCombo("##partysel", "Select from party"))
            {
                foreach (var x in FakeParty.Get())
                    if (ImGui.Selectable(x.Name.ToString()))
                        C.CharacterNames[i] = x.Name.ToString();
                ImGui.EndCombo();
            }

            ImGui.PopID();
        }

        ImGui.Checkbox("Swap if needed", ref C.SwapIfNeeded);
        ImGui.ColorEdit4("Bait Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Bait Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
    }


    private enum State
    {
        None,
        Start,
        End
    }

    private class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public BaitPosition[] BaitPositions = new BaitPosition[8];
        public string[] CharacterNames = ["", "", "", "", "", "", "", ""];
        public bool SwapIfNeeded;
    }
}