using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CheckpointManager
{
    [SerializeField]
    PlayerManager playerManager = null;

    // Once we split this out into areas, this method is not going to
    // work AT ALL but sure why not for now...
    public List<Checkpoint> checkpoints = new List<Checkpoint>();

    // Public so we can view form the editor what the current checkpoint is.
    public Checkpoint currentCheckpoint;

    public bool ReadyToRestart = false;

    public static CheckpointManager instance;

    CheckpointSceneValues checkpointSceneValues;
    PersistenceManager persistenceManager;
    DeadBodyPieceManager deadBodyPieceManager;
    CheatManager cheatManager;

    public void Init(
        CheckpointSceneValues checkpointSceneValues,
        PersistenceManager persistenceManager,
        DeadBodyPieceManager deadBodyPieceManager,
        CheatManager cheatManager,
        SceneSettings sceneSettings
        )
    {
        CheckpointManager.instance = this;
        ResetSphere.AllSpheres = new List<ResetSphere>();

        this.checkpointSceneValues = checkpointSceneValues;
        this.persistenceManager = persistenceManager;
        this.deadBodyPieceManager = deadBodyPieceManager;
        this.cheatManager = cheatManager;

        this.playerManager = sceneSettings.playerManager;
    }
    
    public void Ready()
    {
        // Needs to happen after Start() has been called everywhere because
        // checkpoints register themselves at that point.
        checkpoints.Sort(CheckpointSort);

        bool foundCheckpoint = false;

#if UNITY_EDITOR
        if (checkpointSceneValues.DebugPreferredCheckpoint != -1)
        {
            foreach (Checkpoint checkpoint in checkpoints)
            {
                if (checkpoint.CheckpointId == checkpointSceneValues.DebugPreferredCheckpoint)
                {
                    currentCheckpoint = checkpoint;
                    foundCheckpoint = true;
                    DebugOutput.instance.DebugTransient("DEBUG: Starting at checkpoint number " + checkpointSceneValues.DebugPreferredCheckpoint.ToString(), 10f);
                    break;
                }
            }
        }
        // savedGame.currentCheckpoint == null if there was no game or there was an error.
        else // <----- LOOK OUT! Separating an else/if clause with an #endif compiler directive!
#endif
        // We are transitioning from a different area, not loading from scratch
        // Start from this checkpoint instead.
        if (GameManager.pendingAreaTransitionCheckpoint >= 0)
        {
            foreach (Checkpoint checkpoint in checkpoints)
            {
                if (checkpoint.CheckpointId == GameManager.pendingAreaTransitionCheckpoint)
                {
                    currentCheckpoint = checkpoint;
                    foundCheckpoint = true;
                }
            }

            if (!foundCheckpoint)
            {
                Debug.LogError("Error: GameManager.pendingAreaTransitionCheckpoint was "
                    + GameManager.pendingAreaTransitionCheckpoint.ToString()
                    + " but no checkpoint with that id was in this scene!");
            }
        }
        else if (persistenceManager.savedGame.CurrentCheckpoint != null)
        {
            foreach (Checkpoint checkpoint in checkpoints)
            {
                if (checkpoint.GetComponent<UniqueId>().uniqueId == persistenceManager.savedGame.CurrentCheckpoint)
                {
                    currentCheckpoint = checkpoint;
                    foundCheckpoint = true;
                    break;
                }
            }

            if (!foundCheckpoint)
            {
                Debug.LogError("Error: Saved checkpoint not found!");
                currentCheckpoint = checkpointSceneValues.sceneStartCheckpoint;
            }
        }

        // Saved game will be populated with a default saved state even if none was found,
        // so load it.
        if (!foundCheckpoint)
        {
            persistenceManager.currentSavedPlayerState = checkpointSceneValues.startingPlayerState;
        }
        else
        {
            persistenceManager.currentSavedPlayerState = persistenceManager.savedGame.PlayerState;
        }

        if (!foundCheckpoint)
        {
            currentCheckpoint = checkpointSceneValues.sceneStartCheckpoint;
            persistenceManager.ResetAllStaticCollections();
        }

        PostCheckpointVisualChanges();

        CreatePlayerAtCheckpoint();

        GameCameraAdapter.instance.StartGame();
    }

    int CheckpointSort(Checkpoint a, Checkpoint b)
    {
        return a.CheckpointId.CompareTo(b.CheckpointId);
    }

    public void RestartAtLastCheckpoint()
    {
        persistenceManager.ResetStateForCheckpoint();
        CreatePlayerAtCheckpoint();

        deadBodyPieceManager.FadeOutAll();

        SoundEffects.instance.CheckpointReached();

        GameEventManager.Instance.PlayerRestartedAtCheckpoint.Invoke();
    }

    void CreatePlayerAtCheckpoint()
    {
        // Spawn at checkpoint + .1f y so they're not in the floor
        Vector3 spawnLocation = currentCheckpoint.transform.position + new Vector3(0f, .1f, 0f);

        // Debug.Log("Spawning new player at " + spawnLocation.x.ToString() + " / " + spawnLocation.y.ToString());

        var restoreState = persistenceManager.currentSavedPlayerState;

        // todo: Put this in a better place. Kind of a hack to do it from here.
        InventoryManager.Instance.CurrentPlayerInventory.AddRange(persistenceManager.savedGame.InventoryItems);
        InventoryManager.Instance.InitCharmSlotsFromSaveData(persistenceManager.savedGame.CharmSlots);

        playerManager.SpawnPlayerAtCheckpoint(spawnLocation, restoreState, this);

        // If this value is > -1, then we're cheating our progression.
        if (cheatManager.stateProgressionIndexForCheating > -1)
        {
            // Save transform and then update appropriate values
            SavedPlayerState state = cheatManager.stateProgressionForCheating[cheatManager.stateProgressionIndexForCheating];
            state.positionX = playerManager.currentPlayer.transform.position.x;
            state.positionY = playerManager.currentPlayer.transform.position.y;
            playerManager.currentPlayer.RestoreState(state);
        }
    }

    // Returns bool = Whether or not the save state actually changed
    // Use to determine whether to play sfx/vfx on hitting checkpoint
    public bool CheckpointReached(Checkpoint checkpointReached, SavedPlayerState state)
    {
        persistenceManager.currentSavedPlayerState = state;

        bool changed = persistenceManager.CheckpointReached(checkpointReached, state);

        // If we were using the debug feature, we need to clear it here so we don't override later progression with earlier one
        cheatManager.ResetCheat();

        if (checkpointReached != currentCheckpoint || changed)
        {
            currentCheckpoint = checkpointReached;
            PostCheckpointVisualChanges();
            changed = true;
        }

        return changed;
    }

    void PostCheckpointVisualChanges()
    {
        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] == currentCheckpoint)
            {
                checkpoints[i].StartParticleSystem();
            }
            else
            {
                checkpoints[i].StopParticleSystem();
            }
        }
    }

    public void ResetWithAllAbilities()
    {
        cheatManager.SetAbilityProgressionToEnd();
        ForceRestart();
    }

    public void AdvanceFrame(ControlInputFrame input)
    {
        bool forceRestart = false;

        if (Debug.isDebugBuild)
        {
            if (input.Cheat_NextCheckpoint)
            {
                Debug.Log("Incrementing checkpoint.");
                cheatManager.CheatToNextCheckpoint(this);
                forceRestart = true;
            }

            cheatManager.AdvanceFrame(input);
        }

        if (ReadyToRestart && input.Jumping == ControlInputFrame.ButtonState.Down)
            ForceRestart();

        if (forceRestart)
            ForceRestart();
    }

    public void ForceRestart()
    {
        playerManager.ForceDestroyPlayerObject();
        RestartAtLastCheckpoint();
        ReadyToRestart = false;
        GameCameraAdapter.instance.SnapToPositionQuickly();
    }
}
