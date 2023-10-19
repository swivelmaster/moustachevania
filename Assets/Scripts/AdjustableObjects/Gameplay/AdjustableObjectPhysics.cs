using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustableObjectPhysics : MonoBehaviour
{
    [SerializeField]
    AdjustableObjectDrawGrid MyGrid;

    Rigidbody2D RigidBody2D;

    void Awake()
    {
        RigidBody2D = MyGrid.SpriteDestination.GetComponent<Rigidbody2D>();
    }

    public void SetRotation(float angle)
    {
        RigidBody2D.rotation = angle;
    }

    public float GetRotation()
    {
        return RigidBody2D.rotation;
    }

    private void FixedUpdate()
    {
        (Vector2 _1, Vector2 _2) = PlayerManager.Instance.GetPlayerAndCameraPositions();
        var should = GameUtils.ShouldEnablePhysics(MyGrid.SpriteDestination.position, _1, _2);

        if (should)
        {
            RigidBody2D.WakeUp();
        }
        else
        {
            RigidBody2D.Sleep();
        }
    }

    public void SetCollisionsEnabled(bool enabled)
    {
        foreach (var collider in MyGrid.boxColliders)
            collider.enabled = enabled;

        //if (!enabled && CompositeCollider.gameObject.layer != AdjustableObjectManager.Instance.InactiveLayer)
        //    CompositeCollider.gameObject.layer = AdjustableObjectManager.Instance.InactiveLayer;
        //else if (enabled && CompositeCollider.gameObject.layer != AdjustableObjectManager.Instance.PlatformLayer)
        //    CompositeCollider.gameObject.layer = AdjustableObjectManager.Instance.PlatformLayer;
    }

}
