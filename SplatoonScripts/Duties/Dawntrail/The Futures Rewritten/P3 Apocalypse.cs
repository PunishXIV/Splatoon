using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Schedulers;
using ECommons.SplatoonAPI;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P3_Apocalypse : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1238];

    List<IGameObject> objectsWithTargetDataId = new List<IGameObject>();

    List<IGameObject> allObjects = new List<IGameObject>();
    public override Metadata? Metadata => new(1, "Errer");

    IBattleNpc? OracleofDarkness  => Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.IsTargetable && x.DataId == 17831);

    private IBattleChara[] fire => [.. Svc.Objects.OfType<IBattleChara>().Where(x => x.DataId == 2011391)];

    private TickScheduler? _scheduler;



    public override void OnSetup()
    {

    }

    private void FireCircle(IGameObject obj)
    {

        Controller.RegisterElementFromCode(obj.GameObjectId.ToString(), $"{{\"Name\":\"Cross\",\"type\":1,\"radius\":9.0,\"fillIntensity\":0.5,\"refActorObjectID\":{obj.GameObjectId},\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}}");
        Controller.ScheduleReset(20000);


    }

    public override void OnStartingCast(uint source, uint castId)
    {
        var sourceObj = source.GetObject();
        if (sourceObj == null)
            return;

        if (sourceObj.DataId == 0 || sourceObj.DataId != 17831)
            return;

        if (castId == 40296)
        {

            _scheduler = new TickScheduler(() =>
            {
                ShowElement();
            }, 10000);

            _scheduler = new TickScheduler(() =>
            {
                OnReset();
            }, 18000);



        }





    }

    public void ShowElement()
    {
        var objectsToProcess = allObjects.Take(6).ToList();
        foreach (var obj in objectsToProcess)
        {
          
            FireCircle(obj);
        }

    }



    public override void OnUpdate()
    {
       

        if (OracleofDarkness == null)
            return;
        if (fire.Length == 0)
        {
          objectsWithTargetDataId.Clear();
          allObjects.Clear();
        }

        Calculatelocation();



    }

    public override void OnReset()
    {
        Controller.ClearRegisteredElements();
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Indent();
            foreach (var obj in allObjects)
            {
                ImGui.Text($"gameobjid = {obj.GameObjectId}, pos = ({obj.Position.X}, {obj.Position.Y}, {obj.Position.Z})");
            }
            ImGui.Unindent();
        }

    }

    public void Calculatelocation()
    {
        var targetPosition = new Vector3(100, 0, 100);
        foreach (var gameObject in Svc.Objects)
        {
            if (gameObject.DataId == 0x1EB0FF)
            {
                objectsWithTargetDataId.Add(gameObject);
            }
        }
        if (objectsWithTargetDataId.Count < 2)
        {

            return;
        }
        var closestObjects = objectsWithTargetDataId
            .OrderBy(obj => Vector3.Distance(obj.Position, targetPosition))
            .Take(2)
            .ToList();
        for (int i = 0; i < closestObjects.Count; i++)
        {
            var gameObject = closestObjects[i];
            Vector3 position = gameObject.Position;
            float facing = gameObject.Rotation;
            float facingDegrees = facing * (180 / MathF.PI);
            if (facingDegrees < 0)
            {
                facingDegrees += 360;
            }
            float dx = MathF.Cos(facing);  
            float dz = MathF.Sin(facing);  
            float distance = 15f;
            float newX = position.X + dz * distance;  
            float newZ = position.Z + dx * distance;  
            IGameObject nextTarget = null;
            float minDistance = float.MaxValue;

            foreach (var obj in Svc.Objects)
            {
                if (obj.DataId == 0x1EB0FF)
                {
        
                    float dist = Vector3.Distance(new Vector3(newX, position.Y, newZ), obj.Position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nextTarget = obj;
                    }
                }
            }

        
            if (nextTarget != null)
            {
              
                allObjects.Add(gameObject);
            
                Vector3 nextPosition = nextTarget.Position;
                float nextFacing = nextTarget.Rotation;
     
                float nextDx = MathF.Cos(nextFacing);
                float nextDz = MathF.Sin(nextFacing);

                float nextNewX = nextPosition.X + nextDz * distance;
                float nextNewZ = nextPosition.Z + nextDx * distance;

                IGameObject secondNextTarget = null;
                float secondMinDistance = float.MaxValue;

                foreach (var obj in Svc.Objects)
                {
                    if (obj.DataId == 0x1EB0FF)
                    {
                        float dist = Vector3.Distance(new Vector3(nextNewX, nextPosition.Y, nextNewZ), obj.Position);
                        if (dist < secondMinDistance)
                        {
                            secondMinDistance = dist;
                            secondNextTarget = obj;
                        }
                    }
                }

                if (secondNextTarget != null)
                {
                    allObjects.Add(nextTarget);

                    Vector3 secondNextPosition = secondNextTarget.Position;
                    float secondNextFacing = secondNextTarget.Rotation;
                    float secondNextDx = MathF.Cos(secondNextFacing);
                    float secondNextDz = MathF.Sin(secondNextFacing);
                    float secondNextNewX = secondNextPosition.X + secondNextDz * distance;
                    float secondNextNewZ = secondNextPosition.Z + secondNextDx * distance;
                    IGameObject thirdNextTarget = null;
                    float thirdMinDistance = float.MaxValue;

                    foreach (var obj in Svc.Objects)
                    {
                        if (obj.DataId == 0x1EB0FF)
                        {
                            float dist = Vector3.Distance(new Vector3(secondNextNewX, secondNextPosition.Y, secondNextNewZ), obj.Position);
                            if (dist < thirdMinDistance)
                            {
                                thirdMinDistance = dist;
                                thirdNextTarget = obj;
                            }
                        }
                    }

                    if (thirdNextTarget != null)
                    {

                        allObjects.Add(secondNextTarget);
                    }
                }
            }
        }

      
    }







}
