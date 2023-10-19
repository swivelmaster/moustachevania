using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class ResetSphere : MonoBehaviour
{
    public enum SphereColor {  Red, Green, Blue }

    const float RESET_TIME = 2.5f;

    public static Color FadeOutColor = new Color(1f, 1f, 1f, 0.5f);

    public static List<ResetSphere> AllSpheres;

    public SphereColor MyColor;

    float TimeUsed = -100f;

    [SerializeField]
    private SpriteRenderer MySprite = null;
    [SerializeField]
    private ParticleSystem MyParticles = null;

    [SerializeField]
    private ParticleSystem ActivationParticles = null;

    [Header("Used for custom editor, don't mess with this")]
    public Sprite RedSprite = null;
    public Sprite GreenSprite = null;
    public Sprite BlueSprite = null;

    bool lastReadyValue = true;

    public static void ResetAll()
    {
        foreach (ResetSphere sphere in AllSpheres)
        {
            sphere.ResetFromCheckpoint();
        }
    }

    private void Start()
    {
        AllSpheres.Add(this);
        if (!ColorActive(MyColor))
            DeactivateOnStart();
    }

    public bool Collect()
    {
        if (!IsReady())
            return false;

        TimeUsed = GameplayManager.Instance.GameTime;
        PlayUsedEffects();

        lastReadyValue = false;

        ActivationParticles.Play();

        MainCameraPostprocessingEffects.instance.Punch();

        return true;
    }

    void ResetFromCheckpoint()
    {
        TimeUsed = -100f;
    }

    private void Update()
    {
        if (IsReady() && lastReadyValue == false)
        {
            lastReadyValue = true;
            PlayReadyEffects();
        }
    }

    bool IsReady()
    {
        if (!ColorActive(MyColor))
            return false;

        return GameplayManager.Instance.GameTime >= (TimeUsed + RESET_TIME);
    }

    void DeactivateOnStart()
    {
        lastReadyValue = false;
        MySprite.color = FadeOutColor;
        MyParticles.Stop();
    }

    void PlayUsedEffects()
    {
        MySprite.color = FadeOutColor;
        MyParticles.Stop();
        SoundEffects.instance.resetSphere.Play();
    }

    void PlayReadyEffects()
    {
        MySprite.color = Color.white;
        MyParticles.Play();
    }

    public void SetSprite(Sprite sprite)
    {
        MySprite.sprite = sprite;
    }

    public void SyncParticleColors()
    {
        MyParticles.GetComponent<ResetSphereParticleColors>().ApplyColors();
    }

    public static bool ColorActive(SphereColor color)
    {
        switch (color)
        {
            case SphereColor.Red:
                return PersistenceManager.RedSwitchFound();
            case SphereColor.Green:
                return PersistenceManager.GreenSwitchFound();
            case SphereColor.Blue:
                return PersistenceManager.BlueSwitchFound();
        }

        return false;
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(ResetSphere))]
public class ResetSphereEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Separator();

        ResetSphere sphere = target as ResetSphere;

        if (GUILayout.Button("Make Red"))
        {
            sphere.SetSprite(sphere.RedSprite);
            sphere.MyColor = ResetSphere.SphereColor.Red;
            sphere.SyncParticleColors();
            EditorUtility.SetDirty(target);
        }
        if (GUILayout.Button("Make Green"))
        {
            sphere.SetSprite(sphere.GreenSprite);
            sphere.MyColor = ResetSphere.SphereColor.Green;
            sphere.SyncParticleColors();
            EditorUtility.SetDirty(target);
        }
        if (GUILayout.Button("Make Blue"))
        {
            sphere.SetSprite(sphere.BlueSprite);
            sphere.MyColor = ResetSphere.SphereColor.Blue;
            sphere.SyncParticleColors();
            EditorUtility.SetDirty(target);
        }
    }
}

#endif