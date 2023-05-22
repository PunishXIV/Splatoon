using Dalamud.Configuration;
using Dalamud.Interface.Internal.Notifications;
using Splatoon.Gui;
using Splatoon.SplatoonScripting;
using System.Threading;

namespace Splatoon;

[Serializable]
class Configuration : IPluginConfiguration
{
    [NonSerialized] Splatoon plugin;
    [NonSerialized] SemaphoreSlim ZipSemaphore;

    public int Version { get; set; } = 2;

    [Obsolete] public Dictionary<string, Layout> Layouts = new Dictionary<string, Layout>(); //never delete
    public List<Layout> LayoutsL = new();
    public List<string> GroupOrder = new();
    public bool dumplog = false;
    public bool verboselog = false;
    public int segments = 100;
    public float maxdistance = 100;
    //public float maxcamY = 0.05f;
    public int ChlogReadVer = ChlogGui.ChlogVersion;
    public int lineSegments = 10;
    public bool UseHttpServer = false;
    public int port = 47774;
    public bool TetherOnFind = true;
    public bool DirectNameComparison = false;
    public bool ShowOnUiHide = false;
    public bool Hexadecimal = true;
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

    public bool FocusMode = false;
    public bool NoStreamWarning = false;
    public bool Logging = false;

    public string PluginLanguage = null;
    public bool NoFindReset = false;
    public bool NoCircleFix = false;

    public HashSet<string> DisabledScripts = new();
    public bool DisableScriptCache = false;

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
        if (ChlogGui.ChlogVersion > ChlogReadVer)
        {
            if (!suppressError)
            {
                Svc.Chat.PrintError("[Splatoon] Configuration can not be saved until you have read changelog and closed window");
                Svc.PluginInterface.UiBuilder.AddNotification("[Splatoon] Configuration can not be saved until you have read changelog and closed window", plugin.Name, NotificationType.Error);
            }
        }
        else
        {
            Svc.PluginInterface.SavePluginConfig(this);
            foreach(var x in ScriptingProcessor.Scripts)
            {
                //PluginLog.Debug($"Saving configuration for {x.InternalData.FullName}");
                Safe(x.Controller.SaveConfig);
                Safe(x.Controller.SaveOverrides);
            }
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
        try
        {
            var cFile = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "..", "Splatoon.json");
            var configStr = File.ReadAllText(cFile);
            var bkpFPath = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "Backups");
            Directory.CreateDirectory(bkpFPath);
            tempDir = Path.Combine(bkpFPath, "temp");
            Directory.CreateDirectory(tempDir);
            tempFile = Path.Combine(tempDir, "Splatoon.json");
            bkpFile = Path.Combine(bkpFPath, "Backup." + DateTimeOffset.Now.ToString("yyyy-MM-dd HH-mm-ss-fffffff") + (update ? $"-update-{ChlogGui.ChlogVersion}" : "") + ".zip");
            File.Copy(cFile, tempFile, true);
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
