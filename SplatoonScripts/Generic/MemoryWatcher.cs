using ECommons;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using TerraFX.Interop.Windows;

namespace SplatoonScriptsOfficial.Generic;

public unsafe class MemoryWatcher : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;

    public class Holder
    {
        public volatile bool ShouldRun;
    }
    public Holder DataHolder;
    private volatile nint LastBytes;

    public override void OnEnable()
    {
        var holder = new Holder();
        DataHolder = holder;
        holder.ShouldRun = true;
        new Thread(() =>
        {
            try
            {
                while(holder.ShouldRun)
                {
                    try
                    {
                        var bytes = (nint)GetPrivateWorkingSetBytesForCurrentProcess();
                        if(LastBytes == 0)
                        {
                            PluginLog.Warning($"Current memory usage: {Format(bytes)}");
                            LastBytes = bytes;
                        }
                        else
                        {
                            var diff = bytes - LastBytes;
                            if(diff > 1024 * 1024 * 500) //500 mb difference
                            {
                                PluginLog.Warning($"Memory usage increased ({Format(LastBytes)}->{Format(bytes)} +{Format(diff)})");
                                LastBytes = bytes;
                            }
                            else if(diff < -1024 * 1024 * 500)
                            {
                                PluginLog.Warning($"Memory usage decreased ({Format(LastBytes)}->{Format(bytes)} -{Format(-diff)})");
                                LastBytes = bytes;
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        e.Log();
                    }
                    Thread.Sleep(1000);
                }
            }
            catch(Exception e)
            {
                e.Log();
            }
        }).Start();
    }

    public override void OnDisable()
    {
        DataHolder?.ShouldRun = false;
        DataHolder = null;
    }

    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB", "PB" };

    public static string Format(long bytes, int decimals = 2)
    {
        if(bytes < 0)
        {
            return "-" + Format(-bytes, decimals);
        }

        double value = bytes;
        var unitIndex = 0;

        while(value >= 1024 && unitIndex < Units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value.ToString($"F{decimals}")} {Units[unitIndex]}";
    }

    private const int BatchCapacity = 4096;
    private const ulong SharedBitMask = 1UL << 15;
    private const ulong ValidBitMask = 1UL << 0;

    public static long GetPrivateWorkingSetBytesForCurrentProcess() => GetPrivateWorkingSetBytes(TerraFX.Interop.Windows.Windows.GetCurrentProcess());

    public static long GetPrivateWorkingSetBytes(int processId)
    {
        var hProcess = TerraFX.Interop.Windows.Windows.OpenProcess((uint)(PROCESS.PROCESS_QUERY_INFORMATION | PROCESS.PROCESS_VM_READ), TerraFX.Interop.Windows.Windows.FALSE, (uint)processId);

        if(hProcess == HANDLE.NULL)
        {
            throw new InvalidOperationException($"OpenProcess failed: 0x{Marshal.GetLastWin32Error():X}");
        }

        try
        {
            return GetPrivateWorkingSetBytes(hProcess);
        }
        finally
        {
            TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
        }
    }

    private static long GetPrivateWorkingSetBytes(HANDLE hProcess)
    {
        long pageSize = Environment.SystemPageSize;
        long privateBytes = 0;

        var batch = new PSAPI_WORKING_SET_EX_INFORMATION[BatchCapacity];
        var batchCount = 0;

        nuint addr = 0;
        MEMORY_BASIC_INFORMATION mbi;

        while(true)
        {
            var result = TerraFX.Interop.Windows.Windows.VirtualQueryEx(hProcess, (void*)addr, &mbi, (nuint)sizeof(MEMORY_BASIC_INFORMATION));

            if(result == 0)
            {
                break;
            }

            if(mbi.State == MEM.MEM_COMMIT)
            {
                var regionStart = (nuint)mbi.BaseAddress;
                var regionEnd = regionStart + mbi.RegionSize;

                for(var pageAddr = regionStart; pageAddr < regionEnd; pageAddr += (nuint)pageSize)
                {
                    batch[batchCount++].VirtualAddress = (void*)pageAddr;

                    if(batchCount == BatchCapacity)
                    {
                        privateBytes += FlushBatch(hProcess, batch, batchCount, pageSize);
                        batchCount = 0;
                    }
                }
            }

            var next = (nuint)mbi.BaseAddress + mbi.RegionSize;
            if(next <= addr)
            {
                break;
            }

            addr = next;
        }

        if(batchCount > 0)
        {
            privateBytes += FlushBatch(hProcess, batch, batchCount, pageSize);
        }

        return privateBytes;
    }

    private static long FlushBatch(HANDLE hProcess, PSAPI_WORKING_SET_EX_INFORMATION[] batch, int count, long pageSize)
    {
        long privateBytes = 0;

        fixed(PSAPI_WORKING_SET_EX_INFORMATION* pBatch = batch)
        {
            var ok = TerraFX.Interop.Windows.Windows.QueryWorkingSetEx(hProcess, pBatch, (uint)(count * sizeof(PSAPI_WORKING_SET_EX_INFORMATION)));

            if(!ok)
            {
                return 0;
            }
        }

        for(var i = 0; i < count; i++)
        {
            ulong flags = batch[i].VirtualAttributes.Flags;
            var valid = (flags & ValidBitMask) != 0;
            var shared = (flags & SharedBitMask) != 0;

            if(valid && !shared)
            {
                privateBytes += pageSize;
            }
        }

        return privateBytes;
    }
}
