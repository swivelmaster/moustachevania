using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PersistenceManager
{
    public static PersistenceManager Instance;

    public SavedPlayerState currentSavedPlayerState;

    [HideInInspector]
    public SavedGameState savedGame { private set; get; }

    [HideInInspector]
    public List<IResetable> objectsToResetOnDeath = new List<IResetable>();

    float timeSinceLastSave = 0;

    public bool overrideAllSwitchesOn = false;

    public static string RedResetSphereUUID;
    public static string GreenResetSphereUUID;
    public static string BlueResetSphereUUID;

    // Loads save game
    public void Init()
    {
        // This will automatically return an empty state if it's been deleted or never saved before.
        // Don't care about new game in this context - that's checked manually from
        // the menu screen.
        savedGame = SavedGameState.LoadGameState(out _);

        timeSinceLastSave = GameplayManager.Instance.GameTime;

        Instance = this;
    }

    public void ResetStateForCheckpoint()
    {
        foreach (Destroyable d in Destroyable.DestroyedSinceLastCheckpoint)
        {
            d.Restore();
        }

        Destroyable.DestroyedSinceLastCheckpoint = new List<Destroyable>();

        foreach (IResetable resetable in objectsToResetOnDeath)
        {
            resetable.ResetFromCheckpoint();
        }

        foreach (CheesePickup cheese in CheesePickup.pickedUpSinceLastCheckpoint)
        {
            cheese.ResetFromCheckpoint();
            // Ultimately won't be using this so don't worry
            // about the singleton usage here.
            CollectibleManager.Instance.DecrementScore();
        }

        CheesePickup.pickedUpSinceLastCheckpoint = new List<CheesePickup>();

        PermanentTriggeredMovement.ResetAllFromCheckpoint();

        ResetSphere.ResetAll();

        InventoryManager.Instance.unsavedInventory.Clear();

        DialogueVariableStorageBridge.unsavedVariables.Clear();
    }

    public bool CheckpointReached(Checkpoint checkpointReached, SavedPlayerState state)
    {
        bool changed = false;

        foreach (Destroyable d in Destroyable.DestroyedSinceLastCheckpoint)
        {
            changed = true;
            savedGame.FlaggedIds.Add(d.UniqueId);
        }

        Destroyable.DestroyedSinceLastCheckpoint.Clear();

        foreach (CheesePickup cheese in CheesePickup.pickedUpSinceLastCheckpoint)
        {
            changed = true;
            savedGame.FlaggedIds.Add(cheese.GetComponent<UniqueId>().uniqueId);
        }

        CheesePickup.pickedUpSinceLastCheckpoint.Clear();

        // This order is important - these objects saved into their own List check checkpoint reached is called.
        PermanentTriggeredMovement.CheckpointReached();

        foreach (PermanentTriggeredMovement triggered in PermanentTriggeredMovement.PTMSavedAtDestination)
        {
            changed = true;
            savedGame.FlaggedIds.Add(triggered.UniqueID);
        }

        foreach (int id in InventoryManager.Instance.unsavedInventory)
        {
            savedGame.InventoryItems.Add(id);
            changed = true;
        }

        if (InventoryManager.Instance.CharmSlots.Length != savedGame.CharmSlots.Length)
        {
            savedGame.CharmSlots = InventoryManager.Instance.CharmSlots;
            changed = true;
        }
        else
        {
            for (int i = 0; i < savedGame.CharmSlots.Length; i++)
            {
                if (InventoryManager.Instance.CharmSlots[i] != savedGame.CharmSlots[i])
                {
                    savedGame.CharmSlots = InventoryManager.Instance.CharmSlots;
                    changed = true;
                    break;
                }
            }
        }

        foreach (var pair in DialogueVariableStorageBridge.unsavedVariables)
        {
            savedGame.DialogueVariables[pair.Key] = pair.Value;
            changed = true;
        }


        savedGame.CurrentCheckpoint = checkpointReached.GetComponent<UniqueId>().uniqueId;
        savedGame.CurrentScene = checkpointReached.gameObject.scene.name;
        savedGame.PlayerState = state;

        savedGame.totalGameTime += GameplayManager.Instance.GameTime - timeSinceLastSave;
        timeSinceLastSave = GameplayManager.Instance.GameTime;

        SavedGameState.SaveGameState(savedGame);

        InventoryManager.Instance.OnSave();

        return changed;
    }

    public void PlayerDied(DeathReasons reason)
    {
        savedGame.deaths += 1;
        switch (reason)
        {
            case DeathReasons.Enemy:
                savedGame.deathByBrownEnemy++;
                break;
            case DeathReasons.IndestructibleEnemy:
                savedGame.deathByBlueEnemy++;
                break;
            case DeathReasons.Lava:
                savedGame.deathsByLava++;
                break;
            case DeathReasons.Squished:
                savedGame.deathBySquish++;
                break;
        }

        savedGame.totalGameTime += GameplayManager.Instance.GameTime - timeSinceLastSave;
        timeSinceLastSave = GameplayManager.Instance.GameTime;

        SavedGameState.SaveGameState(savedGame);
    }

    public void ResetAllStaticCollections()
    {
        CheesePickup.pickedUpSinceLastCheckpoint.Clear();
        Destroyable.DestroyedSinceLastCheckpoint.Clear();
        PermanentTriggeredMovement.TriggeredSinceLastCheckpoint.Clear();
        PermanentTriggeredMovement.ResetAllFromCheckpoint();
        DialogueVariableStorageBridge.unsavedVariables.Clear();
    }

    bool SwitchFound(ResetSphere.SphereColor color)
    {
        string uuid = "";

        switch (color)
        {
            case ResetSphere.SphereColor.Red:
                uuid = RedResetSphereUUID;
                break;
            case ResetSphere.SphereColor.Green:
                uuid = GreenResetSphereUUID;
                break;
            case ResetSphere.SphereColor.Blue:
                uuid = BlueResetSphereUUID;
                break;
        }

        return savedGame.FlaggedIds.Contains(uuid) ||
            Destroyable.DestroyedButNotSavedByUUID(uuid);
    }

    public static bool RedSwitchFound()
    {
#if UNITY_EDITOR
        if (Instance.overrideAllSwitchesOn)
            return true;
#endif

        return Instance.SwitchFound(ResetSphere.SphereColor.Red);
    }

    public static bool GreenSwitchFound()
    {
#if UNITY_EDITOR
        if (Instance.overrideAllSwitchesOn)
            return true;
#endif

        return Instance.SwitchFound(ResetSphere.SphereColor.Green);
    }

    public static bool BlueSwitchFound()
    {
#if UNITY_EDITOR
        if (Instance.overrideAllSwitchesOn)
            return true;
#endif

        return Instance.SwitchFound(ResetSphere.SphereColor.Blue);
    }
}

[System.Serializable]
public class SavedPlayerState
{
    [HideInInspector]
    public float positionX;
    [HideInInspector]
    public float positionY;

    public int maxJumps;
    public bool hasJumpExtended;
    public bool hasDash;
    public bool hasXJump;
    public bool hasDownSmash;
    public bool hasAutomap;

    public bool hasFallJump;

    public SavedPlayerState() { }

    public SavedPlayerState(int maxJumps, bool hasJumpExtended, bool hasDash, bool hasXJump, bool hasDownSmash, bool hasAutomap, bool hasFallJump)
    {
        this.maxJumps = Mathf.Max(1, maxJumps);
        this.hasJumpExtended = hasJumpExtended;
        this.hasDash = hasDash;
        this.hasXJump = hasXJump;
        this.hasDownSmash = hasDownSmash;
        this.hasAutomap = hasAutomap;
        this.hasFallJump = hasFallJump;
    }

    override public string ToString()
    {
        return "Max Jumps: " + this.maxJumps.ToString() +
            " HighJ: " + this.hasJumpExtended.ToString() +
            " Dash: " + this.hasDash.ToString() +
            " XJump: " + this.hasXJump.ToString() +
            " DownDash: " + this.hasDownSmash.ToString() +
            " AutoMap: " + this.hasAutomap.ToString();
    }
}