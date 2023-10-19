using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class ResetSphereParticleColors : MonoBehaviour
{
    public ResetSphere MySphere;
    public ParticleSystem MyParticles;

    [Header("Don't mess with these")]
    public Color RedStart;
    public Color RedFinish;

    public Color GreenStart;
    public Color GreenFinish;

    public Color BlueStart;
    public Color BlueFinish;

    private void Start()
    {
        ApplyColors();
    }

    public void ApplyColors()
    {
        ParticleSystem.MainModule main = MyParticles.main;

        switch (MySphere.MyColor)
        {
            case ResetSphere.SphereColor.Red:
                main.startColor = new ParticleSystem.MinMaxGradient(RedStart, RedFinish);
                break;
            case ResetSphere.SphereColor.Green:
                main.startColor = new ParticleSystem.MinMaxGradient(GreenStart, GreenFinish);
                break;
            case ResetSphere.SphereColor.Blue:
                main.startColor = new ParticleSystem.MinMaxGradient(BlueStart, BlueFinish);
                break;
        }
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(ResetSphereParticleColors))]
class ResetSphereParticleColorsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Separator();

        if (GUILayout.Button("Apply Colors"))
        {
            (target as ResetSphereParticleColors).ApplyColors();
        }
    }
}

#endif