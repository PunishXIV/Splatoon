﻿using Splatoon.RenderEngines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Services;
public static class S
{
    public static ThreadPool ThreadPool { get; private set; }
    internal static RenderManager RenderManager { get; private set; }
    internal static VbmCamera VbmCamera { get; private set; }
    internal static ScriptFileWatcher ScriptFileWatcher { get; private set; }
    internal static InfoBar InfoBar { get; private set; }
    //internal static StatusEffectManager StatusEffectManager { get; private set; }
}
