using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.LazyDataHelpers;
using ECommons.Logging;
using ECommons.Reflection;
using ECommons.Schedulers;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;

public class RSVInject : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;
    public static readonly string RemoteUrl = "https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/rsv.ini";

    private HttpClient HttpClient
    {
        get
        {
            if(field == null)
            {
                field = new();
                Purgatory.Add(() =>
                {
                    field.Dispose();
                    field = null;
                });
            }
            return field;
        }
    }

    private Dictionary<ReadOnlySeString, ReadOnlySeString> Lookup
    {
        get
        {
            var s = DalamudReflector.GetService("Dalamud.Data.DataManager");
            return s.GetFoP("rsvResolver").GetFoP< Dictionary<ReadOnlySeString, ReadOnlySeString>>("Lookup");
        }
    }
    private Dictionary<ReadOnlySeString, ReadOnlySeString> FileContent = [];

    public override void OnEnable()
    {
        FileContent.Clear();
        new Thread(() =>
        {
            try
            {
                var content = HttpClient.GetStringAsync(RemoteUrl).Result;
                _ = new TickScheduler(() =>
                {
                    foreach(var x in content.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        var spl = x.Split("|");
                        var key = spl[0];
                        var value = spl[1];
                        if(key != "" && value != "")
                        {
                            try
                            {
                                var keyDecoded = Utils.BrotliDecompress(Convert.FromBase64String(key));
                                var valueDecoded = Utils.BrotliDecompress(Convert.FromBase64String(value));
                                var keySeString = new ReadOnlySeString(keyDecoded);
                                var valueSeString = new ReadOnlySeString(valueDecoded);
                                if(Lookup.ContainsKey(keySeString))
                                {
                                    PluginLog.Debug($"{keySeString} already present in RSV lookup, skipping");
                                }
                                else
                                {
                                    Lookup[keySeString] = valueSeString;
                                    PluginLog.Debug($"Adding RSV entry {keySeString} = {valueSeString}");
                                }
                                FileContent[keySeString] = valueSeString;
                            }
                            catch(Exception ex)
                            {
                                ex.Log();
                            }
                        }
                    }
                });
            }
            catch(Exception e)
            {
                e.Log();
            }
        }).Start();
    }

    public override void OnSettingsDraw()
    {
        var missing = Lookup.Where(x => !FileContent.ContainsKey(x.Key)).ToArray();
        if(missing.Length != 0)
        {
            ImGuiEx.TextWrapped($"""New RSV keys were found. Please copy them to clipboard, then make pull request to a file on Github.""");
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Copy, "Copy missing keys"))
            {
                List<string> s = [];
                foreach(var x in Lookup)
                {
                    s.Add($"{Convert.ToBase64String(Utils.BrotliCompress(x.Key.Data.ToArray()))}|{Convert.ToBase64String(Utils.BrotliCompress(x.Value.Data.ToArray()))}");
                }
                GenericHelpers.Copy(s.Join("\n"));
            }
            ImGui.SameLine();
        }
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Globe, "Open RSV File on Github"))
            {
                GenericHelpers.ShellStart("https://github.com/PunishXIV/Splatoon/blob/main/SplatoonScripts/rsv.ini");
            }
            ImGuiEx.Tooltip("PR missing lines to it");
            ImGui.SameLine();

            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Download, "Reload CSV file from Github"))
            {
                OnEnable();
            }
            ImGuiEx.Tooltip("May take up to 10 minutes for cache to update");
            ImGui.SameLine();

            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Trash, "Clear Dalamud's RSV"))
            {
                Lookup.Clear();
            }
            if(missing.Length != 0) 
        {
            ImGuiEx.Text("Missing entries:");
            if(ImGuiEx.BeginDefaultTable("RsvMissing", ["Key", "~Value"]))
            {
                foreach(var x in missing)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.Text(x.Key.ToString());
                    ImGui.TableNextColumn();
                    ImGuiEx.Text(x.Value.ToString());
                }
                ImGui.EndTable();
            }
        }
        if(ImGui.CollapsingHeader("All RSV Entries"))
        {
            if(ImGuiEx.BeginDefaultTable("RsvAll", ["Key", "~Value"]))
            {
                foreach(var x in Lookup)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.Text(x.Key.ToString());
                    ImGui.TableNextColumn();
                    ImGuiEx.Text(x.Value.ToString());
                }
                ImGui.EndTable();
            }
        }
        if(ImGui.CollapsingHeader("All RSV Entries (Raw)"))
        {
            if(ImGuiEx.BeginDefaultTable("RsvAll2", ["Key", "~Value"]))
            {
                foreach(var x in Lookup)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.Text(Convert.ToBase64String(Utils.BrotliCompress(x.Key.Data.ToArray())));
                    ImGui.TableNextColumn();
                    ImGuiEx.Text(Convert.ToBase64String(Utils.BrotliCompress(x.Value.Data.ToArray())));
                }
                ImGui.EndTable();
            }
        }
    }
}
