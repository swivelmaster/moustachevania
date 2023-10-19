using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryTargetSelectView : MonoBehaviour
{
    [SerializeField]
    private GameObject ParentObject = null;
    [SerializeField]
    private Image TargetImage = null;
    [SerializeField]
    private Image SelectedModifierImage = null;

    public void SetState(InventoryItem target, List<InventoryItem> modifiers, int currentlySelected)
    {
        if (TargetImage == null)
            return;

        TargetImage.sprite = target.sprite;

        SelectedModifierImage.sprite =
            InventoryManager.Instance.GetModifierById(currentlySelected).sprite;
    }

    public void Disable()
    {
        ParentObject.SetActive(false);
    }

    public void Enable()
    {
        ParentObject.SetActive(true);
    }
}
