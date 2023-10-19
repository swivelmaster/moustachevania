using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheesePickup : MonoBehaviour, IResetable {

	// Only using cheese in one scene (the Original Game Scene)
	// so we can use this terrible hack to get the maximum count.
	// If we had cheese in multiple scenes we would have to find
	// another way to count them all!
	public static int TotalCheesePickups = 0;

	public ParticleSystem pickupParticle;

	[SerializeField]
	private SpriteRenderer CheeseSprite = null;

	public static List<CheesePickup> pickedUpSinceLastCheckpoint = new List<CheesePickup> ();

	public void Start()
	{
		string uniqueId = GetComponent<UniqueId> ().uniqueId;

		TotalCheesePickups++;

		if (PersistenceManager.Instance.savedGame.FlaggedIds.Contains(uniqueId))
		{
			Collect (true);
		}
	}

	public void Collect(bool fromSave=false)
	{
		// Obviously, if we're loading from a saved game, don't treat like we just picked it up.
		if (!fromSave)
		{
			CheesePickup.pickedUpSinceLastCheckpoint.Add (this);	
			Instantiate(pickupParticle, transform.position, pickupParticle.transform.rotation);
            CollectibleManager.Instance.DoCheeseDoober(transform.position);
		}

		GetComponent<Collider2D>().enabled = false;
		CheeseSprite.enabled = false;
		CollectibleManager.Instance.IncrementScore ();
	}

	public void ResetFromCheckpoint()
	{
		GetComponent<Collider2D>().enabled = true;
		CheeseSprite.enabled = true;
	}
}
