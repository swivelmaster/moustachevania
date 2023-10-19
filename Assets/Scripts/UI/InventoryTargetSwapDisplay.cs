using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryTargetSwapDisplay : MonoBehaviour
{
    [SerializeField]
    private GameObject ParentContainer = null;
    [SerializeField]
    private Sprite SquareASprite = null;
    [SerializeField]
    private Sprite SquareBSprite = null;
    [SerializeField]
    private Image TargetImage = null;

    public void SetEnabled(bool enabled)
    {
        ParentContainer.SetActive(enabled);
    }

    public void SetTarget(AdjustableObjectTargetType targetType)
    {
        if (targetType == AdjustableObjectTargetType.SquareA)
        {
            TargetImage.sprite = SquareASprite;
        }
        else if (targetType == AdjustableObjectTargetType.SquareB)
        {
            TargetImage.sprite = SquareBSprite;
        }
        else
        {
            Debug.LogWarning("What?");
        }
    }
}
