using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For when you have an editor icon sprite that shouldn't show up in the game.
/// Used mostly for the 'widget' objects, which appear as a yellow border in editor
/// but need to be invisible in game.
/// </summary>
public class SpriteRemover : MonoBehaviour {

	void Start () {
		SpriteRenderer renderer = transform.GetComponent<SpriteRenderer> ();
		Destroy (renderer);
		Destroy (this);
	}
}
