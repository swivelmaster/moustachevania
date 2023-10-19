using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxRoom : ParallaxManager
{
    const float GIZMO_SIZE = 20f;

    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.DrawCube(transform.position, Vector3.one * GIZMO_SIZE);
    //}

    [Header("Reference point for object positions relative to camera")]
    public Transform RoomOriginPoint;

    /// <summary>
    /// Calculate position relative to its original distance from this room's
    /// "origin point" (can be this object, can be another referenceo bject)
    /// This ensures that all objects in the 'room' are in the right place
    /// relative to each other when the player is at the room's origin point
    /// </summary>
    /// <param name="o"></param>
    protected override void UpdatePosition(ParallaxObject o)
    {
        o.transform.position = OriginalPositions[o] +
            ((Vector2)RoomOriginPoint.position + (Vector2)MainCamera.transform.position) * o.parallaxSettings.ScrollSpeedMultiplier;
    }
}
