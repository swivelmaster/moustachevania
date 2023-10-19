using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameObjectBase : MonoBehaviour
{
    public abstract void AdvanceFrame();
    public abstract void PhysicsStep();
    public abstract void Register();
}
