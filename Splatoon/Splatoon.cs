using Dalamud;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Events;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.LanguageHelpers;
using ECommons.MathHelpers;
using ECommons.ObjectLifeTracker;
using ECommons.SimpleGui;
using ECommons.Singletons;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.GeneratedSheets;
using NotificationMasterAPI;
using PInvoke;
using Splatoon.Gui;
using Splatoon.Memory;
using Splatoon.Modules;
using Splatoon.RenderEngines;
using Splatoon.RenderEngines.DirectX11;
using Splatoon.Serializables;
using Splatoon.SplatoonScripting;
using Splatoon.Structures;
using Splatoon.Utility;
using System.Net.Http;
using System.Text.RegularExpressions;
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
    internal ChlogGui ChangelogGui = null;
    internal Configuration Config;
    internal Dictionary<ushort, TerritoryType> Zones;
    internal long CombatStarted = 0;
    internal HashSet<Element> InjectedElements = new();
    internal double CamAngleX;
    internal Dictionary<int, string> Jobs = new();
    //internal HashSet<(float x, float y, float z, float r, float angle)> draw = new HashSet<(float x, float y, float z, float r, float angle)>();
    internal float CamAngleY;
    internal float CamZoom = 1.5f;
    internal bool prevMouseState = false;
    internal List<SearchInfo> SFind = new();
    internal int CurrentLineSegments;
    internal ConcurrentQueue<System.Action> tickScheduler;
    internal List<DynamicElement> dynamicElements;
    internal HTTPServer HttpServer;
    internal bool prevCombatState = false;
    static internal Vector3? PlayerPosCache = null;
    internal Profiling Profiler;
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
    internal UnsafeElement UnsafeElement;
    public NotificationMasterApi NotificationMasterApi;

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
        var configRaw = Svc.PluginInterface.GetPluginConfig();
        Config = configRaw as Configuration ?? new Configuration();
        Config.Initialize(this);
        ConfigurationMigrator1to2.Migrate(Config); //never delete this
        if (configRaw == null)
        {
            Notify.Info("New configuration file has been created".Loc());
            Config.Save();
        }
        ChatMessageQueue = new Queue<string>();
        Profiler = new Profiling(this);
        CommandManager = new Commands(this);
        Zones = Svc.Data.GetExcelSheet<TerritoryType>().ToDictionary(row => (ushort)row.RowId, row => row);
        Jobs = Svc.Data.GetExcelSheet<ClassJob>().ToDictionary(row => (int)row.RowId, row => row.Name.ToString());
        if (ChlogGui.ChlogVersion > Config.ChlogReadVer && ChangelogGui == null)
        {
            ChangelogGui = new ChlogGui(this);
        }
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
        UnsafeElement = new();
        NotificationMasterApi = new(pluginInterface);
        SingletonServiceManager.Initialize(typeof(S));
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

    public Splatoon(IDalamudPluginInterface pluginInterface)
    {
        P = this;
        Svc.Init(pluginInterface);
        Localization.Init((Svc.PluginInterface.GetPluginConfig() is Configuration cfg) ? cfg.PluginLanguage : Localization.GameLanguageString);
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
        if (Profiler.Enabled) Profiler.MainTickChat.StartTick();
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
        if (Profiler.Enabled) Profiler.MainTickChat.StopTick();
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
        if (Profiler.Enabled) Profiler.MainTick.StartTick();
        try
        {
            if (UnsafeElement.IsEnabled)
            {
                UnsafeElement.IsUnsafeElement[0] = false;
            }
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
            if (Profiler.Enabled) Profiler.MainTickDequeue.StartTick();
            while (tickScheduler.TryDequeue(out var action))
            {
                action.Invoke();
            }
            if (Profiler.Enabled)
            {
                Profiler.MainTickDequeue.StopTick();
                Profiler.MainTickPrepare.StartTick();
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
                CamAngleX = Camera.GetAngleX() + Math.PI;
                if (CamAngleX > Math.PI) CamAngleX -= 2 * Math.PI;
                CamAngleY = Camera.GetAngleY();
                CamZoom = Math.Min(Camera.GetZoom(), 20);

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

                if (Profiler.Enabled)
                {
                    Profiler.MainTickPrepare.StopTick();
                    Profiler.MainTickFind.StartTick();
                }

                if (PinnedElementEditWindow.Script != null && PinnedElementEditWindow.EditingElement != null && !PinnedElementEditWindow.Script.InternalData.UnconditionalDraw)
                {
                    ProcessElement(PinnedElementEditWindow.EditingElement, null, true);
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
                        ProcessElement(findEl);
                    }
                }

                ProcessS2W();

                if (Profiler.Enabled)
                {
                    Profiler.MainTickFind.StopTick();
                    Profiler.MainTickCalcPresets.StartTick();
                }

                foreach (var i in Config.LayoutsL)
                {
                    ProcessLayout(i);
                }

                ScriptingProcessor.Scripts.ForEach(x => { if (x.IsEnabled) x.Controller.Layouts.Values.Each(ProcessLayout); });
                ScriptingProcessor.Scripts.ForEach(x => { if (x.IsEnabled || x.InternalData.UnconditionalDraw) x.Controller.Elements.Each(z => ProcessElement(z.Value, null, x.InternalData.UnconditionalDraw && x.InternalData.UnconditionalDrawElements.Contains(z.Key))); });
                foreach (var e in InjectedElements)
                {
                    ProcessElement(e);
                    //PluginLog.Information("Processing type " + e.type + JsonConvert.SerializeObject(e, Formatting.Indented));
                }
                InjectedElements.Clear();

                if (Profiler.Enabled)
                {
                    Profiler.MainTickCalcPresets.StopTick();
                    Profiler.MainTickCalcDynamic.StartTick();
                }

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
                        ProcessElement(e);
                    }
                }

                if (Profiler.Enabled) Profiler.MainTickCalcDynamic.StopTick();
            }
            else
            {
                Profiler.MainTickPrepare.StopTick();
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
        if (Profiler.Enabled) Profiler.MainTick.StopTick();
    }

    internal void ProcessLayout(Layout l)
    {
        if (IsLayoutVisible(l))
        {
            LayoutAmount++;
            if (l.Freezing)
            {
                if (l.FreezeInfo.CanDisplay())
                {
                    S.RenderManager.StoreDisplayObjects();
                    for (var i = 0; i < l.ElementsL.Count; i++)
                    {
                        ProcessElement(l.ElementsL[i], l);
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
                        l.FreezeInfo.AllowRefreezeAt = Environment.TickCount64 + (int)(l.IntervalBetweenFreezes * 1000f);
                    }
                    S.RenderManager.RestoreDisplayObjects();
                }
            }
            else
            {
                for (var i = 0; i < l.ElementsL.Count; i++)
                {
                    ProcessElement(l.ElementsL[i], l);
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
                S.RenderManager.GetRenderer().DisplayObjects.Add(new DisplayObjectLine(x + 2f, y + 2f, z, x - 2f, y - 2f, z, 2f, Colors.Red));
                S.RenderManager.GetRenderer().DisplayObjects.Add(new DisplayObjectLine(x - 2f, y + 2f, z, x + 2f, y - 2f, z, 2f, Colors.Red));
            }
        }
    }

    public void InjectElement(Element e)
    {
        InjectedElements.Add(e);
    }

    internal void ProcessElement(Element e, Layout i = null, bool forceEnable = false)
    {
        if (!e.Enabled && !forceEnable) return;
        ElementAmount++;
        float radius = e.radius;
        if (e.type == 0)
        {
            if (i == null || !i.UseDistanceLimit || CheckDistanceCondition(i, e.refX, e.refY, e.refZ))
            {
                S.RenderManager.GetRenderer().DrawCircle(e, e.refX, e.refY, e.refZ, radius, 0f);
            }
        }
        else if (e.type == 1 || e.type == 3 || e.type == 4)
        {
            if (e.includeOwnHitbox) radius += Svc.ClientState.LocalPlayer.HitboxRadius;
            if (e.refActorType == 1 && CheckCharacterAttributes(e, Svc.ClientState.LocalPlayer, true))
            {
                if (e.type == 1)
                {
                    var pointPos = Utils.GetPlayerPositionXZY();
                    S.RenderManager.GetRenderer().DrawCircle(e, pointPos.X, pointPos.Y, pointPos.Z, radius, e.includeRotation ? Svc.ClientState.LocalPlayer.Rotation : 0f,
                        e.overlayPlaceholders ? Svc.ClientState.LocalPlayer : null);
                }
                else if (e.type == 3)
                {
                    S.RenderManager.GetRenderer().AddRotatedLine(Utils.GetPlayerPositionXZY(), Svc.ClientState.LocalPlayer.Rotation, e, radius, 0f);
                }
                else if (e.type == 4)
                {
                    S.RenderManager.GetRenderer().DrawCone(e, Utils.GetPlayerPositionXZY(), radius, Svc.ClientState.LocalPlayer.Rotation);
                }
            }
            else if (e.refActorType == 2 && Svc.Targets.Target != null
                && Svc.Targets.Target is IBattleNpc && CheckCharacterAttributes(e, Svc.Targets.Target, true))
            {
                if (i == null || !i.UseDistanceLimit || CheckDistanceCondition(i, Svc.Targets.Target.GetPositionXZY()))
                {
                    if (e.includeHitbox) radius += Svc.Targets.Target.HitboxRadius;
                    if (e.type == 1)
                    {
                        S.RenderManager.GetRenderer().DrawCircle(e, Svc.Targets.Target.GetPositionXZY().X, Svc.Targets.Target.GetPositionXZY().Y,
                            Svc.Targets.Target.GetPositionXZY().Z, radius, e.includeRotation ? Svc.Targets.Target.Rotation : 0f,
                            e.overlayPlaceholders ? Svc.Targets.Target : null);
                    }
                    else if (e.type == 3)
                    {
                        var angle = e.FaceMe ?
                                            (180 - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians()
                                            : Svc.Targets.Target.Rotation;
                        S.RenderManager.GetRenderer().AddRotatedLine(Svc.Targets.Target.GetPositionXZY(), angle, e, radius, Svc.Targets.Target.HitboxRadius);
                    }
                    else if (e.type == 4)
                    {
                        var baseAngle = e.FaceMe ?
                                    (180 - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians()
                                    : Svc.Targets.Target.Rotation;
                        S.RenderManager.GetRenderer().DrawCone(e, Svc.Targets.Target.GetPositionXZY(), radius, baseAngle);
                    }
                }
            }
            else if (e.refActorType == 0)
            {
                if (Profiler.Enabled) Profiler.MainTickActorTableScan.StartTick();
                foreach (var a in Svc.Objects)
                {
                    var targetable = a.Struct()->GetIsTargetable();
                    if (IsAttributeMatches(e, a)
                            && (!e.onlyTargetable || targetable)
                            && (!e.onlyUnTargetable || !targetable)
                            && CheckCharacterAttributes(e, a)
                            && (!e.refTargetYou || CheckTargetingOption(e, a))
                            && (!e.refActorObjectLife || a.GetLifeTimeSeconds().InRange(e.refActorLifetimeMin, e.refActorLifetimeMax))
                            && (!e.LimitDistance || Vector3.Distance(a.GetPositionXZY(), new(e.DistanceSourceX, e.DistanceSourceY, e.DistanceSourceZ)).InRange(e.DistanceMin, e.DistanceMax).Invert(e.LimitDistanceInvert)))
                    {
                        if (i == null || !i.UseDistanceLimit || CheckDistanceCondition(i, a.GetPositionXZY()))
                        {
                            var aradius = radius;
                            if (e.includeHitbox) aradius += a.HitboxRadius;
                            if (e.type == 1)
                            {
                                S.RenderManager.GetRenderer().DrawCircle(e, a.GetPositionXZY().X, a.GetPositionXZY().Y, a.GetPositionXZY().Z, aradius,
                                    e.includeRotation ? a.Rotation : 0f,
                                    e.overlayPlaceholders ? a : null);
                            }
                            else if (e.type == 3)
                            {
                                var angle = e.FaceMe ?
                                            (180 - (MathHelper.GetRelativeAngle(a.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians()
                                            : a.Rotation;
                                S.RenderManager.GetRenderer().AddRotatedLine(a.GetPositionXZY(), angle, e, aradius, a.HitboxRadius);
                            }
                            else if (e.type == 4)
                            {
                                var baseAngle = e.FaceMe ?
                                    (180 - (MathHelper.GetRelativeAngle(a.Position.ToVector2(), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians()
                                    : (a.Rotation);
                                S.RenderManager.GetRenderer().DrawCone(e, a.GetPositionXZY(), aradius, baseAngle);
                            }
                        }
                    }
                }
                if (Profiler.Enabled) Profiler.MainTickActorTableScan.StopTick();
            }

        }
        else if (e.type == 2)
        {
            var line = new DisplayObjectLine(new Vector3(e.refX, e.refZ, e.refY), new Vector3(e.offX, e.offZ, e.offY), e.radius, e.StyleWithOverride, e.LineEndA, e.LineEndB);
            S.RenderManager.GetRenderer().DisplayObjects.Add(line);
            if (e.radius > 0)
            {

                if (UnsafeElement.IsEnabled && e.IsDangerous) UnsafeElement.ProcessLine(line);
            }
            else if (
                    (
                        i == null || !i.UseDistanceLimit || CheckDistanceToLineCondition(i, e)
                    ) &&
                    (
                    ShouldDraw(e.offX, Utils.GetPlayerPositionXZY().X, e.offY, Utils.GetPlayerPositionXZY().Y)
                    || ShouldDraw(e.refX, Utils.GetPlayerPositionXZY().X, e.refY, Utils.GetPlayerPositionXZY().Y)
                    )
                    )
                S.RenderManager.GetRenderer().DisplayObjects.Add(line);
        }
        else if (e.type == 5)
        {
            var baseAngle = e.FaceMe ?
                (180 - (MathHelper.GetRelativeAngle(new Vector2(e.refX + e.offX, e.refY + e.offY), Marking.GetPlayer(e.faceplayer).Position.ToVector2()))).DegreesToRadians()
                : 0;
            var pos = new Vector3(e.refX + e.offX, e.refY + e.offY, e.refZ + e.offZ);
            S.RenderManager.GetRenderer().DrawCone(e, pos, radius, baseAngle);
        }
    }

    private bool CheckTargetingOption(Element e, IGameObject a)
    {
        if (e.refTargetYou)
        {
            return ((e.refActorTargetingYou == 1 && a.TargetObjectId != Svc.ClientState.LocalPlayer.EntityId) || (e.refActorTargetingYou == 2 && a.TargetObjectId == Svc.ClientState.LocalPlayer.EntityId));
        }

        return false;
    }

    static bool CheckCharacterAttributes(Element e, IGameObject a, bool ignoreVisibility = false)
    {
        return
            (ignoreVisibility || !e.onlyVisible || (a is ICharacter chr && chr.IsCharacterVisible()))
            && (!e.refActorRequireCast || (e.refActorCastId.Count > 0 && a is IBattleChara chr2 && IsCastingMatches(e, chr2) != e.refActorCastReverse))
            && (!e.refActorRequireBuff || (e.refActorBuffId.Count > 0 && a is IBattleChara chr3 && CheckEffect(e, chr3)))
            && (!e.refActorUseTransformation || (a is IBattleChara chr4 && CheckTransformationID(e, chr4)))
            && (!e.refMark || (a is IBattleChara chr5 && Marking.HaveMark(chr5, (uint)e.refMarkID)))
            && (!e.LimitRotation || (a.Rotation >= e.RotationMax && a.Rotation <= e.RotationMin));
    }

    static bool CheckTransformationID(Element e, ICharacter c)
    {
        return e.refActorTransformationID == c.GetTransformationID();
    }

    static bool IsCastingMatches(Element e, IBattleChara chr)
    {
        if (chr.IsCasting(e.refActorCastId))
        {
            if (e.refActorUseCastTime)
            {
                return chr.IsCastInRange(e.refActorCastTimeMin, e.refActorCastTimeMax);
            }
            else
            {
                return true;
            }
        }
        else
        {
            if (e.refActorUseOvercast)
            {
                if (AttachedInfo.TryGetCastTime(chr.Address, e.refActorCastId, out var castTime))
                {
                    return castTime.InRange(e.refActorCastTimeMin, e.refActorCastTimeMax);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }

    static bool CheckEffect(Element e, IBattleChara c)
    {
        if (e.refActorRequireAllBuffs)
        {
            if (e.refActorUseBuffTime)
            {
                return c.StatusList.Where(x => x.RemainingTime.InRange(e.refActorBuffTimeMin, e.refActorBuffTimeMax) && (!e.refActorUseBuffParam || x.Param == e.refActorBuffParam)).Select(x => x.StatusId).ContainsAll(e.refActorBuffId).Invert(e.refActorRequireBuffsInvert);
            }
            else
            {
                return c.StatusList.Where(x => !e.refActorUseBuffParam || x.Param == e.refActorBuffParam).Select(x => x.StatusId).ContainsAll(e.refActorBuffId).Invert(e.refActorRequireBuffsInvert);
            }
        }
        else
        {
            if (e.refActorUseBuffTime)
            {
                return c.StatusList.Where(x => x.RemainingTime.InRange(e.refActorBuffTimeMin, e.refActorBuffTimeMax) && (!e.refActorUseBuffParam || x.Param == e.refActorBuffParam)).Select(x => x.StatusId).ContainsAny(e.refActorBuffId).Invert(e.refActorRequireBuffsInvert);
            }
            else
            {
                return c.StatusList.Where(x => !e.refActorUseBuffParam || x.Param == e.refActorBuffParam).Select(x => x.StatusId).ContainsAny(e.refActorBuffId).Invert(e.refActorRequireBuffsInvert);
            }
        }
    }

    static bool IsObjectEffectMatches(Element e, IGameObject o, List<CachedObjectEffectInfo> info)
    {
        if (e.refActorObjectEffectLastOnly)
        {
            if (info.Count > 0)
            {
                var last = info[info.Count - 1];
                return last.data1 == e.refActorObjectEffectData1 && last.data2 == e.refActorObjectEffectData2;
            }
            return false;
        }
        else
        {
            return info.Any(last => last.data1 == e.refActorObjectEffectData1 && last.data2 == e.refActorObjectEffectData2 && last.Age.InRange(e.refActorObjectEffectMin, e.refActorObjectEffectMax));
        }
    }

    static bool IsAttributeMatches(Element e, IGameObject o)
    {
        if (e.refActorComparisonAnd)
        {
            return (e.refActorNameIntl.Get(e.refActorName) == String.Empty || IsNameMatches(e, o)) &&
             (e.refActorModelID == 0 || (o is ICharacter c && c.Struct()->CharacterData.ModelCharaId == e.refActorModelID)) &&
             (e.refActorObjectID == 0 || o.EntityId == e.refActorObjectID) &&
             (e.refActorDataID == 0 || o.DataId == e.refActorDataID) &&
             (e.refActorNPCID == 0 || o.Struct()->GetNameId() == e.refActorNPCID) &&
             (e.refActorPlaceholder.Count == 0 || e.refActorPlaceholder.Any(x => ResolvePlaceholder(x) == o.Address)) &&
             (e.refActorNPCNameID == 0 || (o is ICharacter c2 && c2.NameId == e.refActorNPCNameID)) &&
             (e.refActorVFXPath == "" || (AttachedInfo.TryGetSpecificVfxInfo(o, e.refActorVFXPath, out var info) && info.Age.InRange(e.refActorVFXMin, e.refActorVFXMax))) &&
             ((e.refActorObjectEffectData1 == 0 && e.refActorObjectEffectData2 == 0) || (AttachedInfo.ObjectEffectInfos.TryGetValue(o.Address, out var einfo) && IsObjectEffectMatches(e, o, einfo)) &&
             (e.refActorNamePlateIconID == 0 || o.Struct()->NamePlateIconId == e.refActorNamePlateIconID));
        }
        else
        {
            if (e.refActorComparisonType == 0 && IsNameMatches(e, o)) return true;
            if (e.refActorComparisonType == 1 && o is ICharacter c && c.Struct()->CharacterData.ModelCharaId == e.refActorModelID) return true;
            if (e.refActorComparisonType == 2 && o.EntityId == e.refActorObjectID) return true;
            if (e.refActorComparisonType == 3 && o.DataId == e.refActorDataID) return true;
            if (e.refActorComparisonType == 4 && o.Struct()->GetNameId() == e.refActorNPCID) return true;
            if (e.refActorComparisonType == 5 && e.refActorPlaceholder.Any(x => ResolvePlaceholder(x) == o.Address)) return true;
            if (e.refActorComparisonType == 6 && o is ICharacter c2 && c2.NameId == e.refActorNPCNameID) return true;
            if (e.refActorComparisonType == 7 && AttachedInfo.TryGetSpecificVfxInfo(o, e.refActorVFXPath, out var info) && info.Age.InRange(e.refActorVFXMin, e.refActorVFXMax)) return true;
            if (e.refActorComparisonType == 8 && AttachedInfo.ObjectEffectInfos.TryGetValue(o.Address, out var einfo) && IsObjectEffectMatches(e, o, einfo)) return true;
            if (e.refActorComparisonType == 9 && o.Struct()->NamePlateIconId == e.refActorNamePlateIconID) return true;
            return false;
        }
    }

    static bool IsNameMatches(Element e, IGameObject o)
    {
        return !string.IsNullOrEmpty(e.refActorNameIntl.Get(e.refActorName)) && (e.refActorNameIntl.Get(e.refActorName) == "*" || o.Name.ToString().ContainsIgnoreCase(e.refActorNameIntl.Get(e.refActorName)));
    }

    static nint ResolvePlaceholder(string ph)
    {
        if (PlaceholderCache.TryGetValue(ph, out var val))
        {
            return val;
        }
        else
        {
            var result = nint.Zero;
            if (Svc.Condition[ConditionFlag.DutyRecorderPlayback])
            {
                result = (nint)FakePronoun.Resolve(ph);
            }
            else
            {
                if (ph.StartsWithIgnoreCase("<t") && int.TryParse(ph[2..3], out var n))
                {
                    result = Utils.GetRolePlaceholder(CombatRole.Tank, n)?.Address ?? 0;
                }
                else if (ph.StartsWithIgnoreCase("<h") && int.TryParse(ph[2..3], out n))
                {
                    result = Utils.GetRolePlaceholder(CombatRole.Healer, n)?.Address ?? 0;
                }
                else if (ph.StartsWithIgnoreCase("<d") && int.TryParse(ph[2..3], out n))
                {
                    result = Utils.GetRolePlaceholder(CombatRole.DPS, n)?.Address ?? 0;
                }
                else
                {
                    result = (nint)FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUIModule()->GetPronounModule()->ResolvePlaceholder(ph, 0, 0);
                }
            }
            PlaceholderCache[ph] = result;
            //PluginLog.Information($"Phaceholder {ph} result {result}");
            return result;
        }
    }

    internal bool IsLayoutVisible(Layout i)
    {
        if (!i.Enabled) return false;
        if (i.DisableInDuty && Svc.Condition[ConditionFlag.BoundByDuty]) return false;
        if ((i.ZoneLockH.Count > 0 && !i.ZoneLockH.Contains(Svc.ClientState.TerritoryType)).Invert(i.IsZoneBlacklist)) return false;
        if (i.Scenes.Count > 0 && !i.Scenes.Contains(*Scene.ActiveScene)) return false;
        if (i.Phase != 0 && i.Phase != this.Phase) return false;
        if (i.JobLock != 0 && !Bitmask.IsBitSet(i.JobLock, (int)Svc.ClientState.LocalPlayer.ClassJob.Id)) return false;
        if ((i.DCond == 1 || i.DCond == 3) && !Svc.Condition[ConditionFlag.InCombat]) return false;
        if ((i.DCond == 2 || i.DCond == 3) && !Svc.Condition[ConditionFlag.BoundByDuty]) return false;
        if (i.DCond == 4 && !(Svc.Condition[ConditionFlag.InCombat]
            || Svc.Condition[ConditionFlag.BoundByDuty])) return false;
        if (i.UseDistanceLimit && i.DistanceLimitType == 0)
        {
            if (Svc.Targets.Target != null)
            {
                var dist = Vector3.Distance(Svc.Targets.Target.GetPositionXZY(), Utils.GetPlayerPositionXZY()) - (i.DistanceLimitTargetHitbox ? Svc.Targets.Target.HitboxRadius : 0) - (i.DistanceLimitMyHitbox ? Svc.ClientState.LocalPlayer.HitboxRadius : 0);
                if (!(dist >= i.MinDistance && dist < i.MaxDistance)) return false;
            }
            else
            {
                return false;
            }
        }
        if (i.UseTriggers)
        {
            foreach (var t in i.Triggers)
            {
                if (t.FiredState == 2) continue;
                if ((t.Type == 2 || t.Type == 3) && !t.Disabled)
                {
                    foreach (var CurrentChatMessage in CurrentChatMessages)
                    {
                        var trg = t.MatchIntl.Get(t.Match);
                        if (trg != string.Empty &&
                            (t.IsRegex ? Regex.IsMatch(CurrentChatMessage, trg) : CurrentChatMessage.ContainsIgnoreCase(trg))
                            )
                        {
                            if (t.Duration == 0)
                            {
                                t.FiredState = 0;
                            }
                            else
                            {
                                t.FiredState = 1;
                                t.DisableAt.Add(Environment.TickCount64 + (int)(t.Duration * 1000) + (int)(t.MatchDelay * 1000));
                            }
                            if (t.MatchDelay != 0)
                            {
                                t.EnableAt.Add(Environment.TickCount64 + (int)(t.MatchDelay * 1000));
                            }
                            else
                            {
                                i.TriggerCondition = t.Type == 2 ? 1 : -1;
                            }
                            if (t.FireOnce)
                            {
                                t.Disabled = true;
                            }
                        }
                    }
                }
                if (t.FiredState == 0 && (t.Type == 0 || t.Type == 1))
                {
                    if (CombatStarted != 0 && Environment.TickCount64 - CombatStarted > t.TimeBegin * 1000)
                    {
                        if (t.Duration == 0)
                        {
                            t.FiredState = 2;
                        }
                        else
                        {
                            t.FiredState = 1;
                            t.DisableAt.Add(Environment.TickCount64 + (int)(t.Duration * 1000));
                        }
                        i.TriggerCondition = t.Type == 0 ? 1 : -1;
                    }
                }
                for (var e = 0; e < t.EnableAt.Count; e++)
                {
                    if (Environment.TickCount64 > t.EnableAt[e])
                    {
                        i.TriggerCondition = t.Type == 2 ? 1 : -1;
                        t.EnableAt.RemoveAt(e);
                        break;
                    }
                }
                for (var e = 0; e < t.DisableAt.Count; e++)
                {
                    if (Environment.TickCount64 > t.DisableAt[e])
                    {
                        t.FiredState = (t.Type == 2 || t.Type == 3) ? 0 : 2;
                        t.DisableAt.RemoveAt(e);
                        i.TriggerCondition = 0;
                        break;
                    }
                }

            }
            if (i.TriggerCondition == -1 || (i.TriggerCondition == 0 && i.DCond == 5)) return false;
        }
        return true;
    }

    internal bool CheckDistanceCondition(Layout i, float x, float y, float z)
    {
        return CheckDistanceCondition(i, new Vector3(x, y, z));
    }

    internal bool CheckDistanceCondition(Layout i, Vector3 v)
    {
        if (i.DistanceLimitType != 1) return true;
        var dist = Vector3.Distance(v, Utils.GetPlayerPositionXZY());
        if (!(dist >= i.MinDistance && dist < i.MaxDistance)) return false;
        return true;
    }

    internal bool CheckDistanceToLineCondition(Layout i, Element e)
    {
        if (i.DistanceLimitType != 1) return true;
        var dist = Vector3.Distance(Utils.FindClosestPointOnLine(Utils.GetPlayerPositionXZY(), new Vector3(e.refX, e.refY, e.refZ), new Vector3(e.offX, e.offY, e.offZ)), Utils.GetPlayerPositionXZY());
        if (!(dist >= i.MinDistance && dist < i.MaxDistance)) return false;
        return true;
    }

    internal bool ShouldDraw(float x1, float x2, float y1, float y2)
    {
        return ((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2)) < Config.maxdistance * Config.maxdistance;
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
