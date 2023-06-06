using ECommons;
using ECommons.DalamudServices;
using ECommons.Hooks;
using ECommons.Schedulers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P9S_JP_LC_Strat : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1148 };
        public override Metadata? Metadata => new(1, "Kopi");
        bool mechanicActive = false;

        bool isBola5InN = false;
        bool isBola5InNE = false;
        bool isBola5InE = false;
        bool isBola5InSE = false;
        bool isBola5InS = false;
        bool isBola5InSW = false;
        bool isBola5InW = false;
        bool isBola5InNW = false;

        bool isBola7InN = false;
        bool isBola7InNE = false;
        bool isBola7InE = false;
        bool isBola7InSE = false;
        bool isBola7InS = false;
        bool isBola7InSW = false;
        bool isBola7InW = false;
        bool isBola7InNW = false;

        TickScheduler? sched = null;

        public override void OnSetup()
        {
            Controller.RegisterElementFromCode("PuddleNorth", "{\"Name\":\"PuddleNorth\",\"Enabled\":false,\"refX\":100.68455,\"refY\":80.81478,\"refZ\":-3.8146973E-06,\"radius\":2.0,\"color\":4278255360,\"overlayBGColor\":4261413119,\"thicc\":4.0,\"overlayText\":\"PUDDLE\",\"tether\":true}");
            Controller.RegisterElementFromCode("PuddleEast", "{\"Name\":\"PuddleEast\",\"Enabled\":false,\"refX\":119.30016,\"refY\":101.13147,\"radius\":2.0,\"color\":4278255360,\"overlayBGColor\":4261413119,\"thicc\":4.0,\"overlayText\":\"PUDDLE\",\"tether\":true}");
            Controller.RegisterElementFromCode("PuddleSouth", "{\"Name\":\"PuddleSouth\",\"Enabled\":false,\"refX\":100.5027,\"refY\":119.368065,\"radius\":2.0,\"color\":4278255360,\"overlayBGColor\":4261413119,\"thicc\":4.0,\"overlayText\":\"PUDDLE\",\"tether\":true}");
            Controller.RegisterElementFromCode("PuddleWest", "{\"Name\":\"PuddleWest\",\"Enabled\":false,\"refX\":80.37972,\"refY\":99.94977,\"radius\":2.0,\"color\":4278255360,\"overlayBGColor\":4261413119,\"thicc\":4.0,\"overlayText\":\"PUDDLE\",\"tether\":true}");
            Controller.RegisterElementFromCode("PuddleNorthWest", "{\"Name\":\"PuddleNorthWest\",\"Enabled\":false,\"refX\":86.39617,\"refY\":86.30669,\"refZ\":-9.536743E-07,\"radius\":2.0,\"color\":4278255360,\"overlayBGColor\":4261413119,\"thicc\":4.0,\"overlayText\":\"PUDDLE\",\"tether\":true}");
            Controller.RegisterElementFromCode("PuddleNorthEast", "{\"Name\":\"PuddleNorthEast\",\"Enabled\":false,\"refX\":113.45364,\"refY\":86.63834,\"radius\":2.0,\"color\":4278255360,\"overlayBGColor\":4261413119,\"thicc\":4.0,\"overlayText\":\"PUDDLE\",\"tether\":true}");
            Controller.RegisterElementFromCode("PuddleSouthEast", "{\"Name\":\"PuddleSouthEast\",\"Enabled\":false,\"refX\":113.90593,\"refY\":114.04176,\"refZ\":9.536743E-07,\"radius\":2.0,\"color\":4278255360,\"overlayBGColor\":4261413119,\"thicc\":4.0,\"overlayText\":\"PUDDLE\",\"tether\":true}");
            Controller.RegisterElementFromCode("PuddleSouthWest", "{\"Name\":\"PuddleSouthWest\",\"Enabled\":false,\"refX\":85.779465,\"refY\":114.211334,\"radius\":2.0,\"color\":4278255360,\"overlayBGColor\":4261413119,\"thicc\":4.0,\"overlayText\":\"PUDDLE\",\"tether\":true}");
            Controller.RegisterElementFromCode("DefamNorth", "{\"Name\":\"DefamNorth\",\"Enabled\":false,\"refX\":100.68455,\"refY\":80.81478,\"refZ\":-3.8146973E-06,\"radius\":2.0,\"color\":4294901760,\"overlayBGColor\":4294901760,\"thicc\":4.0,\"overlayText\":\"DEFAM\",\"tether\":true}");
            Controller.RegisterElementFromCode("DefamEast", "{\"Name\":\"DefamEast\",\"Enabled\":false,\"refX\":119.30016,\"refY\":101.13147,\"radius\":2.0,\"color\":4294901760,\"overlayBGColor\":4294901760,\"thicc\":4.0,\"overlayText\":\"DEFAM\",\"tether\":true}");
            Controller.RegisterElementFromCode("DefamSouth", "{\"Name\":\"DefamSouth\",\"Enabled\":false,\"refX\":100.5027,\"refY\":119.368065,\"radius\":2.0,\"color\":4294901760,\"overlayBGColor\":4294901760,\"thicc\":4.0,\"overlayText\":\"DEFAM\",\"tether\":true}");
            Controller.RegisterElementFromCode("DefamWest", "{\"Name\":\"DefamWest\",\"Enabled\":false,\"refX\":80.37972,\"refY\":99.94977,\"radius\":2.0,\"color\":4294901760,\"overlayBGColor\":4294901760,\"thicc\":4.0,\"overlayText\":\"DEFAM\",\"tether\":true}");
            Controller.RegisterElementFromCode("DefamNorthWest", "{\"Name\":\"DefamNorthWest\",\"Enabled\":false,\"refX\":86.39617,\"refY\":86.30669,\"refZ\":-9.536743E-07,\"radius\":2.0,\"color\":4294901760,\"overlayBGColor\":4294901760,\"thicc\":4.0,\"overlayText\":\"DEFAM\",\"tether\":true}");
            Controller.RegisterElementFromCode("DefamNorthEast", "{\"Name\":\"DefamNorthEast\",\"Enabled\":false,\"refX\":113.45364,\"refY\":86.63834,\"radius\":2.0,\"color\":4294901760,\"overlayBGColor\":4294901760,\"thicc\":4.0,\"overlayText\":\"DEFAM\",\"tether\":true}");
            Controller.RegisterElementFromCode("DefamSouthEast", "{\"Name\":\"DefamSouthEast\",\"Enabled\":false,\"refX\":113.90593,\"refY\":114.04176,\"refZ\":9.536743E-07,\"radius\":2.0,\"color\":4294901760,\"overlayBGColor\":4294901760,\"thicc\":4.0,\"overlayText\":\"DEFAM\",\"tether\":true}");
            Controller.RegisterElementFromCode("DefamSouthWest", "{\"Name\":\"DefamSouthWest\",\"Enabled\":false,\"refX\":85.779465,\"refY\":114.211334,\"radius\":2.0,\"color\":4294901760,\"overlayBGColor\":4294901760,\"thicc\":4.0,\"overlayText\":\"DEFAM\",\"tether\":true}");

        }

        public override void OnVFXSpawn(uint target, string vfxPath)
        {
            var obj = target.GetObject();
            Dictionary<string, Dictionary<(double, double), Action>> vfxPositionMap = new()
            {
                {
                    "vfx/lockon/eff/m0361trg_a5t.avfx", //bola 5
                    new Dictionary<(double, double), Action>
                    {
                        { (92, 92), () => isBola5InNW = true },
                        { (100, 90), () => isBola5InN = true },
                        { (107, 92), () => isBola5InNE = true },
                        { (110, 100), () => isBola5InE = true },
                        { (107, 107), () => isBola5InSE = true },
                        { (100, 110), () => isBola5InS = true },
                        { (92, 107), () => isBola5InSW = true },
                        { (90, 100), () => isBola5InW = true }
                    }
                },
                {
                    "vfx/lockon/eff/m0361trg_a7t.avfx", //bola 7
                    new Dictionary<(double, double), Action>
                    {
                        { (92, 92), () => isBola7InNW = true },
                        { (100, 90), () => isBola7InN = true },
                        { (107, 92), () => isBola7InNE = true },
                        { (110, 100), () => isBola7InE = true },
                        { (107, 107), () => isBola7InSE = true },
                        { (100, 110), () => isBola7InS = true },
                        { (92, 107), () => isBola7InSW = true },
                        { (90, 100), () => isBola7InW = true }
                    }
                }
            };

            double positionTolerance = 1.0;

            if (vfxPositionMap.ContainsKey(vfxPath))
            {
                var positionMap = vfxPositionMap[vfxPath];

                foreach (var entry in positionMap)
                {
                    var (x, z) = entry.Key;
                    if (Math.Abs(obj.Position.X - x) <= positionTolerance && Math.Abs(obj.Position.Z - z) <= positionTolerance)
                    {
                        entry.Value.Invoke();
                        break;
                    }
                }
            }




            //bola 5 di NW
            if (isBola5InNW && isBola7InNE)
            {
                DisplayHide("PuddleNorth", "DefamSouth");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleSouth", "DefamNorth");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }
            if (isBola5InNW && isBola7InSW)
            {
                DisplayHide("PuddleWest", "DefamEast");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleEast", "DefamWest");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }

            //bola 5 di NE
            if (isBola5InNE && isBola7InNW)
            {
                DisplayHide("PuddleNorth", "DefamSouth");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleSouth", "DefamNorth");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }
            if (isBola5InNE && isBola7InSE)
            {
                DisplayHide("PuddleEast", "DefamWest");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleWest", "DefamEast");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }



            //bola 5 di SE
            if (isBola5InSE && isBola7InNE)
            {
                DisplayHide("PuddleEast", "DefamWest");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleWest", "DefamEast");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }
            if (isBola5InSE && isBola7InSW)
            {
                DisplayHide("PuddleSouth", "DefamNorth");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleNorth", "DefamSouth");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }

            //bola 5 di SW
            if (isBola5InSW && isBola7InNW)
            {
                DisplayHide("PuddleWest", "DefamEast");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleEast", "DefamWest");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }
            if (isBola5InSW && isBola7InSE)
            {
                DisplayHide("PuddleSouth", "DefamNorth");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleNorth", "DefamSouth");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }

            //bola 5 di N
            if (isBola5InN && isBola7InE)
            {
                DisplayHide("PuddleNorthEast", "DefamSouthWest");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleSouthWest", "DefamNorthEast");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }
            if (isBola5InN && isBola7InW)
            {
                DisplayHide("PuddleNorthWest", "DefamSouthEast");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleSouthEast", "DefamNorthWest");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }


            //bola 5 di E
            if (isBola5InE && isBola7InN)
            {
                DisplayHide("PuddleNorthEast", "DefamSouthWest");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleSouthWest", "DefamNorthEast");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }
            if (isBola5InE && isBola7InS)
            {
                DisplayHide("PuddleSouthEast", "DefamNorthWest");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleNorthWest", "DefamSouthEast");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }



            //bola 5 di S
            if (isBola5InS && isBola7InE)
            {
                DisplayHide("PuddleSouthEast", "DefamNorthWest");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleNorthWest", "DefamSouthEast");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }
            if (isBola5InS && isBola7InW)
            {
                DisplayHide("PuddleSouthWest", "DefamNorthEast");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleNorthEast", "DefamSouthWest");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }



            //bola 5 di W
            if (isBola5InW && isBola7InN)
            {
                DisplayHide("PuddleNorthWest", "DefamSouthEast");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleEast", "DefamNorthWest");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }
            if (isBola5InW && isBola7InS)
            {
                DisplayHide("PuddleSouthWest", "DefamNorthEast");

                Task.Delay(21000).ContinueWith(_ =>
                {
                    DisplayHide("PuddleNorthEast", "DefamSouthWest");
                });
                Task.Delay(33000).ContinueWith(_ =>
                {
                    DisplayHide();
                });
            }

        }


        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if (category.EqualsAny(DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
            {
                DisplayHide();
            }
        }

        void DisplayHide(params string[] elementsToEnable)
        {
            Svc.Framework.RunOnFrameworkThread(() =>
            {
                foreach (var element in Controller.GetRegisteredElements())
                {
                    element.Value.Enabled = false;
                }

                sched?.Dispose();

                foreach (var elementToEnable in elementsToEnable)
                {
                    if (Controller.GetElementByName(elementToEnable) != null)
                    {
                        Controller.GetElementByName(elementToEnable).Enabled = true;
                    }
                }

                if (elementsToEnable.Length > 0)
                {
                    sched = new TickScheduler(() => DisplayHide(), 21000);
                }

                // Reset the variable to false
                isBola5InN = false;
                isBola5InNE = false;
                isBola5InE = false;
                isBola5InSE = false;
                isBola5InS = false;
                isBola5InSW = false;
                isBola5InW = false;
                isBola5InNW = false;

                isBola7InN = false;
                isBola7InNE = false;
                isBola7InE = false;
                isBola7InSE = false;
                isBola7InS = false;
                isBola7InSW = false;
                isBola7InW = false;
                isBola7InNW = false;
            });
        }

    }
}