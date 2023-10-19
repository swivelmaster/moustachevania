using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Rewired;

/// <summary>
/// This is very sloppy and hacky.
/// </summary>
public class DemoIntroUI : MonoBehaviour
{
    public static DemoIntroUI instance;

    Rewired.Player controller;

    private void Awake()
    {
        instance = this;
        fancyIntro.alpha = 0f;
        basicIntro.alpha = 0f;
    }

    [SerializeField]
    CanvasGroup basicIntro;

    [SerializeField]
    CanvasGroup fancyIntro;

    [SerializeField]
    GameObject loadingText;

    private void Start()
    {
        controller = ReInput.players.GetPlayer(0);

        if (MainMenu.LastGameModeSelected == GameManager.GameMode.Classic)
        {
            basicIntro.DOFade(1f, 1f);
        }
        else
        {
            fancyIntro.DOFade(1f, 1f);
        }

        StartCoroutine(WaitForInput());
    }

    bool inputEnabled = false;

    private void Update()
    {
        if (!inputEnabled) { return; }

        if (controller.GetAnyButtonDown())
            LoadNextScene();
    }

    private void OnMouseDown()
    {
        if (!inputEnabled) { return; }

        LoadNextScene();
    }

    void LoadNextScene()
    {
        basicIntro.gameObject.SetActive(false);
        fancyIntro.gameObject.SetActive(false);
        loadingText.SetActive(true);

        GameManager.instance.StartGame(true, MainMenu.LastGameModeSelected);
    }

    IEnumerator WaitForInput()
    {
        yield return new WaitForSeconds(1f);

        inputEnabled = true;
    }
}
