using ECommons;
using ECommons.DalamudServices;
using ECommons.Logging;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests;
public class DownloadPluginTest : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => null;
    public override Metadata Metadata => new(1, "NightmareXIV");

    public override void OnSettingsDraw()
    {
        if(ImGui.Button("Test"))
        {
            Task.Run(() =>
            {
                try
                {
                    DuoLog.Information($"Begin downloading");
                    var x = DownloadPluginAsync("https://github.com/NightmareXIV/DynamicBridge/releases/download/1.0.5.8/latest.zip");
                    x.Wait();
                    DuoLog.Information($"Downloaded plugin, {x.Result.Length} size");
                }
                catch(Exception e)
                {
                    e.Log();
                }
            });
        }
    }

    private async Task<Stream> DownloadPluginAsync(string downloadUrl)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl)
        {
            Headers =
            {
                Accept =
                {
                    new MediaTypeWithQualityHeaderValue("application/zip"),
                },
            },
        };
        using var httpClient = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
        });
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync();
    }
}
