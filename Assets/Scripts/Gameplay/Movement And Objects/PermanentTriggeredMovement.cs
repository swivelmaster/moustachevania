using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PermanentTriggeredMovement : TriggeredMovement 
{
	public static List<PermanentTriggeredMovement> TriggeredSinceLastCheckpoint = new List<PermanentTriggeredMovement>();
	public static List<PermanentTriggeredMovement> HitTargetSinceLastCheckpoint = new List<PermanentTriggeredMovement>();

	public static List<PermanentTriggeredMovement> PTMSavedAtDestination = new List<PermanentTriggeredMovement> ();

	public string UniqueID { private set; get; }

	public override void Start()
	{
		base.Start ();

		UniqueID = GetComponent<UniqueId> ().uniqueId;

		if (PersistenceManager.Instance.savedGame.FlaggedIds.Contains(UniqueID))
		{
			transform.position = pointB;
			reachedDestination = true;

			if (debug)
			{
				Debug.Log ("PermanentTriggeredMovement resuming from save state at position " +
					transform.position.ToString () +
					" (reached destination: " + reachedDestination.ToString () + ")");
			}
		}
	}

	public static void CheckpointReached()
	{
		// Deliberately not covering the occasion where an object has been triggered but not reached its destination yet
		// This should not happen anyway!
		// The objects move so quickly that the player shouldn't have time to get to a checkpoint after triggering one
		// but before it reaches its destination trigger.
		foreach (PermanentTriggeredMovement movement in HitTargetSinceLastCheckpoint)
		{
			PTMSavedAtDestination.Add (movement);
		}

		HitTargetSinceLastCheckpoint.Clear ();
	}

	public static void ResetAllFromCheckpoint()
	{
		foreach (PermanentTriggeredMovement movement in TriggeredSinceLastCheckpoint)
		{
			movement.ResetFromCheckpoint ();
		}

		foreach (PermanentTriggeredMovement movement in HitTargetSinceLastCheckpoint)
		{
			movement.ResetFromCheckpoint();
		}
	}

	public override void ResetFromCheckpoint()
	{
		if (debug)
		{
			Debug.Log ("PermamentTriggeredMovement: Resetting from checkpoint. Current position is " + transform.position.ToString () + " and moving to position " + startPosition.ToString());
		}

		reachedDestination = false;
		lastFrameVelocity = new Vector2 (0f, 0f);
		base.ResetFromCheckpoint();
	}

	// Don't register with CheckpointManager, we're handling this a different way
	public override void RegisterToReset()
	{
		return;
	}

	private bool reachedDestination = false;

	public override void Trigger()
	{
		if (debug)
		{
			Debug.Log ("triggered!");
		}

		if (reachedDestination)
		{
			if (debug)
			{
				Debug.Log ("...but reached destination, so doing nothing.");
			}
			return;
		}

		base.Trigger ();
		TriggeredSinceLastCheckpoint.Add (this);
	}

	public override void HitReverseTrigger()
	{
		if (debug)
		{
			Debug.Log ("PermanentTriggeredMovement hit destination so we're stopping.");
		}

		// Don't save progress if player is dead when we hit the final position.
		if (PlayerManager.Instance.currentPlayer.alive)
		{
			TriggeredSinceLastCheckpoint.Remove (this);
			HitTargetSinceLastCheckpoint.Add (this);	
			reachedDestination = true;

			if (debug)
			{
				Debug.Log ("Player is alive so we ARE saving progress.");
			}


		}
		else 
		{
			if (debug)
			{
				Debug.Log ("Player is dead so we're NOT saving progress.");
			}
		}

		triggered = false;
	}



}
