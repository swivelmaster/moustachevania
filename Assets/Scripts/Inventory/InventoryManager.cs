using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class InventoryManager : MonoBehaviour
{
    public CharmSlot[] CharmSlots { get
        {
            if (_charmSlots == null)
            {
                _charmSlots = new CharmSlot[3];
                _charmSlots[0] = new CharmSlot();
                _charmSlots[1] = new CharmSlot();
                _charmSlots[2] = new CharmSlot();
            }

            return _charmSlots;
        }
    }

    CharmSlot[] _charmSlots;

    [Header("All Possible Inventory Items")]
    [SerializeField]
    private InventoryItem[] AllInventoryItems = new InventoryItem[0];

    public List<InventoryItem> AllInventoryItemsSorted { private set; get; }
    public UnityEvent<CharmChange> onCharmsUpdated = new UnityEvent<CharmChange>();
    public UnityEvent onSwapTargetUpdatedOnly = new UnityEvent();

    Dictionary<int, InventoryItem> IdToTarget;
    Dictionary<int, InventoryItem> IdToModifier;
    Dictionary<int, InventoryItem> IdToEnhancer;
    Dictionary<AdjustableObjectTargetType, InventoryItem> TargetTypeToInventoryItem;

    public int CurrentCharmPosition { private set; get; }
    public int CurrentModifierPosition { private set; get; }

    public static InventoryManager Instance { private set; get; }

    [SerializeField]
    private InventoryItem EmptyInventoryItem = null;
    public InventoryItem emptyInventoryItem { get { return EmptyInventoryItem; } }

    /// <summary>
    /// When the player gains an item, but before they hit a checkpoint,
    /// it goes here. When hitting a checkpoint, it's removed from here
    /// and added to the player save state.
    /// </summary>
    public List<int> unsavedInventory { private set; get; }

    public List<int> CurrentPlayerInventory { get
        {
            var temp = new List<int>(PersistenceManager.Instance.savedGame.InventoryItems);
            temp.AddRange(unsavedInventory);
            return temp;
        }
    }

    private void Awake()
    {
        Instance = this;

        IdToTarget = new Dictionary<int, InventoryItem>();
        IdToModifier = new Dictionary<int, InventoryItem>();
        IdToEnhancer = new Dictionary<int, InventoryItem>();
        TargetTypeToInventoryItem = new Dictionary<AdjustableObjectTargetType, InventoryItem>();

        if (AllInventoryItems == null)
            return;

        AllInventoryItemsSorted = new List<InventoryItem>(AllInventoryItems);
        AllInventoryItemsSorted.Add(emptyInventoryItem);
        AllInventoryItemsSorted.Sort();

        foreach (var item in AllInventoryItemsSorted)
        {
            switch (item.itemType)
            {
                case InventoryItemType.AdjustableTarget:
                    IdToTarget[item.itemId] = item;
                    TargetTypeToInventoryItem[item.targetType] = item;
                    break;
                case InventoryItemType.AdjustableModifier:
                    IdToModifier[item.itemId] = item;
                    break;
                case InventoryItemType.AdjustableEnhancer:
                    IdToEnhancer[item.itemId] = item;
                    break;
            }
        }

        unsavedInventory = new List<int>();
    }

    public void InitCharmSlotsFromSaveData(CharmSlot[] slots)
    {
        for (int i=0;i<slots.Length;i++)
        {
            if (slots[i] == null)
                continue;

            CharmSlots[i] = slots[i];
        }

        onCharmsUpdated.Invoke(new CharmChange(true, true, true));
    }

    public void PlayerGotInventoryItem(int itemId)
    {
        // todo: Make this work properly once we're not in the demo anymore
        // Right now it just adds the target item to the specific target slots
        // Not sure if we'll keep the idea of swapping the targets or keep
        // this rigid approach. It's certainly easier for the player to manage.
        if (itemId == 4)
        {
            CharmSlots[0].TargetId = itemId;
        }
        else if (itemId == 9)
        {
            CharmSlots[1].TargetId = itemId;
        }
        else if (itemId == 10)
        {
            CharmSlots[2].TargetId = itemId;
        }

        unsavedInventory.Add(itemId);

        onCharmsUpdated.Invoke(new CharmChange(true, true, true));
    }

    public void RemoveItemFromInventory(int itemId)
    {
        unsavedInventory.Remove(itemId);
        // Manipulating this directly/manually is generally not a good idea.
        // todo: If removing items actually becomes a part of the game for some
        // reason, evaluate whether or not this needs to be re-written.
        // Right now it's only built for editing/debugging
        PersistenceManager.Instance.savedGame.InventoryItems.Remove(itemId);

        switch (GetInventoryItemDataById(itemId).itemType)
        {
            case InventoryItemType.AdjustableTarget:
                foreach (var slot in CharmSlots)
                    if (slot.TargetId == itemId)
                        slot.TargetId = 0;
                break;
            case InventoryItemType.AdjustableModifier:
                foreach (var slot in CharmSlots)
                    if (slot.ModifierId == itemId)
                        slot.ModifierId = 0;
                break;
            default:
                Debug.LogError("Removing item of this type from inventory is not yet supported.");
                break;
        }

        onCharmsUpdated.Invoke(new CharmChange(true, true, true));
    }

    public void OnSave()
    {
        unsavedInventory.Clear();
    }

    // Call this after updating charms from an external source
    // Making this manual to make updating easier and to allow
    // for multiple updates before triggering the AdjustableObjectManager
    // to loop through all adjustable objects and update their modifiers.
    public void DoneUpdatingCharms()
    {
        onCharmsUpdated.Invoke(new CharmChange(true, true, true));
    }

    public List<InventoryItem> GetAllModifierItems()
    {
        return IdToModifier.Values.ToList();
    }

    public List<InventoryItem> GetAllTargetItems()
    {
        return IdToTarget.Values.ToList();
    }

    public List<InventoryItem> GetAllEnhancerItems()
    {
        return IdToEnhancer.Values.ToList();
    }

    public InventoryItem GetTargetById(int id)
    {
        return IdToTarget[id];
    }

    public InventoryItem GetModifierById(int id)
    {
        return IdToModifier[id];
    }

    public InventoryItem GetTargetByTargetType(AdjustableObjectTargetType type)
    {
        // hack to make this getter work with SquareB, which doesn't have its
        // own pickup since the Square pickup controls both A and B
        if (type == AdjustableObjectTargetType.SquareB)
            return GetTargetByTargetType(AdjustableObjectTargetType.SquareA);

        return TargetTypeToInventoryItem[type];
    }

    public List<InventoryItem> GetPlayerModifierInventory()
    {
        var playerModifierInventory = new List<InventoryItem> { };
        bool addedEmpty = false;
        foreach (int id in CurrentPlayerInventory)
        {
            if (IdToModifier.ContainsKey(id))
            {
                var modifier = IdToModifier[id];
                playerModifierInventory.Add(modifier);

                if (modifier.modifierType == AdjustableObjectModifierType.RotateRight)
                {
                    addedEmpty = true;
                    playerModifierInventory.Add(emptyInventoryItem);
                }       
            }       
        }

        if (!addedEmpty)
            playerModifierInventory.Insert(0, emptyInventoryItem);

        return playerModifierInventory;
    }

    /// <summary>
    /// Called from Player, only if player is Grounded to the actual ground
    /// (not on a platform)
    /// </summary>
    /// <param name="input"></param>
    /// <returns>Whether or not anything changed</returns>
    public bool ChangeSelectedModifiers(ControlInputFrame input)
    {
        // Guarantee that SOMETHING should happen
        // otherwise return
        if (!input.LeftTargetDown && !input.LeftTargetUp
            && !input.RightTargetDown && !input.RightTargetUp)
            return false;

        // No charm target in any slot, do nothing
        if (CharmSlots[0].TargetId == 0)
            return false;

        List<InventoryItem> modifiers = GetPlayerModifierInventory();
        var leftModifier = GetModifierById(CharmSlots[0].ModifierId);
        int leftIndex = modifiers.IndexOf(leftModifier);
        var rightModifier = GetModifierById(CharmSlots[1].ModifierId);
        int rightIndex = modifiers.IndexOf(rightModifier);


        // So it's possible to hit left AND right bumpers at the same
        // time and change both target modifiers.
        // So uhhh... guess we have to account for that in the sound effects below.
        bool changed = false;
        bool leftInteract = false;
        bool rightInteract = false;

        if (input.LeftTargetDown || input.LeftTargetUp)
        {
            changed = UpdateLeftCharm(input, modifiers, leftIndex, rightIndex);
            leftInteract = true;
        }        

        if (CharmSlots[1].TargetId > 0 && (input.RightTargetDown || input.RightTargetUp))
        {
            changed = UpdateRightCharm(input, modifiers, leftIndex, rightIndex);
            rightInteract = true;
        }

        onCharmsUpdated.Invoke(new CharmChange(leftInteract, rightInteract, false));

        if (!changed)
        {
            SoundEffects.instance.adjustableObjectNoChange.Play();
            return false;
        }

        // Really though, this shouldn't be here
        // todo: Put somewhere else?

        // WHAT IS GOING ON HERE?
        // We know at least one modifier changed
        // We need to find out if at least one of them changed
        // to a MODIFIER or if one or both changed to NO MODIFIER
        // because the sound effect is different.

        var newLeftModifier = GetModifierById(CharmSlots[0].ModifierId);
        var newRightModifier = GetModifierById(CharmSlots[1].ModifierId);

        bool eitherChangeWasNotNothing = false;
        if (leftModifier != newLeftModifier && newLeftModifier != emptyInventoryItem)
            eitherChangeWasNotNothing = true;
        if (rightModifier != newRightModifier && newRightModifier != emptyInventoryItem)
            eitherChangeWasNotNothing = true;

        if (eitherChangeWasNotNothing)
            SoundEffects.instance.adjustableObjectSwitch.Play();
        else
            SoundEffects.instance.adjustableObjectOff.Play();

        MainCameraPostprocessingEffects.instance.Punch();

        return true;
    }

    bool UpdateLeftCharm(ControlInputFrame input, List<InventoryItem> modifiers, int currentLeftIndex, int currentRightIndex)
    {
        int newIndex = currentLeftIndex;

        if (input.LeftTargetDown)
        {
            newIndex = GetNewModifierIndex(modifiers, currentLeftIndex - 1, -1, currentRightIndex);
            CharmSlots[0].ModifierId = modifiers[newIndex].itemId;
        }
        else if (input.LeftTargetUp)
        {
            newIndex = GetNewModifierIndex(modifiers, currentLeftIndex + 1, 1, currentRightIndex);
            CharmSlots[0].ModifierId = modifiers[newIndex].itemId;
        }

        return newIndex != currentLeftIndex;
    }

    bool UpdateRightCharm(ControlInputFrame input, List<InventoryItem> modifiers, int currentLeftIndex, int currentRightIndex)
    {
        int newIndex = currentRightIndex;

        if (input.RightTargetDown)
        {
            newIndex = GetNewModifierIndex(modifiers, currentRightIndex - 1, -1, currentLeftIndex);
            CharmSlots[1].ModifierId = modifiers[newIndex].itemId;
        }
        else if (input.RightTargetUp)
        {
            newIndex = GetNewModifierIndex(modifiers, currentRightIndex + 1, 1, currentLeftIndex);
            CharmSlots[1].ModifierId = modifiers[newIndex].itemId;
        }

        return newIndex != currentRightIndex;
    }


    /// <summary>
    /// What's the point of this? We need to cycle through a list, moving backwards
    /// and forwards, skipping the id of a modifier equipped by another slot,
    /// but not if it's 0 because either slot can use (have) the empty item.
    /// </summary>
    /// <param name="modifiers"></param>
    /// <param name="currentIndex"></param>
    /// <param name="skipIndex"></param>
    /// <returns></returns>
    int GetNewModifierIndex(List<InventoryItem> modifiers, int nextIndex, int direction, int skipIndex)
    {
        int newIndex = GameUtils.UpdateCollectionCursor(modifiers, nextIndex, true);
        // Empty inventory item location changes depending if we have
        // the rotate right item right now, so check for index of it
        // Used to be checking if newIndex was 0, oh well
        if (newIndex == modifiers.IndexOf(emptyInventoryItem) || newIndex != skipIndex)
            return newIndex;

        var nextTry = GameUtils.UpdateCollectionCursor(modifiers, newIndex + direction, true);
        // Return original if trying again lands on the skip
        // Since we can't wrap, without this logic we would hit
        // the end of the list and then stay there anyway
        // even if we landed on skipIndex
        if (nextTry == skipIndex)
            return nextIndex - direction;

        return nextTry;
    }


    List<int> EquippedTargetsList = new List<int>();
    /// <summary>
    /// WARNING: Re-using a return array to save GC, be careful!
    /// </summary>
    /// <returns></returns>
    public List<int> GetEquippedTargets()
    {
        EquippedTargetsList.Clear();
        foreach (CharmSlot slot in CharmSlots)
        {
            if (slot.TargetId != 0)
                EquippedTargetsList.Add(slot.TargetId);
        }

        return EquippedTargetsList;
    }

    List<int> EquippedModifiers = new List<int>();
    /// <summary>
    /// WARNING: Re-using a return array to save GC, be careful!
    /// </summary>
    /// <returns></returns>
    public List<int> GetEquippedModifiers()
    {
        EquippedModifiers.Clear();
        foreach (CharmSlot slot in CharmSlots)
        {
            if (slot.ModifierId != 0)
                EquippedModifiers.Add(slot.ModifierId);
        }

        return EquippedModifiers;
    }

    List<int> EquippedEnhancers = new List<int>();
    /// <summary>
    /// WARNING: Re-using a return array to save GC, be careful!
    /// </summary>
    /// <returns></returns>
    public List<int> GetEquippedEnhancers()
    {
        EquippedEnhancers.Clear();
        foreach (CharmSlot charmSlot in CharmSlots)
        {
            if (charmSlot.EnhancerId != 0)
                EquippedEnhancers.Add(charmSlot.EnhancerId);
        }

        return EquippedEnhancers;
    }

    void PopulateDummyInventory()
    {
        foreach (InventoryItem item in AllInventoryItemsSorted)
            if (!CurrentPlayerInventory.Contains(item.itemId))
                unsavedInventory.Add(item.itemId);
    }

    public InventoryItem GetInventoryItemDataById(int id)
    {
        foreach (var item in AllInventoryItemsSorted)
            if (item.itemId == id)
                return item;

        throw new Exception("Tried to get InventoryItemData with invalid id " + id.ToString());
    }

    public void ActivateSwapSlot()
    {
        if (CharmSlots[2].TargetId == 0)
            return;

        CharmSlots[2].Activated = !CharmSlots[2].Activated;
        AdjustableObjectManager.Instance.FlipSquares();
        SoundEffects.instance.adjustableObjectSwitch.Play();
        
        onSwapTargetUpdatedOnly.Invoke();
    }
}

public struct CharmChange
{
    public bool LeftUpdate;
    public bool RghtUpdate;
    public bool SquareUPdate;

    public CharmChange(bool LeftUpdate, bool RightUpdate, bool SquareUpdate)
    {
        this.LeftUpdate = LeftUpdate;
        this.RghtUpdate = RightUpdate;
        this.SquareUPdate = SquareUpdate;
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(InventoryManager))]
class InventoryManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (!Application.isPlaying)
            return;

        var manager = target as InventoryManager;

        EditorGUILayout.LabelField("Inventory Management");

        foreach (var item in manager.AllInventoryItemsSorted)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(item.itemName);

            if (manager.CurrentPlayerInventory.Contains(item.itemId))
            {
                if (GUILayout.Button("Remove"))
                {
                    manager.RemoveItemFromInventory(item.itemId);
                }
            }
            else
            {
                if (GUILayout.Button("Add"))
                {
                    manager.PlayerGotInventoryItem(item.itemId);
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}

#endif

// todo: This is temporary until there's a real inventory system
// for equippable abilities.
public class PlayerAbilityInfo
{
    public bool hasFallJump = false;
    public int maxJumps = 1;
    public bool hasJumpExtended = false;
    public bool hasDash = false;
    public bool hasXJump = false;
    public bool hasDownSmash = false;
    public bool hasAutomap = false;

    public float dashDuration = .4f; // How long does Dash last
    public float dashSpeed = 10.5f; // How fast is dash? originally 7.5

    public float XJumpVelocity = 21f; // was 20f
    public float XJumpChargeGoalTime = 1f;

    public AltAbilityType currentAltAbility = AltAbilityType.Dash;

    public bool hasTeleport = true;
    public float teleportDistance = 3.5f;
    public int maxTeleportsPerJump = 2;

    public void RestoreState(SavedPlayerState state)
    {
        maxJumps = state.maxJumps;
        hasJumpExtended = state.hasJumpExtended;
        hasDash = state.hasDash;
        hasXJump = state.hasXJump;
        hasDownSmash = state.hasDownSmash;
        hasAutomap = state.hasAutomap;
        hasFallJump = state.hasFallJump;
    }

    public SavedPlayerState GetState(Vector2 position)
    {
        SavedPlayerState state = new SavedPlayerState();
        state.hasDash = this.hasDash;
        state.hasJumpExtended = this.hasJumpExtended;
        state.maxJumps = this.maxJumps;
        state.positionX = position.x;
        state.positionY = position.y;
        state.hasXJump = this.hasXJump;
        state.hasDownSmash = this.hasDownSmash;
        state.hasAutomap = this.hasAutomap;
        state.hasFallJump = this.hasFallJump;

        return state;
    }
}

/// <summary>
/// NOT CURRENTLY USED
/// Still using the old set of bools for player save state.
/// Replace with this when using a real inventory system.
/// </summary>
public class Ability
{
    public int id { private set; get; }
    public string name { private set; get; }

    public int maxJumps;
    public bool extendsJump;
    public bool addsDash;
    public bool addsXJump;
    public bool addsDownSmash;
    public bool addsTeleport;

    public float dashDuration;
    public float dashSpeed;
    public float XJumpVelocity;

    public float teleportDistance;
    public int maxTeleportsPerJump;

    public float jumpHeightChange;
    public float terminalVelocityChange;

    public float horizontalMoveSpeedChange;
    public float horizsontalAccelerationChange;
    public float horizontalAccelerationWhileReversingChange;

    public bool enableCancelSpeedBoost;
    public bool enableCancelJumpBoost;

    public int overSpeedFrameCountChange;
}

public enum InventoryItemType
{
    Cheese, Trinket, AdjustableTarget, AdjustableModifier, AdjustableEnhancer, Ability
}

public enum AltAbilityType
{
    Dash, Teleport
}

[Serializable]
public class CharmSlot : IEquatable<CharmSlot>
{
    public int TargetId;
    public int ModifierId;
    public int EnhancerId;
    public bool Activated;

    public InventoryItem Target { get {
            if (TargetId == 0)
                return null;

            return InventoryManager.Instance.GetTargetById(TargetId);
        }
    }

    public InventoryItem Modifier { get
        {
            if (ModifierId == 0)
                return InventoryManager.Instance.emptyInventoryItem;

            return InventoryManager.Instance.GetModifierById(ModifierId);
        }
    }

    public bool Equals(CharmSlot other)
    {
        return TargetId == other.TargetId && ModifierId == other.ModifierId
            && EnhancerId == other.EnhancerId && Activated == other.Activated;
    }
}

[System.Serializable]
public enum ClothingPickupType
{
    HighJump, AddJump, Dash, XJump, Points, DownDash, AutoMap, FallJump
}