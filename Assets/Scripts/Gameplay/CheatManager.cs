using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheatManager
{
    // This is for cheating... so we can incremnet through the state progression
    // Starts at -1 to indicate that we're not cheating yet ;)
    public List<SavedPlayerState> stateProgressionForCheating = new List<SavedPlayerState>();
    public int stateProgressionIndexForCheating { private set; get; }

    public CheatManager()
    {
        stateProgressionIndexForCheating = -1;
        LoadStateProgressionForCheating();
    }

    public void CheatToNextCheckpoint(CheckpointManager checkpointManager)
    {
        int i = checkpointManager.checkpoints.IndexOf(checkpointManager.currentCheckpoint);

        //Debug.Log ("Current checkpoint index is " + i.ToString ());
        if (i == checkpointManager.checkpoints.Count - 1)
            checkpointManager.currentCheckpoint = checkpointManager.checkpoints[0];
        else
            checkpointManager.currentCheckpoint = checkpointManager.checkpoints[i + 1];

        DebugOutput.instance.DebugTransient("Moving to checkpoint number " + checkpointManager.currentCheckpoint.CheckpointId.ToString());
    }

    public void AdvanceFrame(ControlInputFrame input)
    {
        if (input.Cheat_IncreaseAbility)
        {
            stateProgressionIndexForCheating++;
            if (stateProgressionIndexForCheating >= stateProgressionForCheating.Count)
            {
                stateProgressionIndexForCheating = 0;
            }
            Debug.Log("On next checkpoint update, state progression  will update to " +
                stateProgressionForCheating[stateProgressionIndexForCheating].ToString());
        }
    }

    public void SetAbilityProgressionToEnd()
    {
        // Jump to the end
        stateProgressionIndexForCheating = stateProgressionForCheating.Count - 1;
    }


    public void ResetCheat()
    {
        stateProgressionIndexForCheating = -1;
    }

    void LoadStateProgressionForCheating()
    {
        stateProgressionForCheating.Add(new SavedPlayerState(1, false, false, false, false, false, false));
        stateProgressionForCheating.Add(new SavedPlayerState(1, false, false, false, false, false, true));
        stateProgressionForCheating.Add(new SavedPlayerState(1, true, false, false, false, false, true));
        stateProgressionForCheating.Add(new SavedPlayerState(1, true, true, false, false, false, true));
        stateProgressionForCheating.Add(new SavedPlayerState(2, true, true, false, false, false, true));
        stateProgressionForCheating.Add(new SavedPlayerState(2, true, true, true, false, false, true));
        stateProgressionForCheating.Add(new SavedPlayerState(2, true, true, true, true, false, true));
        stateProgressionForCheating.Add(new SavedPlayerState(2, true, true, true, true, true, true));
    }

}
