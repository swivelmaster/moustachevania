using System.Collections;
using System.Collections.Generic;
using Com.LuisPedroFonseca.ProCamera2D;
using Rewired;
using UnityEngine;

public class DeadBodyPiece : MonoBehaviour 
{
	public ParticleSystem dissolveParticlesPrefab;
	public ParticleSystem hitDarkEnemyParticle;
	public ParticleSystem smallWhiteFlashParticle;
	public ParticleSystem explodeParticle;

	SpriteRenderer sprite;
	float burnDuration = 6f;
	float burnStartTime = 0f;

	Rigidbody2D rb2d;
	Collider2D myCollider;

	float moveSpeed = .1f;

	ParticleSystem myDissolveParticles;

	float fadeStartTime = 0f;
	float fadeDuration = 4.5f;

	public GameObject spawnMarker;

    // Need one of these so we can squish pieces between platforms and walls
    // and not sending shooting THROUGH the walls
	public PlayerController playerController;

    // Flag this when marked to explode later due to Squish death
    // so that FixedUpdate can know to skip the squish check for each piece.
	bool willExplode = false;

	void Start () {
		sprite = GetComponent<SpriteRenderer> ();
		rb2d = GetComponent<Rigidbody2D> ();
		myCollider = GetComponent<Collider2D> ();

		DeadBodyPieceManager.Instance.RegisterDeadBodyPiece (this);
	}

	public void ExplodeAfter(float seconds)
	{
		willExplode = true;
		StartCoroutine(_ExplodeAfter (seconds));
	}

	IEnumerator _ExplodeAfter(float seconds)
	{
		yield return new WaitForSeconds (seconds);
		ExplodeNow();
	}

    public void ExplodeNow()
    {
		Instantiate(explodeParticle, transform.position, explodeParticle.transform.rotation);
		DeadBodyPieceManager.Instance.UnregisterDeadBodyPiece(this);

		// todo: Move this somewhere else? Use events????
		// Why is this here?
		// It's possible that the camera is centered on this object when it
		// explodes and will snap back to origin when the object disappears.
		// If we remove manually...
		ProCamera2D.Instance.RemoveCameraTarget(transform);

		Destroy(this.gameObject);
	}

	void OnCollisionEnter2D(Collision2D collision)
	{
		// Damage tag on collider = lava
		if (collision.gameObject.CompareTag("Damage"))
		{
			PieceHitLava (collision);
		}
		else if (collision.gameObject.CompareTag("Ground"))
		{
			//todo: Prevent stuff from spawning INSIDE the wall!
			SoundEffects.instance.wallHit.Play ();	
		}
		else 
		{
			SoundEffects.instance.wallHit.Play ();			
		}
	}

	bool alreadyHitLava = false;

	void PieceHitLava(Collision2D collision)
	{
		// Used to get the contact point but the pieces didn't always hit
		// at the bottom-most point so lets try this...
		PieceHitLava (new Vector2(myCollider.bounds.center.x, myCollider.bounds.min.y));
	}

	void PieceHitLava(Vector2 spawnPoint)
	{
		if (alreadyHitLava)
			return;

		alreadyHitLava = true;
		
		Ignite ();
		myDissolveParticles = Instantiate (dissolveParticlesPrefab, spawnPoint, Quaternion.identity);

		rb2d.isKinematic = true;
		rb2d.angularVelocity = 0f; // Stop spinning!
		rb2d.velocity = new Vector2 (0, 0);

		if (gameObject.name == "Body")
		{
			SoundEffects.instance.lavaSink.Play ();
		}
		else
		{
			SoundEffects.instance.lavaSinkSmall.Play ();
		}

		TriggerFadeOut ();
	}

	void Ignite()
	{
		burnStartTime = GameplayManager.Instance.GameTime;
	}

	void Update()
	{
		if (burnStartTime != 0f)
		{
			// Don't move down forever...
			if (GameplayManager.Instance.GameTime - fadeStartTime < fadeDuration)
			{
				rb2d.MovePosition (rb2d.position - Vector2.up * moveSpeed * Time.deltaTime);	
			}
			else 
			{
				rb2d.velocity = new Vector2 (0, 0);
			}

			ParticleSystem.EmissionModule emittor = myDissolveParticles.emission;
			emittor.rateOverTimeMultiplier *= .985f;
		}

		if (fadeStartTime != 0f)
		{
			float t = (GameplayManager.Instance.GameTime - fadeStartTime) / (fadeDuration * .5f);
			sprite.color = new Color(1f,1f,1f,Mathf.SmoothStep(1.0f, 0f, t));	
		}
	}

    private void FixedUpdate()
    {
		if (willExplode)
			return;

		playerController.UpdateHorizontalCollisions(HorizontalInput.None, true);
        if (playerController.squished)
        {
			ExplodeNow();
        }
    }

    bool waitingForRestart = false;

	IEnumerator KillAfterDuration(float duration)
	{
		yield return new WaitForSeconds (duration);

		// This is kind of a roundabout way to do this but...
		// This value will change when we restart because it will be assigned
		// to the new Player object. So we can check this to see if
		// there's a new player object when we're trying to kill the current object.
		// If there is, we can kill it. If not, we need to wait until there is.
		// CheckpointManager will call registered dead body pieces to
		// fade out when Restart is called, so that will in turn called TriggerFadeOut again.
		if (GameCameraAdapter.instance.GetObjectToFollow() != this.gameObject)
		{
			Destroy (myDissolveParticles);
			DeadBodyPieceManager.Instance.UnregisterDeadBodyPiece (this);
			Destroy (this.gameObject);	
		}
		else 
		{
			waitingForRestart = true;
		}
	}

	public void TriggerFadeOut()
	{
		// Don't reset the time, only start if we haven't started already!
		if (fadeStartTime == 0f)
		{
			fadeStartTime = GameplayManager.Instance.GameTime;
			StartCoroutine (KillAfterDuration (Mathf.Max(burnDuration, fadeDuration)));
		}
		else if (waitingForRestart)
		{
			// This is a bit confusing but waitingForRestart is only called if KillAfterDuration was already 
			// called, executed, and couldn't destroy self because "this" was the currently followed camera object.
			// So under those circumstances, it's already faded out and we can instantly kill this when
			// TriggerFadeOut() is called again by CheckpointManager when the player finally restarts.
			KillAfterDuration (.1f);
		}
	}
}
