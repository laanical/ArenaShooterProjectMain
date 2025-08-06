using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ProjectileUtility
{
    // Returns a position just outside the first collider hit, or a default offset if nothing is hit
    public static Vector3 GetSafeProjectileSpawnPoint(Vector3 origin, Vector3 direction, float minDistance, LayerMask blockMask)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, minDistance, blockMask))
        {
            // We hit something too close! Place the spawn point just before the hit point.
            return hit.point - direction.normalized * 0.01f; // 1 cm before the hit surface
        }
        else
        {
            // Nothing in the way, spawn at min distance
            return origin + direction.normalized * minDistance;
        }
    }
}