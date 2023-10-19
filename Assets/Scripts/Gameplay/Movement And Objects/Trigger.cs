using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger : MonoBehaviour {

	// Use this for easy access if there's only one :)
	public TriggeredMovement objectToTrigger;

	public TriggeredMovement[] objectsToTrigger;

	bool UseUniqueId = false;
	string UniqueIdToTrigger = "";

	public bool RequireGrounded = true;

	[SerializeField]
	private FlickerOverlay TriggerOverlay;
	[SerializeField]
	private SpriteRenderer VisibleSprite;
	[SerializeField]
	private Sprite OnSprite;
	[SerializeField]
	private Sprite OffSprite;

	[SerializeField]
	private bool debugThisObject;

	private void Start()
    {
        if (objectToTrigger != null && objectToTrigger.GetType() == typeof(ManagedTriggeredMovement))
        {
			UseUniqueId = true;
			UniqueIdToTrigger = objectToTrigger.GetComponent<UniqueId>().uniqueId;

            if (objectsToTrigger.Length > 0)
            {
				Debug.LogError(
                    "Warning: Not supported - simultaneous use of " +
                    "objectToTrigger that is a ManagedTriggeredMovement " +
                    "AND use of objectsToTrigger array."
                );
            }
        }
    }

    public void OnTriggerEnter2D(Collider2D collider)
	{
		TriggerIfEligible(collider);
	}

    public void OnTriggerStay2D(Collider2D collider)
    {
		TriggerIfEligible(collider);
	}

	/// <summary>
    /// Note: Don't need to check if already triggered here; the object
    /// to trigger should do that checking anyway.
    /// </summary>
    /// <param name="collider"></param>
	void TriggerIfEligible(Collider2D collider)
    {
		if (!collider.CompareTag("Player"))
			return;

		Player player = PlayerManager.Instance.currentPlayer;

		// Don't let dead player trigger platform :)
		if (player == null || !player.alive)
			return;

		if (RequireGrounded && !player.getGrounded())
			return;

		if (UseUniqueId)
		{
			TriggeredMovementManager.instance.
				GetCurrentWaitingByUniqueId(UniqueIdToTrigger).Trigger();
			return;
		}

		if (objectToTrigger)
		{
			objectToTrigger.Trigger();
		}

		foreach (TriggeredMovement movement in objectsToTrigger)
		{
			movement.Trigger();
		}

		if (VisibleSprite != null)
        {
			VisibleSprite.sprite = OnSprite;
        }

		if (TriggerOverlay != null)
        {
			TriggerOverlay.AutoRun = true;
			TriggerOverlay.IsActiveAuto = true;
			// This is a hack because triggers are not currently
			// aware of the state of the triggered object,
			// so if player dies and the object resets,
			// there's no way to turn off the flicker etc.!
		}

		activationCount++;

		StartCoroutine(TurnOffAfter(2f, activationCount));

	}

	int activationCount;

	IEnumerator TurnOffAfter(float seconds, int currentActivationCount)
    {
		yield return new WaitForSeconds(seconds);

		// Prevents coroutines from overwriting up-to-date trigger state
		if (currentActivationCount == activationCount)
        {
			if (VisibleSprite != null)
				VisibleSprite.sprite = OffSprite;

			if (TriggerOverlay != null)
			{
				TriggerOverlay.IsActiveAuto = false;
			}

		}
	}
}
