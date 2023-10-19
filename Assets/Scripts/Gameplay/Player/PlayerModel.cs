using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MovementMode
{
    Normal, Boost, Pause, Dash
}

public enum HorizontalInput
{
    None, Left, Right
}

public enum JumpState
{
    None, ButtonDown, ButtonUp, ButtonDownBoosted, ButtonDownXJump
}

public enum VerticalInput
{
    None, Up, Down
}

public enum GravityState
{
    None,
    HoldJumpButton,
    ReleaseJumpButton,
    Falling,
    Default
}

public class PlayerFrameState
{
    public MoveCommand moveCommand;
    public ControlInputFrame InputFrame;
    public PlayerDashState DashState = PlayerDashState.NotDashing;
    public PlayerActionState ActionState = PlayerActionState.Normal;
    public PlayerFrameState PreviousFrame = null;

    public bool Grounded;
    public bool StartedDash;
    public bool StartedJump;
    public bool HitCeiling;
    public bool EndedDash;
    public bool FallingFast;

    public bool CurrentJumpIsSuperJump;

    public bool HitGroundThisFrame;

    public bool XJumpCharging;

    public float ResetSphereFreezeCountdown;

    public Vector2 controllerVelocity;

    public bool OverSpeed;

    public bool DestroyedMonsterThisFrame;

    public int ThingsDestroyedThisSuperjump;
    public bool DestroyedBreakableThisFrame;

    public bool Controller_ShouldCancelDash;

    public bool TeleportStartedThisFrame;
    public bool TeleportFailedThisFrame;
    public bool TeleportExecutedOnThisFrame;

    public bool AOChangedThisFrame;

    /// <summary>
    /// This is set AFTER the MoveCommand for this frame
    /// was sent to the PlayerController to have physics stuff
    /// happen. Initially added to this object so we can
    /// find out if the player bounced off an AO in the previous
    /// frame so we can cancel out of dash (bounce cancels dash)
    /// </summary>
    public MoveCommandResult MoveCommandResult;

    public PlayerFrameState(ControlInputFrame input)
    {
        InputFrame = input;
    }

    public bool JustCameOutOfDash()
    {
        if (PreviousFrame == null)
            return false;

        if (ActionState != PlayerActionState.Dashing
            && PreviousFrame.ActionState == PlayerActionState.Dashing)
            return true;

        return false;
    }

    public bool JustStartedFallingFast()
    {
        return !Grounded && FallingFast && !PreviousFrame.FallingFast;
    }
}

// This was originally going to be more complicated (including a Cooldown)
// but I simplified it. Could use a bool now but decided not to for clarity.
public enum PlayerDashState
{
    NotDashing, Dashing
}