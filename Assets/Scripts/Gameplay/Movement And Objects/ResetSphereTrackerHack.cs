using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetSphereTrackerHack : MonoBehaviour
{
    public ResetSphere.SphereColor sphereColor;

    private void Awake()
    {
        var uuid = GetComponent<UniqueId>().uniqueId;

        switch (sphereColor)
        {
            case ResetSphere.SphereColor.Blue:
                PersistenceManager.BlueResetSphereUUID = uuid;
                break;
            case ResetSphere.SphereColor.Green: 
                PersistenceManager.GreenResetSphereUUID = uuid;
                break;  
            case ResetSphere.SphereColor.Red: 
                PersistenceManager.RedResetSphereUUID = uuid;
                break;

        }
    }
}
