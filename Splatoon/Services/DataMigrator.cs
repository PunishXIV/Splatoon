using ECommons.ExcelServices;
using ECommons.MathHelpers;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Services;
public unsafe class DataMigrator
{
    private DataMigrator()
    {
        try
        {
            foreach(var x in P.Config.LayoutsL)
            {
                PluginLog.Warning($"Checking migrations for {x.Name}");
                x.Migrate();
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    internal static void MigrateJobs(Layout x)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        if(x.JobLock != 0)
        {
            PluginLog.Warning($"Migrating jobs for layout {x.Name}");
            foreach(var k in Svc.Data.GetExcelSheet<ClassJob>().Where(x => x.RowId > 0))
            {
                if(Bitmask.IsBitSet(x.JobLock, (int)k.RowId))
                {
                    x.JobLockH.Add((Job)k.RowId);
                }
            }
            x.JobLock = 0;
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
