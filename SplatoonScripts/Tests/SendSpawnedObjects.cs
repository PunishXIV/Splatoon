using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Schedulers;
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
                        chr == null?"":$"{chr.Struct()->ModelCharaId}",
                        chr == null?"":$"{chr.GetTransformationID()}",
                        $"{obj.Position.X}",
                        $"{obj.Position.Y}",
                        $"{obj.Position.Z}",
                        $"{obj.Rotation}"
                    }.Join("|");
                    PluginLog.Verbose($"Sending {str}");
                    Client?.SendAsync(new HttpRequestMessage()
                    {
                        Content = new StringContent(str),
                        RequestUri = new("http://127.0.0.1:8080/")
                    });
                }
            });
        }
    }
}
