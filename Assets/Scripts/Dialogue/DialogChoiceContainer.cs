using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogChoiceContainer : MonoBehaviour
{
    const float APPEAR_DELAY_TIME = .25f;

    [SerializeField]
    private GameObject IndividualChoicePrefab = null;

    [SerializeField]
    private TMP_Text PressToContinue = null;
    [SerializeField]
    private GameObject ChoiceContainer = null;

    private List<DialogueChoiceLine> DialogueChoiceLines;

    int currentlySelectedIndex = 0;
    Action<DialogChoice> onOptionSelected;
    Action<int> yarnOnChoiceSelected;

    bool waitingForChoice = false;

    SoundEffects soundEffects;

    public void Init(SoundEffects soundEffects)
    {
        this.soundEffects = soundEffects;
    }

    void Start()
    {
        // Set to active should be handled when needed by the dialog thing
        PressToContinue.gameObject.SetActive(false);
        Initialize();
    }

    void Initialize()
    {
        
        ChoiceContainer.SetActive(false);
        DialogueChoiceLines = new List<DialogueChoiceLine>();
        for (int i = ChoiceContainer.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(ChoiceContainer.transform.GetChild(i).gameObject);
        }

        waitingForChoice = false;
    }

    public void InitChoices(string[] choices, Action<DialogChoice> onOptionSelected, Action<int> yarnOnChoiceSelect)
    {
        currentlySelectedIndex = 0;
        this.onOptionSelected = onOptionSelected;
        waitingForChoice = true;
        ChoiceContainer.SetActive(true);
        this.yarnOnChoiceSelected = yarnOnChoiceSelect;

        for (int i=0;i<choices.Length;i++)
        {
            var choiceObj = Instantiate(IndividualChoicePrefab, ChoiceContainer.transform) as GameObject;
            var component = choiceObj.GetComponent<DialogueChoiceLine>();
            component.UpdateText(choices[i], APPEAR_DELAY_TIME * i);
            component.SetIsCurrentSelection(i == 0);

            DialogueChoiceLines.Add(component);
        }
    }

    public void AdvanceFrame(ControlInputFrame input)
    {
        if (!waitingForChoice)
            return;

        if (input.Jumping == ControlInputFrame.ButtonState.Down)
        {
            onOptionSelected(
                new DialogChoice(currentlySelectedIndex,
                DialogueChoiceLines[currentlySelectedIndex].GetText(),
                yarnOnChoiceSelected)
            );
            soundEffects.PlayerDialogueSelectAudio();
            Initialize();
            return;
        }

        if (!input.VerticalDownThisFrame)
        {
            return;
        }

        if (input.Vertical == VerticalInput.Down)
        {
            currentlySelectedIndex = GameUtils.UpdateCollectionCursor(DialogueChoiceLines, currentlySelectedIndex + 1);
            soundEffects.PlayDialogueCursorMoveAudio();
            UpdateSelection();
        }
        else if (input.Vertical == VerticalInput.Up)
        {
            currentlySelectedIndex = GameUtils.UpdateCollectionCursor(DialogueChoiceLines, currentlySelectedIndex - 1);
            soundEffects.PlayDialogueCursorMoveAudio();
            UpdateSelection();
        }
    }

    void UpdateSelection()
    {
        for (int i = 0; i < DialogueChoiceLines.Count; i++)
            DialogueChoiceLines[i].SetIsCurrentSelection(i == currentlySelectedIndex);
    }
}
