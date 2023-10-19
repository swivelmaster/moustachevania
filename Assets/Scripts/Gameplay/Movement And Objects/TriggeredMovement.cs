using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggeredMovement : BasicMovement, IResetable {

	[SerializeField]
	private TriggeredPlatformAnimator PlatformAnimator;

	protected Vector2 startPosition;
	protected bool triggered = false;

	public override void Start () 
	{
		base.Start ();
		startPosition = transform.position;
		StartCoroutine(LateStart ());
	}
		
	// Need to do this at end of frame to make sure the CheckpointManager instance has been initialized
	IEnumerator LateStart()
	{
		yield return new WaitForEndOfFrame ();
		RegisterToReset ();
	}

	public virtual void RegisterToReset()
	{
		PersistenceManager.Instance.objectsToResetOnDeath.Add (this);
	}

	public virtual void Trigger()
	{
		if (triggered)
			return;

		triggered = true;
		triggeredTime = GameplayManager.Instance.FixedGameTime;	

		// Interesting fix to a really strange bug.
		// If the object is reset from checkpoint during play, then this value
		// will be incorrect on the FIRST frame it's triggered again post-checkpoint,
		// which can result in a player-squish event if the player is in the way
		// of the platform on the first frame of its movement.
		lastPosition = transform.position;
	}

	public virtual void ResetFromCheckpoint()
	{
		if (debug)
		{
			Debug.LogWarning ("I've been reset!");
		}

		triggered = false;
		transform.position = startPosition;
	}
		
	public override void PhysicsStep () {
		if (!triggered)
		{
			lastFrameVelocity = new Vector2(0, 0);
			return;
		}

		base.PhysicsStep();
	}

	public override void AdvanceFrame()
	{
		base.AdvanceFrame();

		if (PlatformAnimator != null)
			PlatformAnimator.AdvanceFrame(isMoving());
	}

}
