using ECommons.Configuration;
using ECommons.ExcelServices;
using Newtonsoft.Json;
using NightmareUI;
using Pictomancy;
using Splatoon.Modules.TranslationWorkspace;
using Splatoon.RenderEngines;
using Splatoon.Serializables;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System.Collections.Specialized;
using System.Threading;

namespace Splatoon;

[Serializable]
internal class Configuration : IEzConfig
{
    [NonSerialized] private Splatoon plugin;
    [NonSerialized] private SemaphoreSlim ZipSemaphore;

    private static JsonSerializerSettings JsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        Formatting = Formatting.None,
        ObjectCreationHandling = ObjectCreationHandling.Replace,
    };

    public int Version { get; set; } = 2;

    public bool DX11EnabledOnMacLinux = false;
    public bool DX11MacLinuxWarningHidden = false;

    public RenderEngineKind RenderEngineKind = RenderEngineKind.DirectX11;
    public HashSet<RenderEngineKind> EnabledRenderers = [RenderEngineKind.ImGui_Legacy, RenderEngineKind.DirectX11];

    public List<Layout> LayoutsL = [];
    public List<string> GroupOrder = [];
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

    public HashSet<string> DisabledScripts = [];
    public bool DisableScriptCache = false;
    public List<WrappedRect> RenderableZones = [];
    public List<WrappedRect> ClipZones = [];
    public bool AutoClipNativeUI = true;
    public bool RenderableZonesValid = false;
    public bool SplatoonLowerZ = false;
    public int ElementMinFillAlpha = 0;
    public int ElementMaxFillAlpha = 255;
    public int MaxAlpha = 0xFF;
    public bool UseVfxRendering = false;
    [JsonConverter(typeof(DictionaryWithEnumKeyConverter<MechanicType, Tuple<bool, DisplayStyle>>))]
    public Dictionary<MechanicType, Tuple<bool, DisplayStyle>> StyleOverrides = [];
    public Dictionary<string, Dictionary<string, string>> ScriptConfigurationNames = [];
    public Dictionary<string, string> ActiveScriptConfigurations = [];
    public Dictionary<string, string> DefaultScriptConfigurationNames = [];
    public string ExtraTrustedRepos = "";
    public string ExtraUpdateLinks = "";
    public List<uint> NoPrioPopupTerritories = [];
    public List<RolePlayerAssignment> RolePlayerAssignments = [];
    public bool PrioUnifyDps = false;
    public List<string> FileWatcherPathes = [];
    public bool UseServerBar = true;
    public Dictionary<Job, RolePosition> PreferredPositions = [];
    public PriorityInfoOption ScriptPriorityNotification = PriorityInfoOption.Display_notification;
    public bool ConfigurationsHideDisabled = false;
    public List<string> DisabledGroups = [];
    public List<Page> TranslatorPages = [];
    public NightmareUIState NightmareUIState = new();
    public Dictionary<string, Dictionary<uint, string>> MapEffectNames = [];

    public bool EnableProjection = false;
    public Vector4 ProjectionColor1 = ImGuiEx.Vector4FromRGBA(0xAA0058FF);
    public Vector4 ProjectionColor2 = ImGuiEx.Vector4FromRGBA(0xFF0084FF);
    public int ProjectionPulseTime = 1000;
    public CastAnimationKind ProjectionCastAnimation = CastAnimationKind.Fill;
    public Vector4 AnimationColor1 = ImGuiEx.Vector4FromRGBA(0xFF000064);
    public Vector4 AnimationColor2 = ImGuiEx.Vector4FromRGBA(0x44000064);
    public float ProjectionFillIntensity = 0.2f;
    public List<BlacklistedAction> ProjectionBlacklistedActions = [];

    public List<string> NoAutoUpdateScript = [];

    public uint ClampFillColorAlpha(uint fillColor)
    {
        var alpha = fillColor >> 24;
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
        NuiTools.SetState(this.NightmareUIState);
    }

    public void Save(bool suppressError = false)
    {
        EzConfig.Save();
        foreach(var x in ScriptingProcessor.Scripts)
        {
            //PluginLog.Debug($"Saving configuration for {x.InternalData.FullName}");
            Safe(x.Controller.SaveConfig);
            Safe(x.Controller.SaveOverrides);
        }
    }

    public bool Backup(bool update = false)
    {
        if(!ZipSemaphore.Wait(0))
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
            catch(Exception e)
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
        catch(FileNotFoundException e)
        {
            ZipSemaphore.Release();
            LogErrorAndNotify(e, "Could not find configuration to backup.");
        }
        catch(Exception e)
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
                catch(Exception e)
                {
                    e.Log();
                }
                plugin.tickScheduler.Enqueue(delegate
                {
                    plugin.Log("Backup created: " + bkpFile);
                    Notify.Info("A backup of your current configuration has been created.");
                });
            }
            catch(Exception e)
            {
                plugin.tickScheduler.Enqueue(delegate
                {
                    plugin.Log("Failed to create backup: " + e.ToStringFull(), true);
                });
            }
            ZipSemaphore.Release();
        }));
        return true;
    }
}
