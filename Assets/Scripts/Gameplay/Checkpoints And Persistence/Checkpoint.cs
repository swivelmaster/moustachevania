using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour {

	public ParticleSystem particleSystemWhenActive;

	public ParticleSystem particlesWhenReached;

    // Used for incrementing through Checkpoints when cheating
	public int CheckpointId;

	void Awake () {
        CheckpointManager.instance.checkpoints.Add(this);

		if (particleSystemWhenActive)
		{
			particleSystemWhenActive.Stop ();	
		}
	}

	/**
	 * These methods are called by the CheckpointManager when the player enters a checkpoint.
	 */
	public void StartParticleSystem()
	{
		if (particleSystemWhenActive)
		{
			particleSystemWhenActive.Play ();	
		}
	}

	public void StopParticleSystem()
	{
		if (particleSystemWhenActive)
		{
			particleSystemWhenActive.Stop ();
		}
	}
	
	void OnTriggerEnter2D(Collider2D collider)
	{
		if (collider.CompareTag("Player"))
		{
			Player player = collider.GetComponent<Player> ();

			// This avoids an awesome bug where if you die and then fall onto
			// a Checkpoint, you save there.
			if (!player.alive)
				return;

			if (CheckpointManager.instance.CheckpointReached(this, player.GetState()))
			{
				Instantiate(particlesWhenReached, transform);
				SoundEffects.instance.CheckpointReached();
			}
		}
	}
}
