using Dalamud.Interface;
using ECommons;
using ECommons.Automation.NeoTaskManager;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Schedulers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class QuestionableQuestQueue : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [];
    public override Metadata? Metadata => new(5, "NightmareXIV");

    [EzIPC("Questionable.IsRunning", false)] Func<bool> QuestionableIsRunning;
    [EzIPC("Questionable.StartSingleQuest", false)] Func<string, bool> QuestionableStartSingleQuest;
    [EzIPC("Questionable.StartQuest", false)] Func<string, bool> QuestionableStartQuest;
    [EzIPC("Questionable.GetCurrentQuestId", false)] Func<string> QuestionableGetCurrentQuestId;

    [EzIPC("Lifestream.IsBusy", false)] public Func<bool> LifestreamIsBusy;
    [EzIPC("Lifestream.Teleport", false)] public Func<uint, byte, bool> LifestreamTeleport;
    [EzIPC("Lifestream.AethernetTeleport", false)] public Func<string, bool> LifestreamAethernetTeleport;


    Config C => Controller.GetConfig<Config>();
    Dictionary<uint, string> Aetherytes = [new KeyValuePair<uint, string>(0, "Disabled"), .. Svc.Data.GetExcelSheet<Aetheryte>().Where(x => x.IsAetheryte && x.PlaceName.Value.Name != "").ToDictionary(x => x.RowId, x => x.PlaceName.Value.Name.GetText())];
    Dictionary<uint, string> Aethernets = [new KeyValuePair<uint, string>(0, "Disabled"), ..Svc.Data.GetExcelSheet<Aetheryte>().Where(x => !x.IsAetheryte && x.AethernetName.Value.Name != "").ToDictionary(x => x.RowId, x => x.AethernetName.Value.Name.GetText())];

    public bool IsNotified = false;

    public override void OnSetup()
    {
        EzIPC.Init(this);
    }

    TaskManager TaskManager;

    public override void OnEnable()
    {
        TaskManager = new(new(timeLimitMS: 30000, abortOnTimeout: true, showDebug: true));
    }

    public override void OnDisable()
    {
        TaskManager.Dispose();
    }

    public override void OnUpdate()
    {
        if(!C.Active) return;
        if(EzThrottler.Throttle(this.InternalData.FullName + "Notify", 60000)) DuoLog.Warning($"{this.InternalData.Name} is running.");
        if(QuestionableIsRunning())
        {
            IsNotified = false;
            var allowedQuests = C.Quests.Where(x => x.Enabled && !QuestManager.IsQuestComplete(x.ID + 65536)).Select(x => x.ID.ToString());
            if(!allowedQuests.Contains(QuestionableGetCurrentQuestId()))
            {
                if(EzThrottler.Check(this.InternalData.FullName + "NoRestart"))
                {
                    Svc.Commands.ProcessCommand("/qst stop");
                }
            }
            else
            {
                EzThrottler.Throttle(this.InternalData.FullName + "NoRestart", 3000, true);
            }
        }
        if(IsBusy() || TaskManager.IsBusy)
        {
            EzThrottler.Throttle(this.InternalData.FullName + "Busy", 1000, true);
        }
        if(EzThrottler.Check(this.InternalData.FullName + "Busy"))
        {
            var next = C.Quests.FirstOrDefault(x => x.Enabled && !QuestManager.IsQuestComplete(x.ID + 65536));
            if(next == null)
            {
                if(!IsNotified)
                {
                    DuoLog.Warning("No more quests left in queue, script disabled");
                    Splatoon.Splatoon.P.NotificationMasterApi.DisplayTrayNotification(this.InternalData.Name, "No more quests left in queue");
                    Splatoon.Splatoon.P.NotificationMasterApi.FlashTaskbarIcon();
                }
                if(C.AutoDeactivate) C.Active = false;
                IsNotified = true; 
            }
            else
            {
                IsNotified = false;
                Enqueue(next);
            }
        }
    }

    void Enqueue(QuestInfo data)
    {
        if(data.Aetheryte != 0)
        {
            var aetheryte = Svc.Data.GetExcelSheet<Aetheryte>().GetRowOrDefault(data.Aetheryte);
            if(aetheryte != null && aetheryte.Value.Territory.RowId != Player.Territory)
            {
                TaskManager.Enqueue(() => LifestreamTeleport(data.Aetheryte, 0), "Teleport");
                TaskManager.Enqueue(() => !GenericHelpers.IsScreenReady(), "Wait 1");
                TaskManager.Enqueue(() => !IsBusy(), "Wait 2");
            }
        }
        if(data.Aethernet != 0)
        {
            TaskManager.Enqueue(() => LifestreamAethernetTeleport(this.Aethernets[data.Aethernet]), "Aethernet teleport");
            TaskManager.Enqueue(() => !GenericHelpers.IsScreenReady(), "Wait 3");
            TaskManager.Enqueue(() => !IsBusy(), "Wait 4");
        }
        TaskManager.Enqueue(() => (data.Cont? QuestionableStartQuest: QuestionableStartSingleQuest)(data.ID.ToString()), "Start quest");
    }

    public bool IsBusy() => !Player.Interactable || QuestionableIsRunning() || LifestreamIsBusy() || GenericHelpers.IsOccupied() || Player.IsAnimationLocked || !GenericHelpers.IsScreenReady() || Player.Object.IsCasting;

    ImGuiEx.RealtimeDragDrop<QuestInfo> DragDrop = new("QuestInfoDragDrop", (x) => x.DragDropID);
    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Automatically do quests", ref C.Active);
        ImGui.SameLine();
        ImGui.Checkbox("Auto deactivate on completion", ref C.AutoDeactivate);
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add new quest"))
        {
            C.Quests.Add(new());
        }
        if(TaskManager.IsBusy)
        {
            ImGui.SameLine();
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Ban, "Stop all tasks"))
            {
                TaskManager.Abort();
            }
        }
        ImGui.SameLine();
        ImGuiEx.Text($"Qst: {QuestionableGetCurrentQuestId()}");
        DragDrop.Begin();
        if(ImGuiEx.BeginDefaultTable(["##drag", "##enabled", "~Quest", "Aetheryte", "Aethernet", "##delete"]))
        {
            for(int i = 0; i < C.Quests.Count; i++)
            {
                ImGui.PushID($"Quest{i}");
                var q = C.Quests[i];
                ImGui.TableNextRow();
                DragDrop.SetRowColor(q.DragDropID);
                ImGui.TableNextColumn();
                DragDrop.NextRow();
                DragDrop.DrawButtonDummy(q, C.Quests, i);
                ImGui.TableNextColumn();
                ImGui.Checkbox("##enabled", ref q.Enabled);
                ImGui.SameLine();
                ImGuiEx.ButtonCheckbox(FontAwesomeIcon.FastForward, ref q.Cont);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(100f);
                var quest = Svc.Data.GetExcelSheet<Quest>().GetRowOrDefault(q.ID + 65536);
                ImGuiEx.InputUint($"{quest?.Name ?? "Invalid quest"}###questid", ref q.ID);
                if(q.ID > 65536) q.ID -= 65536;
                if(quest != null)
                {
                    if(QuestManager.IsQuestComplete(quest.Value.RowId))
                    {
                        ImGuiEx.HelpMarker("This quest is completed", EColor.GreenBright, FontAwesomeIcon.Check.ToIconString());
                    }
                    else
                    {
                        ImGuiEx.HelpMarker("This quest is not completed", EColor.YellowBright, FontAwesomeIcon.Times.ToIconString());
                    }
                }

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(150f);
                ImGuiEx.Combo("##aetheryte", ref q.Aetheryte, this.Aetherytes.Keys, names: this.Aetherytes);
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(150f);
                ImGuiEx.Combo("##aethernet", ref q.Aethernet, this.Aethernets.Keys, names: this.Aethernets);
                ImGui.TableNextColumn();
                if(ImGuiEx.IconButton(FontAwesomeIcon.Play))
                {
                    Enqueue(q);
                }
                ImGui.SameLine();
                if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                {
                    new TickScheduler(() => C.Quests.Remove(q));
                }
                ImGui.PopID();
            }
            ImGui.EndTable();
            DragDrop.End();
        }
    }

    public class Config : IEzConfig
    {
        public bool Active = false;
        public List<QuestInfo> Quests = [];
        public bool AutoDeactivate = true;
    }

    public class QuestInfo(uint iD, uint aetheryte, uint aethernet = 0)
    {
        public QuestInfo():this(0,0) { }

        internal string DragDropID = Guid.NewGuid().ToString();
        public bool Enabled = true;
        public uint ID = iD;
        public uint Aetheryte = aetheryte;
        public uint Aethernet = aethernet;
        public bool Cont = false;
    }

}
