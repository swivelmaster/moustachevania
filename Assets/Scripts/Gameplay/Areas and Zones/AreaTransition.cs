using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Originally used because each area had a name and the names would
/// pop up when you entered/exited an area. Not currently used.
/// </summary>
public class AreaTransition : MonoBehaviour
{
    public string DestinationScene;
    public int DestinationSceneCheckpointId;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        GameManager.instance.AreaTransition(DestinationScene, DestinationSceneCheckpointId);
    }
}
