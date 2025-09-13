using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Stormblood
{
    public class UCOB_dragon_baits : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => [733];

        public override Metadata? Metadata => new(4, "NightmareXIV, damolitionn");
        private const string TargetVFX = "vfx/lockon/eff/bahamut_wyvn_glider_target_02tm.avfx";
        private int diveCnt = 0;
        private uint[] baiters = new uint[3];
        private List<TickScheduler> Schedulers = [];

        public override void OnSetup()
        {
            for(var i = 0; i < 5; i++)
            {
                Controller.RegisterElementFromCode($"Tether{i}", "{\"Name\":\"\",\"type\":2,\"radius\":5.0,\"color\":1677721855}");
            }
        }

        public override void OnEnable()
        {
            ActionEffect.ActionEffectEvent += ActionEffect_ActionEffectEvent;
        }
        public override void OnDisable()
        {
            ActionEffect.ActionEffectEvent -= ActionEffect_ActionEffectEvent;
        }

        private void ActionEffect_ActionEffectEvent(ActionEffectSet set)
        {

        }

        public override void OnUpdate()
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            if(baiters.Any(x => x != 0))
            {
                var dragons = GetOrderedDragons();
                for(var i = 0; i < baiters.Length; i++)
                {
                    if(baiters[i] != 0)
                    {
                        GetElementsForNumber(i).Each(x => x.element.Enabled = true);
                        if(baiters[i].TryGetObject(out var pc) && dragons.Length == 5)
                        {
                            foreach(var x in GetElementsForNumber(i))
                            {
                                x.element.SetRefPosition(pc.Position);
                                x.element.SetOffPosition(dragons[x.num].Position);
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<int> GetOrderForNumber(int num)
        {
            if(num == 0)
            {
                yield return 0;
                yield return 1;
            }
            if(num == 1)
            {
                yield return 2;
            }
            if(num == 2)
            {
                yield return 3;
                yield return 4;
            }
        }

        private IEnumerable<(Element element, int num)> GetElementsForNumber(int num)
        {
            foreach(var x in GetOrderForNumber(num))
            {
                yield return (Controller.GetElementByName($"Tether{x}"), x);
            }
        }

        public override void OnVFXSpawn(uint target, string vfxPath)
        {
            if(vfxPath == TargetVFX)
            {
                if(diveCnt < 3)
                {
                    var c = diveCnt;
                    baiters[c] = target;
                    Schedulers.Add(new(() => baiters[c] = uint.MaxValue, 5000));
                    Schedulers.Add(new(() => baiters[c] = 0, 12000));
                    diveCnt++;
                    if(target.TryGetObject(out var o) && o is IPlayerCharacter pc)
                    {
                        if(diveCnt == 1)
                        {
                            //DuoLog.Information($"{pc.Name} baits first");
                        }
                        else if(diveCnt == 2)
                        {
                            //DuoLog.Information($"{pc.Name} baits second");
                        }
                        else if(diveCnt == 3)
                        {
                            //DuoLog.Information($"{pc.Name} baits third");
                        }
                    }
                }
            }
        }

        private void Reset()
        {
            baiters = new uint[3];
            diveCnt = 0;
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            Schedulers.Each(x => x.Dispose());
            Schedulers.Clear();
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
            {
                Reset();
            }
        }

        private static readonly Vector2 TrueNorth = new(-0.015319824f, -24.002502f);
        private IBattleChara[] GetOrderedDragons()
        {
            return GetDragons()
                .OrderBy(d =>
                {
                    var target = new Vector2(d.Position.X, d.Position.Z);
                    float angle = MathHelper.GetRelativeAngle(TrueNorth, target);
                    return angle;
                })
                .ToArray();
        }

        private IEnumerable<IBattleChara> GetDragons()
        {
            foreach(var x in Svc.Objects)
            {
                if(x is IBattleChara c && c.NameId.EqualsAny<uint>(2631, 6958, 2632, 2630, 6957) && c.IsCharacterVisible())
                {
                    yield return c;
                }
            }
        }

        public override void OnSettingsDraw()
        {
            if(ImGui.CollapsingHeader("debug"))
            {
                ImGuiEx.Text($"Dragons: ");
                foreach(var x in GetOrderedDragons())
                {
                    ImGuiEx.Text($"{x.Name}");
                }
            }
        }
    }
}
