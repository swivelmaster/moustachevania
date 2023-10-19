using Rewired.UI.ControlMapper;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseScreenManager : GameStateBase
{
    public override GameState GetGameState() { return GameState.Paused; }

    public ControlMapper controlMapper;

    PauseScreenController pauseScreenController;

    GameStateManager gameStateManager;

    bool controlOptionsMenuActive;

    public override void FirstSetup(GameStateManager gameStateManager, SceneSettings sceneSettings)
    {
        this.gameStateManager = gameStateManager;
        pauseScreenController = sceneSettings.pauseScreenController;
    }

    public override GameState CheckForStateChange(ControlInputFrame input)
    {
        // Can't push a new state from Pause menu, can only pop from stack.
        // So do nothing here.
        return GameState.None;
    }

    public override void Begin()
    {
        pauseScreenController.ShowPauseMenu();
    }

    public override void End()
    {
        pauseScreenController.HidePauseMenu();
    }

    public override void Resume()
    {
        throw new System.Exception("Resuming Pause Menu, which implies that it at some point it wasn't at the top of the stack.");
    }

    public override void AdvanceFrame(ControlInputFrame input)
    {
        if (controlOptionsMenuActive) return;

        float direction = input.RawVertical;

        if (direction != 0 && input.VerticalDownThisFrame)
        {
            if (direction == -1f)
            {
                pauseScreenController.IncrementSelection();
            }
            else if (direction == 1f)
            {
                pauseScreenController.DecrementSelection();
            }
        }

        if (input.Jumping == ControlInputFrame.ButtonState.Down)
        {
            DoPauseMenuAction(pauseScreenController.PauseMenuItems[pauseScreenController.CurrentSelectedItem].MyAction);
        }
        else if (input.Pause)
        {
            gameStateManager.StateClosedItself(this);
        }
    }

    public void DoPauseMenuAction(PauseMenuItem.PauseMenuActions Action)
    {
        switch (Action)
        {
            case PauseMenuItem.PauseMenuActions.Continue:
                this.gameStateManager.StateClosedItself(this);
                break;
            //case PauseMenuItem.PauseMenuActions.RestartDemo:
            //    paused = false;
            //    SavedGameState.DeleteCurrentSaveGame();
            //    SuspendMovement(false);
            //    SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
            //    break;
            case PauseMenuItem.PauseMenuActions.QuitToMenu:
                SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
                break;
            case PauseMenuItem.PauseMenuActions.QuitToDesktop:
                Application.Quit();
                break;
            case PauseMenuItem.PauseMenuActions.CameraModeEnable:
                GameEventManager.Instance.ToggleCameraMode.Invoke();
                break;
            case PauseMenuItem.PauseMenuActions.GrantAllAbilities:
                this.gameStateManager.StateClosedItself(this);
                GameEventManager.Instance.GrantAllAbilities.Invoke();
                break;
            case PauseMenuItem.PauseMenuActions.ShowControlOptions:
                controlOptionsMenuActive = true;
                controlMapper.Open();
                break;
        }
    }

    public void OnCloseControlOptionsMenu()
    {
        StartCoroutine(AndThenRestoreControls());
    }

    // Wait a frame, otherwise close input falls through and the button gets pushed again
    IEnumerator AndThenRestoreControls()
    {
        yield return null;
        controlOptionsMenuActive = false;
    }
}
