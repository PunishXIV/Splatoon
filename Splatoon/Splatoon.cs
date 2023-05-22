using Dalamud;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Internal.Notifications;
using ECommons;
using ECommons.Events;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.LanguageHelpers;
using ECommons.MathHelpers;
using ECommons.ObjectLifeTracker;
using ECommons.SimpleGui;
using Lumina.Excel.GeneratedSheets;
using PInvoke;
using Splatoon.Gui;
using Splatoon.Memory;
using Splatoon.Modules;
using Splatoon.SplatoonScripting;
using Splatoon.Structures;
using Splatoon.Utils;
using System.Net.Http;
using System.Text.RegularExpressions;
using Localization = ECommons.LanguageHelpers.Localization;

namespace Splatoon;
public unsafe class Splatoon : IDalamudPlugin
{
    public const string DiscordURL = "https://discord.gg/m8NRt4X8Gf";
    public string Name => "Splatoon";
    internal static Splatoon P;
    internal OverlayGui DrawingGui;
    internal CGui ConfigGui;
    internal Commands CommandManager;
    internal ChlogGui ChangelogGui = null;
    internal Configuration Config;
    internal Dictionary<ushort, TerritoryType> Zones;
    internal long CombatStarted = 0;
    internal HashSet<DisplayObject> displayObjects = new();
    internal HashSet<Element> injectedElements = new();
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
    internal Dictionary<(string Name, uint ObjectID, long ObjectIDLong, uint DataID, int ModelID, uint NPCID, uint NameID, ObjectKind type), ObjectInfo> loggedObjectList = new();
    internal bool LogObjects = false;
    internal bool DisableLineFix = false;
    private int phase = 1;
    internal int Phase { get => phase; set { phase = value; ScriptingProcessor.OnPhaseChange(value); } }
    internal int LayoutAmount = 0;
    internal int ElementAmount = 0;
    /*internal static readonly string[] LimitGaugeResets = new string[] 
    {
        "The limit gauge resets!",
        "リミットゲージがリセットされた……",
        "Der Limitrausch-Balken wurde geleert.",
        "La jauge de Transcendance a été réinitialisée.",
        "极限槽被清零了……"
    };*/
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

    internal void Load(DalamudPluginInterface pluginInterface)
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

        DrawingGui = new OverlayGui(this);
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
        ProperOnLogin.Register(delegate
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
        Safe(DrawingGui.Dispose);
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
        AttachedInfo.Dispose();
        ScriptingProcessor.Dispose();
        ECommonsMain.Dispose();
        P = null;
        //Svc.Chat.Print("Disposing");
    }

    public Splatoon(DalamudPluginInterface pluginInterface)
    {
        P = this;
        Svc.Init(pluginInterface);
        Localization.Init((Svc.PluginInterface.GetPluginConfig() is Configuration cfg)?cfg.PluginLanguage : Localization.GameLanguageString);
        loader = new Loader(this);
    }

    internal static void OnLogout(object _, object __)
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
        }) ;
    }

    public void RemoveDynamicElements(string name)
    {
        dynamicElements.RemoveAll(x => x.Name == name);
    }

    internal static readonly string[] InvalidSymbols = { "", "", "", "“", "”", "" };
    internal void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (Profiler.Enabled) Profiler.MainTickChat.StartTick();
        var inttype = (int)type;
        if(inttype == 2105 && LimitGaugeResets.Equals(message.ToString()))
        {
            Phase++;
            CombatStarted = Environment.TickCount64;
            Svc.PluginInterface.UiBuilder.AddNotification($"Phase transition to Phase ??".Loc(Phase), this.Name, NotificationType.Info, 10000);
        }
        if(!type.EqualsAny(ECommons.Constants.NormalChatTypes))
        {
            var m = message.Payloads.Where(p => p is ITextProvider)
                    .Cast<ITextProvider>()
                    .Aggregate(new StringBuilder(), (sb, tp) => sb.Append(tp.Text.RemoveSymbols(InvalidSymbols).Replace("\n", " ")), sb => sb.ToString());
            ChatMessageQueue.Enqueue(m);
            if (P.Config.Logging && !((uint)type).EqualsAny(BlacklistedMessages))
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
            if(HttpServer == null)
            {
                try
                {
                    HttpServer = new HTTPServer(this);
                }
                catch(Exception e)
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

    internal void TerritoryChangedEvent(object sender, ushort e)
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
            foreach(var l in de.Layouts)
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
        foreach(var l in Config.LayoutsL)
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
            l.freezeInfo = new();
        }
    }

    
    internal void Tick(Framework framework)
    {
        if (Profiler.Enabled) Profiler.MainTick.StartTick();
        try
        {
            PlaceholderCache.Clear();
            LayoutAmount = 0;
            ElementAmount = 0;
            if (LogObjects && Svc.ClientState.LocalPlayer != null)
            {
                foreach(var t in Svc.Objects)
                {
                    var ischar = t is Character;
                    var obj = (t.Name.ToString(), t.ObjectId, t.Struct()->GetObjectID(), t.DataId, ischar ? ((Character)t).Struct()->ModelCharaId : 0, t.Struct()->GetNpcID(), ischar ? ((Character)t).NameId : 0, t.ObjectKind);
                    loggedObjectList.TryAdd(obj, new ObjectInfo());
                    loggedObjectList[obj].ExistenceTicks++;
                    loggedObjectList[obj].IsChar = ischar;
                    if (ischar)
                    {
                        loggedObjectList[obj].Targetable = t.Struct()->GetIsTargetable();
                        loggedObjectList[obj].Visible = ((Character)t).IsCharacterVisible();
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
            displayObjects.Clear();
            if (Svc.ClientState.LocalPlayer != null)
            {
                if (ChatMessageQueue.Count > 5 * dequeueConcurrency)
                {
                    dequeueConcurrency++;
                    PluginLog.Debug($"Too many queued messages ({ChatMessageQueue.Count}); concurrency increased to {dequeueConcurrency}");
                }
                for(var i = 0; i < dequeueConcurrency; i++)
                {
                    if(ChatMessageQueue.TryDequeue(out var ccm))
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
                /*Range conversion https://stackoverflow.com/questions/5731863/mapping-a-numeric-range-onto-another
                slope = (output_end - output_start) / (input_end - input_start)
                output = output_start + slope * (input - input_start) */
                CurrentLineSegments = (int)((3f + -0.108108f * (CamZoom - 1.5f)) * Config.lineSegments);

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

                if(Profiler.Enabled)
                {
                    Profiler.MainTickPrepare.StopTick();
                    Profiler.MainTickFind.StartTick();
                }

                if(PinnedElementEditWindow.Script != null && PinnedElementEditWindow.EditingElement != null && !PinnedElementEditWindow.Script.InternalData.UnconditionalDraw)
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
                foreach (var e in injectedElements)
                {
                    ProcessElement(e);
                    //PluginLog.Information("Processing type " + e.type + JsonConvert.SerializeObject(e, Formatting.Indented));
                }
                injectedElements.Clear();

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
        catch(Exception e)
        {
            Log("Caught exception: "+e.Message);
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
                if (l.freezeInfo.CanDisplay())
                {
                    var a = displayObjects;
                    displayObjects = new();
                    for (var i = 0; i < l.ElementsL.Count; i++)
                    {
                        ProcessElement(l.ElementsL[i], l);
                    }
                    if (displayObjects.Count > 0)
                    {
                        l.freezeInfo.States.Add(new()
                        {
                            Objects = displayObjects,
                            ShowUntil = Environment.TickCount64 + (int)(l.FreezeFor * 1000f),
                            ShowAt = Environment.TickCount64 + (int)(l.FreezeDisplayDelay * 1000f)
                        }) ;
                        l.freezeInfo.AllowRefreezeAt = Environment.TickCount64 + (int)(l.IntervalBetweenFreezes * 1000f);
                    }
                    displayObjects = a;
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
        for (var i = l.freezeInfo.States.Count - 1;i>=0;i--)
        {
            var x = l.freezeInfo.States[i];
            if (x.IsActive())
            {
                displayObjects.UnionWith(x.Objects);
            }
            else
            {
                if (x.IsExpired())
                {
                    l.freezeInfo.States.RemoveAt(i);
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
                displayObjects.Add(new DisplayObjectLine(x + 2f, y + 2f, z, x - 2f, y - 2f, z, 2f, Colors.Red));
                displayObjects.Add(new DisplayObjectLine(x - 2f, y + 2f, z, x + 2f, y - 2f, z, 2f, Colors.Red));
            }
        }
    }

    public void InjectElement(Element e)
    {
        injectedElements.Add(e);
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
                DrawCircle(e, e.refX, e.refY, e.refZ, radius, 0f);
            }
        }
        else if (e.type == 1 || e.type == 3 || e.type == 4)
        {
            if (e.includeOwnHitbox) radius += Svc.ClientState.LocalPlayer.HitboxRadius;
            if (e.refActorType == 1 && CheckCharacterAttributes(e, Svc.ClientState.LocalPlayer, true))
            {
                if (e.type == 1)
                {
                    var pointPos = GetPlayerPositionXZY();
                    DrawCircle(e, pointPos.X, pointPos.Y, pointPos.Z, radius, e.includeRotation ? Svc.ClientState.LocalPlayer.Rotation : 0f, 
                        e.overlayPlaceholders?Svc.ClientState.LocalPlayer:null);
                }
                else if (e.type == 3)
                {
                    AddRotatedLine(GetPlayerPositionXZY(), Svc.ClientState.LocalPlayer.Rotation, e, radius, 0f);
                    //Svc.Chat.Print(Svc.ClientState.LocalPlayer.Rotation.ToString());
                }
                else if (e.type == 4)
                {
                    if(e.coneAngleMax > e.coneAngleMin)
                    {
                        for(var x = e.coneAngleMin; x < e.coneAngleMax; x+= GetFillStepCone(e.FillStep))
                        {
                            AddConeLine(GetPlayerPositionXZY(), (Svc.ClientState.LocalPlayer.Rotation.RadiansToDegrees() - x.Float()).DegreesToRadians(), e, radius);
                        }
                        AddConeLine(GetPlayerPositionXZY(), (Svc.ClientState.LocalPlayer.Rotation.RadiansToDegrees() - e.coneAngleMax.Float()).DegreesToRadians(), e, radius);
                    }
                }
            }
            else if (e.refActorType == 2 && Svc.Targets.Target != null
                && Svc.Targets.Target is BattleNpc && CheckCharacterAttributes(e, Svc.Targets.Target, true))
            {
                if (i == null || !i.UseDistanceLimit || CheckDistanceCondition(i, Svc.Targets.Target.GetPositionXZY()))
                {
                    if (e.includeHitbox) radius += Svc.Targets.Target.HitboxRadius;
                    if (e.type == 1)
                    {
                        DrawCircle(e, Svc.Targets.Target.GetPositionXZY().X, Svc.Targets.Target.GetPositionXZY().Y,
                            Svc.Targets.Target.GetPositionXZY().Z, radius, e.includeRotation ? Svc.Targets.Target.Rotation : 0f,
                            e.overlayPlaceholders ? Svc.Targets.Target : null);
                    }
                    else if(e.type == 3)
                    {
                        var angle = e.FaceMe ?
                                            (180 - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), Svc.ClientState.LocalPlayer.Position.ToVector2()))).DegreesToRadians()
                                            : Svc.Targets.Target.Rotation;
                        AddRotatedLine(Svc.Targets.Target.GetPositionXZY(), angle, e, radius, Svc.Targets.Target.HitboxRadius);
                    }
                    else if (e.type == 4)
                    {
                        if (e.coneAngleMax > e.coneAngleMin)
                        {
                            for (var x = e.coneAngleMin; x < e.coneAngleMax; x += GetFillStepCone(e.FillStep))
                            {
                                var angle = e.FaceMe ?
                                            (180 - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), Svc.ClientState.LocalPlayer.Position.ToVector2()) - x.Float())).DegreesToRadians()
                                            : (Svc.Targets.Target.Rotation.RadiansToDegrees() - x.Float()).DegreesToRadians();
                                AddConeLine(Svc.Targets.Target.GetPositionXZY(), angle, e, radius);
                            }
                            {
                                var angle = e.FaceMe ?
                                                (180 - (MathHelper.GetRelativeAngle(Svc.Targets.Target.Position.ToVector2(), Svc.ClientState.LocalPlayer.Position.ToVector2()) - e.coneAngleMax.Float())).DegreesToRadians()
                                                : (Svc.Targets.Target.Rotation.RadiansToDegrees() - e.coneAngleMax.Float()).DegreesToRadians();
                                AddConeLine(Svc.Targets.Target.GetPositionXZY(), angle, e, radius);
                            }
                        }
                        //displayObjects.Add(new DisplayObjectCone(e, Svc.Targets.Target.Position, Svc.Targets.Target.Rotation, radius));
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
                            && (!e.refActorObjectLife || a.GetLifeTimeSeconds().InRange(e.refActorLifetimeMin, e.refActorLifetimeMax))
                            && (!e.LimitDistance || Vector3.Distance(a.GetPositionXZY(), new(e.DistanceSourceX, e.DistanceSourceY, e.DistanceSourceZ)).InRange(e.DistanceMin, e.DistanceMax).Invert(e.LimitDistanceInvert)))
                    {
                        if (i == null || !i.UseDistanceLimit || CheckDistanceCondition(i, a.GetPositionXZY()))
                        {
                            var aradius = radius;
                            if (e.includeHitbox) aradius += a.HitboxRadius;
                            if (e.type == 1)
                            {
                                DrawCircle(e, a.GetPositionXZY().X, a.GetPositionXZY().Y, a.GetPositionXZY().Z, aradius, 
                                    e.includeRotation ? a.Rotation : 0f,
                                    e.overlayPlaceholders ? a : null);
                            }
                            else if (e.type == 3)
                            {
                                var angle = e.FaceMe ?
                                            (180 - (MathHelper.GetRelativeAngle(a.Position.ToVector2(), Svc.ClientState.LocalPlayer.Position.ToVector2()))).DegreesToRadians()
                                            : a.Rotation;
                                AddRotatedLine(a.GetPositionXZY(), angle, e, aradius, a.HitboxRadius);
                            }
                            else if (e.type == 4)
                            {
                                if (e.coneAngleMax > e.coneAngleMin)
                                {
                                    for (var x = e.coneAngleMin; x < e.coneAngleMax; x += GetFillStepCone(e.FillStep))
                                    {
                                        var angle = e.FaceMe ?
                                            (180-(MathHelper.GetRelativeAngle(a.Position.ToVector2(), Svc.ClientState.LocalPlayer.Position.ToVector2()) - x.Float())).DegreesToRadians()
                                            : (a.Rotation.RadiansToDegrees() - x.Float()).DegreesToRadians();
                                        AddConeLine(a.GetPositionXZY(), angle, e, aradius);
                                    }
                                    {
                                        var angle = e.FaceMe ?
                                            (180 - (MathHelper.GetRelativeAngle(a.Position.ToVector2(), Svc.ClientState.LocalPlayer.Position.ToVector2()) - e.coneAngleMax.Float())).DegreesToRadians()
                                            : (a.Rotation.RadiansToDegrees() - e.coneAngleMax.Float()).DegreesToRadians();
                                        AddConeLine(a.GetPositionXZY(), angle, e, aradius);
                                    }
                                }
                                //displayObjects.Add(new DisplayObjectCone(e, a.Position, a.Rotation, aradius));
                            }
                        }
                    }
                }
                if (Profiler.Enabled) Profiler.MainTickActorTableScan.StopTick();
            }

        }
        else if (e.type == 2)
        {
            if (e.radius > 0)
            {
                PerpOffset(new Vector2(e.refX, e.refY), new Vector2(e.offX, e.offY), 0f, e.radius, out _, out var p1);
                PerpOffset(new Vector2(e.refX, e.refY), new Vector2(e.offX, e.offY), 0f, -e.radius, out _, out var p2);
                PerpOffset(new Vector2(e.refX, e.refY), new Vector2(e.offX, e.offY), 1f, e.radius, out _, out var p3);
                PerpOffset(new Vector2(e.refX, e.refY), new Vector2(e.offX, e.offY), 1f, -e.radius, out _, out var p4);
                var rect = new DisplayObjectRect()
                {
                    l1 = new DisplayObjectLine(p1.X, p1.Y, e.refZ,
                    p2.X, p2.Y, e.refZ,
                    e.thicc, e.color),
                    l2 = new DisplayObjectLine(p3.X, p3.Y, e.offZ,
                    p4.X, p4.Y, e.offZ,
                    e.thicc, e.color)
                };
                if (Config.AltRectFill)
                {
                    AddAlternativeFillingRect(rect, GetFillStepRect(e.FillStep));
                }
                else
                {
                    displayObjects.Add(rect);
                }
            }
            else
            {
                if (
                    (
                        i == null || !i.UseDistanceLimit || CheckDistanceToLineCondition(i, e)
                    ) &&
                    (
                    ShouldDraw(e.offX, GetPlayerPositionXZY().X, e.offY, GetPlayerPositionXZY().Y)
                    || ShouldDraw(e.refX, GetPlayerPositionXZY().X, e.refY, GetPlayerPositionXZY().Y)
                    )
                    )
                    displayObjects.Add(new DisplayObjectLine(e.refX, e.refY, e.refZ, e.offX, e.offY, e.offZ, e.thicc, e.color));
            }
        }
        else if(e.type == 5)
        {
            if (e.coneAngleMax > e.coneAngleMin)
            {
                var pos = new Vector3(e.refX + e.offX, e.refY + e.offY, e.refZ + e.offZ);
                for (var x = e.coneAngleMin; x < e.coneAngleMax; x += GetFillStepCone(e.FillStep))
                {
                    var angle = e.FaceMe ?
                        (180 - (MathHelper.GetRelativeAngle(new Vector2(e.refX + e.offX, e.refY + e.offY), Svc.ClientState.LocalPlayer.Position.ToVector2()) - x.Float())).DegreesToRadians()
                        : (-x.Float()).DegreesToRadians();
                    AddConeLine(pos, angle, e, e.radius);
                }
                {
                    var angle = e.FaceMe ?
                        (180 - (MathHelper.GetRelativeAngle(new Vector2(e.refX + e.offX, e.refY + e.offY), Svc.ClientState.LocalPlayer.Position.ToVector2()) - e.coneAngleMax.Float())).DegreesToRadians()
                        : (-e.coneAngleMax.Float()).DegreesToRadians();
                    AddConeLine(pos, angle, e, e.radius);
                }
            }
        }
    }

    void AddAlternativeFillingRect(DisplayObjectRect rect, float step)
    {
        var thc = P.Config.AltRectForceMinLineThickness || rect.l1.thickness < P.Config.AltRectMinLineThickness ? P.Config.AltRectMinLineThickness : rect.l1.thickness;
        var col = P.Config.AltRectHighlightOutline ? (rect.l1.color.ToVector4() with { W = 1f }).ToUint() : rect.l1.color;
        var fl1 = new DisplayObjectLine(rect.l1.ax, rect.l1.ay, rect.l1.az, rect.l2.ax, rect.l2.ay, rect.l2.az, thc, col);
        var fl2 = new DisplayObjectLine(rect.l1.bx, rect.l1.by, rect.l1.bz, rect.l2.bx, rect.l2.by, rect.l2.bz, thc, col);
        var fl3 = new DisplayObjectLine(rect.l1.ax, rect.l1.ay, rect.l1.az, rect.l1.bx, rect.l1.by, rect.l1.bz, thc, col);
        var fl4 = new DisplayObjectLine(rect.l2.ax, rect.l2.ay, rect.l2.az, rect.l2.bx, rect.l2.by, rect.l2.bz, thc, col);
        displayObjects.Add(fl1);
        displayObjects.Add(fl2);
        displayObjects.Add(fl3);
        displayObjects.Add(fl4);
        {
            var v1 = new Vector3(rect.l1.ax, rect.l1.ay, rect.l1.az);
            var v2 = new Vector3(rect.l2.ax, rect.l2.ay, rect.l2.az);
            var v3 = new Vector3(rect.l1.bx, rect.l1.by, rect.l1.bz);
            var v4 = new Vector3(rect.l2.bx, rect.l2.by, rect.l2.bz);
            var dst = Vector3.Distance(v2, v1);
            var stp = dst / step;
            var d1 = (v2 - v1) / stp;
            var d2 = (v4 - v3) / stp;
            for (var i = step; i < dst; i += step)
            {
                v1 += d1;
                v3 += d2;
                displayObjects.Add(new DisplayObjectLine(v1.X, v1.Y, v1.Z, v3.X, v3.Y, v3.Z, thc, rect.l1.color));
            }
        }
        {
            var v1 = new Vector3(rect.l1.ax, rect.l1.ay, rect.l1.az);
            var v3 = new Vector3(rect.l2.ax, rect.l2.ay, rect.l2.az);
            var v2 = new Vector3(rect.l1.bx, rect.l1.by, rect.l1.bz);
            var v4 = new Vector3(rect.l2.bx, rect.l2.by, rect.l2.bz);
            var dst = Vector3.Distance(v2, v1);
            var stp = dst / step;
            var d1 = (v2 - v1) / stp;
            var d2 = (v4 - v3) / stp;
            for (var i = step; i < dst; i += step)
            {
                v1 += d1;
                v3 += d2;
                displayObjects.Add(new DisplayObjectLine(v1.X, v1.Y, v1.Z, v3.X, v3.Y, v3.Z, thc, rect.l1.color));
            }
        }
    }

    static bool CheckCharacterAttributes(Element e, GameObject a, bool ignoreVisibility = false)
    {
        return
            (ignoreVisibility || !e.onlyVisible || (a is Character chr && chr.IsCharacterVisible()))
            && (!e.refActorRequireCast || (e.refActorCastId.Count > 0 && a is BattleChara chr2 && IsCastingMatches(e, chr2) != e.refActorCastReverse))
            && (!e.refActorRequireBuff || (e.refActorBuffId.Count > 0 && a is BattleChara chr3 && CheckEffect(e, chr3)))
            && (!e.refActorUseTransformation || (a is BattleChara chr4 && CheckTransformationID(e, chr4)))
            && (!e.LimitRotation || (a.Rotation >= e.RotationMax && a.Rotation <= e.RotationMin));
    }

    static bool CheckTransformationID(Element e, Character c)
    {
        return e.refActorTransformationID == c.GetTransformationID();
    }

    static bool IsCastingMatches(Element e, BattleChara chr)
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
                if(AttachedInfo.TryGetCastTime(chr.Address, e.refActorCastId, out var castTime))
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

    static bool CheckEffect(Element e, BattleChara c)
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

    static bool IsObjectEffectMatches(Element e, GameObject o, List<CachedObjectEffectInfo> info)
    {
        if (e.refActorObjectEffectLastOnly)
        {
            if(info.Count > 0)
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

    static bool IsAttributeMatches(Element e, GameObject o)
    {
        if (e.refActorComparisonAnd)
        {
            return (e.refActorNameIntl.Get(e.refActorName) == String.Empty || IsNameMatches(e, o)) &&
             (e.refActorModelID == 0 || (o is Character c && c.Struct()->ModelCharaId == e.refActorModelID)) &&
             (e.refActorObjectID == 0 || o.ObjectId == e.refActorObjectID) &&
             (e.refActorDataID == 0 || o.DataId == e.refActorDataID) &&
             (e.refActorNPCID == 0 || o.Struct()->GetNpcID() == e.refActorNPCID) &&
             (e.refActorPlaceholder.Count == 0 || e.refActorPlaceholder.Any(x => ResolvePlaceholder(x) == o.Address)) &&
             (e.refActorNPCNameID == 0 || (o is Character c2 && c2.NameId == e.refActorNPCNameID)) &&
             (e.refActorVFXPath == "" || (AttachedInfo.TryGetSpecificVfxInfo(o, e.refActorVFXPath, out var info) && info.Age.InRange(e.refActorVFXMin, e.refActorVFXMax))) &&
             ((e.refActorObjectEffectData1 == 0 && e.refActorObjectEffectData2 == 0) || (AttachedInfo.ObjectEffectInfos.TryGetValue(o.Address, out var einfo) && IsObjectEffectMatches(e, o, einfo) ));
        }
        else
        {
            if (e.refActorComparisonType == 0 && IsNameMatches(e, o)) return true;
            if (e.refActorComparisonType == 1 && o is Character c && c.Struct()->ModelCharaId == e.refActorModelID) return true;
            if (e.refActorComparisonType == 2 && o.ObjectId == e.refActorObjectID) return true;
            if (e.refActorComparisonType == 3 && o.DataId == e.refActorDataID) return true;
            if (e.refActorComparisonType == 4 && o.Struct()->GetNpcID() == e.refActorNPCID) return true;
            if (e.refActorComparisonType == 5 && e.refActorPlaceholder.Any(x => ResolvePlaceholder(x) == o.Address)) return true;
            if (e.refActorComparisonType == 6 && o is Character c2 && c2.NameId == e.refActorNPCNameID) return true;
            if (e.refActorComparisonType == 7 && AttachedInfo.TryGetSpecificVfxInfo(o, e.refActorVFXPath, out var info) && info.Age.InRange(e.refActorVFXMin, e.refActorVFXMax)) return true;
            if (e.refActorComparisonType == 8 && AttachedInfo.ObjectEffectInfos.TryGetValue(o.Address, out var einfo) && IsObjectEffectMatches(e, o, einfo)) return true;
            return false;
        }
    }

    static bool IsNameMatches(Element e, GameObject o)
    {
        return !string.IsNullOrEmpty(e.refActorNameIntl.Get(e.refActorName)) && (e.refActorNameIntl.Get(e.refActorName) == "*" || o.Name.ToString().ContainsIgnoreCase(e.refActorNameIntl.Get(e.refActorName)));
    }

    static nint ResolvePlaceholder(string ph)
    {
        if(PlaceholderCache.TryGetValue(ph, out var val))
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
                    result = Static.GetRolePlaceholder(CombatRole.Tank, n)?.Address ?? nint.Zero;
                }
                else if (ph.StartsWithIgnoreCase("<h") && int.TryParse(ph[2..3], out n))
                {
                    result = Static.GetRolePlaceholder(CombatRole.Healer, n)?.Address ?? nint.Zero;
                }
                else if (ph.StartsWithIgnoreCase("<d") && int.TryParse(ph[2..3], out n))
                {
                    result = Static.GetRolePlaceholder(CombatRole.DPS, n)?.Address ?? nint.Zero;
                }
                else
                {
                    result = (nint)FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetPronounModule()->ResolvePlaceholder(ph, 0, 0);
                }
            }
            PlaceholderCache[ph] = result;
            //PluginLog.Information($"Phaceholder {ph} result {result}");
            return result;
        }
    }

    void DrawCircle(Element e, float x, float y, float z, float r, float angle, GameObject go = null)
    {
        var cx = x + e.offX;
        var cy = y + e.offY;
        if (e.includeRotation)
        {
            var rotatedPoint = RotatePoint(x, y, -angle + e.AdditionalRotation, new Vector3(x - e.offX, y + e.offY, z));
            cx = rotatedPoint.X;
            cy = rotatedPoint.Y;
        }
        if (e.tether)
        {
            displayObjects.Add(new DisplayObjectLine(cx,
                cy,
                z,
                GetPlayerPositionXZY().X, GetPlayerPositionXZY().Y, GetPlayerPositionXZY().Z,
                e.thicc, e.color));
        }
        if (!ShouldDraw(cx, GetPlayerPositionXZY().X, cy, GetPlayerPositionXZY().Y)) return;
        if (e.thicc > 0)
        {
            if (r > 0)
            {
                displayObjects.Add(new DisplayObjectCircle(cx, cy, z + e.offZ, r, e.thicc, e.color, e.Filled));
                if(e != null && e.Donut > 0)
                {
                    var donutR = GetFillStepDonut(e.FillStep);
                    while(donutR < e.Donut)
                    {
                        displayObjects.Add(new DisplayObjectCircle(cx, cy, z + e.offZ, r + donutR, e.thicc, e.color, e.Filled));
                        donutR += GetFillStepDonut(e.FillStep);
                    }
                    displayObjects.Add(new DisplayObjectCircle(cx, cy, z + e.offZ, r + e.Donut, e.thicc, e.color, e.Filled));
                }
            }
            else
            {
                displayObjects.Add(new DisplayObjectDot(cx, cy, z + e.offZ, e.thicc, e.color));
            }
        }
        if (e.overlayText.Length > 0)
        {
            var text = e.overlayText;
            if (go != null)
            {
                text = text
                    .Replace("$NAMEID", $"{(go is Character chr2 ? chr2.NameId : 0).Format()}")
                    .Replace("$NAME", go.Name.ToString())
                    .Replace("$OBJECTID", $"{go.ObjectId.Format()}")
                    .Replace("$DATAID", $"{go.DataId.Format()}")
                    .Replace("$MODELID", $"{(go is Character chr ? chr.Struct()->ModelCharaId : 0).Format()}")
                    .Replace("$HITBOXR", $"{go.HitboxRadius:F1}")
                    .Replace("$KIND", $"{go.ObjectKind}")
                    .Replace("$NPCID", $"{go.Struct()->GetNpcID().Format()}")
                    .Replace("$LIFE", $"{go.GetLifeTimeSeconds():F1}")
                    .Replace("$DISTANCE", $"{Vector3.Distance((Svc.ClientState.LocalPlayer?.Position ?? Vector3.Zero), go.Position):F1}")
                    .Replace("$CAST", go is BattleChara chr3?$"[{chr3.CastActionId.Format()}] {chr3.CurrentCastTime}/{chr3.TotalCastTime}":"")
                    .Replace("\\n", "\n")
                    .Replace("$VFXID", $"{(go is Character chr4 ? chr4.GetStatusVFXId() : 0).Format()}")
                    .Replace("$TRANSFORM", $"{(go is Character chr5 ? chr5.GetTransformationID() : 0).Format()}")
                    .Replace("$MSTATUS", $"{(*(int*)(go.Address + 0x104)).Format()}");
            }
            displayObjects.Add(new DisplayObjectText(cx, cy, z + e.offZ + e.overlayVOffset, text, e.overlayBGColor, e.overlayTextColor, e.overlayFScale));
        }
    }

    void AddConeLine(Vector3 tPos, float angle, Element e, float radius)
    {
        tPos += new Vector3(e.offX, e.offY, e.offZ);
        var pointA = RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X,
                    tPos.Y,
                    tPos.Z));
        var pointB = RotatePoint(tPos.X, tPos.Y,
            -angle + e.AdditionalRotation, new Vector3(
            tPos.X,
            tPos.Y + radius,
            tPos.Z));
        displayObjects.Add(new DisplayObjectLine(pointA.X, pointA.Y, pointA.Z,
            pointB.X, pointB.Y, pointB.Z,
            e.thicc, e.color));
    }

    void AddRotatedLine(Vector3 tPos, float angle, Element e, float aradius, float hitboxRadius)
    {
        if (e.includeRotation)
        {
            if (aradius == 0f)
            {
                var pointA = RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X + -e.refX,
                    tPos.Y + e.refY,
                    tPos.Z + e.refZ) + new Vector3(e.LineAddHitboxLengthXA ? hitboxRadius : 0f, e.LineAddHitboxLengthYA ? hitboxRadius : 0f, e.LineAddHitboxLengthZA ? hitboxRadius : 0f) + new Vector3(e.LineAddPlayerHitboxLengthXA ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthYA ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthZA ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f));
                var pointB = RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X + -e.offX,
                    tPos.Y + e.offY,
                    tPos.Z + e.offZ) + new Vector3(e.LineAddHitboxLengthX ? hitboxRadius : 0f, e.LineAddHitboxLengthY ? hitboxRadius : 0f, e.LineAddHitboxLengthZ ? hitboxRadius : 0f) + new Vector3(e.LineAddPlayerHitboxLengthX ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthY ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthZ ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f));
                displayObjects.Add(new DisplayObjectLine(pointA.X, pointA.Y, pointA.Z,
                    pointB.X, pointB.Y, pointB.Z,
                    e.thicc, e.color));
            }
            else
            {
                var pointA = RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X + -e.refX - aradius,
                    tPos.Y + e.refY,
                    tPos.Z + e.refZ));
                var pointB = RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X + -e.offX - aradius,
                    tPos.Y + e.offY,
                    tPos.Z + e.offZ));
                var pointA2 = RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X + -e.refX + aradius,
                    tPos.Y + e.refY,
                    tPos.Z + e.refZ));
                var pointB2 = RotatePoint(tPos.X, tPos.Y,
                    -angle + e.AdditionalRotation, new Vector3(
                    tPos.X + -e.offX + aradius,
                    tPos.Y + e.offY,
                    tPos.Z + e.offZ));

                var rect = new DisplayObjectRect()
                {
                    l1 = new DisplayObjectLine(pointA.X, pointA.Y, pointA.Z,
                    pointB.X, pointB.Y, pointB.Z,
                    e.thicc, e.color),
                    l2 = new DisplayObjectLine(pointA2.X, pointA2.Y, pointA2.Z,
                    pointB2.X, pointB2.Y, pointB2.Z,
                    e.thicc, e.color)
                };
                if (Config.AltRectFill)
                {
                    AddAlternativeFillingRect(rect, GetFillStepRect(e.FillStep));
                }
                else
                {
                    displayObjects.Add(rect);
                }
            }
        }
        else
        {
            var pointA = new Vector3(
                tPos.X + e.refX,
                tPos.Y + e.refY,
                tPos.Z + e.refZ) + new Vector3(e.LineAddHitboxLengthXA ? hitboxRadius : 0f, e.LineAddHitboxLengthYA ? hitboxRadius : 0f, e.LineAddHitboxLengthZA ? hitboxRadius : 0f) + new Vector3(e.LineAddPlayerHitboxLengthXA ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthYA ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthZA ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f);
            var pointB = new Vector3(
                tPos.X + e.offX,
                tPos.Y + e.offY,
                tPos.Z + e.offZ) + new Vector3(e.LineAddHitboxLengthX ? hitboxRadius : 0f, e.LineAddHitboxLengthY ? hitboxRadius : 0f, e.LineAddHitboxLengthZ ? hitboxRadius : 0f) + new Vector3(e.LineAddPlayerHitboxLengthX ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthY ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f, e.LineAddPlayerHitboxLengthZ ? Svc.ClientState.LocalPlayer.HitboxRadius : 0f);
            displayObjects.Add(new DisplayObjectLine(pointA.X, pointA.Y, pointA.Z,
                pointB.X, pointB.Y, pointB.Z,
                e.thicc, e.color));
        }
    }

    internal static float GetFillStepRect(float original)
    {
        if (P.Config.AltRectStepOverride || original < P.Config.AltRectStep)
        {
            return P.Config.AltRectStep;
        }
        return original;
    }

    internal static float GetFillStepDonut(float original)
    {
        if (P.Config.AltDonutStepOverride || original < P.Config.AltDonutStep)
        {
            return P.Config.AltDonutStep;
        }
        return original;
    }

    internal static int GetFillStepCone(float original)
    {
        if (P.Config.AltConeStepOverride || original < P.Config.AltConeStep)
        {
            return P.Config.AltConeStep;
        }
        return (int)Math.Max(1f, original);
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
        if(i.UseDistanceLimit && i.DistanceLimitType == 0)
        {
            if (Svc.Targets.Target != null)
            {
                var dist = Vector3.Distance(Svc.Targets.Target.GetPositionXZY(), GetPlayerPositionXZY()) - (i.DistanceLimitTargetHitbox ? Svc.Targets.Target.HitboxRadius : 0) - (i.DistanceLimitMyHitbox ? Svc.ClientState.LocalPlayer.HitboxRadius : 0);
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
        var dist = Vector3.Distance(v, GetPlayerPositionXZY());
        if (!(dist >= i.MinDistance && dist < i.MaxDistance)) return false;
        return true;
    }

    internal bool CheckDistanceToLineCondition(Layout i, Element e)
    {
        if (i.DistanceLimitType != 1) return true;
        var dist = Vector3.Distance(FindClosestPointOnLine(GetPlayerPositionXZY(), new Vector3(e.refX, e.refY, e.refZ), new Vector3(e.offX, e.offY, e.offZ)), GetPlayerPositionXZY());
        if (!(dist >= i.MinDistance && dist < i.MaxDistance)) return false;
        return true;
    }

    internal bool ShouldDraw(float x1, float x2, float y1, float y2)
    {
        return ((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2)) < Config.maxdistance * Config.maxdistance;
    }

    internal void Log(string s, bool tochat = false)
    {
        if (tochat)
        {
            Svc.Chat.Print("[Splatoon] " + s);
        }
        InternalLog.Information(s);
    }
}
