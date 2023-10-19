using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameplayManager : GameStateBase
{
    public static GameplayManager Instance;

    public override GameState GetGameState() { return GameState.Play; }

    public bool Active { private set; get; }

    GameStateManager gameStateManager;

    // Storing this in a state manager because it's a prefab, so... okay fine
    public GameObject CameraModePrefab;

    // These were once static, but they should not be anymore
    // so that when we init the scene, we start these from scratch.
    // Since movement cycles are linked to these numbers, we need
    // them to always start the same when a scene loads.
    public float FixedGameTime { private set; get; }
    public float GameTime { private set; get; }

    // Both of these managers rely on checkpoints in the scene
    // So for now they'll be managed at the scene level by the GameplayManager
    // In the future we'll need to pull them out into a higher level
    // once the game becomes aware of multiple scenes at once
    CheckpointManager checkpointManager;
    CheatManager cheatManager;

    // Not sure how the persistence manager is going to translate to
    // a multi-scene setup so leaving it here for now.
    public PersistenceManager persistenceManager { private set; get; }

    // Keeping this one at the scene level to keep the memory management
    // aspecft simpler ie new manager = no hanging onto references to
    // dead body pieces from a different scene if there are still some
    // around when player transitions to a new scene.
    DeadBodyPieceManager deadBodyPieceManager;

    // Per-scene because it's a monobehavior and we should
    // probably be doing a fresh setup of the player every time.
    PlayerManager playerManager;

    InfoTextController infoTextController;

    SceneSettings sceneSettings;

    ObjectManager objectManager;

    MusicManager musicManager;
    string areaName;
    string areaMusicName;

    // Cheat stuff hooray
    GameObject CameraModeObject;
    CameraMode CameraModeInstanceScript;

    public bool CameraModeEnabled { private set; get; }

    public override void FirstSetup(
        GameStateManager gameStateManager,
        SceneSettings sceneSettings)
    {
        this.gameStateManager = gameStateManager;
        Instance = this;

        cheatManager = new CheatManager();
        persistenceManager = new PersistenceManager();
        deadBodyPieceManager = new DeadBodyPieceManager();

        checkpointManager = new CheckpointManager();
        this.sceneSettings = sceneSettings;
        objectManager = new ObjectManager();
        this.playerManager = sceneSettings.playerManager;
        playerManager.Init(checkpointManager, sceneSettings);

        persistenceManager.Init();

        checkpointManager.Init(
            sceneSettings.checkpointSceneValues,
            persistenceManager,
            deadBodyPieceManager,
            cheatManager,
            sceneSettings
        );

        // Just to be clear...
        CameraModeEnabled = false;

        musicManager = sceneSettings.musicManager;
        areaName = sceneSettings.AreaName;
        areaMusicName = sceneSettings.MusicName;

        sceneSettings.collectibleManager.Init(sceneSettings.cheeseCountHud);
    }

    void GrantAllAbilities()
    {
        checkpointManager.ResetWithAllAbilities();
    }

    void ToggleCameraMode()
    {
        if (CameraModeEnabled)
        {
            Destroy(CameraModeObject);
            // Camera mode enabled so disable it
            playerManager.GetCamera().SetObjectToFollow(
                playerManager.currentPlayer.gameObject
            );
        }
        else
        {
            // Camera mode not enabled so enable it
            CameraModeObject = Instantiate(
                CameraModePrefab,
                playerManager.currentPlayer.transform.position,
                CameraModePrefab.transform.rotation);
            CameraModeInstanceScript = CameraModeObject.GetComponent<CameraMode>();
            playerManager.GetCamera().SetObjectToFollow(CameraModeObject);
        }

        CameraModeEnabled = !CameraModeEnabled;
    }

    public override void Begin()
    {
        GameEventManager.Instance.ToggleCameraMode.AddListener(ToggleCameraMode);
        GameEventManager.Instance.GrantAllAbilities.AddListener(GrantAllAbilities);

        infoTextController = sceneSettings.infoTextController;
        infoTextController.Init(checkpointManager);

        musicManager.StackMusic(musicManager.GetMusicConfigByMusicName(areaMusicName), true);

        checkpointManager.Ready();
        Active = true;

        // Calling this here to ensure execution order but
        // todo: get inventory management into a more consistent state
        sceneSettings.inventoryGameplayView.UpdateView(new CharmChange());


        // This is a hack, I'm sorry
        var eventManager = GameEventManager.Instance;
        Debug.Log(eventManager);
        var gameManager = GameManager.instance;
        Debug.Log(gameManager);
        eventManager.ResetSphereActivated.AddListener(ResetSphereActivated);
    }

    void ResetSphereActivated(ResetSphere.SphereColor color)
    {
        StartCoroutine(ResetSphereNextFrame(color));
    }

    // This is a hack - because the cutscene needs to call out to signify that
    // we're ready to activate the spheres, but the spheres rely on Destroyable,
    // and Destroyable.Destroy doesn't get called until the cutscene ends.
    // So we need to wait a sec to catch up and call this a frame later.
    IEnumerator ResetSphereNextFrame(ResetSphere.SphereColor color)
    {
        yield return null;
        ResetSphere.ResetAll();
    }

    public override void Suspend()
    {
        playerManager.Suspend();
        Active = false;
    }

    public override void Resume()
    {
        playerManager.Resume();
        Active = true;
    }

    public override void End()
    {
        Active = false;
    }

    public override GameState CheckForStateChange(ControlInputFrame input)
    {
        GameState state = base.CheckForStateChange(input);

        if (input.Automap)
        {
            return GameState.Automap;
        }
        else if (input.Pause)
        {
            return GameState.Paused;
        }

        return GameState.None;
    }

    public override void AdvanceFrame(ControlInputFrame input)
    {
        if (CameraModeEnabled)
        {
            if (!input.JumpButtonDownHold)
            {
                DebugOutput.instance.DebugOnUpdate("Camera Mode Enabled", "");
                DebugOutput.instance.DebugOnUpdate("Hold Jump to hide this", "");
            }

            CameraModeInstanceScript.AdvanceFrame(input);
            return;
        }

        GameTime += Time.deltaTime;
        checkpointManager.AdvanceFrame(input);
        objectManager.AdvanceFrame();
        playerManager.AdvanceFrame(input);
        infoTextController.AdvanceFrame();
        AdjustableObjectManager.Instance.AdvanceFrame();

        // Needed for inventory fade out lerp (should only increment
        // during active gameplay, not cutscenes)
        sceneSettings.inventoryGameplayView.AdvanceFrame();
    }

    public override void PhysicsStep(ControlInputFrame input)
    {
        if (CameraModeEnabled)
            return;

        FixedGameTime += Time.fixedDeltaTime;
        objectManager.PhysicsStep();
        playerManager.PhysicsStep(input);
        AdjustableObjectManager.Instance.PhysicsStep();
    }

    public void OnClothingPickup(ClothingPickupType pickupType)
    {
        playerManager.currentPlayer.PlayerPickedUpClothingInventoryType(pickupType);
	}

    public void OnInventoryPickup(int inventoryId)
    {
        InventoryManager.Instance.PlayerGotInventoryItem(inventoryId);
    }

    public override void LateAdvanceFrame()
    {
        GameCameraAdapter.instance.LateAdvanceFrame();
    }

    public void AOBecameClip(AdjustableObject ao)
    {
        playerManager.AOBecameClip(ao);
    }
}
