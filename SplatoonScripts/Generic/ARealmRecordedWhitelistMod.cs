using Dalamud.Interface;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Reflection;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System;
using Player = ECommons.GameHelpers.LegacyPlayer.Player;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Generic;

public unsafe class ARealmRecordedWhitelistMod : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => null;
    public override Metadata Metadata => new(7, "lillylilim, NightmareXIV, Poneglyph");

    public class WhitelistEntry
    {
        public string Name { get; set; } = string.Empty;
        public uint Id { get; set; }
        public bool Enabled { get; set; } = true;

        public WhitelistEntry() { }

        public WhitelistEntry(string name, uint id, bool enabled = true)
        {
            Name = name;
            Id = id;
            Enabled = enabled;
        }
    }

    public override void OnEnable()
    {
        Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        Svc.ClientState.Login += ClientState_Login;

        LoadContentTypeSheet();

        if(Player.Available)
        {
            ClientState_Login();
        }
    }

    private List<(uint Id, string Name)> _cachedContentTypes = [];
    private int _selectedContentTypeIndex = 0;

    private void LoadContentTypeSheet()
    {
        try
        {
            var sheet = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.ContentType>();
            if(sheet != null)
            {
                foreach(var row in sheet)
                {
                    uint id = row.RowId;
                    string name = row.Name.ToString();
                    if(string.IsNullOrWhiteSpace(name))
                    {
                        name = "No Name";
                    }
                    _cachedContentTypes.Add((id, name));
                }
                _cachedContentTypes = _cachedContentTypes.OrderBy(x => x.Id).ToList();
            }
            else
            {
                var sheetObj = Svc.Data.GetType().GetMethod("GetExcelSheet", [typeof(uint)])?.Invoke(Svc.Data, [1u]);
                if(sheetObj == null)
                {
                    sheetObj = Svc.Data.GetType().GetMethod("GetExcelSheet", [typeof(string)])?.Invoke(Svc.Data, ["ContentType"]);
                }

                if(sheetObj != null && sheetObj is System.Collections.IEnumerable enumerable)
                {
                    foreach(var row in enumerable)
                    {
                        var rowIdProp = row.GetType().GetProperty("RowId");
                        var nameProp = row.GetType().GetProperty("Name");
                        if(rowIdProp != null)
                        {
                            uint id = (uint)rowIdProp.GetValue(row)!;
                            string name = nameProp?.GetValue(row)?.ToString() ?? string.Empty;
                            if(string.IsNullOrWhiteSpace(name))
                            {
                                name = "No Name";
                            }
                            _cachedContentTypes.Add((id, name));
                        }
                    }
                    _cachedContentTypes = _cachedContentTypes.OrderBy(x => x.Id).ToList();
                }
            }
        }
        catch(Exception e)
        {
            e.LogDebug();
        }
    }

    private void ClientState_Login()
    {
        ClientState_TerritoryChanged(0);
    }

    public override void OnUpdate()
    {
        if(!Svc.ClientState.IsLoggedIn)
        {
            if(EzThrottler.Throttle("PeriodicARRCheck"))
            {
                ClientState_TerritoryChanged(0);
            }
        }
    }

    private void ClientState_TerritoryChanged(uint obj)
    {
        if(Svc.PluginInterface.InstalledPlugins.Any(x => x.InternalName == "ARealmRecorded" && x.IsLoaded))
        {
            try
            {
                if(DalamudReflector.TryGetDalamudPlugin("ARealmRecorded", out var plugin, true, true))
                {
                    var whitelist = plugin.GetStaticFoP<HashSet<uint>>("ARealmRecorded.Game", "whitelistedContentTypes");

                    foreach(var entry in C.Entries.Where(e => e.Enabled))
                    {
                        whitelist.Add(entry.Id);
                    }
                }
            }
            catch(Exception e)
            {
                e.LogDebug();
            }
        }
    }

    public override void OnDisable()
    {
        Svc.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        Svc.ClientState.Login -= ClientState_Login;
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text("ARealmRecorded Whitelist Content Types Manager");
        ImGui.Spacing();

        if(ImGui.BeginTable("ARRWhitelistTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Active", ImGuiTableColumnFlags.WidthFixed, 50f);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 0f);
            ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 70f);
            ImGui.TableHeadersRow();

            int removeIndex = -1;
            for(int i = 0; i < C.Entries.Count; i++)
            {
                var entry = C.Entries[i];
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                bool enabled = entry.Enabled;
                if(ImGui.Checkbox($"##en_{i}", ref enabled))
                {
                    entry.Enabled = enabled;
                    ClientState_TerritoryChanged(0);
                }

                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(-1);
                string name = entry.Name;
                if(ImGui.InputText($"##name_{i}", ref name, 64))
                {
                    entry.Name = name;
                }

                ImGui.TableSetColumnIndex(2);
                ImGui.SetNextItemWidth(-1);
                string idStr = entry.Id.ToString();
                if(ImGui.InputText($"##id_{i}", ref idStr, 10, ImGuiInputTextFlags.CharsDecimal))
                {
                    if(uint.TryParse(idStr, out uint parsedId))
                    {
                        entry.Id = parsedId;
                        ClientState_TerritoryChanged(0);
                    }
                }

                ImGui.TableSetColumnIndex(3);
                if(ImGui.Button($"Delete##{i}"))
                {
                    removeIndex = i;
                }
            }

            if(removeIndex != -1)
            {
                C.Entries.RemoveAt(removeIndex);
                ClientState_TerritoryChanged(0);
            }

            ImGui.EndTable();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGuiEx.Text("Add New Content Type from Game Data");

        if(_cachedContentTypes.Count > 0)
        {
            string previewText = "Select Content Type...";
            if(_selectedContentTypeIndex >= 0 && _selectedContentTypeIndex < _cachedContentTypes.Count)
            {
                var selected = _cachedContentTypes[_selectedContentTypeIndex];
                previewText = $"{selected.Item2} (ID: {selected.Item1})";
            }

            ImGui.SetNextItemWidth(300);
            if(ImGui.BeginCombo("##ContentTypeCombo", previewText))
            {
                for(int i = 0; i < _cachedContentTypes.Count; i++)
                {
                    var item = _cachedContentTypes[i];
                    bool isSelected = (_selectedContentTypeIndex == i);
                    if(ImGui.Selectable($"{item.Item2} (ID: {item.Item1})##combo_{i}", isSelected))
                    {
                        _selectedContentTypeIndex = i;
                    }

                    if(isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if(ImGui.Button("Add Selected Content Type"))
            {
                var chosen = _cachedContentTypes[_selectedContentTypeIndex];
                if(!C.Entries.Any(e => e.Id == chosen.Item1))
                {
                    C.Entries.Add(new WhitelistEntry(chosen.Item2, chosen.Item1, true));
                    ClientState_TerritoryChanged(0);
                }
            }
        }
        else
        {
            ImGuiEx.Text(EColor.RedBright, "Content Type excel sheet could not be loaded.");
        }
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public List<WhitelistEntry> Entries =
        [
            new("Deep Dungeon", 21),
            new("Eureka", 26),
            new("Carnivale", 27),
            new("Occult", 38),
            new("Quantum", 39),
        ];
    }
}
