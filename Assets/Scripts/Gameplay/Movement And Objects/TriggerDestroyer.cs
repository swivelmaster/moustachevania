using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerDestroyer : MonoBehaviour {

	public GameObject ObjectToDestroy;

	void OnTriggerEnter2D(Collider2D collider)
	{
		if (collider.CompareTag("Player"))	
		{
			ObjectToDestroy.GetComponent<Destroyable> ().Destroyed ();
			GetComponent<Destroyable> ().Destroyed ();
		}
	}
}
