using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;

public class Destroyable : MonoBehaviour {

	public ParticleSystem[] destroyParticles;
	public IFakeDestroyable[] destroyables;

	public static List<Destroyable> DestroyedSinceLastCheckpoint = new List<Destroyable>();
	public static bool DestroyedButNotSavedByUUID(string uuid)
	{
		// This is EXTRAORDINARILY inefficient but we need it for the ResetSpheres
		// to check if they've been activated or not.
		return DestroyedSinceLastCheckpoint.Find((destroyable) => destroyable.UniqueId == uuid) != null;
	}

	Vector3 startPosition;

	public bool dead = false;

	ParticleSystem[] activeParticles;

    List<Action> RestoreCallbacks = new List<Action>();

	DOTweenAnimation MyTween;

	public string UniqueId { private set; get; }

	public DestroyableSoundTypes DestroyedSound = DestroyableSoundTypes.None;
	public DestroyableVFXOptions DestroyedScreenShake = DestroyableVFXOptions.None;

	public void Start()
	{
		startPosition = transform.localPosition;

		// This is a hack solution but unfortunately, interface fields can't be serialized so...
		destroyables = GetComponents<IFakeDestroyable> ();

		UniqueId = GetComponent<UniqueId> ().uniqueId;

		activeParticles = GetComponentsInChildren<ParticleSystem> ();

		MyTween = GetComponent<DOTweenAnimation>();

		if (PersistenceManager.Instance.savedGame.FlaggedIds.Contains(UniqueId))
		{
			FakeDestroySelf ();
		}
	}

    public void AddRestoreCallback(Action action)
    {
        RestoreCallbacks.Add(action);
    }

	public void Destroyed()
	{
		foreach (ParticleSystem system in destroyParticles)
		{
			GameObject o = Instantiate (system).gameObject;
			o.transform.position = new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);
		}

		DestroyedSinceLastCheckpoint.Add (this);

		FakeDestroySelf ();

		SoundEffects.instance.PlayDestroyableDestroyedSound(DestroyedSound);
		MainCameraPostprocessingEffects.instance.Punch();

	}

	public void FakeDestroySelf()
	{
		transform.position = new Vector2 (transform.position.x + 1000f, transform.position.y + 1000f);

		if (destroyables != null)
		{
			foreach (IFakeDestroyable d in destroyables)
			{
				d.Stop ();
			}
		}

		dead = true;

		if (activeParticles != null && activeParticles.Length > 0)
		{
			foreach (ParticleSystem system in activeParticles)
			{
				system.Stop ();
			}	
		}

		if (MyTween != null)
			MyTween.DOPause();
	}

	public void Restore()
	{
		transform.localPosition = startPosition;

		if (destroyables != null)
		{
			foreach (IFakeDestroyable d in destroyables) 
			{
				d.Restart ();
			}
		}

		dead = false;

		if (activeParticles != null && activeParticles.Length > 0)
		{
			foreach (ParticleSystem system in activeParticles)
			{
				system.Play ();
			}	
		}

        foreach (Action action in RestoreCallbacks)
        {
            action.Invoke();
        }

		if (MyTween != null)
			MyTween.DOPlay();
	}

}

public interface IFakeDestroyable
{
	void Stop();
	void Restart();
}

public enum DestroyableSoundTypes
{
	None,
	BrownEnemy
}

public enum DestroyableVFXOptions
{
	None,
	Mild
}