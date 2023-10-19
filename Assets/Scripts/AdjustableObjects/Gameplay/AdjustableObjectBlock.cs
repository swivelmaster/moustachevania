using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class AdjustableObjectBlock : MonoBehaviour
{
    public int HorizontalSize = 1;
    public int VerticalSize = 1;

#if UNITY_EDITOR
    public void SyncValues()
    {
        var collider = GetComponent<BoxCollider2D>();
        var renderer = GetComponent<SpriteRenderer>();

        var size = new Vector2(HorizontalSize, VerticalSize);

        collider.size = size;
        renderer.size = size;

        EditorUtility.SetDirty(this);
    }
#endif

}

#if UNITY_EDITOR
[CustomEditor(typeof(AdjustableObjectBlock))]
public class AdjustableObjectBlockEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        (target as AdjustableObjectBlock).SyncValues();
    }
}
#endif