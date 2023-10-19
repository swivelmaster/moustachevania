using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    public Stack<GameStateBase> GameStateStack;

    // Note: I originally included the loading state manager here,
    // but because this is going to be on a per-scene basis and the loading
    // happens between scenes, the loading stuff should be managed
    // by the outer manager that persists between scene loads
    [Header("State-Related Managers")]
    public GameplayManager gameplayManager;
    public PauseScreenManager pauseScreenManager;
    public AutomapManager automapManager;
    public InventoryScreenManager inventoryScreenManager;
    public CutsceneManager cutsceneManager;

    // todo: Maybe centralize sound effects requests into events?
    // That way we don't need to pass the audio manager anywhere
    //AudioManager audioManager;
    ControllerInputManager inputManager;

    [Header("The One True Scene Settings")]
    public SceneSettings sceneSettings;

    [Header("Leave This Blank, Normally")]
    public string queuedCutscene = "";
    Action onCutsceneComplete;

    public GameState CurrentGameState
    {
        get
        {
            return GameStateStack.Peek().GetGameState();
        }
    }

    private void Awake()
    {
        Instance = this;

        GlobalPersistentObjectManager.Instance.GetGlobalManagers(
            //out audioManager,
            out inputManager
        );

        gameplayManager.FirstSetup(this, sceneSettings);
        pauseScreenManager.FirstSetup(this, sceneSettings);
        automapManager.FirstSetup(this, sceneSettings);
        inventoryScreenManager.FirstSetup(this, sceneSettings);

        // CutsceneManager has to be set up last because it depends
        // on PersistenceManager, which is initialized in gameplayManager
        cutsceneManager.FirstSetup(this, sceneSettings);

        GameStateStack = new Stack<GameStateBase>();
    }

    void Start()
    {
        // Clear input on restart because the check to respawn (and subsequent respawn)
        // happen BEFORE the player's AdvanceFrame is called, so if it's passed a non-initialized
        // input, they'll jump.
        // We could actually modify the input in-place because it's a class instance
        // being passed around (instead of a struct, which would be passed by value
        // and thus modifying the return in one place wouldn't affect it in another...)
        // But this is more clear, I think, because we're defining the cause-effect
        // relationship clearly in one place.
        // I don't know, it's all subjective.
        GameEventManager.Instance.PlayerRestartedAtCheckpoint.AddListener(inputManager.InitializeInput);
        GameEventManager.Instance.CutsceneTriggered.AddListener(CutsceneTriggered);
        GameEventManager.Instance.ClothingPickupCutsceneComplete.AddListener(OnClothingPickup);
        GameEventManager.Instance.InventoryItemPickedUp.AddListener(OnInventoryItemPickup);

        GameEventManager.Instance.AOBecameClip.AddListener(gameplayManager.AOBecameClip);

        // Default state, hooray!
        GameStateStack.Push(gameplayManager);
        gameplayManager.Begin();

        bool shouldShowCutscene = cutsceneManager.shouldShowStartingCutscene;

#if UNITY_EDITOR
        shouldShowCutscene = shouldShowCutscene ||
            (sceneSettings.Debug_SkipCutscene == DebugCutsceneSettings.ForceShow);
        shouldShowCutscene = shouldShowCutscene &&
            (sceneSettings.Debug_SkipCutscene != DebugCutsceneSettings.ForceSkip);
#endif

        if (shouldShowCutscene)
        {
            queuedCutscene = sceneSettings.DefaultCutscene;
            
            // Don't play area's default music if we're about to
            // play the default cutscene.
            // Might need to make this a setting later?
            sceneSettings.musicManager.PauseMusic(true);
        }

        // When loading from another scene, we can load the scene in the same frame
        // and pull the input that caused the scene to load (ie hitting the Jump
        // button in the menu to select a game mode and then having the Jump
        // input fall through to the game scene!)
        inputManager.InitializeInput();
    }

    void Update()
    {
        bool consumeInput = CurrentGameState != GameState.Play;

        var input = GetInput(consumeInput);
        var switched = SwitchStateIfNecessary(input);

        // Reset input if state changed
        if (switched)
            input = GetInput(consumeInput);

        AdvanceFrameForCurrentState(input);
    }

    private void FixedUpdate()
    {
        // UI doesn't used input in fixed update
        var input = GetInput(false);
        AdvancePhysicsStepForCurrentState(input);
    }

    private void LateUpdate()
    {
        LateAdvanceFrameForCurrentState();
    }

    ControlInputFrame GetInput(bool consumeInput)
    {
        return inputManager.GetCurrentInput(consumeInput);
    }

    /// <summary>
    /// Switches state before processing the frame in the current state.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>Whether or not the state was changed.
    /// Use that return bool to determine whether to reset the input state
    /// so that inputs don't bleed between states (which would activate and
    /// deactivate a menu on the same frame, for instance!</returns>
    bool SwitchStateIfNecessary(ControlInputFrame input)
    {
        var state = GameStateStack.Peek().CheckForStateChange(input);

        // Tricky stuff here. If we're in gameplay mode and there's
        // a cutscene ready to go, we can stomp on a request to view
        // inventory or automap, but not on a request to pause the game.
        // If we DO pause the game and then unpause, the queued dialogue
        // should persist and will start as soon as we unpause the game.
        // (This happens if the player hits a button the moment the
        // cutscene is triggered!)
        if (queuedCutscene != "")
        {
            if (state != GameState.Paused)
            {
                state = GameState.Cutscene;
            }
        }

        // No change
        if (state == GameState.None)
            return false;

        var top = GameStateStack.Peek();

        if (state == top.GetGameState())
        {
            Debug.LogError("Tried to switch to same state, this should not happen.");
            return false;
        }

        var newState = GetManagerForState(state);

        // From play -> anything = add to stack
        // From anything -> pause = add to stack
        // From other -> other = swap top of stack
        // (other -> other = toggling between automap and quick inventory)
        if (top.GetGameState() == GameState.Play || state == GameState.Paused)
        {
            GameStateStack.Push(newState);
            NewStateAddedToStack(top, newState);
            if (queuedCutscene != "" && newState == cutsceneManager)
            {
                cutsceneManager.ConversationStart(queuedCutscene, this.onCutsceneComplete);
                this.onCutsceneComplete = null;
                queuedCutscene = "";
            }
        }
        else
        {
            GameStateStack.Pop();
            GameStateStack.Push(newState);
            TopOfStackSwapped(top, newState);
        }

        // State changed, clear input so inputs don't leak between states.
        // (For instance, pressing Jump to close a menu should not make
        // the player jump once the next state becomes active. This will
        // happen because the next state is activated in the same frame
        // the previous one was closed.)
        inputManager.InitializeInput();

        return true;
    }

    public void StateClosedItself(GameStateBase state)
    {
        var stateEnum = state.GetGameState();

        if (GameStateStack.Count == 1)
            throw new System.Exception("Only game state on stack attempted to remove itself. Bad." + stateEnum.ToString());

        if (state != GameStateStack.Peek())
            throw new System.Exception("State that wasn't on top of the stack tried to remove itself. Bad." + stateEnum.ToString());

        // Does this always execute in order? Would be crazy if it evaluated the arguments backwards!
        StateRemovedFromStack(GameStateStack.Pop(), GameStateStack.Peek());
    }

    void NewStateAddedToStack(GameStateBase oldState, GameStateBase newState)
    {
        oldState.Suspend();
        newState.Begin();
    }

    void TopOfStackSwapped(GameStateBase oldTop, GameStateBase newTop)
    {
        oldTop.End();
        newTop.Begin();
    }

    void StateRemovedFromStack(GameStateBase toBeRemoved, GameStateBase newlyOnTop)
    {
        toBeRemoved.End();
        newlyOnTop.Resume();
    }

    void AdvanceFrameForCurrentState(ControlInputFrame input)
    {
        GameStateStack.Peek().AdvanceFrame(input);
    }

    void AdvancePhysicsStepForCurrentState(ControlInputFrame input)
    {
        GameStateStack.Peek().PhysicsStep(input);
    }

    void LateAdvanceFrameForCurrentState()
    {
        GameStateStack.Peek().LateAdvanceFrame();
    }

    GameStateBase GetManagerForState(GameState state)
    {
        switch (state)
        {
            case GameState.Automap:
                return automapManager;
            case GameState.Cutscene:
                return cutsceneManager;
            case GameState.InventoryMenu:
                return inventoryScreenManager;
            case GameState.Paused:
                return pauseScreenManager;
            case GameState.Play:
                return gameplayManager;
        }

        throw new System.Exception("Trying to get state for which there is no manager: " + state.ToString());
    }

    public void CutsceneTriggered(string startNodeName, Action onCutsceneComplete)
    {
        queuedCutscene = startNodeName;
        this.onCutsceneComplete = onCutsceneComplete;
    }

    public void OnClothingPickup(ClothingPickupType pickupType)
    {
        gameplayManager.OnClothingPickup(pickupType);
    }

    public void OnInventoryItemPickup(int inventoryId)
    {
        gameplayManager.OnInventoryPickup(inventoryId);
    }

    //public void ActivateTestDialog()
    //{
    //    if (CurrentGameState != GameState.Play)
    //    {
    //        Debug.LogError("Can't activate test dialog if current game state != Play");
    //        return;
    //    }

    //    queuedConversation = new Conversation();
    //    queuedConversation.Dialogs = CutsceneManager.GetTestDialogs();
    //    queuedConversation.MusicName = "FunkyChaunceyTheme";
    //}
}

public enum GameState
{
    Loading, Play, Paused, Automap, InventoryMenu, Cutscene, None
}

#if UNITY_EDITOR
[CustomEditor(typeof(GameStateManager))]
class GameStateManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (!Application.isPlaying)
            return;

        EditorGUILayout.Separator();

        var manager = target as GameStateManager;

        var currentState = manager.GameStateStack.Peek().GetGameState();
        GUILayout.Label("Currently Active: " + currentState.ToString());

        if (currentState == GameState.Play)
        {
            if (GUILayout.Button("Activate Test Cutscene"))
            {
                Debug.LogError("Test dialog is not active right now");
                //manager.ActivateTestDialog();
            }
        }        
    }
}

#endif