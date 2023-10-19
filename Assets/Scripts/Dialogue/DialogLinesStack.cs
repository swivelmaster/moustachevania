using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogLinesStack : MonoBehaviour
{
    const int MAX_LINES = 8;

    const float FADE_START = -2000;
    const float FADE_COMPLETE = -1750f;

    const float END_DIALOG_FADE_OUT_TIME = .5f;

    [SerializeField]
    private Transform DialogsContainer = null;

    [Header("Ye Olde Dialog Prefabs")]
    [SerializeField]
    private DialogLine JQDDialogPrefab = null;

    [SerializeField]
    private DialogLine ChaunceyDialogPrefab = null;

    List<DialogLine> Lines = new List<DialogLine>();

    public enum DialogPrintState { Printing, Waiting, FadingOut }
    public DialogPrintState PrintState = DialogPrintState.Waiting;

    public DialogLine CurrentDialogLine { private set; get; }

    float fadeoutTimer;

    public void Init()
    {
        for (int i=DialogsContainer.childCount-1;i>=0;i--)
        {
            Destroy(DialogsContainer.GetChild(i).gameObject);
        }

        Lines.Clear();
        CurrentDialogLine = null;
    }

    public void SpawnDialog(DialogCharacterConfig characterConfig, string text, DialogScreeenSide side, bool forceFast)
    {
        PrintState = DialogPrintState.Printing;

        DialogLine PrefabToUse = null;

        switch (side)
        {
            case DialogScreeenSide.Right:
                PrefabToUse = ChaunceyDialogPrefab;
                break;
            case DialogScreeenSide.Left:
                PrefabToUse = JQDDialogPrefab;
                break;
        }

        if (PrefabToUse == null)
        {
            Debug.LogError("Tried to create a dialog for a type that isn't implemented yet.");
            return;
        }

        CurrentDialogLine = Instantiate(PrefabToUse, DialogsContainer) as DialogLine;
        CurrentDialogLine.Init(characterConfig, text, forceFast);
        Lines.Add(CurrentDialogLine);

        if (Lines.Count > MAX_LINES)
        {
            var first = Lines[0];
            Lines.Remove(first);
            Destroy(first.gameObject);
        }
    }

    public void FastForwardCurrentLine()
    {

    }

    public void AdvanceFrame(bool speedUp=false)
    {
        if (PrintState == DialogPrintState.FadingOut)
        {
            foreach (var line in Lines)
            {
                // Account for already-faded-out items by
                // not jumping them to a HIGHER alpha by accidentally
                // fading them from 1 instead of their current value
                float alpha = line.GetAlpha();
                line.SetAlpha(
                    Mathf.Min(alpha,
                        Mathf.Lerp(1f, 0f,
                            1f - (fadeoutTimer / END_DIALOG_FADE_OUT_TIME))));
            }

            fadeoutTimer -= Time.deltaTime;

            return;
        }

        foreach (var line in Lines)
        {
            line.AdvanceFrame(speedUp);
            float y = line.GetYPosition();
            if (y > FADE_START)
                line.SetAlpha(Mathf.Lerp(1f, 0f,
                    (y - FADE_START)/(FADE_COMPLETE - FADE_START)));
        }

        if (PrintState == DialogPrintState.Waiting)
            return;

        if (CurrentDialogLine != null && CurrentDialogLine.Done)
            PrintState = DialogPrintState.Waiting;
            
    }

    public void StartFadeOut()
    {
        fadeoutTimer = END_DIALOG_FADE_OUT_TIME;
        PrintState = DialogPrintState.FadingOut;
    }
}

