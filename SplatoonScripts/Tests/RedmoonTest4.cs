// Ignore Spelling: Metadata Leve wks

using ECommons;
using ECommons.Automation;
using ECommons.Automation.UIInput;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Callback = ECommons.Automation.Callback;

namespace SplatoonScriptsOfficial.Tests;
internal unsafe class RedmoonTest4 :SplatoonScript
{
    private class NodeData
    {
        public AtkResNode* ResNode = null;
        public AtkComponentBase* Component = null;

        public bool HasData => ResNode != null && Component != null;
        public NodeData(AtkResNode* node, AtkComponentBase* component)
        {
            ResNode = node;
            Component = component;
        }
    }

    private enum Achevement
    {
        None = 0,
        Silver,
        Gold,
    }

    private class LeveData
    {
        public int index = 0;
        public uint RowId = 0u;
        public byte Rank = 0;
        public string Name { get; set; }
        public AtkResNode* ResNode = null;
        public int CosmoCredit = 0;
        public int MoonCredit = 0;
        public int TokenLv1 = 0;
        public int TokenLv2 = 0;
        public int TokenLv3 = 0;
        public int TokenLv4 = 0;
        public Achevement Achievement = Achevement.None;
        public LeveData(int index, uint rowId, byte rank, string name, AtkResNode* resNode, Achevement achievement)
        {
            this.index = index;
            Achievement = achievement;
            RowId = rowId;
            Rank = rank;
            Name = name;
            ResNode = resNode;
        }

        public void Select([DisallowNull] AtkUnitBase* wksMission)
        {
            int achievement = 0;
            if (Achievement == Achevement.Gold)
            {
                achievement = 8;
            }
            else if (Achievement == Achevement.Silver)
            {
                achievement = 4;
            }
            Callback.Fire(wksMission, true, 12, RowId, (uint)achievement, (uint)index);
        }

        public void GetTokenCounts([DisallowNull] AtkUnitBase* wksMission)
        {
            InternalLog.Information($"GetTokenCounts {RowId} {Name} {Rank}");
            for (uint i = 560001u; i < 560007u; i++)
            {
                if (!SearchNode(wksMission, i, out var multiNode))
                {
                    InternalLog.Error($"multiNode not found");
                    return;
                }

                if (!multiNode.ResNode->IsVisible()) continue;

                var ImageNode = (AtkImageNode*)SearchResNodeOnly(multiNode, 3);
                if (ImageNode == null)
                {
                    InternalLog.Error("texNode not found");
                }

                var str = GetTexturePath(ImageNode);

                if (!SearchNode(multiNode, 2, out var tokenCountNode))
                {
                    InternalLog.Error($"tokenCountNode not found");
                    return;
                }

                var textNode = (AtkTextNode*)SearchResNodeOnly(tokenCountNode, 2);
                if (textNode == null)
                {
                    InternalLog.Error($"textNode not found");
                    return;
                }

                InternalLog.Information($"GetTokenCounts result {str} {textNode->GetText()}");

                if (str.Contains("065000/065112_hr1.tex"))
                {
                    CosmoCredit = int.Parse(textNode->GetText().ToString());
                }
                if (str.Contains("065000/065126_hr1.tex"))
                {
                    MoonCredit = int.Parse(textNode->GetText().ToString());
                }
                if (str.Contains("070000/070803_hr1.tex"))
                {
                    TokenLv1 = int.Parse(textNode->GetText().ToString());
                }
                if (str.Contains("070000/070814_hr1.tex"))
                {
                    TokenLv2 = int.Parse(textNode->GetText().ToString());
                }
                if (str.Contains("070000/070825_hr1.tex"))
                {
                    TokenLv3 = int.Parse(textNode->GetText().ToString());
                }
                if (str.Contains("070000/070836_hr1.tex"))
                {
                    TokenLv4 = int.Parse(textNode->GetText().ToString());
                }
            }
        }
    }

    public class Config :IEzConfig
    {
        public bool SkipA = false;
    }

    public override HashSet<uint>? ValidTerritories { get; } = null;
    public override Metadata? Metadata => new(1, "Redmoon");

    private List<LeveData> _leveData = new();
    [DisallowNull]
    private long _delayTime = long.MinValue;
    private bool _setupDone = false;
    private Job _job = 0;
    private bool _start = false;

    public override void OnUpdate()
    {
        if (!_start)
        {
            this.OnReset();
            return;
        }
        if (_job != Player.Job)
        {
            InternalLog.Information($"Job changed {Player.Job}");
            WarmReset();
            _job = Player.Job;
        }
        if (!_setupDone)
        {
            SetUp();
            return;
        }
    }

    public override void OnReset()
    {
        _leveData.Clear();
        _setupDone = false;
        _delayTime = long.MinValue;
        _job = 0;
    }

    public override void OnSettingsDraw()
    {
        var c = Controller.GetConfig<Config>();
        ImGui.Checkbox("Skip A", ref c.SkipA);
        if (!_start)
        {
            if (ImGui.Button("Start")) _start = true;
        }
        else
        {
            if (ImGui.Button("Stop")) _start = false;
        }

        if (ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"_job: {_job}");
            ImGuiEx.Text($"DelayTime: {_delayTime}");
            ImGuiEx.Text($"SetupDone: {_setupDone}");
            ImGuiEx.Text($"LeveData Count: {_leveData.Count}");
            if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("WKSHud", out var wksHud))
            {
                ImGuiEx.Text(EzColor.RedBright, "WKSHud not found");
                return;
            }
            if (wksHud == null || !wksHud->IsReady()) return;

            if (!SearchNode(wksHud, 6, out var btnData)) return;
            if (ImGui.Button("Toggle"))
            {
                var btn = (AtkComponentButton*)btnData.Component;
                btn->ClickAddonButton(wksHud);
            }
            if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("WKSMission", out var addon)) return;
            if (addon == null || !addon->IsReady())
            {
                _leveData.Clear();
                ImGuiEx.Text(EzColor.RedBright, "WKSMission not found");
                return;
            }

            if (_leveData.Count != 0)
            {
                ImGuiEx.Text("Leve Data");
                List<ImGuiEx.EzTableEntry> entries = new();
                foreach (var leve in _leveData)
                {
                    if (leve == null) continue;
                    entries.Add(new ImGuiEx.EzTableEntry("Index", delegate { ImGui.Text(leve.index.ToString()); }));
                    entries.Add(new ImGuiEx.EzTableEntry("RowId", delegate { ImGui.Text(leve.RowId.ToString()); }));
                    entries.Add(new ImGuiEx.EzTableEntry("Rank", delegate { ImGui.Text(leve.Rank.ToString()); }));
                    entries.Add(new ImGuiEx.EzTableEntry("Name", delegate { ImGui.Text(leve.Name); }));
                    entries.Add(new ImGuiEx.EzTableEntry("Address", delegate { ImGui.Text(((nint)leve.ResNode).ToString("X")); }));
                    entries.Add(new ImGuiEx.EzTableEntry("CosmoCredit", delegate { ImGui.Text(leve.CosmoCredit.ToString()); }));
                    entries.Add(new ImGuiEx.EzTableEntry("MoonCredit", delegate { ImGui.Text(leve.MoonCredit.ToString()); }));
                    entries.Add(new ImGuiEx.EzTableEntry("TokenLv1", delegate { ImGui.Text(leve.TokenLv1.ToString()); }));
                    entries.Add(new ImGuiEx.EzTableEntry("TokenLv2", delegate { ImGui.Text(leve.TokenLv2.ToString()); }));
                    entries.Add(new ImGuiEx.EzTableEntry("TokenLv3", delegate { ImGui.Text(leve.TokenLv3.ToString()); }));
                    entries.Add(new ImGuiEx.EzTableEntry("TokenLv4", delegate { ImGui.Text(leve.TokenLv4.ToString()); }));
                    entries.Add(new ImGuiEx.EzTableEntry("GoldAchievement", delegate { ImGui.Text(leve.Achievement.ToString()); }));
                }
                ImGuiEx.EzTable("LeveData", entries);
            }

            var buttonPtr = addon->GetComponentButtonById(94);
            if (buttonPtr == null)
            {
                ImGuiEx.Text(EzColor.RedBright, "buttonPtr not found");
                return;
            }
            if (ImGui.Button("Click##Button94"))
            {
                buttonPtr->ClickAddonButton(addon);
            }

            if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("SelectYesno", out var SelectYesno))
            {
                ImGuiEx.Text(EzColor.RedBright, "SelectYesno not found");
                return;
            }

            buttonPtr = SelectYesno->GetComponentButtonById(8);
            if (buttonPtr == null)
            {
                ImGuiEx.Text(EzColor.RedBright, "buttonPtr not found");
                return;
            }
            if (ImGui.Button("Click##SelectYesno"))
            {
                buttonPtr->ClickAddonButton(SelectYesno);
            }
        }
    }

    private void WarmReset()
    {
        _setupDone = false;
        _delayTime = long.MinValue;
        _leveData.Clear();
    }

    private void SetUp()
    {
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("WKSMission", out var addon) ||
            addon == null ||
            !addon->IsReady())
        {
            WarmReset();
            _delayTime = Environment.TickCount64 + 500;
            return;
        }

        if (!SearchNode(addon, 8, out var btnData) || btnData.ResNode->IsVisible())
        {
            WarmReset();
            _delayTime = Environment.TickCount64 + 500;
            return;
        }

        if (_leveData.Count == 0 && _delayTime < Environment.TickCount64)
        {
            _leveData.Clear();
            _delayTime = long.MinValue;

            var treeComp = (AtkComponentTreeList*)addon->GetComponentByNodeId(18);
            if (treeComp == null) return;
            var treeCompRootNode = treeComp->OwnerNode;
            if (treeCompRootNode == null) return;
            var NodeList = treeComp->UldManager.NodeList;
            int index = 0;
            int prevRank = 0;
            for (var i = 0; i < treeComp->UldManager.NodeListCount; i++)
            {
                var node = NodeList[i];
                if (node == null) continue;
                if (node->Type != (NodeType)1027) continue;
                var comp = (AtkComponentListItemRenderer*)node->GetComponent();
                if (comp == null)
                {
                    ImGuiEx.Text(EzColor.RedBright, "Component not found");
                    continue;
                }
                var text = comp->ButtonTextNode;
                if (text == null || text->GetText() == "") break;
                var wKSMissionUnitData = Svc.Data.GetExcelSheet<WKSMissionUnit>()
                    .FirstOrNull(x => x.Name.ToString() == text->GetText().ToString()
                        && x.GoldStarRequirement == (ushort)(Player.Job + 1));
                if (wKSMissionUnitData == null) continue;
                var texNode = (AtkImageNode*)SearchResNodeOnly(new NodeData(node, (AtkComponentBase*)comp), 6);
                if (texNode == null)
                {
                    InternalLog.Error($"goldTexNode not found");
                    continue;
                }
                Achevement achevement = Achevement.None;
                if (texNode->IsVisible())
                {
                    string texturePath = GetTexturePath(texNode);
                    if (texturePath == "")
                    {
                        InternalLog.Error($"goldTexNode texturePath not found");
                        continue;
                    }
                    if (texturePath.Contains("WKSMission_hr1.tex") && (texNode->PartId == 19))
                    {
                        achevement = Achevement.Silver;
                    }
                    if (texturePath.Contains("WKSMission_hr1.tex") && (texNode->PartId == 18))
                    {
                        achevement = Achevement.Gold;
                    }
                }
                index++;
                if (wKSMissionUnitData.Value.LevelGroup != prevRank)
                {
                    if (prevRank != 0)
                    {
                        if (!((prevRank == 4 && wKSMissionUnitData.Value.LevelGroup == 5) ||
                              (prevRank == 5 && wKSMissionUnitData.Value.LevelGroup == 4)))
                        {
                            index++;
                        }
                    }
                    prevRank = wKSMissionUnitData.Value.LevelGroup;
                }
                _leveData.Add(new LeveData(
                    index,
                    wKSMissionUnitData.Value.RowId,
                    wKSMissionUnitData.Value.LevelGroup,
                    text->GetText().ToString(),
                    node,
                    achevement));
            }
        }

        if (_leveData.Count != 0 && addon != null)
        {
            if (_delayTime <= Environment.TickCount64)
            {
                if (_delayTime == long.MinValue)
                {
                    foreach (var leve in _leveData)
                    {
                        if (leve.CosmoCredit == 0 &&
                            leve.MoonCredit == 0 &&
                            leve.TokenLv1 == 0 &&
                            leve.TokenLv2 == 0 &&
                            leve.TokenLv3 == 0 &&
                            leve.TokenLv4 == 0)
                        {
                            leve.Select(addon);
                            _delayTime = Environment.TickCount64 + 300;
                            return;
                        }
                    }
                }
                foreach (var leve in _leveData)
                {
                    if (leve.CosmoCredit == 0 &&
                        leve.MoonCredit == 0 &&
                        leve.TokenLv1 == 0 &&
                        leve.TokenLv2 == 0 &&
                        leve.TokenLv3 == 0 &&
                        leve.TokenLv4 == 0)
                    {
                        leve.GetTokenCounts(addon);
                        _delayTime = long.MinValue;
                        return;
                    }
                }
                _delayTime = long.MaxValue;
                _setupDone = true;
            }
        }
    }

    private static string GetTexturePath([NotNull] AtkImageNode* imageNode)
    {
        var PartsListFirst = imageNode->PartsList[0];
        if (PartsListFirst.Parts->UldAsset == null) return "";
        if (PartsListFirst.Parts->UldAsset->AtkTexture.Resource == null) return "";
        if (PartsListFirst.Parts->UldAsset->AtkTexture.Resource->TexFileResourceHandle == null) return "";
        if (imageNode->PartsList[0].Parts->UldAsset->AtkTexture.TextureType == TextureType.Resource)
        {
            return imageNode->PartsList[0].Parts->
                UldAsset->AtkTexture.Resource->TexFileResourceHandle->FileName.ToString();
        }
        return "";
    }

    private static bool SearchNode(AtkUnitBase* WKSMissionAddon, uint nodeId, out NodeData nodeData)
    {
        nodeData = new NodeData(null, null);
        if (WKSMissionAddon == null || !WKSMissionAddon->IsReady()) return false;
        AtkResNode* resNode = null;
        for (int i = 0; i < (int)WKSMissionAddon->UldManager.NodeListCount; i++)
        {
            var node = WKSMissionAddon->UldManager.NodeList[i];
            if (node == null) continue;
            if (node->NodeId == nodeId)
            {
                resNode = (AtkResNode*)node;
                break;
            }
        }
        if (resNode == null) return false;
        if (resNode->GetComponent() == null) return false;
        nodeData.ResNode = resNode;
        nodeData.Component = resNode->GetComponent();
        return true;
    }

    private static bool SearchNode(NodeData nodeData, uint nodeId, out NodeData output)
    {
        output = new NodeData(null, null);
        if (!nodeData.HasData) return false;
        if (nodeData.Component->UldManager.NodeListCount == 0) return false;

        AtkResNode* resNode = null;
        for (int i = 0; i < (int)nodeData.Component->UldManager.NodeListCount; i++)
        {
            var node = nodeData.Component->UldManager.NodeList[i];
            if (node == null) continue;
            if (node->NodeId == nodeId)
            {
                resNode = node;
            }
        }
        if (resNode == null) return false;
        if (resNode->GetComponent() == null) return false;
        output.ResNode = resNode;
        output.Component = resNode->GetComponent();
        return true;
    }

    private static AtkResNode* SearchResNodeOnly(NodeData nodeData, uint nodeId)
    {
        if (!nodeData.HasData) return null;
        if (nodeData.Component->UldManager.NodeListCount == 0) return null;

        for (int i = 0; i < (int)nodeData.Component->UldManager.NodeListCount; i++)
        {
            var node = nodeData.Component->UldManager.NodeList[i];
            if (node == null) continue;
            if (node->NodeId == nodeId)
            {
                return node;
            }
        }

        return null;
    }
}
