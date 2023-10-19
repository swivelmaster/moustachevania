using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Automap hasn't been tested in a while.
public class AutomapManager : GameStateBase
{
    public override GameState GetGameState() { return GameState.Automap; }

    public GameObject AutomapContainer;
    public GameObject AutomapCamera;

    GameStateManager gameStateManager;

    public override void FirstSetup(GameStateManager gameStateManager, SceneSettings sceneSettings)
    {
        // todo: Move UI code to a separate class
        this.gameStateManager = gameStateManager;

        if (!AutomapContainer)
            return;
        AutomapContainer.SetActive(false);
        AutomapCamera.SetActive(false);
    }

    public override void Begin()
    {
        if (!AutomapContainer)
            return;
        AutomapContainer.SetActive(true);
        AutomapCamera.SetActive(true);
    }

    public override void Suspend()
    {
        if (!AutomapContainer)
            return;
        AutomapContainer.SetActive(false);
        AutomapCamera.SetActive(false);
    }
}
