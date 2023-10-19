using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using CircularBuffer;

public class Player : MonoBehaviour
{
    const int COYOTE_FRAMES = 4;

    [HideInInspector]
    public bool alive = true;

    public GameObject spriteObject;
    [SerializeField]
    PlayerSprite playerSprite = null;

    public static Transform CurrentPlayerTransformInstance;

    PlayerController controller;

    public PlayerAbilityInfo playerAbilityInfo { private set; get; }

    float XJumpChargeStartTime = 0f;
    bool XJumpReady = false;

    int jumpsSinceGrounded = 0;
    int teleportsSinceGrounded = 0;

    bool dashedSinceLastJump = false;
    
    // Time of last dash start
    float dashStartTime = 0f;

    float teleportCountdown;
    // Need public access for VFX purposes
    public Vector2 teleportDestination { private set; get; }
    const float TeleportDuration = .1f;

    // Always start facing right
    [HideInInspector]
    public int facing = 1; // Public for camera

    public bool FollowFast { get; private set; }

    public PlayerAnimator MyAnimation;

    // Keeping this as a local variable AND recording it into the
    // frame action buffer for now. Only record it into the frame
    // action buffer when there's a related action (ie thing was destroyed)
    // because, for instance, the sound effect changes based on that.
    int thingsDestroyedThisSuperjump = 0;

    const float ResetSphereFreezeTime = .25f;
    float ResetSphereFreezeCountdown = 0f;

    const int teleportControlBufferFrames = 5;
    const int postActionControlBufferFrames = 10;

    const float boostPauseDuration = .15f;
    float boostPauseCountdown;

    PlayerManager playerManager;
    PlayerSounds playerSounds;

    [SerializeField]
    private PlayerVFX playerVFX = null;

    PlayerActionState currentActionState;

    CircularBuffer<PlayerFrameState> FrameStateHistory;

    PlayerFrameState CurrentFrame
    {
        get
        {
            return FrameStateHistory[0];
        }
    }

    PlayerFrameState PreviousFrame
    {
        get
        {
            return FrameStateHistory[1];
        }
    }

    public void Init(PlayerManager playerManager)
    {
        this.playerManager = playerManager;

        controller = GetComponent<PlayerController>();

        CurrentPlayerTransformInstance = gameObject.transform;

        playerVFX.Init(this);

        currentActionState = PlayerActionState.Normal;

        FrameStateHistory = new CircularBuffer<PlayerFrameState>(20);
        // Start with two empty so that Current and Previous getters work
        FrameStateHistory.PushFront(new PlayerFrameState(new ControlInputFrame()));
        FrameStateHistory.PushFront(new PlayerFrameState(new ControlInputFrame()));
        FrameStateHistory[0].PreviousFrame = FrameStateHistory[1];

        FrameStateHistory[1].moveCommand = new MoveCommand();
        FrameStateHistory[0].moveCommand = new MoveCommand();
        DetermineFacing(HorizontalInput.Right);

        playerAbilityInfo = new PlayerAbilityInfo();
        // todo: this is a temporary solution
        controller.playerAbilityInfo = playerAbilityInfo;

        playerSounds = new PlayerSounds(this);
    }

    /**
     * bool onActualGroundOnly Pass in true to return true only if the player
     * is on solid ground instead of a platform. Needed in order to know if
     * player can trigger the quick inventory select UI to manipulate
     * adjustable objects.
     */
    public bool getGrounded(bool onActualGroundOnly = false)
    {
        return onActualGroundOnly ? controller.GroundedToGroundOnly : controller.Grounded;
    }

    public void Suspend()
    {
        controller.Suspend();
        MyAnimation.Suspend();
    }

    public void Resume()
    {
        controller.Resume();
        MyAnimation.Resume();
    }

    public void AdvanceFrame(ControlInputFrame input)
    {
        if (!alive)
        {
            playerVFX.PlayerIsDead();
            return;
        }

        MyAnimation.SetAnimationState(CurrentFrame);

        SetCameraFollowState(controller.Grounded);

        playerVFX.AdvanceFrame(CurrentFrame);

        playerSprite.AdvanceFrame(CurrentFrame);
    }

    public void PhysicsStep(ControlInputFrame input)
    {
        if (!alive)
            return;

        UpdateFrameStateHistory(input);

        // Need to do something interesting here
        // If player is dashing, they should be moving horizontally
        // with no input. Important to make this clear when updating collisions.
        // todo: set this value in the dash code and not here, but make sure
        // there are no weird side effects in the player controller.
        // (make sure player controller doesn't do something with horizontal
        // controller input even when the player is dashing!)
        var horizontalInput = CurrentFrame.InputFrame.Horizontal;
        if (PreviousFrame.moveCommand.movementMode == MovementMode.Dash)
        {
            horizontalInput = PreviousFrame.moveCommand.facingDirection;
        }

        controller.UpdateCollisions(horizontalInput, PreviousFrame.StartedJump);

        if (controller.squished)
        {
            BeDead(DeathReasons.Squished);
            return;
        }

        HandleCollisionDependentState();

        // If anything in the control flow ABOVE the normal ProcessInput
        // method call needs to force the movement command to be something
        // in particular, set this to true.
        bool moveCommandSupplied = HandleSpecialStates(input);

        if (!moveCommandSupplied)
        {
            if (currentActionState == PlayerActionState.Normal
                || currentActionState == PlayerActionState.Dashing)
                ProcessInput(input);

            var result = PostProcessActions();

            if (result == FinalStepResult.StartedBoostJump)
                HandleBoostJumpStartPause();
        }

        SetGravityFactor(CurrentFrame.InputFrame);

        DetermineFacing(input.Horizontal);

        CurrentFrame.MoveCommandResult = controller.ExecuteFixedUpdate(CurrentFrame.moveCommand);

        CheckForBreakablesAbove();

        SetFinalFramePhysicsValues();

        playerSounds.PhysicsStep(CurrentFrame);

        if (CurrentFrame.MoveCommandResult.BouncedCollider != null)
            AdjustableObjectManager.Instance.ColliderBounced(CurrentFrame.MoveCommandResult.BouncedCollider);
    }

    private void SetFinalFramePhysicsValues()
    {
        CurrentFrame.ActionState = currentActionState;

        CurrentFrame.HitGroundThisFrame = controller.Grounded && !PreviousFrame.Grounded && CurrentFrame.DashState != PlayerDashState.Dashing;
        CurrentFrame.OverSpeed = controller.CurrentlyOverSpeed;

        // Don't become grounded while dashing to prevent playing the sound and particle effect
        CurrentFrame.Grounded = controller.Grounded && CurrentFrame.DashState != PlayerDashState.Dashing;
        CurrentFrame.HitCeiling = controller.HeadBump;
        CurrentFrame.FallingFast = !controller.Grounded && IsFallingFast(controller.GetVelocity());
        CurrentFrame.Controller_ShouldCancelDash = controller.HitWall;

        CurrentFrame.controllerVelocity = controller.GetVelocity();

        if (!CurrentFrame.CurrentJumpIsSuperJump)
            thingsDestroyedThisSuperjump = 0;
    }

    private bool HandleSpecialStates(ControlInputFrame input)
    {
        // End existing superjump when player stops going up
        if (PreviousFrame.CurrentJumpIsSuperJump)
            CurrentFrame.CurrentJumpIsSuperJump = controller.GetVelocity().y > 0;

        if (currentActionState == PlayerActionState.ResetSphereWaiting)
        {
            ResetSphereFreezeCountdown -= Time.fixedDeltaTime;

            // Check for button inputs
            // Player will freeze in place until timer is up OR
            // jump or dash is pressed
            if (input.Jumping == ControlInputFrame.ButtonState.Down
                || input.Dashing == true
                || ResetSphereFreezeCountdown <= Mathf.Epsilon)
            {
                ResetSphereFreezeCountdown = 0f;
                currentActionState = PlayerActionState.Normal;
            }
            else
            {
                HandleResetSpherePause();
                return true;
            }
        }
        else if (currentActionState == PlayerActionState.Teleporting)
        {
            teleportCountdown -= Time.fixedDeltaTime;
            if (teleportCountdown <= Mathf.Epsilon)
            {
                transform.position = teleportDestination;
                currentActionState = PlayerActionState.Normal;
                CurrentFrame.TeleportExecutedOnThisFrame = true;

                GrabBufferedActionInput(input, teleportControlBufferFrames);
            }
        }
        else if (currentActionState == PlayerActionState.BoostPause)
        {
            boostPauseCountdown -= Time.fixedDeltaTime;
            if (boostPauseCountdown <= Mathf.Epsilon)
            {
                HandleBoostJumpStartPauseEnd();
                // Above method sets up the MoveCommand properly
                // so skip the final step because it's making assumptions
                // that can never be true about this input and then
                // messing with it.
                return true;
            }
        }

        return false;
    }

    void HandleResetSpherePause()
    {
        CurrentFrame.moveCommand.ResetValues();
        CurrentFrame.moveCommand.snapHorizontalAcceleration = true;
        CurrentFrame.moveCommand.horizontalMovement = HorizontalInput.None;
    }

    private void GrabBufferedActionInput(ControlInputFrame input, int frames)
    {
        var frame = PreviousFrame;

        for (int i = frames; i > 0; i--)
        {
            // Don't go backwards all the way to a used action
            if (frame.InputFrame.ActionWasUsed)
                return;

            if (frame.InputFrame.Dashing)
            {
                input.Dashing = true;
                input.ActionWasBuffered = true;
                return;
            }
            else if (frame.InputFrame.Jumping == ControlInputFrame.ButtonState.Down)
            {
                input.Jumping = ControlInputFrame.ButtonState.Down;
                input.ActionWasBuffered = true;
                return;
            }

            frame = frame.PreviousFrame;
            if (frame == null)
                return;
        }
    }

    private void UpdateFrameStateHistory(ControlInputFrame input)
    {
        if (FrameStateHistory.IsFull)
        {
            // Remove reference to previous frame
            // so it can be garbage collected.
            // Otherwise, this causes a very obvious
            // and very preventable memory leak!
            // (Was using a temporary variable here but it was
            // generating garbage, so nope!)
            FrameStateHistory[FrameStateHistory.Size - 1].PreviousFrame = null;
        }

        FrameStateHistory.PushFront(new PlayerFrameState(input));
        FrameStateHistory[0].PreviousFrame = FrameStateHistory[1];
        FrameStateHistory[0].moveCommand = new MoveCommand();
    }

    /// <summary>
    /// Final step before sending to controller
    /// Check for special conditions, modify current frame input vector
    /// Probably will add more later.
    /// </summary>
    /// <returns></returns>
    FinalStepResult PostProcessActions()
    {
        if (currentActionState == PlayerActionState.BoostPause)
            return FinalStepResult.Normal;

        // save typing
        MoveCommand moveCommand = CurrentFrame.moveCommand;
        if (moveCommand.snapHorizontalAcceleration 
            && moveCommand.horizontalMovement == HorizontalInput.None)
        {
            // don't set horizontal movement again
            // Empty if block for clarity, sorry.
        }
        else
        {
            moveCommand.horizontalMovement = CurrentFrame.InputFrame.Horizontal;
        }
        
        moveCommand.verticalInput = CurrentFrame.InputFrame.Vertical;

        if (CurrentFrame.DashState == PlayerDashState.Dashing)
            moveCommand.snapHorizontalAcceleration = true;

        // Special rules when coming out of dash
        if (CurrentFrame.JustCameOutOfDash())
        {
            if (CurrentFrame.StartedJump)
            {
                // Force x velocity to be 0 when jump-canceling dash
                // w/no horizontal input
                if (moveCommand.horizontalMovement == HorizontalInput.None)
                {
                    moveCommand.snapHorizontalAcceleration = true;
                }
                else if (controller.movementSettings.AllowOverspeed)
                {
                    return FinalStepResult.StartedBoostJump;
                }
            }
            // No input when coming out of dash = 0 x velocity
            else if (moveCommand.horizontalMovement == HorizontalInput.None

                // If, when coming out of dash, player pushed opposite direction from dash
                // snap their acceleration.
                || ControllerInputManager.GetHorizontalMovementFromFloat(PreviousFrame.moveCommand.facing)
                    != moveCommand.horizontalMovement)
            {
                moveCommand.snapHorizontalAcceleration = true;
            }
        }

        // Boost the jump height when buffering a jump during a teleport
        // Also boost X velocity if jumping left or right when doing this!
        // (Should feel similar to jump-canceling a dash as above)
        if (controller.movementSettings.AllowOverspeed && CurrentFrame.StartedJump && CurrentFrame.InputFrame.ActionWasBuffered)
        {
            if (moveCommand.horizontalMovement != HorizontalInput.None)
            {
                return FinalStepResult.StartedBoostJump;
            }

            ApplyJumpBonusToMoveCommand(moveCommand);
        }

        // Backtrack through frames to check for end of dash or teleport
        // Retroactively apply boosts
        if (CurrentFrame.StartedJump && (playerAbilityInfo.hasDash || playerAbilityInfo.hasTeleport))
        {
            var frame = PreviousFrame;
            for (int i = postActionControlBufferFrames; i > 0; i--)
            {
                // Found an action that was used before finding a finished teleport
                // or boost, so don't do anything.
                if (frame.InputFrame.ActionWasUsed)
                    break;

                if (frame.JustCameOutOfDash() || frame.TeleportExecutedOnThisFrame)
                {
                    if (controller.movementSettings.AllowOverspeed && moveCommand.horizontalMovement != HorizontalInput.None)
                    {
                        return FinalStepResult.StartedBoostJump;
                    }

                    if (frame.TeleportExecutedOnThisFrame)
                    {
                        ApplyJumpBonusToMoveCommand(moveCommand);
                        break;
                    }
                }

                frame = frame.PreviousFrame;

                // If player starts pushing buttons as soon as the scene loads
                // then frame history might not go back far enough...
                if (frame == null)
                    break;
            }
        }

        if (currentActionState == PlayerActionState.ResetSphereWaiting)
            moveCommand.snapHorizontalAcceleration = true;

        return FinalStepResult.Normal;
    }

    void HandleBoostJumpStartPause()
    {
        currentActionState = PlayerActionState.BoostPause;
        boostPauseCountdown = boostPauseDuration;
        CurrentFrame.moveCommand.ResetValues();
        CurrentFrame.moveCommand.horizontalMovement = HorizontalInput.None;
        CurrentFrame.moveCommand.snapHorizontalAcceleration = true;
        CurrentFrame.StartedJump = false; // Cancel this!

        // Cancel this to prevent the ground hit sound from playing
        if (CurrentFrame.JustCameOutOfDash() && CurrentFrame.Grounded)
            CurrentFrame.HitGroundThisFrame = false;
    }

    void HandleBoostJumpStartPauseEnd()
    {
        CurrentFrame.moveCommand.snapHorizontalAcceleration = true;
        CurrentFrame.moveCommand.movementMode = MovementMode.Boost;

        // Okay, this is interesting. If the player pushed jump during a dash
        // it will trigger the boost. But if the player lets go of the button
        // before the boost pause completes, we shouldn't boost their jump.
        // ie we should only boost it if they're still holding the button down!
        if (CurrentFrame.InputFrame.JumpButtonDownHold)
            CurrentFrame.moveCommand.jumpState = JumpState.ButtonDownBoosted;
        else
            CurrentFrame.moveCommand.jumpState = JumpState.ButtonDown;

        CurrentFrame.moveCommand.horizontalMovement =
            facing >= 0f ? HorizontalInput.Right : HorizontalInput.Left;

        currentActionState = PlayerActionState.Normal;

        // Force this because of the order things happen
        // Otherwise player gets an extra jump
        if (controller.Grounded)
        {
            // Move up slightly instead of dealing with
            // "force not-grounded on next frame" etc.
            // Not 100% sure why jumping after boost
            // pause is getting an extra jump but oh well.
            transform.Translate(new Vector3(0f, .1f, 0f));
            jumpsSinceGrounded++;
        }

        MainCameraPostprocessingEffects.instance.Punch();
    }

    void ApplyJumpBonusToMoveCommand(MoveCommand moveCommand)
    {
        moveCommand.jumpState = JumpState.ButtonDownBoosted;
        //moveCommand.inputVector.y *= controller.movementSettings.TeleportBufferJumpVelocityIncrease;
    }

    void CheckForBreakablesAbove()
    {
        var yVelocity = controller.GetVelocity().y;
        if (CurrentFrame.CurrentJumpIsSuperJump && yVelocity > 0)
            CheckForBreakables(true, yVelocity * Time.fixedDeltaTime * 1.1f); // Add slight fudge factor
    }

    // One-time check for when down-break is enabled, right when super-jump is activated
    void CheckForBreakablesBelow()
    {
        CheckForBreakables(false, .1f);
    }

    void CheckForBreakables(bool above, float distance)
    {
        if (controller.CheckForBreakables(above, distance))
            ReactToBreakable(above);
    }

    void ReactToBreakable(bool above)
    {
        if (above)
        {
            CurrentFrame.DestroyedBreakableThisFrame = true;
            CurrentFrame.ThingsDestroyedThisSuperjump = thingsDestroyedThisSuperjump;
            thingsDestroyedThisSuperjump++;
        }
        else
        {
            CurrentFrame.DestroyedBreakableThisFrame = true;
        }
    }

    void HandleCollisionDependentState()
    {
        // Reset jump state if we're on the ground
        // But only if we're not dashing!
        if (controller.Grounded && CurrentFrame.DashState != PlayerDashState.Dashing)
        {
            jumpsSinceGrounded = 0;
            teleportsSinceGrounded = 0;
            dashedSinceLastJump = false;
        }
    }

    public Collider2D GetPlayerCollider()
    {
        return controller.myCollider;
    }

    public Vector2 getVelocity()
    {
        return controller.GetVelocity();
    }

    void ProcessHorizontalMovement(ControlInputFrame input, bool forceStop = false)
    {
        CurrentFrame.moveCommand.horizontalMovement = forceStop ? HorizontalInput.None : input.Horizontal;
        CurrentFrame.moveCommand.snapHorizontalAcceleration = forceStop;
    }

    /// <summary>
    /// Returns a state instead of bool because of previous existence
    /// of multiple dash types. Leaving it this way in case I decide to
    /// make that happen again.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    PlayerDashState ShouldPlayerDash(ControlInputFrame input)
    {
        if (!input.Dashing || !playerAbilityInfo.hasDash)
            return PlayerDashState.NotDashing;

        if (playerAbilityInfo.hasDash && !dashedSinceLastJump)
            return PlayerDashState.Dashing;

        return PlayerDashState.NotDashing;
    }

    void ProcessInput(ControlInputFrame input)
    {
        // Doesn't depend on anything else :)
        if (input.Swap)
            InventoryManager.Instance.ActivateSwapSlot();

        if (controller.GroundedToGroundOnly)
        {
            CurrentFrame.AOChangedThisFrame = InventoryManager.Instance.ChangeSelectedModifiers(input);
        }

        if (playerAbilityInfo.hasDash &&
            playerAbilityInfo.currentAltAbility == AltAbilityType.Dash)
        {
            bool continueExecution = HandleDashState(input);
            if (!continueExecution)
                return;
        }
        else if (playerAbilityInfo.hasTeleport &&
            playerAbilityInfo.currentAltAbility == AltAbilityType.Teleport)
        {
            bool continueExecution = HandleTeleport(input);
            if (!continueExecution)
                return;
        }

        // First, is user holding "down"?
        // Check for XJump.
        // If so, no horizontal movement if we're grounded because we're charging.
        if (PlayerCanAndShouldChargeXJump(input))
        {
            if (!PreviousFrame.XJumpCharging)
            {
                StartXJumpCharging();
            }
            else
            {
                // Carry state forward to current frame
                CurrentFrame.XJumpCharging = true;

                if (XJumpIsFullyCharged() && !XJumpReady)
                    XJumpStartFullCharge();
            }

            // Still process horizontal movement but force a stop.
            // Doing it this way to ensure the code path is the same.
            ProcessHorizontalMovement(input, true);
        }
        else
        {
            CurrentFrame.XJumpCharging = false;
            ProcessHorizontalMovement(input);
            playerVFX.SetSuperJumpState(SuperJumpField.SuperJumpChargeState.None);
            XJumpReady = false;
        }

        bool justJumped = ProcessJumpInput(input);

        if (justJumped)
        {
            controller.Grounded = false;
            CurrentFrame.StartedJump = true;
            jumpsSinceGrounded++;
            CurrentFrame.InputFrame.ActionWasUsed = true;
        }
    }

    /// <summary>
    /// Handles all dash-related processing - continuing dash, canceling dash,
    /// and initiating dash.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>Returns true if input-processing should continue (presumably
    /// because dash was started or is continuing), false if not.</returns>
    private bool HandleDashState(ControlInputFrame input)
    {
        // Find out if we should do a new dash
        PlayerDashState shouldDashType = ShouldPlayerDash(input);

        // Don't "continue" the dash if the player initiated a new one.
        // Dash initiation happens later
        if (currentActionState == PlayerActionState.Dashing &&
            shouldDashType != PlayerDashState.Dashing)
        {
            if (CurrentlyDashing(input))
            {
                ContinueDash();
                return false;
            }
            else
            {
                // Setting this to Normal by default
                // If the dash was canceled by something that
                // changes state, it should change the state later.
                currentActionState = PlayerActionState.Normal;
                return true;
            }
        }

        // shouldDashType being not NotDashing will trigger this,
        // and it's structured this way so the dashes can interrupt each other.
        // ... which doesn't matter anymore because down dash was removed
        // but at least it's still good? I dunno.
        // HAHA JOKE'S ON ME I'M GONNA ADD MULTI-DASH BACK INTO THE GAME!
        // THIS WAS A GOOD IDEA ALL ALONG SUCKER!!!!
        if (shouldDashType == PlayerDashState.Dashing)
        {
            InitiateDash();
            CurrentFrame.StartedDash = true;
            CurrentFrame.CurrentJumpIsSuperJump = false;
            CurrentFrame.InputFrame.ActionWasUsed = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Handles teleport input, begins teleport if necessary.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>Returns true if no teleport & input processing continues, false
    /// if teleport and further input processing stops.</returns>
    private bool HandleTeleport(ControlInputFrame input)
    {
        if (!input.Dashing || teleportsSinceGrounded >= playerAbilityInfo.maxTeleportsPerJump)
            return true;

        teleportDestination = (Vector2)transform.position +
            new Vector2(this.facing * playerAbilityInfo.teleportDistance, 0f);

        // Check for valid teleport destination
        var resultArray = new Collider2D[1];
        int results = Physics2D.OverlapBoxNonAlloc(
            teleportDestination,
            controller.myCollider.bounds.size, 0f,
            resultArray, controller.AllCollisionsLayer);

        if (results > 0)
        {
            CurrentFrame.TeleportFailedThisFrame = true;
            CurrentFrame.InputFrame.ActionWasUsed = true;
            return true;
        }

        teleportCountdown = TeleportDuration;
        currentActionState = PlayerActionState.Teleporting;

        CurrentFrame.TeleportStartedThisFrame = true;
        CurrentFrame.InputFrame.ActionWasUsed = true;
        teleportsSinceGrounded++;

        return false;
    }

    private bool JumpIsAvailable()
    {
        if (jumpsSinceGrounded == 0)
            return playerAbilityInfo.hasFallJump || RecentlyGrounded(COYOTE_FRAMES);

        return jumpsSinceGrounded < playerAbilityInfo.maxJumps;
    }

    private bool RecentlyGrounded(int frames)
    {
        var frame = CurrentFrame;
        while (frames >= 0)
        {
            // Not enough history, default to true
            if (frame == null)
                return true;

            if (frame.Grounded)
                return true;

            frame = frame.PreviousFrame;
            frames--;
        }

        return false;
    }

    // Returns true if player should jump.
    // This sounds simple but there's a bunch of specific calculations going on here
    private bool ProcessJumpInput(ControlInputFrame input)
    {
        bool up = input.Jumping == ControlInputFrame.ButtonState.Up && !controller.Grounded;

        if (/**playerAbilityInfo.hasJumpExtended && **/ up
            && controller.GetVelocity().y >= controller.movementSettings.WeakJumpVelocity)
        {
            CurrentFrame.moveCommand.jumpState = JumpState.ButtonUp;
            return false;
        }

        bool down = input.Jumping == ControlInputFrame.ButtonState.Down;

        if (!down || !JumpIsAvailable())
            return false;

        if (XJumpReady)
        {
            CurrentFrame.moveCommand.jumpState = JumpState.ButtonDownXJump;
            CurrentFrame.CurrentJumpIsSuperJump = true;

            // Play this AND the jump sound! (which triggers elsewhere)
            SoundEffects.instance.xJumpComplete.Play();
            playerVFX.SetSuperJumpState(SuperJumpField.SuperJumpChargeState.None);

            return true;
        }

        CurrentFrame.moveCommand.jumpState = JumpState.ButtonDown;
        CurrentFrame.CurrentJumpIsSuperJump = false;
        return true;
    }

    private bool PlayerCanAndShouldChargeXJump(ControlInputFrame input)
    {
        return playerAbilityInfo.hasXJump && input.RawVertical < 0 && controller.Grounded;
    }

    private bool XJumpIsFullyCharged()
    {
        return GameplayManager.Instance.GameTime > XJumpChargeStartTime + playerAbilityInfo.XJumpChargeGoalTime;
    }

    private void XJumpStartFullCharge()
    {
        XJumpReady = true;

        playerVFX.SetSuperJumpState(SuperJumpField.SuperJumpChargeState.Ready);

        SoundEffects.instance.xJumpStart.Stop();
        SoundEffects.instance.xJumpReady.Play();

        // So this wasn't originally how I planned on doing this.
        // It was going to be "get to super jump charge and then jump
        // to activate the down smash."
        // but... I put the code here by accident
        // and then tried it out
        // and well, it was kinda funny the way it worked out.
        // So I just left it this way.

        // UPDATE: Down smash was removed from the game and 
        // I haven't tested it in forever so who knows what
        // happens?
        if (playerAbilityInfo.hasDownSmash)
            CheckForBreakablesBelow();
    }

    private void StartXJumpCharging()
    {
        // We weren't before, so now we're starting
        CurrentFrame.XJumpCharging = true;
        XJumpChargeStartTime = GameplayManager.Instance.GameTime;
        playerVFX.SetSuperJumpState(SuperJumpField.SuperJumpChargeState.Charging);
        if (!SoundEffects.instance.xJumpStart.isPlaying())
        {
            SoundEffects.instance.xJumpStart.Play();
        }
    }

    // A lot of things have to happen here!
    // We need to end the current dash if there is one, stop player motion for
    // a fraction of a second, and reset dash and jumps.
    void HitResetSphere()
    {
        ResetSphereFreezeCountdown = ResetSphereFreezeTime;
        jumpsSinceGrounded = 0;
        ForceStopDashIfDashing();
        dashedSinceLastJump = false;
        teleportsSinceGrounded = 0;

        // Doing this last so that, when debugging, state still
        // reads as Dashing when we're stopping the dash.
        currentActionState = PlayerActionState.ResetSphereWaiting;
    }

    bool IsFallingFast(Vector2 currentVelocity)
    {
        // Adjusted so that a regular jump / fall doesn't count as falling fast
        // at the end.
        return currentVelocity.y <= -13.5f;
    }

    public int JumpsRemaining()
    {
        return playerAbilityInfo.maxJumps - jumpsSinceGrounded;
    }

    public bool CanDash()
    {
        return !dashedSinceLastJump;
    }

    void SetCameraFollowState(bool grounded)
    {
        // Follow fast when dashing.
        if (PlayerDashState.Dashing == CurrentFrame.DashState || CurrentFrame.CurrentJumpIsSuperJump)
        {
            FollowFast = true;
        }
        else
        {
            FollowFast = false;
        }
    }

    void DetermineFacing(HorizontalInput horizontal)
    {
        // Start with default value in move command
        // (Can't leave as zero because we multiplay facing * various
        // values to get velocity later)
        CurrentFrame.moveCommand.facing = PreviousFrame.moveCommand.facing;

        // Only change facing during these states
        if (currentActionState != PlayerActionState.Normal
            && currentActionState != PlayerActionState.ResetSphereWaiting
            && currentActionState != PlayerActionState.BoostPause)
            return;

        // Only change this value if the player has done some input, otherwise leave it at the last value.
        if (horizontal == HorizontalInput.None)
            return;

        playerSprite.SetFlip(HorizontalInput.Right == horizontal);
        playerVFX.SetDashFieldFacing(HorizontalInput.Left == horizontal);
        MyAnimation.SetFlip(HorizontalInput.Right == horizontal);

        // Used for dash
        facing = HorizontalInput.Left == horizontal ? -1 : 1;
        CurrentFrame.moveCommand.facing = facing;
    }

    void SetGravityFactor(ControlInputFrame input)
    {
        // Don't apply gravity while dashing or dead!
        if (CurrentFrame.DashState == PlayerDashState.Dashing ||
            !alive ||
            currentActionState == PlayerActionState.ResetSphereWaiting ||
            currentActionState == PlayerActionState.Teleporting ||
            currentActionState == PlayerActionState.BoostPause)
        {
            controller.SetGravityFactor(GravityState.None);
            return;
        }

        // uhhhhh well I guess the default gravity state is NEVER used?
        // like, if we're on the ground, it doesn't matter, and if we're
        // not grounded, it's always going to be something else?
        var factor = GravityState.Default;

        if (!controller.Grounded)
        {

            if (input.Jumping == ControlInputFrame.ButtonState.Up &&
                controller.GetVelocity().y > Mathf.Epsilon)
            {
                // Single-frame throttle, Controller will apply some downward
                // force as well to account for how ineffective this is/was
                factor = GravityState.ReleaseJumpButton;
            }
            // Increase gravity when jump button released or falling down
            // (weird videogame physics)
            else if (!input.JumpButtonDownHold && controller.GetVelocity().y < -Mathf.Epsilon)
            {
                factor = GravityState.Falling;
            }
            else if (input.JumpButtonDownHold)
            {
                factor = GravityState.HoldJumpButton;
            }
        }

        controller.SetGravityFactor(factor);
    }

    void InitiateDash()
    {
        if (dashedSinceLastJump)
            return;

        dashStartTime = GameplayManager.Instance.GameTime;
        CurrentFrame.DashState = PlayerDashState.Dashing;
        currentActionState = PlayerActionState.Dashing;
        dashedSinceLastJump = true;
        ContinueDash();
    }

    void ContinueDash()
    {
        CurrentFrame.DashState = PlayerDashState.Dashing;
        CurrentFrame.moveCommand.movementMode = MovementMode.Dash;
        //CurrentFrame.InputVector = new Vector2 (playerAbilityInfo.dashSpeed * facing, 0); // todo
    }

    bool CurrentlyDashing(ControlInputFrame input)
    {
        if (PreviousFrame.DashState == PlayerDashState.Dashing)
        {
            // Cancel dash if cooldown is over OR if we hit a wall
            // OR we jump-cancel it OR we bounced off of something bouncey
            if ((input.Jumping == ControlInputFrame.ButtonState.Down && jumpsSinceGrounded < playerAbilityInfo.maxJumps) ||
                controller.HitWall || dashStartTime + playerAbilityInfo.dashDuration < GameplayManager.Instance.GameTime ||
                PreviousFrame.MoveCommandResult.DidBounce)
            {
                StopDash();
                return false;
            }

            return true;
        }

        return false;
    }

    void ForceStopDashIfDashing()
    {
        // Do nothing
        if (PreviousFrame.DashState == PlayerDashState.NotDashing
            && CurrentFrame.DashState == PlayerDashState.NotDashing)
            return;

        StopDash();
        playerVFX.SetDashFieldActive(false);
    }

    void StopDash()
    {
        CurrentFrame.DashState = PlayerDashState.NotDashing;
        dashedSinceLastJump = true;
        CurrentFrame.EndedDash = true;
    }

    // Need lava to be a collider and not a trigger so physics objects can collide with it
    // (Dead body pieces are physics objects, need to hit it and then sink down...)
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!alive)
            return;

        if (collision.gameObject.CompareTag(Constants.Tags.Damage))
        {
            BeDead(DeathReasons.Lava);
        }
        else if (collision.gameObject.CompareTag(Constants.Tags.AdjustableObject))
        {
            var ao = AdjustableObjectManager.Instance.GetAdjustableObjectFromGameObject(collision.gameObject);
            if (ao.ShouldDamage())
                BeDead(DeathReasons.Enemy);
        }
    }

    // Died because AjustableObject was rotated into current player position!
    public void DeadFromClip()
    {
        BeDead(DeathReasons.ClipChange);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!alive)
            return;

        GameObject otherGo = other.gameObject;

        ResetSphere resetSphere = otherGo.GetComponent<ResetSphere>();
        if (resetSphere != null)
        {
            if (resetSphere.Collect())
            {
                HitResetSphere();
            }
            return;
        }

        Destroyable item = otherGo.GetComponent<Destroyable>();
        if (item)
        {
            if (item.dead)
            {
                return;
            }
        }

        if (otherGo.CompareTag("Damage"))
        {
            BeDead(DeathReasons.IndestructibleEnemy);
            return;
        }

        if (otherGo.CompareTag("DestroyableEnemy"))
        {
            if (CurrentFrame.DashState != PlayerDashState.NotDashing || CurrentFrame.CurrentJumpIsSuperJump)
            {
                // Only way to hit a destroyable enemy while superjumping is
                // to have hit it with super jump, because we're going up.
                // This allows for an advanced technique where players
                // superjump and then steer into monsters that shouldn't be
                // otherwise accessible, but hey why not?
                // Let's just not make that a required technique.
                if (CurrentFrame.CurrentJumpIsSuperJump)
                {
                    CurrentFrame.DestroyedBreakableThisFrame = true;
                    CurrentFrame.ThingsDestroyedThisSuperjump = thingsDestroyedThisSuperjump;

                    thingsDestroyedThisSuperjump++;
                }
                else
                {
                    CurrentFrame.DestroyedMonsterThisFrame = true;
                }

                item.Destroyed();
            }
            else
            {
                BeDead(DeathReasons.Enemy);
            }

            GameCameraAdapter.instance.Shake();
            MainCameraPostprocessingEffects.instance.Punch();

            return;
        }

        // Was originally going to remove this for the 'fancy'
        // part of the game, but since I'm releasing both versions,
        // here it is!
        if (otherGo.CompareTag("Collectible"))
        {
            CheesePickup collectible = otherGo.GetComponent<CheesePickup>();
            collectible.Collect();
            SoundEffects.instance.cheeseCollect.Play();
            return;
        }
    }

    void BeDead(DeathReasons reason)
    {
        MyAnimation.HideAll();

        alive = false;
        CurrentFrame.DashState = PlayerDashState.NotDashing;
        transform.rotation = new Quaternion(90f, 0f, 0f, 0f);
        playerVFX.SetSuperJumpState(SuperJumpField.SuperJumpChargeState.None);
        playerVFX.SetDashFieldActive(false);

        playerSprite.Hide();

        playerSounds.StopSoundsOnPlayerDeath();

        playerManager.PlayerDied(reason);
    }

    public void PlayerPickedUpClothingInventoryType(ClothingPickupType pickupType)
    {
        switch (pickupType)
        {
            case ClothingPickupType.Dash:
                playerAbilityInfo.hasDash = true;
                break;
            case ClothingPickupType.AddJump:
                playerAbilityInfo.maxJumps = playerAbilityInfo.maxJumps + 1;
                break;
            case ClothingPickupType.HighJump:
                playerAbilityInfo.hasJumpExtended = true;
                break;
            case ClothingPickupType.XJump:
                playerAbilityInfo.hasXJump = true;
                break;
            case ClothingPickupType.AutoMap:
                playerAbilityInfo.hasAutomap = true;
                break;
            case ClothingPickupType.FallJump:
                playerAbilityInfo.hasFallJump = true;
                break;
        }
    }

    public void RestoreState(SavedPlayerState state)
    {
        playerAbilityInfo.RestoreState(state);
    }

    public SavedPlayerState GetState()
    {
        return playerAbilityInfo.GetState(transform.position);
    }

    enum FinalStepResult
    {
        Normal, StartedBoostJump
    }

    public Vector2[] GetPlayerRect()
    {
        return new Vector2[4]
        {
            new Vector2(controller.myCollider.bounds.min.x, controller.myCollider.bounds.max.y),
            new Vector2(controller.myCollider.bounds.max.x, controller.myCollider.bounds.max.y),
            new Vector2(controller.myCollider.bounds.min.x, controller.myCollider.bounds.min.y),
            new Vector2(controller.myCollider.bounds.max.x, controller.myCollider.bounds.min.y)
        };
    }
}

public enum PlayerActionState
{
    Normal, Dashing, ResetSphereWaiting, Teleporting, BoostPause
}