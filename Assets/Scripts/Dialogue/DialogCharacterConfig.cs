using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogCharacterConfig
{
    public DialogCharacter Character;
    public string CharacterNameString;
    public Sprite CharacterImage;
    public int CharacterImageHeight;
    public Material CharacterBackgroundMaterial;
    public Sprite CharacterBackgroundImageTile;
    public Sprite CharacterDialogPanelBackground;

    public string DialogAudioGroupName;
    public DialogCharacterSpecialSound[] DialogCharacterSpecialSounds;

    [HideInInspector]
    // Needed for playing character audio
    public SoundEffects soundEffects;

    public void Init()
    {
        if (DialogCharacterSpecialSounds == null)
            DialogCharacterSpecialSounds = new DialogCharacterSpecialSound[0];
    }

    public float PlayDialogueAudio(string line)
    {
        return this.soundEffects.PlayDialogueCharacterAudio(this, line);
    }
}

public enum DialogCharacter
{
    John, Chauncey, MysteriousFigure, Portalmaw1, Portalmaw2, Administrator
}

/// <summary>
/// Supports special sounds for character dialogue, like eating, sighing,
/// grunting, whatever.
/// </summary>
[Serializable]
public struct DialogCharacterSpecialSound
{
    public string SoundText;
    public string SoundToUse;
}