using UnityEngine;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AdjustableObjectDrawGrid : MonoBehaviour
{
    public Transform SpriteDestination;

    public const int GRID_SIZE = 21;
    public const int GRID_CENTER_INDEX = 10;

    public AdjustableObjectGridRow[] Grid;

    public AdjustableObjectTileset Tileset;

    [Header("Half offsets in either direction for additional flexibility.")]
    public bool HalfXOffset;
    public bool HalfYOffset;

    CompositeCollider2D CompositeCollider;

    [HideInInspector]
    public SpriteRenderer[] SpriteRenderers { private set; get; }

    [SerializeField]
    private AdjustableObject MyAO = null;

    /// <summary>
    /// Needed for when an object is broken and we need to
    /// generate shatter particles from it.
    /// </summary>
    public Mesh CachedCompositeColliderMesh { private set; get; }

#if UNITY_EDITOR
    public void InitializeGrid()
    {
        Grid = new AdjustableObjectGridRow[GRID_SIZE]
        {
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow() ,
            new AdjustableObjectGridRow()
        };

        for (int i = 0; i < GRID_SIZE; i++)
        {
            Grid[i].mask = new bool[GRID_SIZE];
        }

        RedrawGrid(AdjustableObjectTileType.Normal);

        EditorUtility.SetDirty(this);
    }
#endif

    [HideInInspector]
    public List<BoxCollider2D> boxColliders { private set; get; }

    private void Awake()
    {
        CompositeCollider = SpriteDestination.GetComponent<CompositeCollider2D>();
        CachedCompositeColliderMesh = CompositeCollider.CreateMesh(false, false);
        SpriteRenderers = SpriteDestination.GetComponentsInChildren<SpriteRenderer>();

        // Prevent it from regenerating geo when rotating
        // Otherwise there is a performance hiccup when rotating
        CompositeCollider.generationType = CompositeCollider2D.GenerationType.Manual;

        // Disable child colliders
        boxColliders = new List<BoxCollider2D>( SpriteDestination.GetComponentsInChildren<BoxCollider2D>() );

        //foreach (var collider in colliders)
        //    Destroy(collider);
    }

    public void RedrawGrid()
    {
        // Call back out to AO because it knows grid type,
        // so it'll call back into the overloaded method
        // with the correct type.
        MyAO.RedrawGridOnly();
    }

    public void RedrawGrid(AdjustableObjectTileType type)
    {
        RedrawTiles(type);
        SetContainerPosition();
    }

    void RedrawTiles(AdjustableObjectTileType type)
    {
        ClearAllTiles();

        for (var outer = 0; outer < GRID_SIZE; outer++)
        {
            for (var inner = 0; inner < GRID_SIZE; inner++)
            {
                if (Grid[outer].mask[inner])
                {
                    var o = PlaceTileAt(inner, outer);
                    o.GetComponent<SpriteRenderer>().sprite = Tileset.GetSpriteForTileType(type);
                }
            }
        }

        SpriteRenderers = SpriteDestination.GetComponentsInChildren<SpriteRenderer>();
    }

    public void SetTilesType(AdjustableObjectTileType type)
    {
        foreach (var renderer in SpriteRenderers)
            renderer.sprite = Tileset.GetSpriteForTileType(type);
    }

    void ClearAllTiles()
    {
        for (int i = SpriteDestination.childCount; i > 0 ; i--)
        {
            if (Application.isPlaying)
                Destroy((SpriteDestination.GetChild(0)).gameObject);
            else
                DestroyImmediate((SpriteDestination.GetChild(0)).gameObject);
        }

        SpriteRenderers = new SpriteRenderer[0];
    }

    GameObject PlaceTileAt(int x, int y)
    {
#if UNITY_EDITOR
        // Using prefab utility because we're doing this in-editor too
        // and we need to maintain the prefab connection, otherwise
        // if we need to make a chance to the tilebox prefab and we've already
        // made everything, we're kinda fucked
        GameObject o = PrefabUtility.InstantiatePrefab(Tileset.TileBoxPrefab, SpriteDestination.transform) as GameObject;
#else
        // Including an alternate version in case we need this at run-time.
        GameObject o = Instantiate(Tileset.TileBoxPrefab, SpriteDestination.transform) as GameObject;
#endif
        o.transform.position = new Vector3(
                SpriteDestination.transform.position.x + (float)x - (float)GRID_CENTER_INDEX,
                SpriteDestination.transform.position.y + -1f * ((float)y - (float)GRID_CENTER_INDEX), 0f);
        return o;
    }

    public void SetContainerPosition()
    {
        SpriteDestination.transform.position = new Vector3(
            transform.position.x + (HalfXOffset ? .5f : 0f),
            transform.position.y + (HalfYOffset ? .5f : 0f),
            0f
        );
    }

    public void SetOpacity(Color tintColor)
    {
        foreach (var renderer in SpriteRenderers)
            renderer.color = tintColor;
    }

    /// <summary>
    /// Used for the breakable to get texture info to use for break particle
    /// </summary>
    /// <returns>A SpriteRenderer, can be null so look out!</returns>
    public SpriteRenderer GetExampleSprite()
    {
        if (SpriteRenderers.Length == 0)
            return null;

        return SpriteRenderers[0];
    }

    /// <summary>
    /// Set shrink = true if AO can damage player, since we want
    /// to be able to shrink the collision to make the spikey AO's more
    /// forgiving
    /// </summary>
    /// <param name="shouldCollide"></param>
    /// <param name="shrink"></param>
    public void SetCollisionState(bool shouldCollide, bool shrink)
    {
        // todo: get this working again
        // CompositeCollider.isTrigger = shouldCollide;

        // todo: Adjust this behavior when re-enabling composite colliders?
        foreach (var collider in boxColliders)
        {
            collider.size = new Vector2(
                shrink ? SHRINK_COLLISION_SIZE : DEFAULT_COLLISION_SIZE,
                shrink ? SHRINK_COLLISION_SIZE : DEFAULT_COLLISION_SIZE);

            collider.enabled = shouldCollide;
        }
    }

    const float DEFAULT_COLLISION_SIZE = 1f;
    const float SHRINK_COLLISION_SIZE = .75f;

#if UNITY_EDITOR

    static Vector3 _1 = new Vector3(1f, 1f, 1f);

    private void OnDrawGizmos()
    {
        if (!_editorRotationGizmosOn)
            return;

        var rotation = this.MyAO.IsSquareType() ? Vector3.zero :
            new Vector3(0f, 0f,
            (this.MyAO.TargetType == AdjustableObjectTargetType.Circle
            ? _editorCircleRotation : _editorTriangleRotation) * -90f);

        foreach (Transform thing in SpriteDestination.transform)
            Gizmos.DrawWireCube(GameUtils.RotatePointAroundPivot(thing.position, transform.position, rotation), _1);
    }

    public static int _editorTriangleRotation = 0;
    public static int _editorCircleRotation = 0;

    public static bool _editorRotationGizmosOn = true;

    [MenuItem("Moustachevania/Rotate Triangle AO's Right _9", false, 10)]
    public static void EditorTriangleRotateLeft()
    {
        _editorTriangleRotation += 1;
        SceneView.RepaintAll();
    }

    [MenuItem("Moustachevania/Rotate Triangle AO's Left _7", false, 11)]
    public static void EditorTriangleRotateRight()
    {
        _editorTriangleRotation -= 1;
        SceneView.RepaintAll();
    }

    [MenuItem("Moustachevania/Rotate Circle AO's Left _4", false, 12)]
    public static void EditorCircleRotateLeft()
    {
        _editorCircleRotation += 1;
        SceneView.RepaintAll();
    }

    [MenuItem("Moustachevania/Rotate Circle AO's Right _6", false, 13)]
    public static void EditorCircleRotateRight()
    {
        _editorCircleRotation -= 1;
        SceneView.RepaintAll();
    }

    [MenuItem("Moustachevania/Reset All Rotations _8", false, 14)]
    public static void EditorResetAllRotations()
    {
        _editorCircleRotation = 0;
        _editorTriangleRotation = 0;
        SceneView.RepaintAll();
    }

    [MenuItem("Moustachevania/Toggle AO Rotation Gizmos")]
    public static void ToggleAORotationGizmos()
    {
        _editorRotationGizmosOn = !_editorRotationGizmosOn;
    }

#endif
}

[System.Serializable]
public class AdjustableObjectGridRow
{
    public bool[] mask;
}

#if UNITY_EDITOR
[CustomEditor(typeof(AdjustableObjectDrawGrid))]
public class AdjustableObjectDrawGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var o = target as AdjustableObjectDrawGrid;

        if (o.Grid.Length == 0)
            o.InitializeGrid();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Triangle r: " + AdjustableObjectDrawGrid._editorTriangleRotation.ToString());
        EditorGUILayout.LabelField("Circle r: " + AdjustableObjectDrawGrid._editorCircleRotation.ToString());

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("SpriteDestination"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Tileset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MyAO"));

        EditorGUILayout.Separator();

        bool dirty = false;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("HalfXOffset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("HalfYOffset"));

        EditorGUILayout.Separator();

        GUILayout.Label("Sprite Grid");

        if (GUILayout.Button("Initialize Grid"))
            o.InitializeGrid();

        try
        {
            dirty = dirty || DrawEditGrid(o);
        }
        catch
        {
            // Exception can only happen during a horizontal block
            EditorGUILayout.EndHorizontal();

            GUI.color = Color.red;
            GUILayout.Label("ERROR: GRID LENGTH MISMATCH.");
            GUILayout.Label("Recommend initializing.");
            GUI.color = Color.white;
        }

        if (dirty)
        {
            EditorUtility.SetDirty(o);
            o.RedrawGrid();
        }

        serializedObject.ApplyModifiedProperties();

        o.SetContainerPosition();
    }

    bool DrawEditGrid(AdjustableObjectDrawGrid o)
    {
        bool dirty = false;

        for (int outer = 0; outer < AdjustableObjectDrawGrid.GRID_SIZE; outer++)
        {
            EditorGUILayout.BeginHorizontal();

            for (int inner = 0; inner < AdjustableObjectDrawGrid.GRID_SIZE; inner++)
            {
                bool result;

                if (outer == AdjustableObjectDrawGrid.GRID_CENTER_INDEX
                    && inner == AdjustableObjectDrawGrid.GRID_CENTER_INDEX)
                {
                    GUI.color = Color.blue;
                    result = EditorGUILayout.Toggle(o.Grid[outer].mask[inner]);
                    GUI.color = Color.white;
                }
                else
                {
                    result = EditorGUILayout.Toggle(o.Grid[outer].mask[inner]);
                }

                if (result != o.Grid[outer].mask[inner])
                {
                    o.Grid[outer].mask[inner] = result;
                    dirty = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        return dirty;
    }
}

#endif