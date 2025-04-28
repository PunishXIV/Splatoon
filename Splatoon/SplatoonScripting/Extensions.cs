﻿using ECommons.GameFunctions;
using System.Diagnostics.CodeAnalysis;
#nullable enable
namespace Splatoon.SplatoonScripting;

public static unsafe class Extensions
{
    /// <summary>
    /// Gets object by it's object ID.
    /// </summary>
    /// <param name="objectID">Object ID to search.</param>
    /// <returns>GameObject if found; null otherwise.</returns>
    public static IGameObject? GetObject(this uint objectID)
    {
        return Svc.Objects.FirstOrDefault(x => x.EntityId == objectID);
    }

    /// <summary>
    /// Attempts to get object by it's object ID.
    /// </summary>
    /// <param name="objectID">Object ID to search.</param>
    /// <param name="obj">Resulting GameObject if found; null otherwise.</param>
    /// <returns>Whether object was found.</returns>
    public static bool TryGetObject(this uint objectID, [NotNullWhen(true)] out IGameObject? obj)
    {
        obj = objectID.GetObject();
        return obj != null;
    }

    /// <summary>
    /// Sets reference position for Element from Vector3, accounting for Y and Z swap.
    /// </summary>
    /// <param name="e">Element to set position of</param>
    /// <param name="Position">Position</param>
    public static void SetRefPosition(this Element e, Vector3 Position)
    {
        e.refX = Position.X;
        e.refY = Position.Z;
        e.refZ = Position.Y;
    }

    /// <summary>
    /// Sets offset position for Element from Vector3, accounting for Y and Z swap.
    /// </summary>
    /// <param name="e">Element to set position of</param>
    /// <param name="Position">Position</param>
    public static void SetOffPosition(this Element e, Vector3 Position)
    {
        e.offX = Position.X;
        e.offY = Position.Z;
        e.offZ = Position.Y;
    }
}
