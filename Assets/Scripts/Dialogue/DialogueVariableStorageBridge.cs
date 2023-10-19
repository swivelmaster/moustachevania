using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueVariableStorageBridge : Yarn.Unity.VariableStorageBehaviour
{
    // Kind of needs to be a singleton because CutsceneTrigger objects need to access it.
    static DialogueVariableStorageBridge Instance;

    public bool Verbose = false;

    private void Awake()
    {
        Instance = this;
    }

    public static DialogueVariableStorageBridge GetInstance() { return Instance; }

    public void SetPersistenceManager(PersistenceManager persistenceManager)
    {
        this.persistenceManager = persistenceManager;
    }

    PersistenceManager persistenceManager;

    public static Dictionary<string, bool> unsavedVariables = new Dictionary<string, bool>();

    bool VariableIsAlreadySet(string name)
    {
        return unsavedVariables.ContainsKey(name) ||
            persistenceManager.savedGame.DialogueVariables.ContainsKey(name);
    }

    bool GetVariable(string name)
    {
        if (unsavedVariables.ContainsKey(name))
            return unsavedVariables[name];

        if (persistenceManager.savedGame.DialogueVariables.ContainsKey(name))
            return persistenceManager.savedGame.DialogueVariables[name];

        return false;
    }

    void SetVariable(string name, bool value)
    {
        unsavedVariables[name] = value;
    }

    /// <summary>
    /// Weird thing going on here? YarnSpinner asks for variables to start with $
    /// but doesn't remove it when passing the variable name in, so that makes
    /// things more confusing. Going to just scrub it from variables so we can
    /// use $ in the yarn scripts but not everywhere else.
    /// </summary>
    /// <param name="variableName"></param>
    /// <param name="value"></param>
    public override void SetValue(string variableName, Yarn.Value value)
    {
        variableName = variableName.Replace("$", "");

        if (VariableIsAlreadySet(variableName)
            && GetVariable(variableName) == value.AsBool)
            return;

        SetVariable(variableName, value.AsBool);
    }

    public override Yarn.Value GetValue(string variableName)
    {
        if (variableName.StartsWith("$EXT_"))
        {
            if (Verbose)
                Debug.Log("Retrieving external variable " + variableName);

            return GetExternalVariable(variableName);
        }

        return new Yarn.Value(GetVariable(variableName.Replace("$", "")));
    }

    const string EXTERNAL_VARIABLE_CHEESE_COUNT = "$EXT_cheeseCount";

    Yarn.Value GetExternalVariable(string variableName)
    {
        switch (variableName)
        {
            case EXTERNAL_VARIABLE_CHEESE_COUNT:
                return new Yarn.Value(CollectibleManager.Instance.currentScore);
        }

        return Yarn.Value.NULL;
    }

    public bool GetBool(string variableName)
    {
        return GetValue(variableName).AsBool;
    }

    public override void ResetToDefaults()
    {
        // Called by the DialogRunner on init, and probably shouldn't be.
        // Make this do nothing and we can find a better way to reset
        // the cutscene data for saved games.
    }
}
