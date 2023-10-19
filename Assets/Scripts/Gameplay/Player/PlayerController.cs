using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class PlayerController : PhysicsController {

	const float ONE_PIXEL = 1f / 32f;

	bool alive = true;

	// Movement state - important stuff!
	bool jumpedLastFrame = false;

	int OverspeedFrameCount = 0;
	public bool CurrentlyOverSpeed { get { return OverspeedFrameCount > 0; } }

    [HideInInspector]
	public PlayerMovementSettings movementSettings;

	[HideInInspector]
	public PlayerAbilityInfo playerAbilityInfo;

    public override void Awake()
    {
		base.Awake();

		movementSettings = new PlayerMovementSettings();
		movementSettings.SetToDefaults();
	}

    public override void Die()
    {
        base.Die();
		alive = false;
	}

    /**
     * WHAT IS THIS MALARKEY?
     * We're tracking this value separately from actual velocity because
     * actual x velocity will be affected by standing on top of horizontally
     * moving platforms and will totally screw up movement left and right
     * now that we're supporting acceleration.
     * To be more clear, if the player is on a platform, we want to still
     * respect the ramp up and ramp down speed AND add the platform velocity
     * (this is the more realistic and reasonable way to handle it)...
     * Thus, this effectively tracks how quickly the player is moving on their
     * own independently of how fast they're ACTUALLY moving.
     * It makes things more complicated but it results in the correct behavior.
     */
    Vector2 inputVelocity = new Vector2();

	/// <summary>
    /// Track this to throttle multiple bounces too quickly
    /// </summary>
	int framesSinceBounce = 0;
	const int MIN_FRAMES_BETWEEN_BOUNCE = 3;

	public MoveCommandResult ExecuteFixedUpdate(MoveCommand moveCommand)
	{
		var result = new MoveCommandResult();

		if (!alive)
			return result;

		HandleBoostFrames(moveCommand);

		// If there's no y input, don't set it here - just let the existing values stay
		// This accounts for gravity. This is because jumps need to set the velocity instantly,
		// and then let gravity do its thing.
		// Horizontal acceleration is handled manually in this controller
		var goalVelocity = GetGoalVelocity(moveCommand);

		// todo: With more specific state management, this may not have been necessary!
		// GravityScale will be 0 if we are dashing.
		if (rb.gravityScale == 0f)
		{
			// Need to do this, otherwise last frame y velocity will carry over and we're
			// basically overriding gravity!
			goalVelocity.y = 0f;
		}

		if (moveCommand.isStartOfJump() || rb.gravityScale == 0)
		{
			// Force this if we're jumping or dashing now
			currentPlatform = null;
		}
		else if (currentPlatform != null && !jumpedLastFrame)// && currentPlatform.JustChangedDirection)
		{
			goalVelocity += PreventVerticalPlatformBounce ();
		}

		// This will also do the bouncing
		BoxCollider2D BouncedCollider = null;
		var bounce = CheckForBounceAdjustableObject(out BouncedCollider);
		if (bounce.magnitude > 0f &&
			framesSinceBounce > MIN_FRAMES_BETWEEN_BOUNCE)
        {
			goalVelocity += bounce;
			result.DidBounce = true;
			result.BouncedCollider = BouncedCollider;
			framesSinceBounce = 0;
        }
		else
        {
			framesSinceBounce++;
        }

		// Keeping this an instance variable instead of providing a reference
		// to Current and Previous frame on player object... may or may not
		// have been the right decision.
		// todo: re-evaluate
		jumpedLastFrame = moveCommand.isStartOfJump();

		goalVelocity = manageHorizontalAcceleration(goalVelocity, moveCommand);

		goalVelocity = PreventPlatformOvershoot(goalVelocity, moveCommand);

		inputVelocity = goalVelocity;

		if (currentPlatform != null)
		{
			// Lastly - If we're on a horizontally moving platform, add some force.
			Vector2 velocity = currentPlatform.lastFrameVelocity * (1 / Time.fixedDeltaTime);

            if (velocity.x != 0)
			{
				goalVelocity += velocity;
				//rb.AddForce (velocity, ForceMode2D.Force);	
			}
		}

		// And finally, prevent player from falling too damn fast.
		if (goalVelocity.y < movementSettings.TerminalVelocity)
			goalVelocity.y = movementSettings.TerminalVelocity;

		rb.velocity = goalVelocity;

		if (HitWall)
        {
			// Have to set this because even if we hit a wall,
			// velocity value can be set and won't get reset
			// to 0 during the internal physics step.
			rb.velocity = new Vector2(0f, rb.velocity.y);

			// Have to set this otherwise the next frame's horizontal
			// acceleration will key off of the goal of this frame instead
			// of 0, which means accelerating the opposite direction
			// after hitting a wall will basically start in the negative.
			inputVelocity = rb.velocity;
        }

		PostProcessPosition();

		return result;
	}

	Vector2 GetGoalVelocity(MoveCommand moveCommand)
    {
		return new Vector2(
			GetGoalXVelocity(moveCommand),
			GetGoalYVelocity(moveCommand)
		);
    }

	float GetGoalXVelocity(MoveCommand moveCommand)
    {
		if (moveCommand.horizontalMovement == HorizontalInput.None
			&& moveCommand.movementMode != MovementMode.Dash)
		{
			return 0f;
		}
		else
		{
			// No need to check if Horizontal is left or right here
			// because facing does the trick and is set in Player
			if (
				moveCommand.movementMode == MovementMode.Boost
				|| (
					moveCommand.movementMode == MovementMode.Normal
					&& OverspeedFrameCount > 0
				)
			)
				return movementSettings.DashCancelVelocityBoostMultiplier
					* movementSettings.MaxHorizontalMoveSpeed
					* moveCommand.facing;

			else if (moveCommand.movementMode == MovementMode.Dash)
				return playerAbilityInfo.dashSpeed * moveCommand.facing;

			else
				return movementSettings.MaxHorizontalMoveSpeed
						* moveCommand.facing;
		}
	}

	Vector2 manageHorizontalAcceleration(Vector2 goalVelocity, MoveCommand moveCommand)
    {
		if (moveCommand.snapHorizontalAcceleration || movementSettings.InstantHorizontalAcceleration)
			return goalVelocity;

		var accelerationToUse = movementSettings.HorizontalAcceleration;
		if (OverspeedFrameCount > 0)
        {
			if (Mathf.Abs(goalVelocity.x) < Mathf.Epsilon)
            {
				accelerationToUse = movementSettings.HorizontalAccelerationOverspeedWhenStopping;
            }
			else
            {
				// Only use one acceleration setting when overspeed
				accelerationToUse = movementSettings.HorizontalAccelerationOverspeed;
			}			
		}
		else if (movementSettings.UseAlternateAccelerationWhenReversing
            && Mathf.Abs(goalVelocity.x) > 0f
            && Mathf.Sign(goalVelocity.x) != Mathf.Sign(inputVelocity.x))
        {
			accelerationToUse = movementSettings.HorizontalAccelerationWhenReversing;
        }

		var difference = goalVelocity.x - inputVelocity.x;
		if (Mathf.Abs(difference) >= accelerationToUse)
			return new Vector2(inputVelocity.x + Mathf.Sign(difference) * accelerationToUse, goalVelocity.y);

		return new Vector2(inputVelocity.x + difference, goalVelocity.y);
    }

	RaycastHit2D[] platformOvershootCache = new RaycastHit2D[10];
	/// <summary>
    /// If no input from player - the player wants to just stand on a platform -
    /// check to see if they're going to slide off and prevent them from doing so.
    /// </summary>
    /// <param name="goalVelocity"></param>
    /// <param name="moveCommand"></param>
    /// <returns></returns>
	Vector2 PreventPlatformOvershoot(Vector2 goalVelocity, MoveCommand moveCommand)
    {
        if (Mathf.Abs(goalVelocity.y) > Mathf.Epsilon)
            return goalVelocity;

        if (Mathf.Abs(goalVelocity.x) < Mathf.Epsilon)
            return goalVelocity;

        if (moveCommand.movementMode == MovementMode.Pause
            || moveCommand.movementMode == MovementMode.Dash)
            return goalVelocity;

        if (moveCommand.isStartOfJump())
            return goalVelocity;

        if (moveCommand.horizontalMovement != HorizontalInput.None)
            return goalVelocity;

        if (!Grounded)
            return goalVelocity;

        Array.Clear(platformOvershootCache, 0, 10);

		Vector2 corner = goalVelocity.x > 0f
			? myCollider.bounds.min :
			new Vector3(myCollider.bounds.max.x, myCollider.bounds.min.y);

		Vector2 destination = corner + goalVelocity * Time.fixedDeltaTime;
		Vector2 belowDestination = destination - new Vector2(0f, .1f);
		int contacts = Physics2D.LinecastNonAlloc(
			destination, belowDestination,
			platformOvershootCache, AllCollisionsLayer);

		// Still ground where we're going, do nothing
		if (contacts > 0)
			return goalVelocity;

		// Player would have gone over edge and probably does not want to.
		// Bring them to a complete stop!
		return new Vector2();
    }

	void HandleBoostFrames(MoveCommand moveCommand)
    {
		if (!movementSettings.AllowOverspeed)
        {
			// Funny story: without this line, a player could initiate
			// a boost, unequip the item that allowed it, and then
			// have a boost until the scene changes because the
			// overspeed frame count would never get back to 0!
			OverspeedFrameCount = 0;
			return;
		}
			
		// Boost movement mode is only passed *once* when boost
		// is initiated, so we need to keep track of overspeed
		// frame count here.
		if (moveCommand.movementMode == MovementMode.Boost)
        {
			OverspeedFrameCount = movementSettings.OverspeedFrameCounter;
			return;
        }

		if (OverspeedFrameCount == 0)
			return;

		// No input so decrement overspeed frame count
		// Once it reaches zero, no more speed boost!
		// Also decrement if we are hitting a wall.
		if (moveCommand.horizontalMovement == HorizontalInput.None || HitWall)
        {
			OverspeedFrameCount--;
        }
    }

	float GetGoalYVelocity(MoveCommand moveCommand)
	{
		if (moveCommand.freezeVertical())
			return 0f;

		switch (moveCommand.jumpState)
		{
			case JumpState.ButtonDown:
				return playerAbilityInfo.hasJumpExtended
					? movementSettings.StrongJumpVelocity
					: movementSettings.WeakJumpVelocity;
			case JumpState.ButtonDownBoosted:
				return movementSettings.StrongJumpVelocity *
					movementSettings.TeleportBufferJumpVelocityIncrease;
			case JumpState.ButtonDownXJump:
				return playerAbilityInfo.XJumpVelocity;
			case JumpState.ButtonUp:
				// Jump button up does nothing with little jump
				if (
					playerAbilityInfo.hasJumpExtended
					&& rb.velocity.y > movementSettings.JumpEndForceVelocity
				)
					return movementSettings.JumpEndForceVelocity;
				break;
		}

		return rb.velocity.y;
	}

	/// <summary>
	/// This is where to put any position corrections that make the game
	/// feel more fair. IE if the player dashes towards a platform
	/// but they are .1 units below the top, let's bump them up when
	/// they hit it instead of smashing them against the side.
	/// </summary>
	void PostProcessPosition()
	{
		if (!HitWall && Mathf.Abs(rb.velocity.x) > Mathf.Epsilon)
			PostProcessVerticalPositionForHorizontalVelocity();

        if (rb.velocity.y < -1f)
            PostProcessHorizontalPositionForLedgeGrab();
    }

	/// <summary>
	/// Bumps player up and down by a pixel or two if they're
	/// moving towards a ledge and are about to bump just the their
	/// head or feet.
	/// </summary>
	void PostProcessVerticalPositionForHorizontalVelocity()
	{
		float xPosition = Mathf.Sign(rb.velocity.x) < 0
			? myCollider.bounds.min.x
			: myCollider.bounds.max.x;

		float threshold = .1f;

		// Check feet!
		// hit1 is the very bottom of the hitbox, hit2 is the threshold above it
		var results = new RaycastHit2D[1];
		var velocity = rb.velocity.x * Time.fixedDeltaTime;

		var start = new Vector2(xPosition, myCollider.bounds.min.y);
		var hit1 = Physics2D.LinecastNonAlloc(
			start, start + new Vector2(velocity, 0f),
			results, AllCollisionsLayer);
		var hit2 = Physics2D.LinecastNonAlloc(
			start + new Vector2(0f, threshold),
			start + new Vector2(velocity, threshold),
			results, AllCollisionsLayer);

		// Hit something at the feet but not above, so bump up by threshold
		// todo: ? Only bump up by the minimum amount?
		if (hit1 > 0 && hit2 == 0)
		{
			// This is supposedly an expensive operation due to
			// the need to sync the rb position. Let's see what happens!
			rb.transform.Translate(new Vector3(0f, threshold, 0f));

			// BOTH OF THESE CANNOT HAPPEN AT THE SAME TIME!
			return;
		}

		// Check head!
		start = new Vector2(xPosition, myCollider.bounds.max.y);
		hit1 = Physics2D.LinecastNonAlloc(
			start, start + new Vector2(velocity, 0f),
			results, AllCollisionsLayer);
		hit2 = Physics2D.LinecastNonAlloc(
			start + new Vector2(0f, -threshold),
			start + new Vector2(velocity, -threshold),
			results, AllCollisionsLayer);

		// Hit something at the head but not below, so bump down by threshold
		// Extra .01f because some kind of weird precision issue with physics I guess????
		if (hit1 > 0 && hit2 == 0)
		{
			rb.transform.Translate(new Vector3(0f, -threshold - .01f, 0f));
		}
	}

	RaycastHit2D[] ledgeGrabCheckPoints = new RaycastHit2D[10];
	/// <summary>
    /// If the player is sliding down a wall and then there's a 1 unit
    /// opening, even if they're holding the direction button they might
    /// slide past it anyway. So we need to nudge them slightly to the
    /// left or right so they end up on the ledge.
    /// </summary>
	void PostProcessHorizontalPositionForLedgeGrab()
    {
		var startPoint = rb.velocity.x < 0 ?
			myCollider.bounds.min :
			new Vector3(myCollider.bounds.max.x, myCollider.bounds.min.y);

		float downAmount = rb.velocity.y * Time.deltaTime;

		Array.Clear(ledgeGrabCheckPoints, 0, 10);

		// First, check for nothing directly below
		var count = Physics2D.LinecastNonAlloc(
			startPoint, startPoint + new Vector3(0, downAmount),
			ledgeGrabCheckPoints, AllCollisionsLayer);

        // Something's directly below us, don't continue
        if (count > 0)
			return;

		// Check for wall to the direct right of the bottom right of the
		// collider. We can't use HitWall for this because the player
		// can fall so fast that there aren't any frames with no
		// wall contacts (ie they fall past the gap too quickly)
		count = Physics2D.LinecastNonAlloc(
			startPoint,
			startPoint + new Vector3((rb.velocity.x < 0 ? -1f : 1f) * ONE_PIXEL * 2f, 0f),
			ledgeGrabCheckPoints, AllCollisionsLayer);

		// feet are touching wall, don't continue
		if (count > 0)
			return;

		// Add two pixels! Without adding the pixels, we're just doing
		// the same check that internal physics are doing, and failing to
		// get onto the ledge with.
		// This is because even though we could have been "touching" enough to
		// stop the character from moving to the side before, the actual
		// positions aren't literally bumping each other - there's a threshold.
		// (Note: I tried reducing this threshold in the physics setting but
		// it didn't help when it was about 20% of the original value, and
		// Unity warns against decreasing it too much so uh... this is what we're
		// doing!)
		float xCastDestination = startPoint.x + rb.velocity.x * Time.fixedDeltaTime
			+ ONE_PIXEL * 2f;

		count = Physics2D.LinecastNonAlloc(startPoint,
			new Vector2(xCastDestination, startPoint.y + downAmount),
			ledgeGrabCheckPoints, AllCollisionsLayer);

		if (count == 0)
			return;

		rb.MovePosition(new Vector2(
				rb.position.x + Mathf.Sign(rb.velocity.x) * ONE_PIXEL * 2f,
				rb.position.y));
	}

	public void SetGravityFactor(GravityState gravityState)
    {
		switch (gravityState)
        {
			case GravityState.None:
				rb.gravityScale = 0f;
				break;
			case GravityState.HoldJumpButton:
				rb.gravityScale = movementSettings.JumpButtonDownGravity;
				break;

			// This only happens for one frame but throttles movement speed immediately
			case GravityState.ReleaseJumpButton:
				rb.gravityScale = movementSettings.JumpButtonUpGravity;
				rb.AddForce(Vector2.down * movementSettings.SingleFrameDownwardForceOnJumpRelease, ForceMode2D.Impulse);
				break;
			case GravityState.Falling:
				rb.gravityScale = movementSettings.FallingGravity;
				break;
			case GravityState.Default:
				rb.gravityScale = movementSettings.DefaultGravity;
				break;
        }
    }

	public Vector2 GetVelocity()
	{
		return rb.velocity;
	}
}

[System.Serializable]
public class PlayerMovementSettings
{
	public float JumpButtonUpGravity;
	public float JumpButtonDownGravity;
	public float FallingGravity;
	public float DefaultGravity;

	public float SingleFrameDownwardForceOnJumpRelease;

	public float MaxHorizontalMoveSpeed;

	// Acceleration is units per frame
    public float HorizontalAcceleration;
	public bool InstantHorizontalAcceleration;

	public float HorizontalAccelerationWhenReversing;
	public bool UseAlternateAccelerationWhenReversing;

	public float HorizontalAccelerationOverspeed = 1.5f;
	public float HorizontalAccelerationOverspeedWhenStopping = 2f;

	public float WeakJumpVelocity;
	public float StrongJumpVelocity;
	public float JumpEndForceVelocity;

	public float TerminalVelocity;

	public float DashCancelVelocityBoostMultiplier = 1f;
	public float TeleportBufferJumpVelocityIncrease = 1f;

	public bool AllowOverspeed;

	// Number of frames of no horizontal input before
	// over speed expires
	public int OverspeedFrameCounter;

	public void SetToDefaults()
    {
		FallingGravity = 1.55f;
		JumpButtonDownGravity = 1.25f;
		JumpButtonUpGravity = 2f + 2f + 2f;
		DefaultGravity = 1.25f;
		SingleFrameDownwardForceOnJumpRelease = 1.5f;

		MaxHorizontalMoveSpeed = 4.5f + .2f;
		HorizontalAcceleration = 1f; // Was .75f
		InstantHorizontalAcceleration = false;

		HorizontalAccelerationWhenReversing = 1f;
		UseAlternateAccelerationWhenReversing = true;

		WeakJumpVelocity = 8f;
		StrongJumpVelocity = 11f + 1.5f;
		JumpEndForceVelocity = 6f;

		TerminalVelocity = -11f;

		DashCancelVelocityBoostMultiplier = 1f;// 2f;
		TeleportBufferJumpVelocityIncrease = 1f; // 1.25f;

		AllowOverspeed = false;
		OverspeedFrameCounter = 10;
	}

	public void SetToTesting()
    {
		FallingGravity = 1.55f;
		JumpButtonDownGravity = 1.25f;
		JumpButtonUpGravity = 2f + 2f;
		DefaultGravity = 1.25f;
		SingleFrameDownwardForceOnJumpRelease = 1f;

		MaxHorizontalMoveSpeed = 4.5f + .2f;
		HorizontalAcceleration = .75f;
		InstantHorizontalAcceleration = false;

		HorizontalAccelerationWhenReversing = 1f;
		UseAlternateAccelerationWhenReversing = true;

		WeakJumpVelocity = 8f;
		StrongJumpVelocity = 11f + 1.5f;
		JumpEndForceVelocity = 6f;

		TerminalVelocity = -11f;

		DashCancelVelocityBoostMultiplier = 2f;
		TeleportBufferJumpVelocityIncrease = 1.25f;

		AllowOverspeed = false;
		OverspeedFrameCounter = 10;
	}

	public void SetToMax()
	{
		FallingGravity = 1.55f;
		JumpButtonDownGravity = 1.25f;
		JumpButtonUpGravity = 2f + 2f;
		DefaultGravity = 1.25f;
		SingleFrameDownwardForceOnJumpRelease = 1f;

		MaxHorizontalMoveSpeed = 4.5f + .2f + 2f;
		HorizontalAcceleration = .75f;
		InstantHorizontalAcceleration = false;

		HorizontalAccelerationWhenReversing = 1f;
		UseAlternateAccelerationWhenReversing = true;

		WeakJumpVelocity = 8f;
		StrongJumpVelocity = 11f + 1.5f + 1.5f;
		JumpEndForceVelocity = 6f;

		TerminalVelocity = -11f;

		DashCancelVelocityBoostMultiplier = 2f;
		TeleportBufferJumpVelocityIncrease = 1.25f;

		AllowOverspeed = true;
		OverspeedFrameCounter = 10;
	}
}

public class MoveCommand
{
	public HorizontalInput horizontalMovement;
	public VerticalInput verticalInput;
	public JumpState jumpState;

	public float facing;
	public HorizontalInput facingDirection
	{
		get
		{
			return facing == -1 ? HorizontalInput.Left : HorizontalInput.Right;
		}
	}

	public bool snapHorizontalAcceleration;

	public MovementMode movementMode;

	public bool isStartOfJump()
	{
		return jumpState == JumpState.ButtonDown
			|| jumpState == JumpState.ButtonDownBoosted
			|| jumpState == JumpState.ButtonDownXJump;
	}

	public bool freezeVertical()
    {
		return movementMode == MovementMode.Dash
			|| movementMode == MovementMode.Pause;
    }

	public void ResetValues()
    {
		horizontalMovement = HorizontalInput.None;
		verticalInput = VerticalInput.None;
		jumpState = JumpState.None;
		facing = 0f;
		snapHorizontalAcceleration = false;
		movementMode = MovementMode.Normal;
    }
}

/// <summary>
/// Use this to pass back results to the Player script
/// Which can then... do stuff.
/// </summary>
public struct MoveCommandResult
{
	/// <summary>
    /// Used to determine whether to play the bounce sound
    /// And probably other stuff later
    /// </summary>
	public bool DidBounce;

	/// <summary>
    /// The collider that caused the bounce.
    /// Will be null if no bounce.
    /// </summary>
	public BoxCollider2D BouncedCollider;
}

#if UNITY_EDITOR
[CustomEditor(typeof(PlayerController))]
class PlayerControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

		var controller = target as PlayerController;

		GUILayout.Label("Debug Info:");
		GUILayout.Label("Grounded: " + controller.Grounded);
    }
}

#endif