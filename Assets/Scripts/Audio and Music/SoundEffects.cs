using System;
using System.Collections;
using System.Collections.Generic;
using DarkTonic.MasterAudio;
using DG.Tweening;
using UnityEngine;

public class SoundEffects : MonoBehaviour {

	public static SoundEffects instance;

	[SerializeField]
	private MasterAudioGroupPlayer checkpoint;
	public MasterAudioGroupPlayer cheeseCollect;
	public MasterAudioGroupPlayer death;

    // Don't allow explosion sound to be played directly
    [SerializeField]
    private MasterAudioGroupPlayer explosion = null;
	[SerializeField]
	private float explosionPitchIncrement = .1f;

	[Header("Movement Sounds")]
	public MasterAudioGroupPlayer floorHit;
	public MasterAudioGroupPlayer jump;
	public MasterAudioGroupPlayer powerup;
	public MasterAudioGroupPlayer lavaSink;
	public MasterAudioGroupPlayer lavaSinkSmall;
	public MasterAudioGroupPlayer wallHit;
	public MasterAudioGroupPlayer dash;
	public MasterAudioGroupPlayer bonk;

	[Header("Super Jump Sounds")]
	public MasterAudioGroupPlayer xJumpStart;
	public MasterAudioGroupPlayer xJumpReady;
    public MasterAudioGroupPlayer xJumpReadyLoop;
    public MasterAudioGroupPlayer xJumpComplete;

	[Header("Silly Cutscene Sounds")]
	public MasterAudioGroupPlayer zoomIn;
	public MasterAudioGroupPlayer zoomOut;
	public MasterAudioGroupPlayer mustacheAppear;

	[Header("Destruction Sounds")]
	public MasterAudioGroupPlayer basicEnemyDestroyed;

	[Header("Other Game Mechanics")]
	public MasterAudioGroupPlayer resetSphere;
	public MasterAudioGroupPlayer adjustableObjectSwitch;
	public MasterAudioGroupPlayer adjustableObjectOff;
	public MasterAudioGroupPlayer adjustableObjectNoChange;
	public MasterAudioGroupPlayer adjustableObjectBounce;

	[Header("Dialogue Stuff")]
	[SerializeField]
	private MasterAudioGroupPlayer[] DialogAmbientSounds = new MasterAudioGroupPlayer[0];

	[SerializeField]
	private MasterAudioGroupPlayer DialogCursorMoveSound = null;
	[SerializeField]
	public MasterAudioGroupPlayer DialogSelectSound = null;

	void Awake () {
		instance = this;

		xJumpReadyLoop.Play();
	}

	public void SetVolume (float newVolume)
	{
		MasterAudio.FadeBusToVolume("Sound Effects", newVolume, .1f);
	}

    // Used to increase pitch when destroying consecutive things w/superjump
    public void PlayExplosion(int sequenceNumber=0)
    {
		var pitch = 1f + (sequenceNumber * explosionPitchIncrement);
		explosion.Play(1f, pitch);
    }

	/// <summary>
    /// Plays the appropriate audio for the character for the line they're speaking.
    /// Accounts for special audio for special lines like Chauncey's *crunch*
    /// when he's chewing his chips.
    /// </summary>
    /// <param name="character"></param>
    /// <param name="line"></param>
    /// <returns>The length of the special sound if there is one being
    /// used, or 0f if no special sound. This allows the dialogue system
    /// to wait the appropriate amount of time before showing the next
    /// word. (If 0f is returned, use the default time between words.)</returns>
	public float PlayDialogueCharacterAudio(DialogCharacterConfig character, string line)
    {
		foreach (var special in character.DialogCharacterSpecialSounds)
        {
			// Using Contains instead of == because we're splitting on spaces,
			// so punctuation might be in the "word" line we're looking at.
			if (line.Contains(special.SoundText))
            {
				var groupInfo = MasterAudio.GetGroupInfo(special.SoundToUse);
				if (groupInfo == null)
                {
					Debug.LogError("Audio group " + special.SoundToUse + " not found.");
					return 0f;
                }
				if (groupInfo.Sources.Count == 0)
                {
					Debug.LogError("Audio group " + special.SoundToUse + " has no sources in it.");
					return 0f;
                }

				var length = groupInfo.Sources[0].Source.clip.length;
				MasterAudio.PlaySound(special.SoundToUse);
				return length;
            }
        }

		MasterAudio.PlaySound(character.DialogAudioGroupName);

		return 0f;
    }

	public void PlayDialogueCursorMoveAudio()
    {
		DialogCursorMoveSound.Play();
	}

	public void PlayerDialogueSelectAudio()
    {
		DialogSelectSound.Play();
    }

	MasterAudioGroupPlayer CurrentAmbientSound = null;

	public void DialogAmbientSoundSet(int index, float fadeInTime)
    {
		if (DialogAmbientSounds.Length < index)
        {
			Debug.LogError("Tried to play ambient dialog sound at index " + index.ToString() + " that doesn't exist.");
			return;
        }

		DialogAmbientSounds[index].Play();
		CurrentAmbientSound = DialogAmbientSounds[index];
		Debug.Log("Todo: Support for fading ambient sounds in and out.");
    }

	public void DialogAmbientSoundStop(float fadeOutTime=0.5f)
    {
		if (CurrentAmbientSound == null)
        {
			Debug.LogWarning("Tried to stop ambient sound when there was none playing.");
			return;
        }

		CurrentAmbientSound.Stop();
    }

	public void DialogPlaySFX(MasterAudioGroupPlayer clip, float volume)
    {
		clip.Play();
    }

	public void PlayDestroyableDestroyedSound(DestroyableSoundTypes soundType)
    {
		switch (soundType)
        {
			case DestroyableSoundTypes.BrownEnemy:
				basicEnemyDestroyed.Play();
				break;

        }
    }

	public void CheckpointReached()
    {
		// hack: Prevent first checkpoint load from triggering the sound
		// YES THERE'S A BETTER WAY TO DO THIS
		if (GameplayManager.Instance.FixedGameTime > 2f)
			checkpoint.Play();
    }
}

[Serializable]
public class MasterAudioGroupPlayer
{
	public string GroupName;

	public string LoopGroupName;

	public void Play()
    {
		MasterAudio.PlaySound(GroupName);
		PlayLoopGroup();
    }

	public void Play(float volume, float? pitch=null)
    {
		MasterAudio.PlaySound(GroupName, volume, pitch);
		PlayLoopGroup();

    }

	/// <summary>
	/// This is for sounds that need to transition to a looped version once
	/// the start has completed. Currently used only for the super-jump charge
	/// sound, which has a start and then needs to loop for as long as the player
	/// holds down the button before jumping or doing something else.
	/// </summary>
	void PlayLoopGroup()
	{
		if (LoopGroupName == "")
			return;

		var groupInfo = MasterAudio.GetGroupInfo(LoopGroupName);
        MasterAudio.PlaySound(LoopGroupName, delaySoundTime: groupInfo.Sources[0].Source.clip.length);
	}

	public void Stop()
    {
		MasterAudio.StopAllOfSound(GroupName);

		if (LoopGroupName != "")
		{
            MasterAudio.StopAllOfSound(LoopGroupName);
        }
    }

	public bool isPlaying()
    {
		return MasterAudio.IsSoundGroupPlaying(GroupName);
    }


}
