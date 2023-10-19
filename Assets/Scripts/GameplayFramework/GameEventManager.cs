using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameEventManager : MonoBehaviour
{
    // This has to be a scene-level instance so we can
    // have in-scene object register for events and then
    // have everything get cleared from memory properly
    // when loading a new scene.
    public static GameEventManager Instance;

    public UnityEvent PlayerRestartedAtCheckpoint = new UnityEvent();
    public CutsceneTriggeredEvent CutsceneTriggered = new CutsceneTriggeredEvent();
    public ResetSphereActivatedEvent ResetSphereActivated = new ResetSphereActivatedEvent();

    public UnityEvent GrantAllAbilities = new UnityEvent();
    public UnityEvent ToggleCameraMode = new UnityEvent();

    public ClothingPickupCutsceneCompleteEvent ClothingPickupCutsceneComplete = new ClothingPickupCutsceneCompleteEvent();
    public InventoryItemPickupEvent InventoryItemPickedUp = new InventoryItemPickupEvent();

    public AOBecameClipEvent AOBecameClip = new AOBecameClipEvent();

    private void Awake()
    {
        Instance = this;
    }
}

public class CutsceneTriggeredEvent : UnityEvent<string, Action> { }
public class ClothingPickupCutsceneCompleteEvent : UnityEvent<ClothingPickupType> { }
public class AOBecameClipEvent : UnityEvent<AdjustableObject> { }
public class InventoryItemPickupEvent : UnityEvent<int> { }
public class ResetSphereActivatedEvent : UnityEvent<ResetSphere.SphereColor> { }