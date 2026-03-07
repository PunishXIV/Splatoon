using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Schedulers;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using Splatoon;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using static Splatoon.Splatoon;
using static Dalamud.Bindings.ImGui.ImGui;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class Another_Merchants_Take_B1_Sunken_Treasure : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => [1317,];
    public override Metadata? Metadata => new(1, "redmoon");

    private class ObjectData
    {
        public uint EntityId = 0;
        public uint BaseId = 0;
        public Vector3 Position = Vector3.Zero;
        public bool IsBroke = false;
    }

    private readonly List<ObjectData> _objectDataList = [];

    public override void OnSetup()
    {
        for (var i = 0; i < 5; i++) Controller.RegisterElement($"aoe{i}", new Element(0));
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        var castId = set.Action?.RowId ?? 0;
        if (castId == 45851) { }
    }

    public override void OnObjectCreation(IntPtr newObjectPtr)
    {
        Controller.Schedule(() =>
        {
            var obj = Svc.Objects.FirstOrDefault(x => x.Address == newObjectPtr);
            if (obj == null || (obj.BaseId != 2015004 && obj.BaseId != 2015005)) return;
            _objectDataList.Add(new ObjectData
            {
                EntityId = obj.EntityId,
                BaseId = obj.BaseId,
                Position = obj.Position,
                IsBroke = false,
            });
        }, 0);
    }

    public override void OnObjectEffect(uint target, ushort data1, ushort data2)
    {
        if (_objectDataList.Count == 0) return;
        if (data1 == 16 && data2 == 32)
        {
            var obj = _objectDataList.FirstOrDefault(x => x.EntityId == target);
            if (obj == null) return;
            obj.IsBroke = true;
        }
        else if (data1 == 4 && data2 == 8)
        {
            var index = _objectDataList.FindIndex(x => x.EntityId == target);
            if (index == -1) return;
            _objectDataList.RemoveAt(index);
        }
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        if (_objectDataList.Count == 0) return;

        // IsBroke == false のインスタンスはsortで一番後にする
        if (_objectDataList.Count(x => x.IsBroke) == 5) _objectDataList.Sort((x, y) => !x.IsBroke ? 1 : -1);

        var index = 0;
        foreach (var obj in _objectDataList)
        {
            if (!obj.IsBroke) continue;
            if (Controller.TryGetElementByName($"aoe{index}", out var e))
            {
                e.radius = obj.BaseId == 2015004 ? 18f : 4f;
                e.Donut = obj.BaseId == 2015004 ? 0f : 16f;
                e.Enabled = true;
                e.SetRefPosition(obj.Position);
            }

            index++;
            if (index == 5) break;
        }
    }

    public override void OnReset()
    {
        _objectDataList.Clear();
        Controller.Hide();
    }

    public override void OnSettingsDraw()
    {
        // Debug
        if (!ImGuiEx.CollapsingHeader("Debug")) return;
        foreach (var obj in _objectDataList)
            ImGuiEx.Text($"Entity: {obj.EntityId} Base: {obj.BaseId} Pos: {obj.Position} IsBroke: {obj.IsBroke}");
        Separator();
        Text("Elements:");
        foreach (var e in Controller.GetRegisteredElements())
            ImGuiEx.Text(
                $"{e.Key}: Enabled={e.Value.Enabled} Pos=({e.Value.refX}, {e.Value.refZ}, {e.Value.refY})");
    }
}