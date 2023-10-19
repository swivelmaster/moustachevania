using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingScreenManager : GameStateBase
{
    public override GameState GetGameState() { return GameState.Loading; }

    GameStateManager gameStateManager;

    public override void FirstSetup(GameStateManager gameStateManager, SceneSettings sceneSettings)
    {
        this.gameStateManager = gameStateManager;
    }
}
