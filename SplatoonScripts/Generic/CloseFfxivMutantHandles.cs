using ECommons;
using ECommons.DalamudServices;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using TerraFX.Interop.Windows;

namespace SplatoonScriptsOfficial.Generic;

public unsafe class CloseFfxivMutantHandles : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;

    public override void OnEnable()
    {
        new Thread(() =>
        {
            try
            {
                CloseFfxivMutants();
            }
            catch(Exception e)
            {
                e.Log();
            }
        }).Start();
    }

    private const int SystemExtendedHandleInformation = 64;
    private const int ObjectNameInformation = 1;
    private const int ObjectTypeInformation = 2;

    private const uint PROCESS_DUP_HANDLE = 0x40;

    private const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
    private const uint DUPLICATE_SAME_ACCESS = 0x00000002;

    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformation(
        int systemInformationClass,
        IntPtr systemInformation,
        int systemInformationLength,
        ref int returnLength);

    [DllImport("ntdll.dll")]
    private static extern int NtQueryObject(
        IntPtr handle,
        int objectInformationClass,
        IntPtr objectInformation,
        int objectInformationLength,
        ref int returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DuplicateHandle(
        IntPtr sourceProcessHandle,
        IntPtr sourceHandle,
        IntPtr targetProcessHandle,
        out IntPtr targetHandle,
        uint desiredAccess,
        bool inheritHandle,
        uint options);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);

    public void CloseFfxivMutants()
    {
        ushort mutantTypeIndex = ResolveMutantTypeIndex();
        if(mutantTypeIndex == 0)
        {
            PluginLog.Error($"[{InternalData.Name}] Failed to resolve Mutant type index");
            return;
        }

        IntPtr buffer = IntPtr.Zero;
        int length = 0x10000;

        var pidCache = new Dictionary<int, bool>();

        try
        {
            buffer = Marshal.AllocHGlobal(length);

            while(NtQuerySystemInformation(SystemExtendedHandleInformation, buffer, length, ref length) != 0)
            {
                Marshal.FreeHGlobal(buffer);
                buffer = Marshal.AllocHGlobal(length);
            }

            long handleCount = Marshal.ReadIntPtr(buffer).ToInt64();
            IntPtr handlePtr = buffer + IntPtr.Size * 2;

            int size = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();

            for(long i = 0; i < handleCount; i++)
            {
                var entry = Marshal.PtrToStructure<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(handlePtr);
                handlePtr += size;

                if(entry.ObjectTypeIndex != mutantTypeIndex)
                {
                    continue;
                }

                int pid = (int)entry.UniqueProcessId;

                if(!IsFfxivProcess(pid, pidCache))
                {
                    continue;
                }

                IntPtr processHandle = OpenProcess(PROCESS_DUP_HANDLE, false, pid);
                if(processHandle == IntPtr.Zero)
                {
                    PluginLog.Error($"[{InternalData.Name}] OpenProcess failed (PID={pid})");
                    continue;
                }

                try
                {
                    if(!DuplicateHandle(
                        processHandle,
                        entry.HandleValue,
                        Process.GetCurrentProcess().Handle,
                        out IntPtr dupHandle,
                        0,
                        false,
                        DUPLICATE_SAME_ACCESS))
                    {
                        PluginLog.Error($"[{InternalData.Name}] DuplicateHandle failed (PID={pid}, Handle=0x{entry.HandleValue.ToInt64():X})");
                        continue;
                    }

                    try
                    {
                        string name = GetObjectName(dupHandle);
                        if(string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        if(name.EndsWith("_ffxiv_game00", StringComparison.OrdinalIgnoreCase) ||
                            name.EndsWith("_ffxiv_game01", StringComparison.OrdinalIgnoreCase))
                        {
                            if(!DuplicateHandle(
                                processHandle,
                                entry.HandleValue,
                                IntPtr.Zero,
                                out _,
                                0,
                                false,
                                DUPLICATE_CLOSE_SOURCE))
                            {
                                PluginLog.Error($"[{InternalData.Name}] CLOSE FAILED (PID={pid}, Handle=0x{entry.HandleValue.ToInt64():X}, Name={name})");
                            }
                            else
                            {
                                PluginLog.Information($"[{InternalData.Name}] CLOSED (PID={pid}, Handle=0x{entry.HandleValue.ToInt64():X}, Name={name})");
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        PluginLog.Error($"[{InternalData.Name}] Exception (PID={pid}, Handle=0x{entry.HandleValue.ToInt64():X}): {ex.Message}");
                    }
                    finally
                    {
                        CloseHandle(dupHandle);
                    }
                }
                finally
                {
                    CloseHandle(processHandle);
                }
            }
        }
        catch(Exception ex)
        {
            PluginLog.Error($"[{InternalData.Name}] Fatal error: {ex}");
        }
        finally
        {
            if(buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private static ushort ResolveMutantTypeIndex()
    {
        int currentPid = Process.GetCurrentProcess().Id;

        IntPtr buffer = IntPtr.Zero;
        int length = 0x10000;

        try
        {
            buffer = Marshal.AllocHGlobal(length);

            while(NtQuerySystemInformation(SystemExtendedHandleInformation, buffer, length, ref length) != 0)
            {
                Marshal.FreeHGlobal(buffer);
                buffer = Marshal.AllocHGlobal(length);
            }

            long handleCount = Marshal.ReadIntPtr(buffer).ToInt64();
            IntPtr handlePtr = buffer + IntPtr.Size * 2;

            int size = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();

            for(long i = 0; i < handleCount; i++)
            {
                var entry = Marshal.PtrToStructure<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(handlePtr);
                handlePtr += size;

                if((int)entry.UniqueProcessId != currentPid)
                {
                    continue;
                }

                IntPtr currentProcess = Process.GetCurrentProcess().Handle;

                if(!DuplicateHandle(
                    currentProcess,
                    entry.HandleValue,
                    currentProcess,
                    out IntPtr dupHandle,
                    0,
                    false,
                    DUPLICATE_SAME_ACCESS))
                {
                    continue;
                }

                try
                {
                    string type = QueryObjectString(dupHandle, ObjectTypeInformation);
                    if(type == "Mutant")
                    {
                        return entry.ObjectTypeIndex;
                    }
                }
                finally
                {
                    CloseHandle(dupHandle);
                }
            }
        }
        catch
        {
        }
        finally
        {
            if(buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        return 0;
    }

    private static bool IsFfxivProcess(int pid, Dictionary<int, bool> cache)
    {
        if(cache.TryGetValue(pid, out bool result))
        {
            return result;
        }

        try
        {
            var process = Process.GetProcessById(pid);
            result = process.ProcessName.Equals("ffxiv_dx11", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            result = false;
        }

        cache[pid] = result;
        return result;
    }

    private static string GetObjectName(IntPtr handle)
    {
        return QueryObjectString(handle, ObjectNameInformation);
    }

    private static string QueryObjectString(IntPtr handle, int infoClass)
    {
        int length = 0;
        NtQueryObject(handle, infoClass, IntPtr.Zero, 0, ref length);

        if(length == 0)
        {
            return null;
        }

        IntPtr ptr = Marshal.AllocHGlobal(length);

        try
        {
            if(NtQueryObject(handle, infoClass, ptr, length, ref length) != 0)
            {
                return null;
            }

            UNICODE_STRING unicode = Marshal.PtrToStructure<UNICODE_STRING>(ptr);

            if(unicode.Length == 0 || unicode.Buffer == IntPtr.Zero)
            {
                return null;
            }

            return Marshal.PtrToStringUni(unicode.Buffer, unicode.Length / 2);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
    {
        public IntPtr Object;
        public IntPtr UniqueProcessId;
        public IntPtr HandleValue;
        public uint GrantedAccess;
        public ushort CreatorBackTraceIndex;
        public ushort ObjectTypeIndex;
        public uint HandleAttributes;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }
}
