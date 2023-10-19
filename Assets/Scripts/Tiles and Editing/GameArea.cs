using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
#endif

public class GameArea : MonoBehaviour
{
    public string AreaName = "Game Area";

#if UNITY_EDITOR

    public GameObject[] parents;
    public GameObject[] gos;

    public void InstantiateAtViewCenter(GameObject go, GameObject parent)
    {
        var position = SceneView.lastActiveSceneView.pivot;
        var newGo = PrefabUtility.InstantiatePrefab(go, parent.transform) as GameObject;
        newGo.transform.position =
            new Vector3(Mathf.Round(position.x), Mathf.Round(position.y), 0f);

        Selection.activeGameObject = newGo;

        // is this necessary?
        EditorUtility.SetDirty(newGo);
    }

#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(GameArea))]
public class GameAreaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var area = target as GameArea;

        if (GUILayout.Button("Set Tilemap Names"))
        {
            var tilemaps = area.GetComponentsInChildren<GameAreaTilemap>();
            foreach (var thing in tilemaps)
            {
                thing.gameObject.name = area.AreaName + " - " + thing.TilemapType.ToString();
                EditorUtility.SetDirty(thing);
            }
        }

        EditorGUILayout.Separator();

        if (area.parents == null || area.gos == null)
        {
            GUILayout.Label("Error: Need to set values of parents and gos");
            return;
        }

        for (int i = 0; i < area.parents.Length; i++)
            if (area.gos[i] != null)
                if (GUILayout.Button("Create " + area.gos[i].name))
                    area.InstantiateAtViewCenter(area.gos[i], area.parents[i]);

    }
}
#endif