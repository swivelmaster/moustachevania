using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public class DialogueCommandMap : MonoBehaviour
{
    [NonSerialized]
    public List<DialogueCommand> Commands;

    Dictionary<string, DialogueCommand> CommandDict = new Dictionary<string, DialogueCommand>();

    private void Start()
    {
        Commands = new List<DialogueCommand>();

        Commands.Add(new Command_BeginDialog());
        Commands.Add(new Command_EndDialog());

        Commands.Add(new Command_StartMusic());
        Commands.Add(new Command_EndMusic());

        Commands.Add(new Command_NarratorText());
        Commands.Add(new Command_NarratorClear());

        Commands.Add(new Command_StartAmbient());
        Commands.Add(new Command_StopAmbient());

        Commands.Add(new Command_OverlayFade());
        Commands.Add(new Command_OverlayFadeOff());

        Commands.Add(new Command_SceneJump());
        Commands.Add(new Command_ShowScene());
        Commands.Add(new Command_SpawnVFX());
        Commands.Add(new Command_HideActor());
        Commands.Add(new Command_ShowActor());

        Commands.Add(new Command_PlayerWarp());
        Commands.Add(new Command_PlaySound());

        Commands.Add(new Command_CameraShakeOn());
        Commands.Add(new Command_CameraShakeOff());

        Commands.Add(new Command_ZoomMainCamera());

        Commands.Add(new Command_VignetteOn());
        Commands.Add(new Command_VignetteOff());

        Commands.Add(new Command_HidePlayer());
        Commands.Add(new Command_ShowPlayer());

        Commands.Add(new Command_PlayOverlayVFX());

        Commands.Add(new DialogueCommand());

        Commands.Add(new Command_ShowSingleObjectCutscene());

        Commands.Add(new Command_PauseMusic());
        Commands.Add(new Command_ResumeMusic());

        Commands.Add(new Command_ActorMove());
        Commands.Add(new Command_CutsceneAction());
        Commands.Add(new Command_TweenCameraToPlayer());

        Commands.Add(new Command_FlipX());

        Commands.Add(new Command_Quit());

        foreach (var command in Commands)
        {
            CommandDict[command.Name] = command;
        }
    }

    public bool RunCommand(string CommandName, Action onContinue, DialogueUIImplementation dialogueUIImplementation)
    {
        string[] args = CommandName.Split(' ');
        string commandName = args[0];

        if (!CommandDict.ContainsKey(commandName))
        {
            Debug.LogError("Error: Trying to run cutscene command " + commandName + " that hasn't been implemented.");
            onContinue.Invoke();
            return true;
        }

        return CommandDict[args[0]].Execute(dialogueUIImplementation, onContinue, args);
    }

    public void AdvanceFrame(bool skip)
    {
        
    }
}

public class DialogueCommand
{
    public string Name = "Undefined";
    public virtual bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments) { return true; }
    public virtual void AdvanceFrame(bool skip) { }

    protected bool AssertArgsLength(string[] arguments, int length)
    {
        if (arguments.Length >= length)
            return true;

        Debug.LogError("DialogCommand error: args length expected at least " +
            length.ToString() + " but it's not. Arg name : " + this.Name);

        return false;
    }

    protected CutsceneScene GetScene(string[] arguments)
    {
        return CutsceneScene.GetCutsceneSceneByName(arguments[1]);
    }

    protected (CutsceneScene, CutsceneActor) GetSceneAndActor(string[] arguments)
    {
        var scene = CutsceneScene.GetCutsceneSceneByName(arguments[1]);
        var actor = scene.GetActor(arguments[2]);

        return (scene, actor);
    }
}

public class Command_BeginDialog : DialogueCommand
{
    public Command_BeginDialog() { Name = "BEGIN_DIALOG"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        dialogUI.currentLeftCharacterConfig = dialogUI.cutsceneManager.GetCharacterConfigByName(arguments[1]);
        dialogUI.currentRightCharacterConfig = dialogUI.cutsceneManager.GetCharacterConfigByName(arguments[2]);

        dialogUI.dialogScreen.Init(dialogUI.soundEffects,
            dialogUI.currentLeftCharacterConfig,
            dialogUI.currentRightCharacterConfig
        );
        dialogUI.dialogScreen.Start_TransitionIn(onContinue);
        return false;
    }
}

public class Command_EndDialog : DialogueCommand
{
    public Command_EndDialog() { Name = "END_DIALOG"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        dialogUI.dialogScreen.EndDialogScene_Start(onContinue);
        return false;
    }
}

public class Command_StartMusic : DialogueCommand
{
    public Command_StartMusic() { Name = "START_MUSIC";  }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 2))
            return true;

        var music = dialogUI.musicManager.GetMusicConfigByMusicName(arguments[1]);
        dialogUI.musicManager.StackMusic(music, false);
        return true;
    }
}

public class Command_EndMusic : DialogueCommand
{
    public Command_EndMusic() { Name = "END_MUSIC"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        var music = dialogUI.musicManager.GetMusicConfigByMusicName(arguments[1]);
        dialogUI.musicManager.UnStackMusic(false);

        return true;
    }
}

/// <summary>
/// Special case command - triggered by a text/dialog special case
/// </summary>
public class Command_NarratorText : DialogueCommand
{
    public Command_NarratorText() { Name = "NARRATOR_TEXT"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 2))
            return true;

        var text = string.Join(" ", arguments).Substring(arguments[0].Length + 1);
        if (text.StartsWith("\""))
            text = text.Substring(1);

        if (text.EndsWith("\""))
            text = text.Substring(0, text.Length - 1);

        dialogUI.infoTextController.FadeInMessage(
            text,
            onContinue);

        return false;
    }
}

public class Command_NarratorClear : DialogueCommand
{
    public Command_NarratorClear() { Name = "NARRATOR_CLEAR"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        dialogUI.infoTextController.FadeOutMessage(onContinue);
        return false;
    }
}

public class Command_StartAmbient : DialogueCommand
{
    public Command_StartAmbient() { Name = "START_AMBIENT"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 3))
            return true;

        dialogUI.soundEffects.DialogAmbientSoundSet(int.Parse(arguments[1]), float.Parse(arguments[2]));
        return true;
    }
}

public class Command_StopAmbient : DialogueCommand
{
    public Command_StopAmbient() { Name = "STOP_AMBIENT";  }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {

        if (!AssertArgsLength(arguments, 2))
            return true;

        dialogUI.soundEffects.DialogAmbientSoundStop(float.Parse(arguments[1]));

        return true;
    }
}

public class Command_OverlayFade : DialogueCommand
{
    public Command_OverlayFade() { Name = "OVERLAY_FADE"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 3))
            return true;

        Color color = Color.white;
        switch (arguments[1])
        {
            case "white":
                color = Color.white;
                break;
            case "black":
                color = Color.black;
                break;
            case "alphaWhite":
                color = new Color(1f, 1f, 1f, .25f);
                break;
            default:
                Debug.LogError("Unsupported color " + arguments[1] + " passed to Command_OverlayFade. Defaulting to white.");
                break;
        }

        dialogUI.cameraEffects.TurnOverlayOn(color, float.Parse(arguments[2]));

        return true;
    }
}

public class Command_OverlayFadeOff : DialogueCommand
{
    public Command_OverlayFadeOff() { Name = "OVERLAY_FADE_OFF"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 2))
            return true;

        dialogUI.cameraEffects.TurnOverlayOff(float.Parse(arguments[1]));

        return true;
    }
}

public class Command_SceneJump : DialogueCommand
{
    public Command_SceneJump() { Name = "SCENE_JUMP"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 2))
            return true;

        var scene = CutsceneScene.GetCutsceneSceneByName(arguments[1]);

        var location = scene.GetCameraLocation();
        dialogUI.mainCamera.transform.position =
            new Vector3(location.x, location.y, dialogUI.mainCamera.transform.position.z);

        // Camera tracks last location and tries to tween to it, so call this
        // method to force the last location to be the same as current location
        // to prevent weird behavior.
        // todo: Replace all of this bullshit with cinemachine
        //dialogUI.mainCamera.GetComponent<PlatformerFollowCamera>().SnapToPositionQuickly();

        scene.Init();

        return true;
    }
}

public class Command_ShowScene : DialogueCommand
{
    public Command_ShowScene() { Name = "SHOW_SCENE";}
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 2))
            return true;

        GetScene(arguments).Init();

        return true;
    }
}

public class Command_SpawnVFX : DialogueCommand
{
    public Command_SpawnVFX() { Name = "SPAWN_VFX"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 3))
            return true;

        var scene = CutsceneScene.GetCutsceneSceneByName(arguments[1]);
        var actor = scene.GetActor(arguments[3]);
        scene.SpawnVFX(int.Parse(arguments[2]), actor);
        
        return true;
    }
}

public class Command_HideActor : DialogueCommand
{
    public Command_HideActor() { Name = "HIDE_ACTOR"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 3))
            return true;

        var scene = CutsceneScene.GetCutsceneSceneByName(arguments[1]);
        scene.GetActor(arguments[2]).gameObject.SetActive(false);

        return true;
    }
}

public class Command_ShowActor : DialogueCommand
{
    public Command_ShowActor() { Name = "SHOW_ACTOR"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 3))
            return true;

        var scene = CutsceneScene.GetCutsceneSceneByName(arguments[1]);
        scene.GetActor(arguments[2]).gameObject.SetActive(true);

        return true;
    }
}

public class Command_PlayerWarp : DialogueCommand
{
    public Command_PlayerWarp() { Name = "PLAYER_WARP"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 3))
            return true;

        var scene = CutsceneScene.GetCutsceneSceneByName(arguments[1]);
        var location = scene.GetActor(arguments[2]);

        dialogUI.MovePlayerToLocation(location.transform.position);

        return true;
    }
}

public class Command_PlaySound : DialogueCommand
{
    public Command_PlaySound() { Name = "PLAY_SOUND"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 4))
            return true;

        var scene = CutsceneScene.GetCutsceneSceneByName(arguments[1]);
        var sound = scene.GetSFX(arguments[2]);
        dialogUI.soundEffects.DialogPlaySFX(sound, float.Parse(arguments[3]));

        return true;
    }
}

public class Command_CameraShakeOn : DialogueCommand
{
    public Command_CameraShakeOn() { Name = "CAMERA_SHAKE_ON"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 2))
            return true;

        dialogUI.cameraEffects.TurnGameCameraShakeOn(float.Parse(arguments[1]));

        return true;
    }
}

public class Command_CameraShakeOff : DialogueCommand
{
    public Command_CameraShakeOff() { Name = "CAMERA_SHAKE_OFF"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        dialogUI.cameraEffects.TurnGameCameraShakeOff();

        return true;
    }
}

public class Command_ZoomMainCamera : DialogueCommand
{
    public Command_ZoomMainCamera() { Name = "ZOOM_MAIN_CAMERA"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 4))
            return true;

        dialogUI.cameraEffects.ZoomMainGameCamera(
            float.Parse(arguments[1]), float.Parse(arguments[2]),
            float.Parse(arguments[3]));

        return true;
    }
}

public class Command_VignetteOn : DialogueCommand
{
    public Command_VignetteOn() { Name = "VIGNETTE_ON"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        dialogUI.cameraEffects.ToggleMainCameraVignette(true);

        return true;
    }
}

public class Command_VignetteOff : DialogueCommand
{
    public Command_VignetteOff() { Name = "VIGNETTE_OFF"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        dialogUI.cameraEffects.ToggleMainCameraVignette(false);

        return true;
    }
}

public class Command_HidePlayer : DialogueCommand
{
    public Command_HidePlayer() { Name = "HIDE_PLAYER"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        dialogUI.HidePlayer();

        return true;
    }
}

public class Command_ShowPlayer : DialogueCommand
{
    public Command_ShowPlayer() { Name = "SHOW_PLAYER"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        dialogUI.ShowPlayer();

        return true;
    }
}

public class Command_PlayOverlayVFX : DialogueCommand
{
    public Command_PlayOverlayVFX() { Name = "PLAY_OVERLAY_VFX"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 3))
            return true;

        var scene = CutsceneScene.GetCutsceneSceneByName(arguments[1]);
        var vfx = scene.GetVFXByIndex(int.Parse(arguments[2]));
        dialogUI.cameraEffects.InstantiateOverlayVFX(vfx);

        return true;
    }
}

public class Command_ShowSingleObjectCutscene : DialogueCommand
{
    public Command_ShowSingleObjectCutscene() { Name = "SHOW_CUTSCENE_OBJECT"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 2))
            return true;

        // includes optional parameter
        if (arguments.Length == 3)
            dialogUI.singleCutsceneObjectManager.Begin(arguments[1], onContinue, arguments[2]);
        else
            dialogUI.singleCutsceneObjectManager.Begin(arguments[1], onContinue);

        return false;
    }
}

public class Command_PauseMusic : DialogueCommand
{
    public Command_PauseMusic() { Name = "PAUSE_MUSIC"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        dialogUI.musicManager.PauseMusic();
        return true;
    }
}

public class Command_ResumeMusic : DialogueCommand
{
    public Command_ResumeMusic() { Name = "RESUME_MUSIC"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        dialogUI.musicManager.ResumeMusic();
        return true;
    }
}

public class Command_ActorMove : DialogueCommand
{
    public Command_ActorMove() { Name = "ACTOR_MOVE"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 6))
            return true;

        var scene = CutsceneScene.GetCutsceneSceneByName(arguments[1]);
        var actor = scene.GetActor(arguments[2]);
        Vector2 direction;
        switch (arguments[3])
        {
            case "Up":
                direction = Vector2.up;
                break;
            case "Down":
                direction = Vector2.down;
                break;
            case "Left":
                direction = Vector2.left;
                break;
            case "Right":
                direction = Vector2.right;
                break;
            default:
                direction = Vector2.up;
                break;
        }

        float amount = float.Parse(arguments[4]);
        float duration = float.Parse(arguments[5]);
        actor.transform.DOLocalMove(
            direction * amount, duration)
            .SetRelative(true)
            .SetUpdate(UpdateType.Manual).Play();

        return true;
    }
}

public class Command_CutsceneAction : DialogueCommand
{
    public Command_CutsceneAction() { Name = "CUTSCENE_ACTION"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 3))
            return true;

        var scene = CutsceneScene.GetCutsceneSceneByName(arguments[1]);
        scene.DoAction(arguments[2]);

        return true;
    }
}

public class Command_FlipX : DialogueCommand
{
    public Command_FlipX() { Name = "FLIP_X"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 3))
            return true;

        var scene = CutsceneScene.GetCutsceneSceneByName(arguments[1]);
        var actor = scene.GetActor(arguments[2]);
        actor.FlipX();

        return true;
    }
}

public class Command_TweenCameraToPlayer : DialogueCommand
{
    public Command_TweenCameraToPlayer() { Name = "TWEEN_CAMERA_TO_PLAYER"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        if (!AssertArgsLength(arguments, 2))
            return true;

        dialogUI.TweenCameraToPlayer(float.Parse(arguments[1]));

        return true;
    }
}

public class Command_Quit : DialogueCommand
{
    public Command_Quit() { Name = "QUIT"; }
    public override bool Execute(DialogueUIImplementation dialogUI, Action onContinue, string[] arguments)
    {
        Application.Quit();
        return true;
    }
}