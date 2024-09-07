using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.SimpleGui;
using ImGuiNET;
using NightmareUI.PrimaryUI;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public class R4S_Electrope_Edge : SplatoonScript
{
    public enum SidewiseSparkPosition : byte
    {
        None = 0,
        North = 1,
        Inside = 2,
        South = 3,
        Side = 4
    }

    private readonly uint LeftSidewiseSparkCastActionId = 38381;
    private readonly uint RightSidewiseSparkCastActionId = 38380;
    private bool IsPairSidewiseSpark = false;


    public override HashSet<uint>? ValidTerritories { get; } = [1232];
    public override Metadata? Metadata => new(9, "NightmareXIV");
    List<uint> Hits = [];
    List<uint> Longs = [];
    uint Debuff = 3999;

    IBattleNpc? WickedThunder => Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.NameId == 13057 && x.IsTargetable);
    IBattleNpc? RelativeTile => Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.DataId == 9020 && x.CastActionId == 38351 && x.IsCasting && x.BaseCastTime - x.CurrentCastTime > 0 && Vector3.Distance(new(100,0,100), x.Position).InRange(15.5f, 17f));

    public override void OnSetup()
    {
        for(int i = 0; i < 8; i++)
        {
            Controller.RegisterElementFromCode($"Count{i}", "{\"Name\":\"\",\"type\":1,\"Enabled\":false,\"radius\":0.0,\"Filled\":false,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"overlayBGColor\":3640655872,\"overlayTextColor\":4294967295,\"overlayVOffset\":2.0,\"overlayFScale\":1.5,\"overlayPlaceholders\":true,\"thicc\":0.0,\"overlayText\":\"Long\\\\n   3\",\"refActorComparisonType\":2,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
        }
        Controller.RegisterElementFromCode("Explode", "{\"Name\":\"\",\"refX\":84.21978,\"refY\":100.50559,\"refZ\":0.0010004044,\"radius\":2.0,\"color\":3356425984,\"Filled\":false,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":5.0,\"overlayText\":\"Explode here\",\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
        Controller.RegisterElementFromCode("Safe", "{\"Name\":\"\",\"refX\":84.07532,\"refY\":99.538475,\"refZ\":0.0010004044,\"radius\":2.0,\"color\":3372218624,\"Filled\":false,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":5.0,\"overlayText\":\"Safe\",\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
    }
    public override void OnScriptUpdated(uint previousVersion)
{
    if(previousVersion < 6)
    {
        new PopupWindow(() =>
        {
            ImGuiEx.Text($"""
                Warning: Splatoon Script 
                {this.InternalData.Name}
                was updated. 
                If you were using Sidewise Spark related functions,
                you must reconfigure the script.
                """);
        });
    }
}

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        var longs = Svc.Objects.OfType<IPlayerCharacter>().Where(x => x.StatusList.Any(s => s.StatusId == Debuff && s.RemainingTime > 40)).ToList();
        if(longs.Count == 4)
        {
            Longs = longs.Select(x => x.EntityId).ToList();
        }
        if(Svc.Objects.OfType<IPlayerCharacter>().Count(x => x.StatusList.Any(s => s.StatusId == Debuff)) < 4)
        {
            Reset();
        }
        // Hits must be left behind to define a safe for the Side Spark.
        // Hits.RemoveAll(x => !(x.GetObject() is IPlayerCharacter pc && pc.StatusList.Any(z => z.StatusId == Debuff)));
        int i = 0;
        foreach(var x in Svc.Objects.OfType<IPlayerCharacter>())
        {
            bool tooFew = false;
            var num = Hits.Count(s => s == x.EntityId);
            string tooFewString = "";
            if(num > 0 && Controller.TryGetElementByName($"Count{i}", out var e))
            {
                e.Enabled = true;
                var l = Longs.Contains(x.EntityId);
                if(l)
                {
                    if(num == 1) tooFew = true;
                }
                else
                {
                    if(num == 2) tooFew = true;
                }
                if(tooFew)
                {
                    tooFewString = Controller.GetConfig<Config>().stringFew;
                }
                else
                {
                    tooFewString = Controller.GetConfig<Config>().stringMuch;
                }
                e.overlayText = l ? "Long" : "Short";
                e.overlayText += $"\n   {num + (Controller.GetConfig<Config>().AddOne && l?1:0)} {(Controller.GetConfig<Config>().showMuchFew? tooFewString:"")}";
                e.overlayTextColor = (x.StatusList.FirstOrDefault(x => x.StatusId == Debuff)?.RemainingTime < 16f?EColor.RedBright:EColor.White).ToUint();
                e.overlayFScale = x.Address == Player.Object.Address ? 2f : 1f;
                e.refActorObjectID = x.EntityId;
            }
            i++;
        }

        var tile = RelativeTile;
        if(tile != null && C.ResolveBox)
        {
            var rotation = 0;
            if(Vector3.Distance(new(84, 0, 100), tile.Position) < 2f) rotation = 90;
            if(Vector3.Distance(new(100, 0, 84), tile.Position) < 2f) rotation = 180;
            if(Vector3.Distance(new(116, 0, 100), tile.Position) < 2f) rotation = 270;
            var doingMechanic = Player.Object.StatusList.FirstOrDefault(x => x.StatusId == Debuff)?.RemainingTime < 15f;
            var num = Hits.Count(s => s == Player.Object.EntityId)+ (Longs.Contains(Player.Object.EntityId) ? 1 : 0);
            var pos = num == 2 ? C.Position2 : C.Position3;
            var posmod = new Vector2((pos.Item2 - 2) * 8, pos.Item1 * 8);
            if(!doingMechanic)
            {
                posmod = new Vector2(0, 0);
            }
            var basePos = new Vector2(100, 84);
            var posRaw = posmod + basePos;
            var newPoint = Utils.RotatePoint(100,100, rotation.DegreesToRadians(), new(posRaw.X, posRaw.Y, 0));
            //PluginLog.Information($"Modifier: {posmod}, num: {num}, raw: {posRaw}, new: {newPoint}, rotation: {rotation}, tile: {tile.Position}");
            if(Controller.TryGetElementByName(doingMechanic?"Explode":"Safe", out var e))
            {
                e.Enabled = true;
                e.radius = 2f;
                e.refX = newPoint.X;
                e.refY = newPoint.Y;
            }
        }

        var wickedThunder = WickedThunder;
        if (C.ResolveBox && wickedThunder is { IsCasting: true } && IsPairSidewiseSpark &&
            wickedThunder.CastActionId.EqualsAny(LeftSidewiseSparkCastActionId, RightSidewiseSparkCastActionId))
        {
            var isSafeRight = wickedThunder.CastActionId == LeftSidewiseSparkCastActionId;
            var isLong = Longs.Contains(Player.Object.EntityId);
            var hitCount = Hits.Count(s => s == Player.Object.EntityId);
            var safeArea = (isLong, hitCount) switch
            {
                (false, 2) => GetSidewiseSparkSafeArea(C.SidewiseSpark2Short, isSafeRight),
                (false, 3) => GetSidewiseSparkSafeArea(C.SidewiseSpark3Short, isSafeRight),
                (true, 1) => GetSidewiseSparkSafeArea(C.SidewiseSpark1Long, isSafeRight),
                (true, 2) => GetSidewiseSparkSafeArea(C.SidewiseSpark2Long, isSafeRight),
                _ => null
            };

            if (Controller.TryGetElementByName("Safe", out var e))
            {
                if (safeArea == null) return;
                e.Enabled = true;
                e.radius = 1.5f;
                e.refX = safeArea.Value.X;
                e.refY = safeArea.Value.Y;
            }
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(vfxPath == "vfx/common/eff/m0888_stlp01_c0t1.avfx" && Hits.Count != 0)
        {
            IsPairSidewiseSpark = true;
        }
    }

    Config C => Controller.GetConfig<Config>();
    (int, int)[] Unsafe = [(0,0), (0,1), (0,3), (0,4),
                           (1,2),
                           (2,2),
                           (3,0),(3,2),(3,4),
                           (4,1),(4,2),(4,3),
    ];

    Dictionary<SidewiseSparkPosition, string> AltRemaps = new()
    {
        [SidewiseSparkPosition.North] = "1 (North all the way)",
        [SidewiseSparkPosition.South] = "2 (North and side)",
        [SidewiseSparkPosition.Side] = "4 (South all the way)",
        [SidewiseSparkPosition.Inside] = "3 (South and side)",
    };
    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(150f);
        ImGui.Checkbox("Add 1 to long debuff bearers", ref C.AddOne);
        ImGuiEx.HelpMarker("If you have a long debuff, this will visually add 1 to its count. It does not affect the actual function of the script");
        ImGui.Checkbox("Show much/few", ref C.showMuchFew);
        ImGuiEx.HelpMarker("If selected, in addition to short and long, it will display \"much\" and \"few\". This indicator is used primarily in Japan, so don't set this if you don't recognize it");
        if(C.showMuchFew)
        {
            ImGui.Indent();
            ImGui.TextWrapped("Change the displayed text from \"much\" and \"few\":");
            ImGui.SetNextItemWidth(150f);
            ImGui.InputText("Much", ref C.stringMuch, 100);
            ImGui.SetNextItemWidth(150f);
            ImGui.InputText("Few", ref C.stringFew, 100);
            ImGui.Unindent();
        }

        ImGui.Checkbox("Resolve safe spots", ref C.ResolveBox);
        ImGuiEx.HelpMarker("If selected, these safe spots will be highlighted when it's time for you to explode");

        if(C.ResolveBox)
        {
            new NuiBuilder().
                Section("Lightning Cage - 2 short / 1 long:")
                .Widget(() =>
                {
                    ImGui.PushID("Pos2");
                    DrawBox(ref C.Position2);
                    ImGui.PopID();
                })
                .Section("Lightning Cage - 3 short / 2 long:")
                .Widget(() =>
                {
                    ImGui.PushID("Pos3");
                    DrawBox(ref C.Position3);
                    ImGui.PopID();
                })
                .Section("Sidewise Spark")
                .Widget(() =>
                    {
                        ImGui.Text("Stack Position Shape");
                        ImGuiEx.HelpMarker("By default (when following popular RaidPlans and guides, i.e. Hector or OQ5), the shape of stacks for this mechanic is a cross, or half of a \"+\" sign with a pair of DPS standing underneath the boss's hitbox; some more obscure guides (i.e. Rainesama) use a semicircle, similar to the shape of \"(\"");
                        ImGuiEx.RadioButtonBool("Semicircle", "Cross", ref C.SidewiseSparkAlt);
                        var names = C.SidewiseSparkAlt ? AltRemaps : null;
                        ImGui.Separator();
                        ImGui.Text("Stack Positions");
                        ImGui.Text("2 short");
                        ImGui.SameLine();
                        ImGuiEx.EnumCombo("##SidewiseSpark2Short", ref C.SidewiseSpark2Short, names);
                        ImGui.Text("3 short");
                        ImGui.SameLine();
                        ImGuiEx.EnumCombo("##SidewiseSpark3Short", ref C.SidewiseSpark3Short, names);
                        ImGui.Text("1 long");
                        ImGui.SameLine();
                        ImGuiEx.EnumCombo("##SidewiseSpark1Long", ref C.SidewiseSpark1Long, names);
                        ImGui.Text("2 long");
                        ImGui.SameLine();
                        ImGuiEx.EnumCombo("##SidewiseSpark2Long", ref C.SidewiseSpark2Long, names);
                    }
                )
                .Draw();
        }

        if(ImGui.CollapsingHeader("Debug"))
        {
            foreach(var x in Svc.Objects.OfType<IPlayerCharacter>())
            {
                ImGuiEx.Text($"{x.Name}: {Hits.Count(s => s == x.EntityId)}, isLong = {Longs.Contains(x.EntityId)}");
            }
            if(ImGui.Button("Self long")) Longs.Add(Player.Object.EntityId);
            if(ImGui.Button("Self short")) Longs.RemoveAll(x => x == Player.Object.EntityId);
            if(ImGui.Button("Self: 3 hits"))
            {
                Hits.RemoveAll(x => x == Player.Object.EntityId);
                Hits.Add(Player.Object.EntityId);
                Hits.Add(Player.Object.EntityId);
                Hits.Add(Player.Object.EntityId);
            }
            if(ImGui.Button("Self: 2 hits"))
            {
                Hits.RemoveAll(x => x == Player.Object.EntityId);
                Hits.Add(Player.Object.EntityId);
                Hits.Add(Player.Object.EntityId);
            }
            if(ImGui.Button("Self: 1 hits"))
            {
                Hits.RemoveAll(x => x == Player.Object.EntityId);
                Hits.Add(Player.Object.EntityId);
            }
        }
    }

    void DrawBox(ref (int, int) value)
    {
        for(int i = 0; i < 5; i++)
        {
            for(int k = 0; k < 5; k++)
            {
                var dis = Unsafe.Contains((i, k));
                if(dis)
                {
                    ImGui.BeginDisabled();
                    var n = (bool?)null;
                    ImGuiEx.Checkbox("##null", ref n);
                    ImGui.EndDisabled();
                }
                else
                {
                    var c = value == (i, k);
                    if(ImGui.Checkbox($"##{i}{k}", ref c))
                    {
                        if(c) value = (i, k);
                    }
                }
                ImGui.SameLine();
            }
            ImGui.NewLine();
        }
    }

    void Reset()
    {
        Hits.Clear();
        IsPairSidewiseSpark = false;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action?.RowId == 38790)
        {
            for(int i = 0; i < set.TargetEffects.Length; i++)
            {
                var obj = ((uint)set.TargetEffects[i].TargetID).GetObject();
                if(obj?.ObjectKind == ObjectKind.Player)
                {
                    PluginLog.Information($"Registered hit on {obj}");
                    Hits.Add(obj.EntityId);
                    break;
                }
            }
        }
    }

    public Vector2? GetSidewiseSparkSafeArea(SidewiseSparkPosition pos, bool isSafeRight = false)
    {
        var center = new Vector2(100, 100);
        if(C.SidewiseSparkAlt)
        {
            var mod = isSafeRight ? new Vector2(-1f,1f) : new Vector2(1f);
            if(pos == SidewiseSparkPosition.North) return center + new Vector2(-1, -8) * mod;
            if(pos == SidewiseSparkPosition.Inside) return center + new Vector2(-7, 4) * mod;
            if(pos == SidewiseSparkPosition.South) return center + new Vector2(-7, -4) * mod;
            if(pos == SidewiseSparkPosition.Side) return center + new Vector2(-1, 8) * mod;
            return null;
        }
        else
        {
            var offsetX = isSafeRight ? 1.5f : -1.5f;
            return pos switch
            {
                SidewiseSparkPosition.North => center + new Vector2(offsetX, -10),
                SidewiseSparkPosition.Inside => center + new Vector2(offsetX, 0),
                SidewiseSparkPosition.South => center + new Vector2(offsetX, 10),
                SidewiseSparkPosition.Side => center + new Vector2(offsetX * 7f, 0),
                _ => null
            };
        }
    }

    public class Config : IEzConfig
    {
        public bool AddOne = false;
        public bool showMuchFew = false;
        public string stringMuch = "Much";
        public string stringFew = "Few";
        public bool ResolveBox = false;
        public (int, int) Position2 = (1, 4);
        public (int, int) Position3 = (4, 4);
        public SidewiseSparkPosition SidewiseSpark2Short = SidewiseSparkPosition.None;
        public SidewiseSparkPosition SidewiseSpark1Long = SidewiseSparkPosition.None;
        public SidewiseSparkPosition SidewiseSpark3Short = SidewiseSparkPosition.None;
        public SidewiseSparkPosition SidewiseSpark2Long = SidewiseSparkPosition.None;
        public bool SidewiseSparkAlt = false;
    }
}
