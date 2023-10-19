using System;
using DG.Tweening;
using UnityEngine;
using Yarn;
using Yarn.Unity;

public class DialogueUIImplementation : Yarn.Unity.DialogueUIBehaviour
{
    [SerializeField]
    private DialogueCommandMap DialogueCommandMap = null;

    public CutsceneManager cutsceneManager { private set; get; }

    // Need access to these for commands
    public DialogScreen dialogScreen { private set; get; }
    public MusicManager musicManager { private set; get; }
    public InfoTextController infoTextController { private set; get; }
    public SoundEffects soundEffects { private set; get; }
    public Camera mainCamera { private set; get; }
    public CameraEffects cameraEffects { private set; get; }

    public SingleCutsceneObject singleCutsceneObjectManager { private set; get; }

    public DialogCharacterConfig currentLeftCharacterConfig;
    public DialogCharacterConfig currentRightCharacterConfig;

    PlayerManager playerManager;

    public void Init(CutsceneManager cutsceneManager, SceneSettings sceneSettings)
    {
        this.cutsceneManager = cutsceneManager;
        dialogScreen = sceneSettings.dialogScreen;
        musicManager = sceneSettings.musicManager;
        infoTextController = sceneSettings.infoTextController;
        soundEffects = sceneSettings.soundEffects;
        mainCamera = sceneSettings.gameplayCamera;
        cameraEffects = sceneSettings.overlayEffects;

        singleCutsceneObjectManager = sceneSettings.singleCutsceneObjectManager;

        playerManager = sceneSettings.playerManager;
    }

    public void MovePlayerToLocation(Vector2 location)
    {
        playerManager.ForceMovePlayerToLocation(location);
    }

    public void HidePlayer()
    {
        playerManager.currentPlayer.spriteObject.SetActive(false);
    }

    public void ShowPlayer()
    {
        playerManager.currentPlayer.spriteObject.SetActive(true);
    }

    public void TweenCameraToPlayer(float time)
    {
        var position = playerManager.currentPlayer.transform.position;
        position.z = mainCamera.transform.position.z;
        mainCamera.transform.DOMove(position, time).onComplete =
            () => mainCamera.GetComponent<GameCameraAdapter>()
                    .ForceLocationDuringPlay();
    }

    public void CutsceneEnded()
    {
        Debug.Log("Cutscene end callback called");
        cutsceneManager.ConversationComplete();
        CutsceneScene.HideAll();
    }

    public override Yarn.Dialogue.HandlerExecutionType RunCommand(Command command, Action onCommandComplete)
    {
        Debug.Log("Run command " + command.Text);

        if (DialogueCommandMap.RunCommand(command.Text, onCommandComplete, this))
        {
            return Yarn.Dialogue.HandlerExecutionType.ContinueExecution;
        }

        return Yarn.Dialogue.HandlerExecutionType.PauseExecution;
    }

    public override Yarn.Dialogue.HandlerExecutionType RunLine(Line line, ILineLocalisationProvider localisationProvider, Action onLineComplete)
    {
        string localized = localisationProvider.GetLocalisedTextForLine(line);
        string[] split = localized.Split(':');
        string speaker = split[0];

        if (split.Length > 2)
            Debug.LogError("Error: Split dialog line length > 2, fix a problem: " + localized);

        if (split.Length < 2)
            Debug.LogError("Uh oh, split dialog line length is < 2, fix a problem: " + localized);

        string text = split[1].Trim();

        if (speaker == "Narrator")
        {
            DialogueCommandMap.RunCommand("NARRATOR_TEXT " + text, onLineComplete, this);
            return Yarn.Dialogue.HandlerExecutionType.PauseExecution;
        }

        dialogScreen.Start_PrintDialogLine(
            speaker == "John" ? currentLeftCharacterConfig : currentRightCharacterConfig,
            text.Trim(), speaker == "John" ? DialogScreeenSide.Left : DialogScreeenSide.Right,
            onLineComplete);

        return Yarn.Dialogue.HandlerExecutionType.PauseExecution;
    }

    public override void RunOptions(OptionSet optionSet, ILineLocalisationProvider localisationProvider, Action<int> onOptionSelected)
    {
        string[] options = new string[optionSet.Options.Length];

        for (int i=0;i<optionSet.Options.Length;i++)
        {
            options[i] = localisationProvider.GetLocalisedTextForLine(optionSet.Options[i].Line);
        }
        
        dialogScreen.dialogChoiceContainer.InitChoices(options, OnOptionSelected, onOptionSelected);
        dialogScreen.Speak(DialogScreeenSide.Left);
    }

    /// <summary>
    /// Okay what's going on here?
    /// Yarn doesn't automatically repeat the line chosen by the user,
    /// so we need to manually send it to the dialog display component and then
    /// send Yarn's choice callback as its callback so we trigger the next line
    /// of dialog after the player's choice once it's done.
    /// </summary>
    /// <param name="choice"></param>
    void OnOptionSelected(DialogChoice choice)
    {
        dialogScreen.Start_PrintDialogLine(currentLeftCharacterConfig, choice.text,
            DialogScreeenSide.Left, () => choice.onOptionSelected(choice.index), true);
    }

    public void AdvanceFrame(ControlInputFrame input)
    {
        dialogScreen.AdvanceFrame(input);
        DialogueCommandMap.AdvanceFrame(input.JumpButtonDownHold);
        singleCutsceneObjectManager.AdvanceFrame(input.Jumping == ControlInputFrame.ButtonState.Down);
    }
}

public struct DialogChoice
{
    public int index;
    public string text;
    public Action<int> onOptionSelected;

    public DialogChoice(int index, string text, Action<int> onOptionSelected)
    {
        this.index = index;
        this.text = text;
        this.onOptionSelected = onOptionSelected;
    }
}
