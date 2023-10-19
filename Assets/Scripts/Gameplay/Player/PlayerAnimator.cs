using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerAnimator : MonoBehaviour
{
    public Animator PlayerAnimationController;

    string currentAnimation;

    public void Suspend()
    {
        PlayAnimation(PlayerAnimations.Idle);
    }

    public void Resume()
    {
        PlayAnimation(currentAnimation);
    }

    public void SetFlip(bool flipped)
    {
        // Originally used for flipping staches
    }

    public void PlayAnimation(string animationToPlay)
    {
        currentAnimation = animationToPlay;
        PlayerAnimationController.Play(animationToPlay);
    }

    public void HideAll()
    {
        // originally used for staches
    }

    public void SetAnimationState(PlayerFrameState frameState)
    {
        if (frameState.ActionState == PlayerActionState.ResetSphereWaiting)
        {
            PlayAnimation(PlayerAnimations.FallSubtle);
            return;
        }            

        if (frameState.ResetSphereFreezeCountdown > 0f)
        {
            PlayAnimation(PlayerAnimations.Jump);
            return;
        }

        if (frameState.DashState == PlayerDashState.Dashing)
        {
            PlayAnimation(PlayerAnimations.Dash);
            return;
        }

        if (frameState.Grounded)
        {
            if (frameState.XJumpCharging)
            {
                PlayAnimation(PlayerAnimations.CrouchCharge);
            }
            else if (frameState.moveCommand.horizontalMovement != HorizontalInput.None)
            {
                PlayAnimation(PlayerAnimations.Walk);
            }
            else
            {
                PlayAnimation(PlayerAnimations.Idle);
            }
            return;
        }

        if (frameState.controllerVelocity.y > 0)
        {
            PlayAnimation(PlayerAnimations.Jump);
        }
        else
        {
            if (frameState.FallingFast)
            {
                PlayAnimation(PlayerAnimations.FallPanic);
            }
            else
            {
                PlayAnimation(PlayerAnimations.FallSubtle);
            }
        }
    }
}

// Replacement for not having enums with string values
public class PlayerAnimations
{
    public static string Dash = "JQD Dash";
    public static string Walk = "JQD Walk Cycle";
    public static string Idle = "JQD Idle Frame";
    public static string Jump = "JQD Jump";
    public static string FallSubtle = "JQD Subtle Falling";
    public static string FallPanic = "JQD Falling";
    public static string CrouchCharge = "JQD CrouchCharge";
}