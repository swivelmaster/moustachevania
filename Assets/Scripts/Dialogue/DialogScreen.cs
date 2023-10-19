using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class DialogScreen : MonoBehaviour
{
    const float END_DIALOG_TRANSITION_TIME = 1.1f;

    [SerializeField]
    private Canvas ParentCanvas = null;

    [SerializeField]
    private GameObject LeftPanel = null;

    [SerializeField]
    private GameObject RightPanel = null;

    [SerializeField]
    private GameObject BottomPanel = null;

    [SerializeField]
    private DialogLinesStack LinesStack = null;

    [SerializeField]
    private GameObject PressToContinueText = null;

    [Header("Start positions needed because (from) tweens don't start on Awake()")]
    [SerializeField]
    private Transform LeftPanelStartPosition = null;
    [SerializeField]
    private Transform RightPanelStartPosition = null;
    [SerializeField]
    private Transform BottomPanelStartPosition = null;

    [SerializeField]
    public DialogChoiceContainer dialogChoiceContainer;

    private DOTweenAnimation LeftPanelTween;
    private DOTweenAnimation RightPanelTween;
    private DOTweenAnimation BottomPanelTween;

    List<Tween> tweens = new List<Tween>();

    public DialogState CurrentState = DialogState.PreInit;

    float endTimer;

    DialogCharacterPanel leftPanelScript;
    DialogCharacterPanel rightPanelScript;

    Action queuedOnCompleteAction;

    private void Start()
    {
        ParentCanvas.enabled = false;
    }

    public void Init(SoundEffects soundEffects,
        DialogCharacterConfig leftCharacter, DialogCharacterConfig rightCharacter)
    {
        // Need this first, otherwise other stuff doesn't work
        ParentCanvas.enabled = true;

        // todo: Evaluate: Do we need to init left and right panels
        // if we're just setting their config via the character config
        // script? Maybe we don't need to expose them publicly and can
        // just make a permanent reference via inspector & never change it.
        InitElements(LeftPanel, RightPanel);

        leftPanelScript.SetCharacterConfig(leftCharacter);
        rightPanelScript.SetCharacterConfig(rightCharacter);
        dialogChoiceContainer.Init(soundEffects);
    }

    void StateComplete()
    {
        CurrentState = DialogState.WaitingForCommand;
        queuedOnCompleteAction.Invoke();
    }

    public void AdvanceFrame(ControlInputFrame input)
    {
        dialogChoiceContainer.AdvanceFrame(input);

        switch (CurrentState)
        {
            case DialogState.TransitionIn:
                Update_TransitionIn();
                break;
            case DialogState.Printing:
                Update_PrintDialogLine(input);
                break;
            case DialogState.WaitingForInput:
                Update_WaitingForInput(input);
                break;
            case DialogState.TransitionOut:
                Update_TransitionOut();
                break;
        }

        //default: do nothing
    }

    public void Start_TransitionIn(Action onContinue)
    {
        queuedOnCompleteAction = onContinue;

        LinesStack.Init();

        CurrentState = DialogState.TransitionIn;
    }

    public void Update_TransitionIn()
    {
        foreach (var tween in tweens)
        {
            if (tween.IsPlaying())
                return;
        }

        StateComplete();
    }

    public void Start_PrintDialogLine(
        DialogCharacterConfig characterConfig, string NextLine,
        DialogScreeenSide side, Action OnComplete,
        bool instant=false)
    {
        queuedOnCompleteAction = OnComplete;

        var currentLine = NextLine;
        Speak(side);

        LinesStack.SpawnDialog(characterConfig, currentLine, side, instant);
        CurrentState = DialogState.Printing;
    }

    public void Speak(DialogScreeenSide side)
    {
        if (side == DialogScreeenSide.Left)
        {
            leftPanelScript.CharacterSpeaking();
            rightPanelScript.CharacterStopSpeaking();
        }
        else
        {
            rightPanelScript.CharacterSpeaking();
            leftPanelScript.CharacterStopSpeaking();
        }
    }

    public void Update_PrintDialogLine(ControlInputFrame input)
    {
        LinesStack.AdvanceFrame(input.JumpButtonDownHold);

        if (LinesStack.PrintState == DialogLinesStack.DialogPrintState.Waiting)
        {
            if (input.JumpButtonDownHold)
            {
                // skip straight to next state by calling the update function
                // knowing that it will handle the jump button held down case
                Update_WaitingForInput(input);
            }
            // Force Instant is used when the player selected a line choice
            // ...therefore don't wait for player input to progress to the next thing.
            else if (LinesStack.CurrentDialogLine != null && LinesStack.CurrentDialogLine.forceInstant)
            {
                Update_WaitingForInput(input, true);
            }
            else
            {
                CurrentState = DialogState.WaitingForInput;
                PressToContinueText.SetActive(true);
            }
        }
    }

    public void Update_WaitingForInput(ControlInputFrame input, bool forceSkip=false)
    {
        if (input.Jumping == ControlInputFrame.ButtonState.Down
            || input.JumpButtonDownHold || forceSkip)
        {
            PressToContinueText.SetActive(false);
            StateComplete();
        }
    }

    public void EndDialogScene_Start(Action onComplete)
    {
        queuedOnCompleteAction = onComplete;

        foreach (var tween in tweens)
        {
            tween.SmoothRewind();
        }

        LinesStack.StartFadeOut();

        endTimer = END_DIALOG_TRANSITION_TIME;

        CurrentState = DialogState.TransitionOut;

        leftPanelScript.CharacterStopSpeaking();
        rightPanelScript.CharacterStopSpeaking();
    }

    public void EndDialogSceneComplete()
    {
        CurrentState = DialogState.PreInit;
        ParentCanvas.enabled = false;
        StateComplete();
    }

    void Update_TransitionOut()
    {
        endTimer -= Time.deltaTime;
        LinesStack.AdvanceFrame();

        if (endTimer <= 0f)
        {
            EndDialogSceneComplete();
        }
    }

    void InitElements(GameObject leftPanel, GameObject rightPanel)
    {
        leftPanelScript = leftPanel.GetComponent<DialogCharacterPanel>();
        rightPanelScript = rightPanel.GetComponent<DialogCharacterPanel>();

        leftPanel.transform.position = LeftPanelStartPosition.position;
        rightPanel.transform.position = RightPanelStartPosition.position;
        BottomPanel.transform.position = BottomPanelStartPosition.position;

        LeftPanelTween = LeftPanel.GetComponent<DOTweenAnimation>();
        RightPanelTween = RightPanel.GetComponent<DOTweenAnimation>();
        BottomPanelTween = BottomPanel.GetComponent<DOTweenAnimation>();

        LeftPanelTween.DORestart();
        RightPanelTween.DORestart();
        BottomPanelTween.DORestart();

        tweens = GameUtils.ConfigureTweensForDialog(
            new DOTweenAnimation[] { LeftPanelTween, RightPanelTween, BottomPanelTween }
        );
        tweens.ForEach(tween => tween.Play());

        PressToContinueText.SetActive(false);
    }
}

public enum DialogState
{
    PreInit, TransitionIn, Printing, WaitingForInput, TransitionOut, WaitingForCommand
}

public enum DialogScreeenSide
{
    Left, Right
}

