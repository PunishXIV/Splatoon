using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;
using SharpDX;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests;
public unsafe class GenericTest5 : SplatoonScript
{
		public override HashSet<uint>? ValidTerritories => new();

		System.Numerics.Vector3[] ArrayOfVectors;
		FFXIVClientStructs.FFXIV.Common.Math.Vector3[] ArrayOfVectorsCS;
		SharpDX.Vector3[] ArrayOfVectorsDX;

		public override void OnSetup()
		{
				var arrayOfVectors = new List<System.Numerics.Vector3>();
				for (int i = 0; i < 1000000; i++)
				{
						arrayOfVectors.Add(new(Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle()));
				}
				ArrayOfVectors = arrayOfVectors.ToArray();
				ArrayOfVectorsCS = arrayOfVectors.Select(x => (FFXIVClientStructs.FFXIV.Common.Math.Vector3)x).ToArray();
		}

		public override void OnSettingsDraw()
		{
				if (ImGui.Button("Test"))
				{
						var stopwatchNormal = new Stopwatch();
						var stopwatchCS = new Stopwatch();
						var stopwatchDX = new Stopwatch();
						var sum1 = 0f;
						var sum2 = 0f;
						var sum3 = 0f;
						foreach (var x in ArrayOfVectors)
						{
								stopwatchNormal.Start();
								sum1 += TestVector3(x);
								stopwatchNormal.Stop();
								stopwatchCS.Start();
								sum2 += TestVector3CS(new(x.X, x.Y, x.Z));
								stopwatchCS.Stop();
								stopwatchDX.Start();
								sum3 += TestVector3DX(new(x.X, x.Y, x.Z));
								stopwatchDX.Stop();
						}
						DuoLog.Information($"Results:\nNormal: {stopwatchNormal.ElapsedMilliseconds}\nConversion to CS: {stopwatchCS.ElapsedMilliseconds}\nConversion to DX: {stopwatchDX.ElapsedMilliseconds}");
				}
		}

		float TestVector3(System.Numerics.Vector3 v)
		{
				return v.X + v.Y + v.Z;
		}

		float TestVector3CS(FFXIVClientStructs.FFXIV.Common.Math.Vector3 v)
		{
				return v.X + v.Y + v.Z;
		}

		float TestVector3DX(SharpDX.Vector3 v)
		{
				return v.X + v.Y + v.Z;
		}
}
