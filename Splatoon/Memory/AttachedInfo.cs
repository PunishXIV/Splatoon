﻿using Dalamud.Hooking;
using ECommons.DalamudServices.Legacy;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;
using Reloaded.Hooks.Definitions.X64;
using Splatoon.Modules;
using Splatoon.SplatoonScripting;
using Splatoon.Structures;

namespace Splatoon.Memory;


#nullable enable
public unsafe static class AttachedInfo
{
    delegate nint GameObject_ctor(nint obj);
    static Hook<GameObject_ctor>? GameObject_ctor_hook = null;
    public static Dictionary<nint, CachedCastInfo> CastInfos = new();
    public static Dictionary<nint, List<CachedObjectEffectInfo>> ObjectEffectInfos = new();
    public static Dictionary<nint, Dictionary<string, VFXInfo>> VFXInfos = new();
    public static Dictionary<nint, List<CachedTetherInfo>> TetherInfos = new();
    static HashSet<nint> Casters = new();

    [Function(Reloaded.Hooks.Definitions.X64.CallingConventions.Microsoft)]
    delegate nint ActorVfxCreateDelegate2(char* a1, nint a2, nint a3, float a4, char a5, ushort a6, char a7);
    static Hook<ActorVfxCreateDelegate2>? ActorVfxCreateHook;

    internal static void Init()
    {
        Safe(delegate
        {
            GameObject_ctor_hook = Svc.Hook.HookFromAddress<GameObject_ctor>(Svc.SigScanner.ScanText("48 8D 05 ?? ?? ?? ?? C7 81 ?? ?? ?? ?? ?? ?? ?? ?? 48 89 01 48 8B C1 C3"), GameObject_ctor_detour);
            GameObject_ctor_hook.Enable();
        });
        Safe(delegate
        {
            var actorVfxCreateAddress = Svc.SigScanner.ScanText("40 53 55 56 57 48 81 EC ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 AC 24 ?? ?? ?? ?? 0F 28 F3 49 8B F8");
            ActorVfxCreateHook = Svc.Hook.HookFromAddress<ActorVfxCreateDelegate2>(actorVfxCreateAddress, ActorVfxNewHandler);
            ActorVfxCreateHook.Enable();
        });
        Svc.Framework.Update += Tick;
    }


    internal static void Dispose()
    {
        Svc.Framework.Update -= Tick;
        if (GameObject_ctor_hook != null)
        {
            GameObject_ctor_hook.Disable();
            GameObject_ctor_hook.Dispose();
            GameObject_ctor_hook = null;
        }
        ActorVfxCreateHook?.Disable();
        ActorVfxCreateHook?.Dispose();
        CastInfos = null!;
        VFXInfos = null!;
        ObjectEffectInfos = null!;
    }

    static nint ActorVfxNewHandler(char* a1, nint a2, nint a3, float a4, char a5, ushort a6, char a7)
    {
        try
        {
            var vfxPath = Dalamud.Memory.MemoryHelper.ReadString(new nint(a1), Encoding.ASCII, 256);
            if (!VFXInfos.ContainsKey(a2))
            {
                VFXInfos[a2] = new();
            }
            VFXInfos[a2][vfxPath] = new()
            {
                SpawnTime = Environment.TickCount64
            };
            var obj = Svc.Objects.CreateObjectReference(a2)!;
            ScriptingProcessor.OnVFXSpawn(obj.EntityId, vfxPath);
            if (!Utils.BlacklistedVFX.Contains(vfxPath))
            {
                if (obj is ICharacter c)
                {
                    var text = $"VFX {vfxPath} spawned on {(obj.Address == Svc.ClientState.LocalPlayer?.Address ? "me" : obj.Name.ToString())} npc id={obj.Struct()->GetNameId()}, model id={c.Struct()->ModelContainer.ModelCharaId}, name npc id={c.NameId}, position={obj.Position.ToString()}";
                    P.ChatMessageQueue.Enqueue(text);
                    if (P.Config.Logging) Logger.Log(text);
                    if(obj is IBattleNpc) P.LogWindow.Log(text);
                }
                else
                {
                    var text = $"VFX {vfxPath} spawned on {obj.Name.ToString()} npc id={obj.Struct()->GetNameId()}, position={obj.Position.ToString()}";
                    P.ChatMessageQueue.Enqueue(text);
                    if (P.Config.Logging) Logger.Log(text);
                    if(obj is IBattleNpc) P.LogWindow.Log(text);
                }
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        return ActorVfxCreateHook!.Original(a1, a2, a3, a4, a5, a6, a7);
    }

    public static bool TryGetVfx(this IGameObject go, out Dictionary<string, VFXInfo>? fx)
    {
        if (VFXInfos.ContainsKey(go.Address))
        {
            fx = VFXInfos[go.Address];
            return true;
        }
        fx = default;
        return false;
    }

    public static List<CachedTetherInfo> GetOrCreateTetherInfo(nint ptr)
    {
        if(TetherInfos.TryGetValue(ptr, out var list))
        {
            return list;
        }
        TetherInfos[ptr] = [];
        return TetherInfos[ptr];
    }

    public static List<CachedTetherInfo> GetOrCreateTetherInfo(Character* ptr) => GetOrCreateTetherInfo((nint)ptr);

    public static bool TryGetSpecificVfxInfo(this IGameObject go, string path, out VFXInfo info)
    {
        if (TryGetVfx(go, out var dict) && dict?.ContainsKey(path) == true)
        {
            info = dict[path];
            return true;
        }
        info = default;
        return false;
    }

    static nint GameObject_ctor_detour(nint ptr)
    {
        CastInfos.Remove(ptr);
        Casters.Remove(ptr);
        VFXInfos.Remove(ptr);
        ObjectEffectInfos.Remove(ptr);
        TetherInfos.Remove(ptr);
        return GameObject_ctor_hook!.Original(ptr);
    }
    static void Tick(object _)
    {
        foreach(var x in Svc.Objects)
        {
            if(x is IBattleChara b) {
                bool isCasting;
                try 
                {
                    isCasting = b.Struct()->GetCastInfo() != null && b.IsCasting;
                }
                catch {
                    // Ignore invalid BattleChara objects that exist during cutscenes
                    continue;
                }

                if (isCasting)
                {
                    if (!Casters.Contains(b.Address)) 
                    {
                        CastInfos[b.Address] = new(b.CastActionId, Environment.TickCount64 - (long)(b.CurrentCastTime * 1000));
                        Casters.Add(b.Address);
                        string text;
                        if (P.Config.LogPosition)
                        {
                            text = $"{b.Name} ({x.Position.ToString()}) starts casting {b.CastActionId} ({b.NameId}>{b.CastActionId})";
                        }
                        else
                        {
                            text = $"{b.Name} starts casting {b.CastActionId} ({b.NameId}>{b.CastActionId})";
                        }
                        ScriptingProcessor.OnStartingCast(b.EntityId, b.CastActionId);
                        P.ChatMessageQueue.Enqueue(text);
                        if (P.Config.Logging)
                        {
                            Logger.Log(text);
                            if(b is IBattleNpc) P.LogWindow.Log(text);
                        }
                    }
                }
                else
                {
                    if (Casters.Contains(b.Address))
                    {
                        Casters.Remove(b.Address);
                    }
                }
            }
        }
    }

    public static bool TryGetCastTime(nint ptr, IEnumerable<uint> castId, out float castTime)
    {
        if(CastInfos.TryGetValue(ptr, out var info))
        {
            if(castId.Contains(info.ID))
            {
                castTime = (float)(Environment.TickCount64 - info.StartTime) / 1000f;
                return true;
            }
        }
        castTime = default;
        return false;
    }
}
