using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Yarn.Unity;

public class SceneSettings : MonoBehaviour
{
    // Use this to hold values that change from scene to scene
    // Or are in prefabs, but not THIS prefab
    [Header("References")]
    public CheckpointSceneValues checkpointSceneValues;

    public InfoTextController infoTextController;

    public PlayerManager playerManager;

    public InventoryManager inventoryManager;
    public AdjustableObjectManager adjustableObjectManager;

    public PauseScreenController pauseScreenController;

    public GameObject verticalLowerLimit;

    public DialogScreen dialogScreen;

    public MusicManager musicManager;
    public SoundEffects soundEffects;

    public DialogueRunner dialogueRunner;
    public DialogueVariableStorageBridge dialogueVariableStorageBridge;
    public DialogueUIImplementation dialogueUIImplementation;

    public SingleCutsceneObject singleCutsceneObjectManager;

    public CollectibleManager collectibleManager;
    public CheeseCountHud cheeseCountHud;

    public CameraEffects overlayEffects;
    public GameCameraAdapter mainCameraComponent;

    [FormerlySerializedAs("mainCamera")]
    public Camera gameplayCamera;

    public InventoryGameplayView inventoryGameplayView;

    [Header("Scene Settings")]
    public string AreaName;
    public string MusicName;
    public string DefaultCutscene;
    public bool ShowDefaultCutscene;
    public string SeenDefaultCutsceneVariableName;

    [Header("Debug Stuff")]
    public DebugCutsceneSettings Debug_SkipCutscene;
}

public enum DebugCutsceneSettings
{
    None, ForceShow, ForceSkip
}
