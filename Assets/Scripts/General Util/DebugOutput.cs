using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Centralized way to print values to the screen.
/// Use key/value pairs to keep the same values on
/// the screen in the same order between frames to 
/// easily track when things change in a way that 
/// would normally involve spamming the console.
/// </summary>
public class DebugOutput : MonoBehaviour
{
    [SerializeField]
    private TMP_Text debugTextUpdate = null;
    [SerializeField]
    private TMP_Text debugTextFixedUpdate = null;
    [SerializeField]
    private TMP_Text debugTextTransient = null;

    Dictionary<string, string> DebugOutputsUpdate = new Dictionary<string, string>();
    Dictionary<string, string> DebugOutputsFixedUpdate = new Dictionary<string, string>();

    List<TransientDebug> TransientDebugOutputs = new List<TransientDebug>();

    public static DebugOutput instance;

    private void Awake()
    {
        instance = this;
        debugTextUpdate.text = "";
        debugTextFixedUpdate.text = "";
        debugTextTransient.text = "";
    }

    private void Update()
    {
        if (DebugOutputsUpdate.Count > 0)
            HandleDebugOutput();
        else
            debugTextUpdate.text = "";

        if (TransientDebugOutputs.Count > 0)
            HandleTransientOutput();
        else
            debugTextTransient.text = "";
    }

    void HandleDebugOutput()
    {
        debugTextUpdate.text = "OnUpdate:\n";

        foreach (var pair in DebugOutputsUpdate)
        {
            debugTextUpdate.text += pair.Key + ": " + pair.Value + "\n";
        }

        DebugOutputsUpdate.Clear();
    }

    void HandleTransientOutput()
    {
        string text = "";
        var toRemove = new List<TransientDebug>();
        foreach (TransientDebug debug in TransientDebugOutputs)
        {
            debug.seconds -= Time.deltaTime;
            if (debug.seconds <= 0f)
            {
                toRemove.Add(debug);
            }
            text += debug.message + "\n";
        }

        debugTextTransient.text = text;

        foreach (var debug in toRemove)
            TransientDebugOutputs.Remove(debug);
    }

    private void FixedUpdate()
    {
        if (DebugOutputsFixedUpdate.Count == 0)
        {
            debugTextFixedUpdate.text = "";
            return;
        }

        debugTextFixedUpdate.text = "OnFixedUpdate:\n";

        foreach (var pair in DebugOutputsFixedUpdate)
        {
            debugTextFixedUpdate.text += pair.Key + ": " + pair.Value + "\n";
        }

        DebugOutputsFixedUpdate.Clear();
    }

    public void DebugOnUpdate(string name, string value)
    {
        DebugOutputsUpdate[name] = value;
    }

    public void DebugOnFixedUpdate(string name, string value)
    {
        DebugOutputsFixedUpdate[name] = value;
    }

    public void DebugTransient(string message, float seconds = 1.5f)
    {
        Debug.Log("Added " + message.ToString());
        TransientDebugOutputs.Add(new TransientDebug(message, seconds));
    }

    class TransientDebug
    {
        public string message;
        public float seconds;

        public TransientDebug(string message, float seconds)
        {
            this.message = message;
            this.seconds = seconds;
        }
    }

}
