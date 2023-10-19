using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryScreenManager : GameStateBase
{
    public override GameState GetGameState() { return GameState.InventoryMenu; }

    public override void FirstSetup(GameStateManager gameStateManager, SceneSettings sceneSettings)
    {

    }

    public override void Begin()
    {
        base.Begin();
    }

    public override void End()
    {

    }

    public override void AdvanceFrame(ControlInputFrame input)
    {
        base.AdvanceFrame(input);
    }



}
