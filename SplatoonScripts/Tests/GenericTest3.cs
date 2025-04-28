﻿using ECommons.Logging;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Tests
{
    public class GenericTest3 : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => [];

        private Class1 C1;
        private Class2 C2;

        private WeakReference<Class1> C1w;
        private WeakReference<Class2> C2w;

        public override void OnEnable()
        {
            C1 = new();
            C2 = new();
            C1.Event += C2.Handler;
            C1w = new(C1);
            C2w = new(C2);
        }

        public override void OnUpdate()
        {
            PluginLog.Information($"C1: {C1w.TryGetTarget(out _)}, C2: {C2w.TryGetTarget(out _)}");
        }

        public override void OnSettingsDraw()
        {
            if(ImGui.Button("Nullify"))
            {
                C1 = null;
                C2 = null;
            }
        }

        private class Class1
        {
            public event Action? Event;
        }

        private class Class2
        {
            public void Handler() { }
        }
    }
}
