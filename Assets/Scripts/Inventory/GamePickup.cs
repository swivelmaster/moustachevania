using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Slowly deprecating this class...
/// Don't use it anymore if you can help it.
/// </summary>
public class GamePickup : MonoBehaviour {

	public bool triggerDialogOnPickup = false;
	public string dialogToTrigger = "";

	public ParticleSystem pickupParticle;

	Destroyable destroyable;

	public enum CutsceneState {
		Inactive, GoingUp, Ding, Zoom, Moustache, Wait, ZoomOut, FinalWait, Dialog
	}

	public ClothingPickupType pickupType;
	GameObject sprite;

    Vector2 spriteStartPosition;

	public bool collected { private set; get; }

	void Start () 
	{
		destroyable = GetComponent<Destroyable> ();
        destroyable.AddRestoreCallback(() => OnRestore());

		string uniqueId = GetComponent<UniqueId> ().uniqueId;

		if (PersistenceManager.Instance.savedGame.FlaggedIds.Contains(uniqueId))
		{
			Collect (null, true);

			// Don't actually need this anywhere if saved as collected but just
            // setting the value to be consistent.
			collected = true; 
		}
        else
        {
			collected = false;
        }

        sprite = GetComponentInChildren<SpriteRenderer>().gameObject;
        spriteStartPosition = sprite.transform.position;
	}

    void OnRestore()
    {
        sprite.transform.position = spriteStartPosition;
		collected = false;
    }

    void StartCutscene()
    {
		Instantiate(pickupParticle, sprite.transform.position, pickupParticle.transform.rotation);
		destroyable.Destroyed();
		GameEventManager.Instance.CutsceneTriggered.Invoke("Pickup_" + pickupType.ToString(), null);
	}

	public void Collect(Player player, bool skipCutscene=false)
	{
        // Prevent player from somehow hitting this twice
		if (collected)
			return;

		collected = true;

		if (!skipCutscene && triggerDialogOnPickup){
			StartCutscene();
		} else {
			destroyable.Destroyed ();
		}
	}
}
