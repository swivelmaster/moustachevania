using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
#endif

public class ParallaxObject : MonoBehaviour
{
    public string LayerName;

    // Since this is a reference to data from a scriptable object,
    // let's set this at runtime to avoid weird reference issues.
    [HideInInspector]
    public ParallaxLayerSetting parallaxSettings;
}

#if UNITY_EDITOR
[CustomEditor(typeof(ParallaxObject))]
class ParallaxObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var o = target as ParallaxObject;

        ParallaxManager manager = o.GetComponentInParent<ParallaxManager>();
        if (manager == null)
        {
            Debug.LogError("Couldn't find parent ParallaxManager of ParallaxObject, can't draw custom editor.");
            return;
        }

        string[] choices = new string[manager.LayerSettings.LayerSettings.Length];
        for (int i = 0; i < choices.Length; i++)
            choices[i] = manager.LayerSettings.LayerSettings[i].LayerName;

        var currentSelection = ArrayUtility.IndexOf(choices, o.LayerName);

        var newIndex = EditorGUILayout.Popup(currentSelection, choices);

        if (newIndex != currentSelection)
        {
            o.LayerName = choices[newIndex];
            EditorUtility.SetDirty(o);
        }
    }

}

#endif