using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public class CutsceneManager : GameStateBase
{
    public override GameState GetGameState() { return GameState.Cutscene; }

    GameStateManager gameStateManager;

    string currentCutscene = "";
    Action onCutsceneComplete;

    DialogueRunner dialogueRunner;
    DialogueVariableStorageBridge dialogueVariableStorageBridge;
    DialogueUIImplementation dialogueUIImplementation;

    public DialogCharacterConfig[] CharacterConfigs;
    Dictionary<string, DialogCharacterConfig> CharacterConfigByName;
    Dictionary<DialogCharacter, DialogCharacterConfig> CharacterConfigByEnum;

    SoundEffects soundEffects;

    public bool shouldShowStartingCutscene { private set; get; }

    public override void FirstSetup(GameStateManager gameStateManager, SceneSettings sceneSettings)
    {
        this.gameStateManager = gameStateManager;

        this.dialogueRunner = sceneSettings.dialogueRunner;
        this.dialogueVariableStorageBridge = sceneSettings.dialogueVariableStorageBridge;
        this.dialogueUIImplementation = sceneSettings.dialogueUIImplementation;

        this.soundEffects = sceneSettings.soundEffects;

        this.dialogueVariableStorageBridge.SetPersistenceManager(gameStateManager.gameplayManager.persistenceManager);

        InitCharacterConfigs();

        dialogueUIImplementation.Init(this, sceneSettings);

        shouldShowStartingCutscene = sceneSettings.ShowDefaultCutscene
            && !dialogueVariableStorageBridge.GetBool(sceneSettings.SeenDefaultCutsceneVariableName);
    }

    void InitCharacterConfigs()
    {
        CharacterConfigByName = new Dictionary<string, DialogCharacterConfig>();
        CharacterConfigByEnum = new Dictionary<DialogCharacter, DialogCharacterConfig>();

        foreach (var config in CharacterConfigs)
        {
            CharacterConfigByName[config.CharacterNameString] = config;
            CharacterConfigByEnum[config.Character] = config;

            config.soundEffects = soundEffects;
        }

        // Hack for two-word names when referenced in script commands
        CharacterConfigByName["MysteriousFigure"] = CharacterConfigByEnum[DialogCharacter.MysteriousFigure];
    }

    public override void Suspend()
    {
        base.Suspend();
    }

    public override void Begin()
    {
        // Handling setup in ConversationStart()
    }

    public override void Resume()
    {
        base.Resume();
    }

    public override void End()
    {
        base.End();
    }

    public override void AdvanceFrame(ControlInputFrame input)
    {
        DOTween.ManualUpdate(Time.deltaTime, Time.unscaledDeltaTime);
        dialogueUIImplementation.AdvanceFrame(input);
        
    }

    public void ConversationStart(string StartNode, Action onCutsceneComplete)
    {
        currentCutscene = StartNode;
        this.onCutsceneComplete = onCutsceneComplete;
        dialogueRunner.StartDialogue(StartNode);
    }

    public void ConversationComplete()
    {
        currentCutscene = "";
        gameStateManager.StateClosedItself(this);
        onCutsceneComplete?.Invoke();
    }

    public DialogCharacterConfig GetCharacterConfigByName(string name)
    {
        return CharacterConfigByName[name];
    }

    public DialogCharacterConfig GetCharacterConfigByEnum(DialogCharacter character)
    {
        return CharacterConfigByEnum[character];
    }
}
