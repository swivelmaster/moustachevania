using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class SnapToGrid : MonoBehaviour {

    public bool AutoUpdateChildren = true;
    public float GridSnapAmount = .5f;

    private void Start()
    {
        if (Application.isPlaying)
            Destroy(this);
    }

#if UNITY_EDITOR
    void Update()
	{
        if (AutoUpdateChildren)
            ToggleSnaps(!Selection.Contains(this.gameObject));

        float snap = 1f / GridSnapAmount;

		// Force to grid half position
		transform.position = 
			new Vector3 (Mathf.Round(transform.position.x* snap) / snap, Mathf.Round(transform.position.y* snap) / snap, transform.position.z);		
	}
#endif

    public void ToggleSnaps(bool shouldEnable)
    {
        foreach (SnapToGrid grid in this.GetComponentsInChildren<SnapToGrid>(true))
        {
            if (grid == this)
                continue;

            grid.enabled = shouldEnable;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SnapToGrid))]
class SnapToGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var snap = target as SnapToGrid;

        if (GUILayout.Button("Disable Children"))
        {
            snap.ToggleSnaps(false);
        }
        if (GUILayout.Button("Enable Children"))
        {
            snap.ToggleSnaps(true);
        }
    }
}

#endif
