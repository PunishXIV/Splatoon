using Dalamud.Interface;
using ECommons.ExcelServices;
using ECommons.ImGuiMethods;
using ECommons;
using Newtonsoft.Json;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Text;
using ECommons.Logging;
using System.Numerics;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace SplatoonScriptsOfficial.Generic;
public class Makeplace2List : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [uint.MaxValue];
    public override Metadata? Metadata { get; } = new(2, "NightmareXIV");

    public override void OnSettingsDraw()
    {
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Paste, "Paste json and parse list"))
        {
            try
            {
                var list = JsonConvert.DeserializeObject<MakePlaceFile>(GenericHelpers.Paste());
                Dictionary<int, int> items = [];
                Dictionary<string, int> dyeItems = [];
                foreach(var x in list.interiorFurniture)
                {
                    if(!items.ContainsKey(x.itemId)) items[x.itemId] = 0;
                    items[x.itemId]++;
                    if(x.properties?.color != null)
                    {
                        var dye = FindDye(x.properties.color);
                        if(dye == null)
                        {
                            PluginLog.Warning($"Could not find dye {x.properties.color}");
                        }
                        else
                        {
                            if(!dyeItems.ContainsKey(dye)) dyeItems[dye] = 0;
                            dyeItems[dye]++;
                        }
                    }
                }
                var tradeable = new StringBuilder();
                var untradeable = new StringBuilder();
                var vendor = new StringBuilder();
                var dyesStr = new StringBuilder();
                foreach(var x in items)
                {
                    var itemNullable = ExcelItemHelper.Get(x.Key);
                    if(itemNullable != null)
                    {
                        var item = itemNullable.Value;
                        (item.IsUntradable ? untradeable : (item.Lot && item.PriceLow > 0 ? vendor : tradeable)).AppendLine($"{item.Name} - x{x.Value}");
                    }
                }
                foreach(var x in dyeItems)
                {
                    dyesStr.AppendLine($"{x.Key} - x{x.Value}");
                }
                GenericHelpers.Copy($"Tradeables:\n{tradeable}\n\nVendor:\n{vendor}\n\nUntradeables:\n{untradeable}\n\nDyes:\n{dyesStr}");
            }
            catch(Exception ex)
            {
                ex.Log();
            }
        }
    }

    string FindDye(string k)
    {
        foreach(var x in Svc.Data.GetExcelSheet<Stain>())
        {
            var color = StainToVector4(x.Color);
            var cr = (int)(color.X * 255);
            var cg = (int)(color.Y * 255);
            var cb = (int)(color.Z * 255);
            var ca = (int)(color.W * 255);
            if(k == $"{cr:X2}{cg:X2}{cb:X2}{ca:X2}") return x.Name.ToString();
        }
        return null;
    }

    Vector4 StainToVector4(uint stainColor)
    {
        var s = 1.0f / 255.0f;

        return new Vector4()
        {
            X = ((stainColor >> 16) & 0xFF) * s,
            Y = ((stainColor >> 8) & 0xFF) * s,
            Z = ((stainColor >> 0) & 0xFF) * s,
            W = ((stainColor >> 24) & 0xFF) * s
        };
    }

    public class MakePlaceFile
    {
        public List<InteriorFurniture> interiorFurniture;
    }

    public class InteriorFurniture
    {
        public int itemId;
        public Properties properties;
    }

    public class Properties
    {
        public string color;
    }
}
