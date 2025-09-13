using Dalamud.Memory;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation;
using ECommons.Automation.LegacyTaskManager;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.EzIpcManager;
using ECommons.EzSharedDataManager;
using ECommons.ImGuiMethods;
using ECommons.UIHelpers;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Callback = ECommons.Automation.Callback;
#pragma warning disable
namespace SplatoonScriptsOfficial.Tests;
public unsafe class GenericTest4 : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => new();
    public override Metadata? Metadata { get; } = new(4, "NightmareXIV");
    int a1;
    string Filter = "";

    public override void OnSettingsDraw()
    {

        if(TaskManager?.IsBusy == true)
        {
            if(ImGui.Button("Stop")) TaskManager.Abort();
            return;
        }
        ImGui.InputInt("Duty ID", ref a1);
        if(ImGui.BeginCombo("Duty", Svc.Data.GetExcelSheet<ContentFinderCondition>().GetRowOrDefault((uint)a1)?.Name.ExtractText()))
        {
            ImGui.InputText($"Filter", ref Filter, 50);
            foreach(var x in Svc.Data.GetExcelSheet<ContentFinderCondition>())
            {
                var name = x.Name.ExtractText();
                if(Filter != "" && !name.Contains(Filter, StringComparison.OrdinalIgnoreCase)) continue;
                if(name == "") continue;
                if(x.TerritoryType.Value.TerritoryIntendedUse.RowId == (uint)TerritoryIntendedUseEnum.Quest_Battle || x.TerritoryType.Value.TerritoryIntendedUse.RowId == (uint)TerritoryIntendedUseEnum.Quest_Battle_2) continue;
                if(ImGui.Selectable(x.Name.ExtractText()))
                {
                    a1 = (int)x.RowId;
                }
            }
            ImGui.EndCombo();
        }
        if(ImGui.Button("SelectDuty"))
        {
            TaskSelectDuty((uint)a1);
        }
        if(GenericHelpers.TryGetAddonByName<AddonContentsFinder>("ContentsFinder", out var addon))
        {
            var list = addon->AtkUnitBase.GetNodeById(52)->GetAsAtkComponentList();
            var length = addon->NumEntries;
            var reader = new ReaderAddonContentsFinder(&addon->AtkUnitBase);
            for(int i = 0; i < length; i++)
            {
                var duty = reader.Duties[i];
                if(Regex.IsMatch(duty.DutyLevel, @"Lv\. ([0-9]{1,3})"))
                {
                    ImGuiEx.Text($"{duty.DutyName}");
                }
            }
        }
    }

    TaskManager TaskManager;

    public override void OnEnable()
    {
        TaskManager = new();
    }

    public override void OnDisable()
    {
        TaskManager.Dispose();
    }


    public void TaskSelectDuty(uint cfc)
    {
        TaskManager.Enqueue(EnsureDFClosed);
        TaskManager.Enqueue(() => OpenDF(cfc));
        TaskManager.Enqueue(ClearDuty);
        TaskManager.Enqueue(() => SelectDuty(cfc));
    }

    public void EnsureDFClosed()
    {
        if(GenericHelpers.TryGetAddonByName<AtkUnitBase>("ContentsFinder", out var addon))
        {
            addon->Close(true);
        }
    }

    public bool OpenDF(uint cfc)
    {
        if(!GenericHelpers.TryGetAddonByName<AtkUnitBase>("ContentsFinder", out _))
        {
            AgentContentsFinder.Instance()->OpenRegularDuty(cfc);
            return true;
        }
        return false;
    }

    public bool? ClearDuty()
    {
        if(GenericHelpers.TryGetAddonByName<AtkUnitBase>("ContentsFinder", out var addon) && GenericHelpers.IsAddonReady(addon))
        {
            var btn = addon->GetComponentButtonById(73);
            if(btn->IsEnabled)
            {
                Callback.Fire(addon, true, 12, 1);
            }
            else
            {
                return true;
            }
        }
        return false;
    }

    public bool? SelectDuty(uint cfc)
    {
        if(GenericHelpers.TryGetAddonByName<AddonContentsFinder>("ContentsFinder", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
        {
            var cfcData = Svc.Data.GetExcelSheet<ContentFinderCondition>().GetRowOrDefault(cfc);
            if(cfcData == null || cfcData.Value.Name.ExtractText() == "") throw new ArgumentOutOfRangeException(nameof(cfc));
            var list = addon->AtkUnitBase.GetNodeById(52)->GetAsAtkComponentList();
            var length = addon->NumEntries;
            var reader = new ReaderAddonContentsFinder(&addon->AtkUnitBase);
            int cnt = 0;
            for(int i = 0; i < length; i++)
            {
                var duty = reader.Duties[i];
                if(Regex.IsMatch(duty.DutyLevel, @"^Lv\. ([0-9]{1,3})$"))
                {
                    cnt++;
                    if(duty.DutyName == cfcData.Value.Name.ExtractText())
                    {
                        Callback.Fire(&addon->AtkUnitBase, true, 3, cnt);
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public class ReaderAddonContentsFinder : AtkReader
    {
        public ReaderAddonContentsFinder(AtkUnitBase* UnitBase, int BeginOffset = 0) : base(UnitBase, BeginOffset)
        {
        }

        public List<DutyInfo> Duties => Loop<DutyInfo>(788, 3, 0x78);

        public class DutyInfo : AtkReader
        {
            private nint unitBasePtr;
            private int beginOffset;
            public DutyInfo(nint UnitBasePtr, int BeginOffset = 0) : base(UnitBasePtr, BeginOffset)
            {
                this.unitBasePtr = UnitBasePtr;
                this.beginOffset = BeginOffset;
            }

            public string DutyName => ReadSeString(0).ExtractText();
            public string DutyLevel => MemoryHelper.ReadStringNullTerminated((nint)((AtkUnitBase*)unitBasePtr)->AtkValues[beginOffset + 1].String.Value);
            public string DutyPF => ReadString(2);
        }
    }
}
