using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

/* Credit to TextAdvance from NightmareXIV for doing the initial work. */

namespace SplatoonScriptsOfficial.Generic;
public unsafe class QuestHighlighter : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => new();
    Config C => Controller.GetConfig<Config>();

    public static class Markers
    {
        // Main Story Quests
        public static readonly uint[] MSQ =
        {
            71201, // MSQ Available
            71202, // MSQ Repeatable
            71203, // MSQ In Progress
            71204, // MSQ Target
            71205, // MSQ Complete
        };
        public static readonly uint[] MSQ_Locked =
        {
            71211, // MSQ Locked Available
            71212, // MSQ Locked Repeatable
            71213, // MSQ Locked In Progress
            71214, // MSQ Locked Target
            71215, // MSQ Locked Complete
        };

        // Feature Quests
        public static readonly uint[] FQ =
        {
            71341, // FQ Available
            71342, // FQ Repeatable
            71343, // FQ In Progress
            71344, // FQ Target
            71345, // FQ Complete
        };
        public static readonly uint[] FQ_Locked =
        {
            71351, // FQ Locked Available
            71352, // FQ Locked Repeatable
            71353, // FQ Locked In Progress
            71354, // FQ Locked Target
            71355, // FQ Locked Complete
        };

        // Side Quests
        public static readonly uint[] SQ =
        {
            71221, // SQ Available
            71222, // SQ Repeatable
            71223, // SQ In Progress
            71224, // SQ Target
            71225, // SQ Complete
        };
        public static readonly uint[] SQ_Locked =
        {
            71231, // SQ Locked Available
            71232, // SQ Locked Repeatable
            71233, // SQ Locked In Progress
            71234, // SQ Locked Target
            71235, // SQ Locked Complete
        };

        // Event Objects
        public static readonly string[] EventObjNameWhitelist =
        {
            "Destination", // English
            "指定地点",     // Chinese
            "Zielort",     // German
        };
        public static readonly uint[] EventObjWhitelist =
        {
            2010816, // ???
            2011073, // ???
            2011072, // ???
            2011071, // ???
        };
    }

    public class Config : IEzConfig
    {
        public int MaxDistance2D = 2048;

        public float LineThickness = 1f;

        public bool ShowTargetName = true;
        public bool ShowTether = true;

        public bool ShowMSQ = false;
        public bool ShowMSQL = false;
        public bool ShowFQ = false;
        public bool ShowFQL = false;
        public bool ShowSQ = false;
        public bool ShowSQL = false;
        public bool ShowEO = false;
    }

    public override void OnUpdate()
    {
        if (!(C.ShowMSQ || C.ShowMSQL || C.ShowFQ || C.ShowFQL || C.ShowSQ || C.ShowSQL || C.ShowEO))
        {
            return;
        }

        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

        int i = 0;
        foreach (var x in Svc.Objects)
        {
            bool want = false;

            if (x.IsTargetable)
            {
                if (Vector2.Distance(Player.Object.Position.ToVector2(), x.Position.ToVector2()) <= C.MaxDistance2D)
                {
                    if (x is ICharacter)
                    {
                        var icon = x.Struct()->NamePlateIconId;

                        if (C.ShowMSQ && Markers.MSQ.Contains(icon))
                        {
                            want = true;
                        }
                        else if (C.ShowMSQL && Markers.MSQ_Locked.Contains(icon))
                        {
                            want = true;
                        }
                        else if (C.ShowFQ && Markers.FQ.Contains(icon))
                        {
                            want = true;
                        }
                        else if (C.ShowFQL && Markers.FQ_Locked.Contains(icon))
                        {
                            want = true;
                        }
                        else if (C.ShowSQ && Markers.SQ.Contains(icon))
                        {
                            want = true;
                        }
                        else if (C.ShowSQL && Markers.SQ_Locked.Contains(icon))
                        {
                            want = true;
                        }
                    }
                    else if (x is IGameObject)
                    {
                        if (C.ShowEO && (Markers.EventObjNameWhitelist.ContainsIgnoreCase(x.Name.ToString()) || Markers.EventObjWhitelist.Contains(x.DataId)))
                        {
                            want = true;
                        }
                    }
                }
            }

            if (want)
            {
                var element = GetElement(i++);
                element.refActorDataID = x.DataId;
                element.overlayText = (C.ShowTargetName) ? "$NAME" : "";
                element.tether = C.ShowTether;
                element.thicc = C.LineThickness;
                element.Enabled = true;
            }
        }
    }

    public Element GetElement(int i)
    {
        if (Controller.TryGetElementByName($"Quest{i}", out var element))
        {
            return element;
        }
        else
        {
            var ret = new Element(1)
            {
                refActorType = 0,
                radius = 0,
                refActorComparisonType = 3,
                Filled = false,
                overlayPlaceholders = true,
            };
            Controller.RegisterElement($"Quest{i}", ret);
            return ret;
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.NewLine();

        SImGuiEx.SizedText("Max 2D Distance:", 125f);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(250f);
        ImGui.DragInt("##max2DDistance", ref C.MaxDistance2D, 1, 0, 2048);

        SImGuiEx.SizedText("Line Thickness:", 125f);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(250f);
        ImGui.DragFloat("##lineThickness", ref C.LineThickness, 0.05f, 0f, 8f);

        ImGui.NewLine();

        SImGuiEx.SizedText("Show Target Name:", 125f);
        ImGui.SameLine();
        ImGui.Checkbox("##showTargetName", ref C.ShowTargetName);

        SImGuiEx.SizedText("Show Tether:", 125f);
        ImGui.SameLine();
        ImGui.Checkbox("##showTether", ref C.ShowTether);

        SImGuiEx.SizedText("Show Event Objects:", 125f);
        ImGui.SameLine();
        ImGui.Checkbox("##showEventObjects", ref C.ShowEO);

        ImGui.NewLine();

        // I originally had this as a table layout, but I didn't like how it expanded to 100% of the width

        SImGuiEx.SizedText("Main Story Quest:", 125f);
        ImGui.SameLine();
        ImGui.Checkbox("Available##showMSQAvailable", ref C.ShowMSQ);
        ImGui.SameLine();
        ImGui.Text("     ");
        ImGui.SameLine();
        ImGui.Checkbox("Locked##showMSQLocked", ref C.ShowMSQL);

        SImGuiEx.SizedText("Feature Quest:", 125f);
        ImGui.SameLine();
        ImGui.Checkbox("Available##showFQAvailable", ref C.ShowFQ);
        ImGui.SameLine();
        ImGui.Text("     ");
        ImGui.SameLine();
        ImGui.Checkbox("Locked##showFQLocked", ref C.ShowFQL);

        SImGuiEx.SizedText("Side Quest:", 125f);
        ImGui.SameLine();
        ImGui.Checkbox("Available##showSQAvailable", ref C.ShowSQ);
        ImGui.SameLine();
        ImGui.Text("     ");
        ImGui.SameLine();
        ImGui.Checkbox("Locked##showSQLocked", ref C.ShowSQL);

        ImGui.NewLine();
    }
}
