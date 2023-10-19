using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameStateBase : MonoBehaviour
{
    public abstract GameState GetGameState();
    public abstract void FirstSetup(GameStateManager gameStateManager, SceneSettings sceneSettings);

    public virtual void Begin() { }

    public virtual void AdvanceFrame(ControlInputFrame input) { }
    public virtual void PhysicsStep(ControlInputFrame input) { }
    public virtual void LateAdvanceFrame() { }

    // For resuming when now top of stack again
    public virtual void Resume() { }

    // For pausing state when no longer top of stack
    public virtual void Suspend() { }

    // For cleanup when state is removed from stack
    public virtual void End() { }

    // Each state is responsible for checking its input to determine if state should change
    // Implement pause handling here at the base level
    public virtual GameState CheckForStateChange(ControlInputFrame input) {
        if (input.Pause)
            return GameState.Paused;

        return GameState.None;
    }


}
