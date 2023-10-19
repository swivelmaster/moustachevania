using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryItemData", menuName = "Moustachevania/Create Inventory Item Data", order = 2)]
public class InventoryItem : ScriptableObject, IComparable<InventoryItem>
{
    [SerializeField]
    InventoryItemType ItemType = InventoryItemType.AdjustableTarget;
    [SerializeField]
    int ItemId = 0;

    [Header("Type-specific settings")]
    [SerializeField]
    ClothingPickupType PickupType = ClothingPickupType.AddJump;
    [SerializeField]
    AdjustableObjectTargetType TargetType = AdjustableObjectTargetType.Circle;
    [SerializeField]
    AdjustableObjectModifierType ModifierType = AdjustableObjectModifierType.Bouncy;
    [SerializeField]
    AdjustableObjectEnhancerType EnhancerType = AdjustableObjectEnhancerType.Double;

    [Header("Pickup/cutscene/inventory data")]
    [SerializeField]
    string Name = "";
    [SerializeField]
    Sprite ItemSprite = null;
    [SerializeField]
    Sprite AlternateInWorldSprite = null;
    [SerializeField]
    bool UseAlternateInWorldSprite = false;
    [SerializeField]
    Sprite ActivatedSprite = null;

    [TextArea]
    [SerializeField]
    string Description = "";


    public InventoryItemType itemType => ItemType;
    public int itemId => ItemId;
    public ClothingPickupType pickupType => PickupType;
    public AdjustableObjectTargetType targetType => TargetType;
    public AdjustableObjectModifierType modifierType => ModifierType;
    public AdjustableObjectEnhancerType enhancerType => EnhancerType;

    public string itemName => Name;
    public Sprite sprite => ItemSprite;
    public string description => Description;

    public Sprite alternateInWorldSprite => AlternateInWorldSprite;
    public bool usealternateInWorldSprite => UseAlternateInWorldSprite;
    public Sprite activatedSprite => ActivatedSprite;

    public bool IsEmptyItem() => itemId == 0;

    public int CompareTo(InventoryItem other)
    {
        return itemId.CompareTo(other.itemId);
    }
}