using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager
{
    public static ObjectManager Instance { private set; get; }

    List<GameObjectBase> gameObjects;

    public ObjectManager()
	{
        Instance = this;
        gameObjects = new List<GameObjectBase>();
	}

    public void RegisterGameObject(GameObjectBase o)
	{
        gameObjects.Add(o);
	}

    public void AdvanceFrame()
	{
        foreach (var o in gameObjects)
            o.AdvanceFrame();
	}

    public void PhysicsStep()
	{
        foreach (var o in gameObjects)
            o.PhysicsStep();
	}

}