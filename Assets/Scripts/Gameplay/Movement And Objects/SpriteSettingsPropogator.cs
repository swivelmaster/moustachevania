using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpriteSettingsPropogator : MonoBehaviour
{
    public SpriteRenderer SourceSprite;
    public SpriteRenderer[] DestinationSprites;

    public void Propagate()
    {
        foreach (var renderer in DestinationSprites)
        {
            renderer.drawMode = SourceSprite.drawMode;
            renderer.tileMode = SourceSprite.tileMode;
            renderer.size = SourceSprite.size;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpriteSettingsPropogator))]
public class SpriteSettingsPropogatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Propogate Now"))
        {
            var thing = target as SpriteSettingsPropogator;
            thing.Propagate();
            foreach (var s in thing.DestinationSprites)
            {
                EditorUtility.SetDirty(s);
            }
        }
    }
}


#endif