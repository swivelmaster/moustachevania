using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EssentialObjectsAutoSetup : MonoBehaviour
{
    public SceneSettings sceneSettings;
}

#if UNITY_EDITOR

[CustomEditor(typeof(EssentialObjectsAutoSetup))]
public class EssentialObjectsAutoSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Separator();

        var setup = target as EssentialObjectsAutoSetup;

        if (GUILayout.Button("Auto Setup"))
        {
            var uiCanvas = GameObject.Find("Gameplay UI Canvas");
            var dialogCanvas = GameObject.Find("Dialog Canvas");
            var actualCamera = GameObject.Find("UI Camera").GetComponent<Camera>();

            uiCanvas.GetComponent<Canvas>().worldCamera = actualCamera;
            dialogCanvas.GetComponent<Canvas>().worldCamera = actualCamera;

            EditorUtility.SetDirty(uiCanvas);
            EditorUtility.SetDirty(dialogCanvas);

            var cheeseCountHud = uiCanvas.GetComponent<CheeseCountHud>();
            setup.sceneSettings.cheeseCountHud = cheeseCountHud;
            
            var singleCutsceneObject = GameObject.Find("Single Cutscene Object Container");
            setup.sceneSettings.singleCutsceneObjectManager = singleCutsceneObject.GetComponent<SingleCutsceneObject>();

            EditorUtility.SetDirty(setup.sceneSettings);
        }
    }
}

#endif


