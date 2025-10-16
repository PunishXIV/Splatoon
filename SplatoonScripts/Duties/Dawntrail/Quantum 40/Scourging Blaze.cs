using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Schedulers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Quantum40;
public class Scourging_Blaze : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1311];

    public override Metadata Metadata => new(3, "damolitionn");

    private uint NSFirst = 44798;
    private uint EWFirst = 44797;

    private uint Crystal = 2014832;

    private bool isNSFirst = false;
    private bool isEWFirst = false;

    private bool isIn12Lane = false;
    private bool isIn34Lane = false;

    private int castCounter = 0;

    private DateTime? castStartTime = null;

    public override void OnSetup()
    {
        //1 Safe
        Controller.RegisterElementFromCode("1", "{\"Name\":\"1\",\"enabled\": false,\"refX\":-594.13434,\"refY\":-313.40497,\"refZ\":-1.2874603E-05,\"radius\":1.58,\"color\":3357671168,\"Filled\":false,\"fillIntensity\":0.5,\"overlayFScale\":1.64,\"overlayText\":\"1 Safe\"}");
        //2 Safe
        Controller.RegisterElementFromCode("2", "{\"Name\":\"2\",\"enabled\": false,\"refX\":-594.2086,\"refY\":-286.23376,\"refZ\":-3.3378597E-06,\"radius\":1.58,\"color\":3357671168,\"Filled\":false,\"fillIntensity\":0.5,\"overlayFScale\":1.64,\"overlayText\":\"2 Safe\"}");
        //3 Safe
        Controller.RegisterElementFromCode("3", "{\"Name\":\"3\",\"enabled\": false,\"refX\":-605.5338,\"refY\":-286.20038,\"refZ\":-3.3378597E-06,\"radius\":1.58,\"color\":3357671168,\"Filled\":false,\"fillIntensity\":0.5,\"overlayFScale\":1.64,\"overlayText\":\"3 Safe\"}");
        //4 Safe
        Controller.RegisterElementFromCode("4", "{\"Name\":\"4\",\"enabled\": false,\"refX\":-605.6291,\"refY\":-313.52478,\"refZ\":-1.430511E-06,\"radius\":1.58,\"color\":3357671168,\"Filled\":false,\"fillIntensity\":0.5,\"overlayFScale\":1.64,\"overlayText\":\"4 Safe\"}");
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == NSFirst)
        {
            castCounter++;
            isNSFirst = true;
            isEWFirst = false;
            castStartTime = DateTime.Now;
            isIn12Lane = false;
            isIn34Lane = false;
        }
        if (castId == EWFirst)
        {
            castCounter++;
            isEWFirst = true;
            isNSFirst = false;
            castStartTime = DateTime.Now;
            isIn12Lane = false;
            isIn34Lane = false;
        }
    }

    public override void OnUpdate()
    {
        //S Safe
        if (isNSFirst)
        {
            //First Set
            if (castStartTime.HasValue && (DateTime.Now - castStartTime.Value).TotalSeconds >= 11 && (DateTime.Now - castStartTime.Value).TotalSeconds <= 14)
            {
                var positions = new List<(float x, float y, float z)>
                {
                    (-594f, 0, -300f), // 1/2 Lane
                    (-606f, 0, -300f)  // 3/4 Lane
                };

                var crystals = Svc.Objects
                    .OfType<IGameObject>()
                    .Where(obj => obj.BaseId == Crystal);

                if (!isIn12Lane)
                {
                    isIn12Lane = crystals.Any(obj =>
                        Math.Abs(obj.Position.X - positions[0].x) < 1.0f &&
                        Math.Abs(obj.Position.Y - positions[0].y) < 1.0f &&
                        Math.Abs(obj.Position.Z - positions[0].z) < 1.0f);
                }

                if (!isIn34Lane)
                {
                    isIn34Lane = crystals.Any(obj =>
                        Math.Abs(obj.Position.X - positions[1].x) < 1.0f &&
                        Math.Abs(obj.Position.Y - positions[1].y) < 1.0f &&
                        Math.Abs(obj.Position.Z - positions[1].z) < 1.0f);
                }
            }
        }

        if (isEWFirst)
        {
            //First Set
            if (castStartTime.HasValue && (DateTime.Now - castStartTime.Value).TotalSeconds >= 11 && (DateTime.Now - castStartTime.Value).TotalSeconds <= 14)
            {
                var positions = new List<(float x, float y, float z)>
                {
                    (-594f, 0, -300f), // 1/2 Lane
                    (-606f, 0, -300f)  // 3/4 Lane
                };

                var crystals = Svc.Objects
                    .OfType<IGameObject>()
                    .Where(obj => obj.BaseId == Crystal);

                if (!isIn12Lane)
                {
                    isIn12Lane = crystals.Any(obj =>
                        Math.Abs(obj.Position.X - positions[0].x) < 1.0f &&
                        Math.Abs(obj.Position.Y - positions[0].y) < 1.0f &&
                        Math.Abs(obj.Position.Z - positions[0].z) < 1.0f);
                }

                if (!isIn34Lane)
                {
                    isIn34Lane = crystals.Any(obj =>
                        Math.Abs(obj.Position.X - positions[1].x) < 1.0f &&
                        Math.Abs(obj.Position.Y - positions[1].y) < 1.0f &&
                        Math.Abs(obj.Position.Z - positions[1].z) < 1.0f);
                }
            }
        }
        if (castStartTime.HasValue && 
            ((castCounter != 4 && (DateTime.Now - castStartTime.Value).TotalSeconds >= 11 && (DateTime.Now - castStartTime.Value).TotalSeconds <= 43) ||
             (castCounter == 4 && (DateTime.Now - castStartTime.Value).TotalSeconds >= 11 && (DateTime.Now - castStartTime.Value).TotalSeconds <= 69)))
        {
            if (isIn12Lane)
            {
                if (isNSFirst)
                {
                    if (Controller.TryGetElementByName("3", out var threeSafe))
                    {
                        threeSafe.Enabled = true;
                    }
                }
                if (isEWFirst)
                {
                    if (Controller.TryGetElementByName("1", out var oneSafe))
                    {
                        oneSafe.Enabled = true;
                    }

                }
            }
            if (isIn34Lane)
            {
                if (isNSFirst)
                {
                    if (Controller.TryGetElementByName("2", out var twoSafe))
                    {
                        twoSafe.Enabled = true;
                    }
                }
                if (isEWFirst)
                {
                    if (Controller.TryGetElementByName("4", out var fourSafe))
                    {
                        fourSafe.Enabled = true;
                    }
                }
            }
        }

        if (castStartTime.HasValue && (DateTime.Now - castStartTime.Value).TotalSeconds > 43)
        {
            Reset();
        }
    }

    public override void OnReset()
    {
        Reset();
        castCounter = 0;
    }

    private void Reset()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        isNSFirst = false;
        isEWFirst = false;
        isIn12Lane = false;
        isIn34Lane = false;
        castStartTime = null;
    }
}
