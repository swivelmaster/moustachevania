using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

public class ControllerInputManager : MonoBehaviour
{
    public bool UseDebugController;
    public ControlInputFrame DebugController;

    ControlInputFrame input;

    Rewired.Player controller;

    bool shouldClear = false;

    bool VerticalLastFrame = false;
    bool HorizontalLastFrame = false;

    private void Start()
    {
        controller = ReInput.players.GetPlayer(0);
        InitializeInput();
    }

    void Update()
    {
        // Okay what's going on here?
        // JUST IN CASE update runs multiple times before FixedUpdate runs,
        // we need to make the values of input CUMULATIVE
        // IE if I reigster a button down in update frame 1, and another
        // button down in update frame 2 but the first button is up,
        // and FixedUpdate doesn't run until after frame 2, BOTH INPUTS
        // NEED TO BE CAPTURED!
        // Note that in-editor this is pretty tightly regulated, but
        // in-build I've seen weird skips where inputs were lost because
        // I wasn't doing this thing.
        if (shouldClear)
        {
            input = new ControlInputFrame();
            shouldClear = false;
        }

        CollectInput();

        // Pause the game so we can manually input controls
        // in the debug controller, which is an inspector thingie
        // that lets us pick control inputs by frame.
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (UseDebugController)
            {
                UseDebugController = false;
            }
            else
            {
                UseDebugController = true;
                Debug.Break();
            }
        }
#endif
    }

    // todo: If an assist mode is added, this will need to be adjusted.
    void FixedUpdate()
    {
        shouldClear = true;
    }

    // This is where the consume param used to live
    // so that UI's could 'consume' the input and take precedent
    // With new state management this *shouldn't* be necessary...
    // But if it turns out we needed it,  put it back here.
    public ControlInputFrame GetCurrentInput(bool consumeInput)
    {
        if (consumeInput)
        {
            var clone = input.Clone();
            input.ClearDownValues();
            return clone;
        }
        else
        {
            return input;
        }
    }

    public void InitializeInput()
    {
        input = new ControlInputFrame();
        input.Dashing = false;
        input.Jumping = ControlInputFrame.ButtonState.None;
        input.RawHorizontal = 0f;

        // Don't reset vertical movement - if player is holding 'down' to charge XJump,
        // skipping a frame could reset the hold and screw them up.
        // AFAIK this is the only time skipping a frame can actively screw up the controls of the game.
        // THIS WAS FIXED by the implementation of the shouldClear flag!
        // input.Vertical = 0f;
    }

    void CollectInput()
    {
        if (UseDebugController)
        {
            input = DebugController.Clone();
            return;
        }

        float x = controller.GetAxis("XAxis");
        input.RawHorizontal = x;

        input.Horizontal = GetHorizontalMovementFromFloat(x);
        if (input.Horizontal == HorizontalInput.None)
        {
            HorizontalLastFrame = false;
        }
        else if (!HorizontalLastFrame)
        {
            // Have to store this outside of the current/last frame
            // system because that stores/discards frames between
            // inputs and consuming them and makes this system fail
            input.HorizontalDownThisFrame = true;
            HorizontalLastFrame = true;
        }

        float y = controller.GetAxis("YAxis");
        if (System.Math.Abs(y) > Mathf.Epsilon)
        {
            input.RawVertical = y;
            input.Vertical = Mathf.Sign(y) == -1f ? VerticalInput.Down : VerticalInput.Up;

            if (!VerticalLastFrame)
            {
                input.VerticalDownThisFrame = true;
                VerticalLastFrame = true;
            }
            //else
            //{
            //    // BEING EXPLICIT SO I DON'T GO INSANE
            //    input.VerticalDownThisFrame = false;
            //    VerticalLastFrame = true;
            //}
        }
        else
        {
            input.Vertical = VerticalInput.None;
            VerticalLastFrame = false;
        }

        //Do NOT default to None here because we could have collected other states before.
        //Unfortunately if the player taps the button very quickly, there might be a chance we lose the ButtonDown input... hmm...
        if (controller.GetButtonDown("Jump"))
        {
            input.Jumping = ControlInputFrame.ButtonState.Down;
        }
        else if (controller.GetButtonUp("Jump"))
        {
            input.Jumping = ControlInputFrame.ButtonState.Up;
        }

        if (controller.GetButtonDown("Dash"))
            input.Dashing = true;

        if (controller.GetButtonDown("Swap"))
            input.Swap = true;

        if (controller.GetButtonDown("Pause"))
            input.Pause = true;

        if (controller.GetButtonDown("IncreaseAbility"))
            input.Cheat_IncreaseAbility = true;

        if (controller.GetButtonDown("NextCheckpoint"))
            input.Cheat_NextCheckpoint = true;

        input.JumpButtonDownHold = controller.GetButton("Jump");

        if (controller.GetButtonDown("LeftTargetUp"))
            input.LeftTargetUp = true;
        if (controller.GetButtonDown("LeftTargetDown"))
            input.LeftTargetDown = true;
        if (controller.GetButtonDown("RightTargetUp"))
            input.RightTargetUp = true;
        if (controller.GetButtonDown("RightTargetDown"))
            input.RightTargetDown = true;
    }

    // Make accessible elsewhere
    public static HorizontalInput GetHorizontalMovementFromFloat(float x)
    {
        if (System.Math.Abs(x) <= Mathf.Epsilon)
        {
            return HorizontalInput.None;
        }

        return Mathf.Sign(x) == -1f ? HorizontalInput.Left : HorizontalInput.Right;
    }
}

[System.Serializable]
public class ControlInputFrame
{
    public ButtonState Jumping;
    public bool Dashing;
    public bool Swap;

    public VerticalInput Vertical;
    public float RawVertical;

    public HorizontalInput Horizontal;
    public float RawHorizontal;

    public bool Automap;

    public bool HorizontalDownThisFrame;
    public bool VerticalDownThisFrame;

    public bool Pause;

    public bool Cheat_IncreaseAbility;
    public bool Cheat_NextCheckpoint;

    public bool JumpButtonDownHold;

    public bool LeftTargetUp;
    public bool LeftTargetDown;

    public bool RightTargetUp;
    public bool RightTargetDown;

    /// <summary>
    /// Mark as true when consuming an action (jump, dash, or teleport)
    /// Needed so actions don't get double-counted for input buffering purposes.
    /// </summary>
    public bool ActionWasUsed;

    /// <summary>
    /// Set this to true when manipulating the ControlInputFrame after its
    /// creation to make sure we know that the action was added later due to
    /// input buffering.
    /// </summary>
    public bool ActionWasBuffered;

    /// <summary>
    /// Clear values that only register the first frame a button is pushed.
    /// Use to avoid double-triggering stuff when... uh... I'm not sure
    /// what's happening.
    /// todo: figure out why this was necessary
    /// </summary>
    public void ClearDownValues()
    {
        HorizontalDownThisFrame = false;
        VerticalDownThisFrame = false;
        Jumping = ButtonState.None;
        Dashing = false;
        Swap = false;
    }

    /// <summary>
    /// Use for the debug instance of this because instances of the input
    /// object are passed around and manipulated and we need a new one
    /// each frame.
    /// </summary>
    /// <returns></returns>
    public ControlInputFrame Clone()
    {
        ControlInputFrame copy = new ControlInputFrame();

        copy.Jumping = Jumping;
        copy.Dashing = Dashing;
        copy.Vertical = Vertical;
        copy.RawVertical = RawVertical;
        copy.Horizontal = Horizontal;
        copy.RawHorizontal = RawHorizontal;
        copy.Automap = Automap;
        copy.HorizontalDownThisFrame = HorizontalDownThisFrame;
        copy.VerticalDownThisFrame = VerticalDownThisFrame;
        copy.Pause = Pause;
        copy.Cheat_IncreaseAbility = Cheat_IncreaseAbility;
        copy.Cheat_NextCheckpoint = Cheat_NextCheckpoint;
        copy.JumpButtonDownHold = JumpButtonDownHold;
        copy.ActionWasUsed = ActionWasUsed;
        copy.ActionWasBuffered = ActionWasBuffered;

        return copy;
    }

    public bool AnyButtonDown()
    {
        return Jumping == ButtonState.Down || Dashing || Swap || Automap
            || Pause
            || LeftTargetDown || LeftTargetUp
            || RightTargetDown || RightTargetUp;
    }

    public enum ButtonState
    {
        None, Down, Up
    }
}