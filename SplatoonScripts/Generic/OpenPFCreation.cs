using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic
{
    public unsafe class OpenPFCreation : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => [];
        private bool OpenCreate = false;

        public override void OnEnable()
        {
            Svc.Commands.AddHandler("/createpf", new(OpenPF));
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "LookingForGroup", OpenRecruitment);
        }

        private void OpenRecruitment(AddonEvent type, AddonArgs args)
        {
            if(OpenCreate)
            {
                OpenCreate = false;
                Callback.Fire((AtkUnitBase*)args.Addon.Address, true, 14);
            }
        }

        private void OpenPF(string command, string arguments)
        {
            if(EzThrottler.Throttle("CreatePF"))
            {
                Chat.Instance.SendMessage("/pfinder");
                OpenCreate = true;
            }
        }

        public override void OnDisable()
        {
            Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "LookingForGroup", OpenRecruitment);
            Svc.Commands.RemoveHandler("/createpf");
            OpenCreate = false;
        }
    }
}
