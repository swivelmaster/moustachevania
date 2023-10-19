using System.Collections;
using System.Collections.Generic;
using DarkTonic.MasterAudio;
using DG.Tweening;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public MusicConfig[] Musics;

    public float FadeOutTime = .75f;

    Dictionary<string, MusicConfig> MusicDict = new Dictionary<string, MusicConfig>();

    Stack<MusicConfig> MusicStack = new Stack<MusicConfig>();

    public static MusicManager Instance { private set; get; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        foreach (var config in Musics)
        {
            MusicDict[config.Name] = config;
        }
    }

    public MusicConfig GetMusicConfigByMusicName(string name)
    {
        if (!MusicDict.ContainsKey(name))
        {
            if (MusicDict.ContainsKey(MusicNames.ERROR))
            {
                return MusicDict[MusicNames.ERROR];
            }

            throw new System.Exception("Invalid music name passed AND no Error music was found.");
        }

        return MusicDict[name];
    }

    public void SwitchMusic(MusicConfig musicConfig, bool immediately)
    {
        if (MusicStack.Count == 0)
        {
            MusicStack.Push(musicConfig);
        }
        else
        {
            MusicStack.Pop();
            MusicStack.Push(musicConfig);
        }

        TransitionMusic(MusicStack.Peek(), immediately);
    }

    public void StackMusic(MusicConfig musicConfig, bool immediately)
    {
        MusicStack.Push(musicConfig);
        TransitionMusic(MusicStack.Peek(), immediately);
    }

    public void UnStackMusic(bool immediately)
    {
        if (MusicStack.Count <= 1)
        {
            Debug.LogError("Trying to unstack music but there's only 1 on the stack!");
            return;
        }

        MusicStack.Pop();
        TransitionMusic(MusicStack.Peek(), immediately);
    }

    void TransitionMusic(MusicConfig newMusic, bool immediately)
    {
        MasterAudio.ChangePlaylistByName(newMusic.PlaylistName);

        if (!immediately)
            Debug.LogWarning("Non-immediate music transitioning doesn't work yet, sorry.");
    }

    public void PauseMusic(bool immediately=false)
    {
        MasterAudio.PausePlaylist();

        if (!immediately)
            Debug.LogWarning("Non-immediate music transitioning doesn't work yet, sorry.");
    }

    public void ResumeMusic(bool immediately=false)
    {
        MasterAudio.UnpausePlaylist();

        if (!immediately)
            Debug.LogWarning("Non-immediate music transitioning doesn't work yet, sorry.");
    }
}

[System.Serializable]
public struct MusicConfig
{
    public string Name;
    public string PlaylistName;
}

public class MusicNames
{
    public const string ERROR = "Error";
    public const string OPENING_MUSIC = "MainMenuMusic";
    public const string MELLOW_CHAUNCEY_EXPLORE = "MellowChaunceyExplore";
    public const string FUNKY_CHAUNCY_THEME = "FunkyChaunceyTheme";
    public const string PROGGY_1 = "Proggy1";
    public const string PROGGY_2 = "Proggy2";
}