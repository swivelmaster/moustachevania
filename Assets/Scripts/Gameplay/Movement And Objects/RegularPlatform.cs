using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Used for quickly syncing collider and sprite size for platforms.
/// </summary>
#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class RegularPlatform : MonoBehaviour
{
    public int Width = 1;
    public int Height = 1;

#if UNITY_EDITOR
    private void Start()
    {
        if (Application.isPlaying)
        {
            Destroy(this);
            return;
        }

        var size = GetComponent<BoxCollider2D>().size;
        Width = Mathf.RoundToInt(size.x);
        Height = Mathf.RoundToInt(size.y);
    }

    private void Update()
    {
        GetComponent<BoxCollider2D>().size = new Vector2((float)Width, (float)Height);
        GetComponent<SpriteRenderer>().size = new Vector2((float)Width, (float)Height);
        EditorUtility.SetDirty(gameObject);
    }
#endif
}
