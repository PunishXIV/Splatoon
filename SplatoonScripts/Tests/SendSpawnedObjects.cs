using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Schedulers;
using Lumina.Excel.GeneratedSheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ECommons.DalamudServices.Legacy;

namespace SplatoonScriptsOfficial.Tests
{
    public unsafe class SendSpawnedObjects : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new();
        HttpClient Client;

        public override void OnEnable()
        {
            Client = new()
            {
                Timeout = TimeSpan.FromSeconds(3),
            };
        }

        public override void OnDisable()
        {
            Client?.Dispose();
        }

        public override void OnActionEffect(uint ActionID, ushort animationID, ActionEffectType type, uint sourceID, ulong targetOID, uint damage)
        {
            //effect|id|name|actor_id|actor_name|x|y|z|facing|target_id|target_name|
            var str = new StringBuilder("effect|")
                .Append(ActionID)
                .Append('|')
                .Append(Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>()?.GetRow(ActionID)?.Name ?? "")
                .Append('|')
                .Append(sourceID.GetObject()?.Name.ToString())
                .Append('|')
                .Append(sourceID.GetObject()?.Position.X ?? 0)
                .Append('|')
                .Append(sourceID.GetObject()?.Position.Y ?? 0)
                .Append('|')
                .Append(sourceID.GetObject()?.Position.Z ?? 0)
                .Append('|')
                .Append(sourceID.GetObject()?.Rotation)
                .Append('|')
                .Append(targetOID)
                .Append('|')
                .Append(Svc.Objects.FirstOrDefault(x => (ulong)(x.Struct()->GetObjectID()) == (ulong)targetOID)?.Name ?? "");
            Send(str);
        }

        public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
        {
            //tether|id|target_one_id|target_one_name|target_one_type|target_two_id|target_two_name|target_two_type
            var str = new StringBuilder("tether|")
                .Append(data2)
                .Append('|')
                .Append(source)
                .Append('|')
                .Append(source.GetObject()?.Name ?? "")
                .Append('|')
                .Append(target)
                .Append('|')
                .Append(target.GetObject()?.Name ?? "")
                .Append('|')
                .Append(target.GetObject()?.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player);
            Send(str);
        }

        public override void OnTetherRemoval(uint source, uint data2, uint data3, uint data5)
        {
            this.OnTetherCreate(source, 0xE0000000, data2, data3, data5);
        }

        public override void OnMapEffect(uint position, ushort data1, ushort data2)
        {
            var str = $"mapevent|{position}|{data1}|{data2}";
            Send(str);
        }

        public override void OnVFXSpawn(uint target, string vfxPath)
        {
            if(target.GetObject()?.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
            {
                var str = $"vfx|{target}|{target.GetObject()?.Name ?? ""}|{vfxPath}";
                Send(str);
            }
        }

        public override void OnObjectCreation(nint newObjectPtr)
        {
            new TickScheduler(delegate
            {
                if(Svc.Objects.TryGetFirst(x => x.Address == newObjectPtr, out var obj))
                {
                    var chr = obj is Character character ? character: null;
                    //name|ObjectID|DataID|NPCID|ModelID|TransformID|Position.X|Position.Y|Position.Z|Angle
                    var str = new string[]
                    {
                        $"{obj.Name.ExtractText()}",
                        $"{obj.ObjectId}",
                        $"{obj.DataId}",
                        $"{obj.Struct()->GetNpcID()}",
                        chr == null?"":$"{chr.Struct()->CharacterData.ModelCharaId}",
                        chr == null?"":$"{chr.GetTransformationID()}",
                        $"{obj.Position.X}",
                        $"{obj.Position.Y}",
                        $"{obj.Position.Z}",
                        $"{obj.Rotation}"
                    }.Join("|");
                    Send(str);
                }
            });
        }

        void Send(string str)
        {
            PluginLog.Verbose($"Sending {str}");
            Client?.SendAsync(new HttpRequestMessage()
            {
                Content = new StringContent(str),
                RequestUri = new("http://127.0.0.1:8080/")
            });
        }

        void Send(StringBuilder str) => Send(str.ToString());
    }
}
