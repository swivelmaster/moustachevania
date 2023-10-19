using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetModifierIcon : MonoBehaviour
{
    [SerializeField]
    private Image image = null;
    [SerializeField]
    private RectTransform myRectTransform = null;
    public RectTransform rectTransform { get { return myRectTransform; } }

    public InventoryItem myInventoryItem { private set; get; }

    public void Init(InventoryItem inventoryItem)
    {
        myInventoryItem = inventoryItem;
        image.sprite = inventoryItem.sprite;
    }

    
}
