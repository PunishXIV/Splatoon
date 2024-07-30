using ECommons.Configuration;
using Newtonsoft.Json;
using Pictomancy;
using Splatoon.RenderEngines;
using Splatoon.Serializables;
using Splatoon.SplatoonScripting;
using System.Threading;

namespace Splatoon;

[Serializable]
internal class Configuration : IEzConfig
{
    [NonSerialized] Splatoon plugin;
    [NonSerialized] SemaphoreSlim ZipSemaphore;

    private static JsonSerializerSettings JsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        Formatting = Formatting.None,
    };

    public int Version { get; set; } = 2;

    public RenderEngineKind RenderEngineKind = RenderEngineKind.DirectX11;
    public List<RenderEngineKind> EnabledRenderers = [RenderEngineKind.ImGui_Legacy, RenderEngineKind.DirectX11];

    public List<Layout> LayoutsL = new();
    public List<string> GroupOrder = new();
    public bool dumplog = false;
    public bool verboselog = false;
    public float maxdistance = 100;
    public AlphaBlendMode AlphaBlendMode = AlphaBlendMode.Add;
    //public float maxcamY = 0.05f;
    public bool UseHttpServer = false;
    public int port = 47774;
    public bool TetherOnFind = true;
    public bool DirectNameComparison = false;
    public bool ShowOnUiHide = false;
    public bool Hexadecimal = true;

    public int segments = 100;
    public int lineSegments = 10;
    public bool AltRectFill = true;
    public bool AltRectStepOverride = false;
    public float AltRectStep = 0.01f;
    public bool AltRectHighlightOutline = true;
    public float AltRectMinLineThickness = 4f;
    public bool AltRectForceMinLineThickness = false;
    public bool AltDonutStepOverride = false;
    public float AltDonutStep = 0.01f;
    public bool AltConeStepOverride = false;
    public int AltConeStep = 1;
    internal bool FillCone = false;
    public bool UseFullDonutFill = true;

    public bool FocusMode = false;
    public bool NoStreamWarning = false;
    public bool Logging = false;
    public bool LogPosition = false;

    public string PluginLanguage = null;
    public bool NoFindReset = false;
    public bool NoCircleFix = false;

    public HashSet<string> DisabledScripts = new();
    public bool DisableScriptCache = false;
    public List<WrappedRect> RenderableZones = new();
    public List<WrappedRect> ClipZones = new();
    public bool AutoClipNativeUI = true;
    public bool RenderableZonesValid = false;
    public bool SplatoonLowerZ = false;
    public int ElementMinFillAlpha = 0;
    public int ElementMaxFillAlpha = 255;
    public int MaxAlpha = 0xFF;
    [JsonConverter(typeof(DictionaryWithEnumKeyConverter<MechanicType, Tuple<bool, DisplayStyle>>))]
    public Dictionary<MechanicType, Tuple<bool, DisplayStyle>> StyleOverrides = new();

    public uint ClampFillColorAlpha(uint fillColor)
    {
        uint alpha = fillColor >> 24;
        alpha = Math.Clamp(alpha, (uint)ElementMinFillAlpha, (uint)ElementMaxFillAlpha);
        return fillColor & 0x00FFFFFF | (alpha << 24);
    }

    public bool ShouldSerializeLayouts()
    {
        return false;
    }

    public void Initialize(Splatoon plugin)
    {
        this.plugin = plugin;
        ZipSemaphore = new SemaphoreSlim(1);
        Svc.PluginInterface.UiBuilder.OpenConfigUi += delegate
        {
            plugin.ConfigGui.Open = true;
        };
    }

    public void Save(bool suppressError = false)
    {
        EzConfig.Save();
        foreach (var x in ScriptingProcessor.Scripts)
        {
            //PluginLog.Debug($"Saving configuration for {x.InternalData.FullName}");
            Safe(x.Controller.SaveConfig);
            Safe(x.Controller.SaveOverrides);
        }
    }

    public bool Backup(bool update = false)
    {
        if (!ZipSemaphore.Wait(0))
        {
            LogErrorAndNotify("Failed to create backup: previous backup did not completed yet. ");
            return false;
        }
        string tempDir = null;
        string bkpFile = null;
        string tempFile = null;
        string archiveFile = null;
        try
        {
            var bkpFPath = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "Backups");
            Directory.CreateDirectory(bkpFPath);
            tempDir = Path.Combine(bkpFPath, "temp");
            Directory.CreateDirectory(tempDir);
            tempFile = Path.Combine(tempDir, EzConfig.DefaultSerializationFactory.DefaultConfigFileName);
            archiveFile = Path.Combine(tempDir, "Archive.json");
            bkpFile = Path.Combine(bkpFPath, "Backup." + DateTimeOffset.Now.ToString("yyyy-MM-dd HH-mm-ss-fffffff") + (update ? $"-update-" : "") + ".zip");
            Copy(EzConfig.DefaultConfigurationFileName, tempFile);
            try
            {
                Copy(Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "Archive.json"), archiveFile);
            }
            catch (Exception e)
            {
                e.LogWarning();
            }
            void Copy(string source, string target)
            {
                using var fileStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var writer = new FileStream(target, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                fileStream.CopyTo(writer);
            }
        }
        catch (FileNotFoundException e)
        {
            ZipSemaphore.Release();
            LogErrorAndNotify(e, "Could not find configuration to backup.");
        }
        catch (Exception e)
        {
            ZipSemaphore.Release();
            LogErrorAndNotify(e, "Failed to create a backup:\n" + e.Message);
        }
        Task.Run(new Action(delegate
        {
            try
            {
                ZipFile.CreateFromDirectory(tempDir, bkpFile, CompressionLevel.Optimal, false);
                File.Delete(tempFile);
                try
                {
                    File.Delete(archiveFile);
                }
                catch (Exception e)
                {
                    e.Log();
                }
                plugin.tickScheduler.Enqueue(delegate
                {
                    plugin.Log("Backup created: " + bkpFile);
                    Notify.Info("A backup of your current configuration has been created.");
                });
            }
            catch (Exception e)
            {
                plugin.tickScheduler.Enqueue(delegate
                {
                    plugin.Log("Failed to create backup: " + e.Message, true);
                    plugin.Log(e.StackTrace, true);
                });
            }
            ZipSemaphore.Release();
        }));
        return true;
    }
}
