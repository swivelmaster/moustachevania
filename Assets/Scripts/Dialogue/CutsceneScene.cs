using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class CutsceneScene : MonoBehaviour
{
    // Since we don't allow cutscenes jumping between scenes,
    // clear the list when a new scene loads so we don't try to
    // find a cutscene scene object that isn't in the scene we're in.
    static string LastRegisteredScene;
    static Dictionary<string, CutsceneScene> CutsceneScenes;

    CutsceneActor[] SceneActors;

    [SerializeField]
    private string SceneName = "";

    [SerializeField]
    private Transform CameraLocation = null;

    [SerializeField]
    private GameObject objectContainer = null;

    [SerializeField]
    private GameObject[] VFX = new GameObject[0];

    [SerializeField]
    private CutsceneSoundMapping[] CutsceneSFX = new CutsceneSoundMapping[0];

    [SerializeField]
    private CutsceneAction[] CutsceneActions = new CutsceneAction[0];

    public static CutsceneScene GetCutsceneSceneByName(string name)
    {
        return CutsceneScenes[name];
    }

    public static void HideAll()
    {
        if (CutsceneScenes == null)
            return;

        foreach (var scene in CutsceneScenes.Values)
            scene.Hide();
    }

    private void Awake()
    {
        if (gameObject.scene.name != LastRegisteredScene)
        {
            LastRegisteredScene = gameObject.scene.name;
            CutsceneScenes = new Dictionary<string, CutsceneScene>();
        }

        CutsceneScenes[SceneName] = this;

        SceneActors = GetComponentsInChildren<CutsceneActor>();

        objectContainer.SetActive(false);
    }

    public void Init()
    {
        objectContainer.SetActive(true);
        foreach (var actor in SceneActors)
            actor.gameObject.SetActive(true);
    }

    public void Hide()
    {
        // Some pickups have their own cutscene scenes that get 
        // destroyed when the pickups disappear.
        // Kind of awkward BUT as long as other cutscenes don't
        // try to reference them, should be okay.
        if (objectContainer != null)
            objectContainer.SetActive(false);
    }

    public CutsceneActor GetActor(string name)
    {
        foreach (var o in SceneActors)
        {
            if (o.GetActorName() == name)
                return o;
        }

        throw new Exception(
            "Trying to get a cutscene actor " + name +
            " from CutsceneScene " + gameObject.name +
            " that doesn't exist. You naughty boy!");
            
    }

    public Vector2 GetCameraLocation()
    {
        return CameraLocation.position;
    }

    public void SpawnVFX(int index, CutsceneActor actorLocation)
    {
        if (VFX.Length < index)
        {
            throw new Exception("Error: Trying to get VFX with index " + index.ToString() + " that doesn't exist.");
        }

        if (!objectContainer.activeInHierarchy)
        {
            throw new Exception("Error: Trying to spawn VFX in a cutscene container that is not active. Use SCENE_JUMP to activate it first.");
        }

        var prefab = VFX[index];

        var system = Instantiate(prefab,
                actorLocation.transform.position, prefab.transform.rotation,
                objectContainer.transform)
            .GetComponent<ParticleSystem>();
        system.Play();
    }

    public MasterAudioGroupPlayer GetSFX(string name)
    {
        foreach (var sfx in CutsceneSFX)
        {
            if (sfx.ScriptingName == name)
                return sfx;
        }

        throw new Exception("Tried to get sfx " + name + " in cutscene scene " + SceneName + " but it wasn't there.");
    }

    public GameObject GetVFXByIndex(int index)
    {
        if (VFX.Length < index)
        {
            throw new Exception("Error: Trying to get VFX with index " + index.ToString() + " that doesn't exist.");
        }

        return VFX[index];
    }

    public void DoAction(string actionName)
    {
        foreach (var action in CutsceneActions)
            if (action.Name == actionName)
            {
                action.Callback.Invoke();
                return;
            }

        Debug.LogError("Called action " + actionName + " on cutsceneScene " + SceneName + " but it wasn't there.");
    }

#if UNITY_EDITOR
    public void SetSceneNameInEditor(string name)
    {
        this.SceneName = name;
        EditorUtility.SetDirty(this.gameObject);
    }
#endif

}

[Serializable]
public class CutsceneSoundMapping : MasterAudioGroupPlayer
{
    public string ScriptingName;
}

[Serializable]
public struct CutsceneAction
{
    public string Name;
    public UnityEvent Callback;
}

#if UNITY_EDITOR
[CustomEditor(typeof(CutsceneScene))]
public class CutsceneSceneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var cs = target as CutsceneScene;

        if (GUILayout.Button("Auto Name Cutscene Scene"))
        {
            var pickup = cs.GetComponentInChildren<ObjectPickupCutsceneTrigger>();
            if (pickup != null)
            {
                cs.SetSceneNameInEditor("pickupCutscene_" + pickup.PickupType.ToString());
                return;
            }

            Debug.LogWarning("Can't auto-set cutscene scene name.");
        }
    }
}
#endif