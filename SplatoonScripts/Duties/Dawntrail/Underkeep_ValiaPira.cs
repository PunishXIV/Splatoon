using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using SharpDX.Direct3D11;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.Structures;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe class Underkeep_ValiaPira : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1266];
    public int RotationAngle = 0;
    public Vector2 ArenaMiddle = new(0, -331);
    public List<Vector3> BlacklistedZones = [];
    public override Metadata Metadata => new(1, "NightmareXIV");

    public static class Entities
    {
        public static readonly uint Cube = 18313;
        public static readonly uint Sphere = 18319;
        public static readonly uint TetheredSphere = 18357;
    }

    public override void OnSetup()
    {
        for(int i = 0; i < 2; i++)
        {
            Controller.RegisterElementFromCode($"AOEV{i}", "{\"Name\":\"\",\"type\":3,\"refY\":40.0,\"offY\":-40.0,\"radius\":4.5,\"refActorObjectID\":0,\"refActorComparisonType\":2,\"includeRotation\":true}");
            Controller.RegisterElementFromCode($"AOEH{i}", "{\"Name\":\"\",\"type\":3,\"refY\":40.0,\"offY\":-40.0,\"radius\":4.5,\"refActorObjectID\":0,\"refActorComparisonType\":2,\"includeRotation\":true,\"AdditionalRotation\":1.5707964}");
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 42737 && source.TryGetObject(out var o))
        {
            BlacklistedZones.Add(o.Position);
        }
    }

    bool HasTether(IGameObject obj)
    {
        foreach(var x in AttachedInfo.TetherInfos)
        {
            if(x.Key == obj.Address && x.Value.Any(t => t.Param2 == 282 && t.AgeF < 15f)) return true;
            foreach(var t in x.Value)
            {
                if(t.Target == obj.EntityId && t.Param2 == 282 && t.AgeF < 15f) return true;
            }
        }
        return false;
    }

    IEnumerable<IBattleNpc> Spheres => Svc.Objects.OfType<IBattleNpc>().Where(x => x.IsCharacterVisible() && !BlacklistedZones.Any(b => Vector3.Distance(b, x.Position) < 1)).Where(x => (x.DataId == Entities.Sphere && !HasTether(x)) || (x.DataId == Entities.TetheredSphere && HasTether(x)));

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        var cubes = Svc.Objects.OfType<IBattleNpc>().Where(x => x.DataId == Entities.Cube && x.IsCharacterVisible()).OrderBy(x => Rotate(x.Position).X).ToArray();
        if(cubes.Length == 2)
        {
            DetermineRotation();
            var leftSpheres = Spheres.Where(x => Rotate(x.Position).X < 0 && Rotate(x.Position).Z > Rotate(cubes[0].Position).Z - 0.1f).OrderBy(x => Vector3.Distance(Rotate(x.Position), Rotate(cubes[0].Position)));
            var rightSpheres = Spheres.Where(x => Rotate(x.Position).X > 0 && Rotate(x.Position).Z > Rotate(cubes[1].Position).Z - 0.1f).OrderBy(x => Vector3.Distance(Rotate(x.Position), Rotate(cubes[1].Position)));
            if(leftSpheres.Any())
            {
                if(Controller.TryGetElementByName("AOEV0", out var e1) && Controller.TryGetElementByName("AOEH0", out var e2))
                {
                    e1.Enabled = true;
                    e1.refActorObjectID = leftSpheres.First().EntityId;
                    e1.RotationOverride = true;
                    e1.RotationOverridePoint = leftSpheres.First().Position.ToVector2().ToPoint2();
                    e2.Enabled = true;
                    e2.refActorObjectID = leftSpheres.First().EntityId;
                    e2.RotationOverride = true;
                    e2.RotationOverridePoint = leftSpheres.First().Position.ToVector2().ToPoint2();
                }
            }
            if(rightSpheres.Any())
            {
                if(Controller.TryGetElementByName("AOEV1", out var e1) && Controller.TryGetElementByName("AOEH1", out var e2))
                {
                    e1.Enabled = true;
                    e1.refActorObjectID = rightSpheres.First().EntityId;
                    e1.RotationOverride = true;
                    e1.RotationOverridePoint = rightSpheres.First().Position.ToVector2().ToPoint2();
                    e2.Enabled = true;
                    e2.refActorObjectID = rightSpheres.First().EntityId;
                    e2.RotationOverride = true;
                    e2.RotationOverridePoint = rightSpheres.First().Position.ToVector2().ToPoint2();
                }
            }
        }
        else
        {
            BlacklistedZones.Clear();
        }
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"Spheres: {Spheres.Print("\n")}");
        ImGuiEx.Text($"Rotation: {RotationAngle}");
        ImGuiEx.Text($"BlacklistedZones: {BlacklistedZones.Print("\n")}");
    }

    Vector3 Rotate(Vector3 position)
    {
        return MathHelper.RotateWorldPoint(ArenaMiddle.ToVector3(), RotationAngle.DegreesToRadians(), position);
    }

    void DetermineRotation()
    {
        if(Svc.Objects.TryGetFirst(x => x.DataId == 18313, out var obj) && !IsPointInsideSquare(ArenaMiddle, obj.Position.ToVector2(), 17.5f))
        {
            if(obj.Position.Z < -348) //spawned north
            {
                RotationAngle = 0;
            }
            else if(obj.Position.Z > -314) // spawned south
            {
                RotationAngle = 180;
            }
            else if(obj.Position.X < -17) //spawned west
            {
                RotationAngle = 90;
            }
            else if(obj.Position.X > 17) // spawned east
            {
                RotationAngle = 270;
            }
        }
    }

    public static bool IsPointInsideSquare(Vector2 point, Vector2 center, float radius)
    {
        float halfSide = radius;

        float left = center.X - halfSide;
        float right = center.X + halfSide;
        float bottom = center.Y - halfSide;
        float top = center.Y + halfSide;

        return (point.X >= left && point.X <= right &&
                point.Y >= bottom && point.Y <= top);
    }
}