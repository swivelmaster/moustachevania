using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
	public float MusicVolume = 1f;
	public AudioSource MainMusicSource;

	public float SoundEffectsVolume = .75f;

	public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		AudioSource music = Camera.main.GetComponent<AudioSource>();

		if (music)
		{
			MainMusicSource = music;
			music.volume = MusicVolume;
		}

		if (SoundEffects.instance != null)
		{
			SoundEffects.instance.SetVolume(SoundEffectsVolume);
		}
	}
}
