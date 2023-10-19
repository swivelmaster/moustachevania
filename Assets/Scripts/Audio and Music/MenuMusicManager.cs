using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuMusicManager : MonoBehaviour {

	public AudioSource menuMusicPart1;
	public AudioSource menuMusicLooping;

	void Start () {
		menuMusicPart1.Play ();
		StartCoroutine (PlayNextMusic ());
	}

	IEnumerator PlayNextMusic()
	{
		yield return new WaitForSeconds (menuMusicPart1.clip.length);
		menuMusicLooping.Play ();
	}
}
