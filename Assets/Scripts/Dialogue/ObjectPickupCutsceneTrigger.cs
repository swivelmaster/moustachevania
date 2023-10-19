using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPickupCutsceneTrigger : CutsceneTrigger
{
    [Header("Only pick one of these")]
    public ClothingPickupType PickupType;
    public int InventoryIdPickup = 0;
    public bool useResetSphereColor = false;

    [Header("Only used if useResetSphereColor is true")]
    public ResetSphere.SphereColor sphereColor = ResetSphere.SphereColor.Green;

    protected override string GetCutsceneName()
    {
        if (InventoryIdPickup == 0 && !useResetSphereColor)
        {
            return "Pickup_" + PickupType.ToString();
        }

        return base.GetCutsceneName();
    }

    protected override string GetEncounteredVariableName()
    {
        if (InventoryIdPickup == 0 && !useResetSphereColor)
            return "$Pickup_" + PickupType.ToString();

        return base.GetEncounteredVariableName();
    }

    public void CutsceneAction()
    {
        if (useResetSphereColor)
        {
            GameEventManager.Instance.ResetSphereActivated.Invoke(sphereColor);
        }
        else if (InventoryIdPickup > 0)
        {
            GameEventManager.Instance.InventoryItemPickedUp.Invoke(InventoryIdPickup);
        }
        else
        {
            GameEventManager.Instance.ClothingPickupCutsceneComplete.Invoke(PickupType);
        }

        GetComponent<Destroyable>().Destroyed();
    }
}
