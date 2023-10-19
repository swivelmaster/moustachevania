using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoTextController : MonoBehaviour
{

    public TMP_Text infoText;
    public Image infoTextBackground;

    public CheckpointManager checkpointManager;

    void Start()
    {
        InitSettings();
    }

    public void Init(CheckpointManager checkpointManager)
    {
        this.checkpointManager = checkpointManager;
    }

    public void FadeInMessage(string message, Action onComplete)
    {
        infoTextBackground.enabled = true;
        infoText.text = message;
        infoText.color = new Color(1f, 1f, 1f, 0f);
        infoText.DOColor(new Color(1f, 1f, 1f, 1f), 1f);
        StartCoroutine(AfterOneSecond(onComplete));
    }

    public void FadeOutMessage(Action onComplete)
    {
        infoTextBackground.DOColor(new Color(0f, 0f, 0f, 0f), 1f);
        infoText.DOColor(new Color(1f, 1f, 1f, 0f), 1f);
        StartCoroutine(AfterOneSecond(onComplete));
    }

    IEnumerator AfterOneSecond(Action onComplete, Action tearDown=null)
    {
        yield return new WaitForSeconds(1f);

        Debug.Log("IntoText - continue");

        onComplete.Invoke();
        if (tearDown != null)
            tearDown.Invoke();
    }

    void InitSettings()
    {
        infoText.color = new Color(1f, 1f, 1f, 1f);
        infoTextBackground.color = new Color(0f, 0f, 0f, 1f);

        infoText.text = "";
        infoTextBackground.enabled = false;
    }

    /// <summary>
    /// This only runs during gameplay, so we can use the infoText
    /// in different ways for cutscenes and AdvanceFrame won't mess with it.
    /// </summary>
    public void AdvanceFrame()
    {
        if (checkpointManager.ReadyToRestart)
        {
            infoTextBackground.enabled = true;
            infoText.text = "Press Jump to restart at the last altar.";
            infoText.color = new Color(1f, 1f, 1f, 1f);
            infoTextBackground.color = new Color(0f, 0f, 0f, 1f);
        }
        else
        {
            infoTextBackground.enabled = false;
            infoText.text = "";
        }
    }
}
