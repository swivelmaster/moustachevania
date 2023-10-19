using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggeredMovementManager : MonoBehaviour
{
    // Fresh copies of the managed triggered platforms that we can Instantiate() whe needed
    static Dictionary<string, GameObject> managedObjects = new Dictionary<string, GameObject>();

    // The currently spawned object waiting to be triggered (can be null)
    // Used by the trigger object in place of the object reference because
    // the object originally referenced will delete itself after the first
    // go-around.
    static Dictionary<string, ManagedTriggeredMovement> objectWaiting = new Dictionary<string, ManagedTriggeredMovement>();

    public static TriggeredMovementManager instance { private set; get; }

    public void Awake()
    {
        instance = this;
        objectWaiting = new Dictionary<string, ManagedTriggeredMovement>();
        managedObjects = new Dictionary<string, GameObject>();
    }

    public ManagedTriggeredMovement GetCurrentWaitingByUniqueId(string uniqueId)
    {
        return objectWaiting[uniqueId];
    }

    public void Register(ManagedTriggeredMovement movement)
    {
        // Prevent infinite loop of instantiating new ones...
        if (managedObjects.ContainsKey(movement.myId.uniqueId))
            return;

        objectWaiting[movement.myId.uniqueId] = movement;

        managedObjects[movement.myId.uniqueId] = Instantiate(movement.gameObject, movement.transform.position, movement.transform.rotation);
        managedObjects[movement.myId.uniqueId].SetActive(false);
    }

    public void SpawnNew(ManagedTriggeredMovement movement)
    {
        GameObject newOne = Instantiate(managedObjects[movement.myId.uniqueId]);
        newOne.SetActive(true);

        objectWaiting[movement.myId.uniqueId] = newOne.GetComponent<ManagedTriggeredMovement>();
        objectWaiting[movement.myId.uniqueId].InitAsSpawned();
    }
}
