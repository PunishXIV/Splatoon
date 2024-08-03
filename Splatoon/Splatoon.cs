using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.LanguageHelpers;
using ECommons.MathHelpers;
using ECommons.ObjectLifeTracker;
using ECommons.SimpleGui;
using ECommons.Singletons;
using Lumina.Excel.GeneratedSheets;
using NotificationMasterAPI;
using PInvoke;
using Splatoon.Gui;
using Splatoon.Memory;
using Splatoon.Modules;
using Splatoon.RenderEngines.DirectX11;
using Splatoon.Serializables;
using Splatoon.SplatoonScripting;
using Splatoon.Structures;
using System.Net.Http;
using Colors = Splatoon.Utility.Colors;
using Localization = ECommons.LanguageHelpers.Localization;

namespace Splatoon;
public unsafe class Splatoon : IDalamudPlugin
{
    public const string DiscordURL = "https://discord.gg/Zzrcc8kmvy";
    public string Name => "Splatoon";
    public static Splatoon P;
    public const int MAX_CONFIGURABLE_CLIP_ZONES = 32;
    internal DirectX11Scene DrawingGui;
    internal CGui ConfigGui;
    internal Commands CommandManager;
    internal Configuration Config;
    internal Dictionary<ushort, TerritoryType> Zones;
    internal long CombatStarted = 0;
    internal HashSet<Element> InjectedElements = new();
    internal Dictionary<int, string> Jobs = new();
    //internal HashSet<(float x, float y, float z, float r, float angle)> draw = new HashSet<(float x, float y, float z, float r, float angle)>();
    internal bool prevMouseState = false;
    internal List<SearchInfo> SFind = new();
    internal ConcurrentQueue<System.Action> tickScheduler;
    internal List<DynamicElement> dynamicElements;
    internal HTTPServer HttpServer;
    internal bool prevCombatState = false;
    static internal Vector3? PlayerPosCache = null;
    //internal Profiling Profiler;
    internal Queue<string> ChatMessageQueue;
    internal HashSet<string> CurrentChatMessages = new();
    internal Element Clipboard = null;
    internal int dequeueConcurrency = 1;
    internal Dictionary<(string Name, uint EntityId, ulong GameObjectId, uint DataID, int ModelID, uint NPCID, uint NameID, ObjectKind type), ObjectInfo> loggedObjectList = new();
    internal bool LogObjects = false;
    internal bool DisableLineFix = false;
    private int phase = 1;
    internal int Phase { get => phase; set { phase = value; ScriptingProcessor.OnPhaseChange(value); } }
    internal int LayoutAmount = 0;
    internal int ElementAmount = 0;
    internal static string LimitGaugeResets = "";
    internal Loader loader;
    public static bool Init = false;
    public bool Loaded = false;
    public bool Disposed = false;
    internal static (Vector2 X, Vector2 Y) Transform = default;
    internal static Dictionary<string, nint> PlaceholderCache = new();
    internal static Dictionary<string, uint> NameNpcIDsAll = new();
    internal static Dictionary<string, uint> NameNpcIDs = new();
    internal MapEffectProcessor mapEffectProcessor;
    internal TetherProcessor TetherProcessor;
    internal ObjectEffectProcessor ObjectEffectProcessor;
    internal HttpClient HttpClient;
    internal PinnedElementEdit PinnedElementEditWindow;
    internal RenderableZoneSelector RenderableZoneSelector;
    internal ClipZoneSelector ClipZoneSelector;
    public NotificationMasterApi NotificationMasterApi;
    public Archive Archive;
    private ActorControlProcessor ActorControlProcessor;

    internal void Load(IDalamudPluginInterface pluginInterface)
    {
        if (Loaded)
        {
            PluginLog.Fatal("Splatoon is already loaded, could not load again...");
            return;
        }
        Loaded = true;
        ECommonsMain.Init(pluginInterface, this, Module.ObjectLife, Module.ObjectFunctions, Module.DalamudReflector);
        Svc.Commands.RemoveHandler("/loadsplatoon");
        EzConfig.Migrate<Configuration>();
        Config = EzConfig.Init<Configuration>() ?? new();
        Config.Initialize(this);
        ChatMessageQueue = new Queue<string>();
        //Profiler = new Profiling(this);
        CommandManager = new Commands(this);
        Zones = Svc.Data.GetExcelSheet<TerritoryType>().ToDictionary(row => (ushort)row.RowId, row => row);
        Jobs = Svc.Data.GetExcelSheet<ClassJob>().ToDictionary(row => (int)row.RowId, row => row.Name.ToString());
        tickScheduler = new ConcurrentQueue<System.Action>();
        dynamicElements = new List<DynamicElement>();
        SetupShutdownHttp(Config.UseHttpServer);

        ConfigGui = new CGui(this);
        EzConfigGui.Init(() => { });
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= EzConfigGui.Open;
        PinnedElementEditWindow = new();
        EzConfigGui.WindowSystem.AddWindow(PinnedElementEditWindow);
        Camera.Init();
        Scene.Init();
        Svc.Chat.ChatMessage += OnChatMessage;
        Svc.Framework.Update += Tick;
        Svc.ClientState.TerritoryChanged += TerritoryChangedEvent;
        Svc.PluginInterface.UiBuilder.DisableUserUiHide = Config.ShowOnUiHide;
        LimitGaugeResets = Svc.Data.GetExcelSheet<LogMessage>().GetRow(2844).Text.ToString();
        foreach (var x in Svc.Data.GetExcelSheet<BNpcName>(ClientLanguage.English)
            .Union(Svc.Data.GetExcelSheet<BNpcName>(ClientLanguage.French))
            .Union(Svc.Data.GetExcelSheet<BNpcName>(ClientLanguage.Japanese))
            .Union(Svc.Data.GetExcelSheet<BNpcName>(ClientLanguage.German)))
        {
            if (x.Singular != "")
            {
                var n = x.Singular.ToString().ToLower();
                NameNpcIDsAll[n] = x.RowId;
                NameNpcIDs[n] = x.RowId;
            }
        }
        var bNames = new HashSet<string>();
        foreach (var lang in Enum.GetValues<ClientLanguage>())
        {
            bNames.Clear();
            foreach (var x in Svc.Data.GetExcelSheet<BNpcName>(lang))
            {
                var n = x.Singular.ToString().ToLower();
                if (bNames.Contains(n))
                {
                    NameNpcIDs[n] = 0;
                    PluginLog.Verbose($"Name npc id {n} is ambiguous");
                }
                else
                {
                    bNames.Add(n);
                }
            }
        }
        NameNpcIDs = NameNpcIDs.Where(x => x.Value != 0).ToDictionary(x => x.Key, x => x.Value);
        StreamDetector.Start();
        AttachedInfo.Init();
        Logger.OnTerritoryChanged();
        Layout.DisplayConditions = new string[] {
            "Always shown".Loc(),
            "Only in combat".Loc(),
            "Only in instance".Loc(),
            "Only in combat AND instance".Loc(),
            "Only in combat OR instance".Loc(),
            "On trigger only".Loc() };
        Element.Init();
        mapEffectProcessor = new();
        TetherProcessor = new();
        ObjectEffectProcessor = new();
        DirectorUpdate.Init(DirectorUpdateProcessor.ProcessDirectorUpdate);
        ActionEffect.Init(ActionEffectProcessor.ProcessActionEffect);
        ActionEffect.ActionEffectEvent += ScriptingProcessor.OnActionEffectEvent;
        ProperOnLogin.RegisterAvailable(delegate
        {
            ScriptingProcessor.TerritoryChanged();
        });
        Svc.ClientState.Logout += OnLogout;
        HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        ScriptingProcessor.TerritoryChanged();
        ScriptingProcessor.ReloadAll();
        ObjectLife.OnObjectCreation = ScriptingProcessor.OnObjectCreation;
        //VFXManager = new();
        RenderableZoneSelector = new();
        EzConfigGui.WindowSystem.AddWindow(RenderableZoneSelector);
        ClipZoneSelector = new();
        EzConfigGui.WindowSystem.AddWindow(ClipZoneSelector);
        NotificationMasterApi = new(pluginInterface);
        SingletonServiceManager.Initialize(typeof(S));
        Archive = EzConfig.LoadConfiguration<Archive>("Archive.json");
        ActorControlProcessor = new ActorControlProcessor();
        Init = true;
        SplatoonIPC.Init();
    }

    public void Dispose()
    {
        Disposed = true;
        Safe(delegate
        {
            Svc.Commands.RemoveHandler("/loadsplatoon");
            Svc.PluginInterface.UiBuilder.Draw -= loader.Draw;
        });
        if (!Loaded)
        {
            P = null;
            return;
        }
        Safe(SplatoonIPC.Dispose);
        Loaded = false;
        Init = false;
        Safe(delegate { Config.Save(); });
        Safe(delegate { SetupShutdownHttp(false); });
        Safe(() => SaveArchive());
        Safe(ConfigGui.Dispose);
        Safe(CommandManager.Dispose);
        Safe(delegate
        {
            Svc.ClientState.TerritoryChanged -= TerritoryChangedEvent;
            Svc.Framework.Update -= Tick;
            Svc.Chat.ChatMessage -= OnChatMessage;
            Svc.ClientState.Logout -= OnLogout;
        });
        Safe(mapEffectProcessor.Dispose);
        Safe(TetherProcessor.Dispose);
        Safe(ObjectEffectProcessor.Dispose);
        Safe(AttachedInfo.Dispose);
        Safe(ScriptingProcessor.Dispose);
        ECommonsMain.Dispose();
        P = null;
        //Svc.Chat.Print("Disposing");
    }

    public void SaveArchive()
    {
        Archive.SaveConfiguration("Archive.json");
    }

    public Splatoon(IDalamudPluginInterface pluginInterface)
    {
        P = this;
        Svc.Init(pluginInterface);
        var cfg = EzConfig.LoadConfiguration<Configuration>(EzConfig.DefaultConfigurationFileName);
        Localization.Init(cfg.PluginLanguage);
        loader = new Loader(this);
    }

    internal static void OnLogout()
    {
        ScriptingProcessor.TerritoryChanged();
    }

    public void AddDynamicElements(string name, Element[] elements, long[] destroyConditions)
    {
        dynamicElements.Add(new()
        {
            Name = name,
            Elements = elements,
            DestroyTime = destroyConditions,
            Layouts = Array.Empty<Layout>()
        });
    }

    public void RemoveDynamicElements(string name)
    {
        dynamicElements.RemoveAll(x => x.Name == name);
    }

    internal static readonly string[] InvalidSymbols = { "", "", "", "“", "”", "" };
    internal void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        var inttype = (int)type;
        if (inttype == 2105 && LimitGaugeResets.Equals(message.ToString()))
        {
            Phase++;
            CombatStarted = Environment.TickCount64;
            Svc.PluginInterface.UiBuilder.AddNotification($"Phase transition to Phase ??".Loc(Phase), this.Name, NotificationType.Info, 10000);
        }
        if (!type.EqualsAny(ECommons.Constants.NormalChatTypes))
        {
            var m = message.Payloads.Where(p => p is ITextProvider)
                    .Cast<ITextProvider>()
                    .Aggregate(new StringBuilder(), (sb, tp) => sb.Append(tp.Text.RemoveSymbols(InvalidSymbols).Replace("\n", " ")), sb => sb.ToString());
            ChatMessageQueue.Enqueue(m);
            if (P.Config.Logging && !((uint)type).EqualsAny(Utils.BlacklistedMessages))
            {
                Logger.Log($"[{type}] {m}");
            }
        }
    }

    internal void SetupShutdownHttp(bool enable)
    {
        if (enable)
        {
            if (HttpServer == null)
            {
                try
                {
                    HttpServer = new HTTPServer(this);
                }
                catch (Exception e)
                {
                    Log("Critical error occurred while starting HTTP server.".Loc(), true);
                    Log(e.Message, true);
                    Log(e.StackTrace);
                    HttpServer = null;
                }
            }
        }
        else
        {
            if (HttpServer != null)
            {
                HttpServer.Dispose();
                HttpServer = null;
            }
        }
    }

    internal void TerritoryChangedEvent(ushort e)
    {
        Phase = 1;
        if (SFind.Count > 0 && !P.Config.NoFindReset)
        {
            SFind.Clear();
            Notify.Info("Search stopped".Loc());
        }
        for (var i = dynamicElements.Count - 1; i >= 0; i--)
        {
            var de = dynamicElements[i];
            foreach (var l in de.Layouts)
            {
                ResetLayout(l);
            }
            foreach (var dt in de.DestroyTime)
            {
                if (dt == (long)DestroyCondition.TERRITORY_CHANGE)
                {
                    dynamicElements.RemoveAt(i);
                }
            }
        }
        foreach (var l in Config.LayoutsL)
        {
            ResetLayout(l);
        }
        ScriptingProcessor.Scripts.ForEach(x => x.Controller.Layouts.Values.Each(ResetLayout));
        AttachedInfo.VFXInfos.Clear();
        Logger.OnTerritoryChanged();
        ScriptingProcessor.TerritoryChanged();
    }

    static void ResetLayout(Layout l)
    {
        if (l.UseTriggers)
        {
            foreach (var t in l.Triggers)
            {
                if (t.ResetOnTChange)
                {
                    t.FiredState = 0;
                    l.TriggerCondition = 0;
                    t.Disabled = false;
                    t.EnableAt.Clear();
                    t.DisableAt.Clear();
                }
            }
        }
        if (l.Freezing && l.FreezeResetTerr)
        {
            l.FreezeInfo = new();
        }
    }


    internal void Tick(IFramework framework)
    {
        try
        {
            PlaceholderCache.Clear();
            LayoutAmount = 0;
            ElementAmount = 0;
            if (LogObjects && Svc.ClientState.LocalPlayer != null)
            {
                foreach (var t in Svc.Objects)
                {
                    var ischar = t is ICharacter;
                    var obj = (t.Name.ToString(), t.EntityId, (ulong)t.Struct()->GetGameObjectId(), t.DataId, ischar ? ((ICharacter)t).Struct()->CharacterData.ModelCharaId : 0, t.Struct()->GetNameId(), ischar ? ((ICharacter)t).NameId : 0, t.ObjectKind);
                    loggedObjectList.TryAdd(obj, new ObjectInfo());
                    loggedObjectList[obj].ExistenceTicks++;
                    loggedObjectList[obj].IsChar = ischar;
                    if (ischar)
                    {
                        loggedObjectList[obj].Targetable = t.Struct()->GetIsTargetable();
                        loggedObjectList[obj].Visible = ((ICharacter)t).IsCharacterVisible();
                        if (loggedObjectList[obj].Targetable) loggedObjectList[obj].TargetableTicks++;
                        if (loggedObjectList[obj].Visible) loggedObjectList[obj].VisibleTicks++;
                    }
                    else
                    {
                        loggedObjectList[obj].Targetable = t.Struct()->GetIsTargetable();
                        if (loggedObjectList[obj].Targetable) loggedObjectList[obj].TargetableTicks++;
                    }
                    loggedObjectList[obj].Distance = Vector3.Distance(Svc.ClientState.LocalPlayer.Position, t.Position);
                    loggedObjectList[obj].HitboxRadius = t.HitboxRadius;
                    loggedObjectList[obj].Life = t.GetLifeTimeSeconds();
                }
            }
            while (tickScheduler.TryDequeue(out var action))
            {
                action.Invoke();
            }
            PlayerPosCache = null;
            S.RenderManager.ClearDisplayObjects();
            if (Svc.ClientState.LocalPlayer != null)
            {
                if (ChatMessageQueue.Count > 5 * dequeueConcurrency)
                {
                    dequeueConcurrency++;
                    PluginLog.Debug($"Too many queued messages ({ChatMessageQueue.Count}); concurrency increased to {dequeueConcurrency}");
                }
                for (var i = 0; i < dequeueConcurrency; i++)
                {
                    if (ChatMessageQueue.TryDequeue(out var ccm))
                    {
                        PluginLog.Verbose("Dequeued message: " + ccm);
                        CurrentChatMessages.Add(ccm);
                        ScriptingProcessor.OnMessage(ccm);
                    }
                    else
                    {
                        break;
                    }
                }
                if (CurrentChatMessages.Count > 0) PluginLog.Verbose($"Messages dequeued: {CurrentChatMessages.Count}");
                var pl = Svc.ClientState.LocalPlayer;
                if (Svc.ClientState.LocalPlayer.Address == nint.Zero)
                {
                    Log("Pointer to LocalPlayer.Address is zero");
                    return;
                }

                if (Svc.Condition[ConditionFlag.InCombat])
                {
                    if (CombatStarted == 0)
                    {
                        CombatStarted = Environment.TickCount64;
                        Log("Combat started event");
                        ScriptingProcessor.OnCombatStart();
                    }
                }
                else
                {
                    if (CombatStarted != 0)
                    {
                        CombatStarted = 0;
                        Log("Combat ended event");
                        ScriptingProcessor.OnCombatEnd();
                        AttachedInfo.VFXInfos.Clear();
                        foreach (var l in Config.LayoutsL)
                        {
                            ResetLayout(l);
                        }
                        foreach (var de in dynamicElements)
                        {
                            foreach (var l in de.Layouts)
                            {
                                ResetLayout(l);
                            }
                        }
                        ScriptingProcessor.Scripts.ForEach(x => x.Controller.Layouts.Values.Each(ResetLayout));
                    }
                }

                //if (CamAngleY > Config.maxcamY) return;

                if (PinnedElementEditWindow.Script != null && PinnedElementEditWindow.EditingElement != null && !PinnedElementEditWindow.Script.InternalData.UnconditionalDraw)
                {
                    S.RenderManager.GetRenderer(PinnedElementEditWindow.EditingElement).ProcessElement(PinnedElementEditWindow.EditingElement, null, true);
                }

                if (SFind.Count > 0)
                {
                    foreach (var obj in SFind)
                    {
                        var col = GradientColor.Get(Colors.Red.ToVector4(), Colors.Yellow.ToVector4(), 750);
                        var findEl = new Element(1)
                        {
                            thicc = 3f,
                            radius = 0f,
                            refActorName = obj.name,
                            refActorObjectID = obj.oid,
                            refActorComparisonType = obj.SearchAttribute,
                            overlayText = "$NAME",
                            overlayVOffset = 1.7f,
                            overlayPlaceholders = true,
                            overlayTextColor = col.ToUint(),
                            color = col.ToUint(),
                            includeHitbox = true,
                            onlyTargetable = !obj.includeUntargetable,
                            tether = Config.TetherOnFind,
                        };
                        S.RenderManager.GetRenderer(findEl).ProcessElement(findEl);
                    }
                }

                ProcessS2W();

                foreach (var i in Config.LayoutsL)
                {
                    ProcessLayout(i);
                }

                ScriptingProcessor.Scripts.ForEach(x => { if (x.IsEnabled) x.Controller.Layouts.Values.Each(ProcessLayout); });
                ScriptingProcessor.Scripts.ForEach(x => { if (x.IsEnabled || x.InternalData.UnconditionalDraw) x.Controller.Elements.Each(z => S.RenderManager.GetRenderer(z.Value).ProcessElement(z.Value, null, x.InternalData.UnconditionalDraw && x.InternalData.UnconditionalDrawElements.Contains(z.Key))); });
                foreach (var e in InjectedElements)
                {
                    S.RenderManager.GetRenderer(e).ProcessElement(e);
                    //PluginLog.Information("Processing type " + e.type + JsonConvert.SerializeObject(e, Formatting.Indented));
                }
                InjectedElements.Clear();

                for (var i = dynamicElements.Count - 1; i >= 0; i--)
                {
                    var de = dynamicElements[i];

                    foreach (var dt in de.DestroyTime)
                    {
                        if (dt == (long)DestroyCondition.COMBAT_EXIT)
                        {
                            if (!Svc.Condition[ConditionFlag.InCombat] && prevCombatState)
                            {
                                dynamicElements.RemoveAt(i);
                                continue;
                            }
                        }
                        else if (dt > 0)
                        {
                            if (Environment.TickCount64 > dt)
                            {
                                dynamicElements.RemoveAt(i);
                                continue;
                            }
                        }
                    }
                    foreach (var l in de.Layouts)
                    {
                        ProcessLayout(l);
                    }
                    foreach (var e in de.Elements)
                    {
                        S.RenderManager.GetRenderer(e).ProcessElement(e);
                    }
                }
            }
            else
            {
            }
            prevCombatState = Svc.Condition[ConditionFlag.InCombat];
            CurrentChatMessages.Clear();
            ScriptingProcessor.OnUpdate();
        }
        catch (Exception e)
        {
            Log("Caught exception: " + e.Message);
            Log(e.StackTrace);
        }
    }

    internal void ProcessLayout(Layout l)
    {
        if (LayoutUtils.IsLayoutVisible(l))
        {
            LayoutAmount++;
            if (l.Freezing)
            {
                if (l.FreezeInfo.CanDisplay())
                {
                    S.RenderManager.StoreDisplayObjects();
                    for (var i = 0; i < l.ElementsL.Count; i++)
                    {
                        var element = l.ElementsL[i];
                        S.RenderManager.GetRenderer(element).ProcessElement(element, l);
                    }
                    var union = S.RenderManager.GetUnifiedDisplayObjects();
                    if (union.Count > 0)
                    {
                        l.FreezeInfo.States.Add(new()
                        {
                            Objects = union,
                            ShowUntil = Environment.TickCount64 + (int)(l.FreezeFor * 1000f),
                            ShowAt = Environment.TickCount64 + (int)(l.FreezeDisplayDelay * 1000f)
                        });
                        l.FreezeInfo.AllowRefreezeAt = Environment.TickCount64 + Math.Max(100, (int)(l.IntervalBetweenFreezes * 1000f));
                    }
                    S.RenderManager.RestoreDisplayObjects();
                }
            }
            else
            {
                for (var i = 0; i < l.ElementsL.Count; i++)
                {
                    var element = l.ElementsL[i];
                    S.RenderManager.GetRenderer(element).ProcessElement(element, l);
                }
            }
        }
        for (var i = l.FreezeInfo.States.Count - 1; i >= 0; i--)
        {
            var x = l.FreezeInfo.States[i];
            if (x.IsActive())
            {
                S.RenderManager.InjectDisplayObjects(x.Objects);
            }
            else
            {
                if (x.IsExpired())
                {
                    l.FreezeInfo.States.RemoveAt(i);
                }
            }
        }
    }

    internal S2WInfo s2wInfo;

    public void BeginS2W(object cls, string x, string y, string z)
    {
        s2wInfo = new(cls, x, y, z);
    }

    internal void ProcessS2W()
    {
        if (s2wInfo != null)
        {
            var lmbdown = Bitmask.IsBitSet(User32.GetKeyState(0x01), 15);
            var mousePos = ImGui.GetIO().MousePos;
            if (Svc.GameGui.ScreenToWorld(new Vector2(mousePos.X, mousePos.Y), out var worldPos, Config.maxdistance * 5))
            {
                s2wInfo.Apply(worldPos.X, worldPos.Z, worldPos.Y);
            }
            if (!lmbdown && prevMouseState)
            {
                s2wInfo = null;
            }
            prevMouseState = lmbdown;
            if (Environment.TickCount64 % 500 < 250 && s2wInfo != null)
            {
                var coords = s2wInfo.GetValues();
                var x = coords.x;
                var y = coords.y;
                var z = coords.z;
                S.RenderManager.GetRenderer().AddLine(x + 2f, y + 2f, z, x - 2f, y - 2f, z, 2f, Colors.Red);
                S.RenderManager.GetRenderer().AddLine(x - 2f, y + 2f, z, x + 2f, y - 2f, z, 2f, Colors.Red);
            }
        }
    }

    public void InjectElement(Element e)
    {
        InjectedElements.Add(e);
    }

    internal void Log(string s, bool tochat = false, ushort? chatColor = null)
    {
        if (tochat)
        {
            Svc.Chat.Print(s, messageTag: "Splatoon", tagColor: chatColor);
        }
        InternalLog.Information(s);
    }
}
