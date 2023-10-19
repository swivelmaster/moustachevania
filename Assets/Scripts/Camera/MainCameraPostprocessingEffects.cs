using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MainCameraPostprocessingEffects : MonoBehaviour
{
    const float VIGNETTE_VALUE = 0.55f;

    [SerializeField]
    private Volume postProcessingVolume = null;

    public static MainCameraPostprocessingEffects instance { private set; get; }

    LensDistortion lensDistortion;
    MotionBlur motionBlur;
    Vignette vignette;

    int startingPixelsPerUnit;

    // Measuring vs GameTime, which starts at 0
    // So if this starts at 0 it'll trigger a ripple when
    // game starts, hah!
    float punchStartTime = -100f;

    float punchDuration = 0.25f;
    float punchAmount = -0.25f;

    float motionBlurActiveValue = 0.5f;

    private void Start()
    {
        instance = this;

        if (!postProcessingVolume.profile.TryGet<LensDistortion>(out lensDistortion))
        {
            Debug.Log("ERROR: Couldn't get Lens Distortion from the postprocessing profile.");
        }

        if (!postProcessingVolume.profile.TryGet<MotionBlur>(out motionBlur))
        {
            Debug.Log("ERROR: Couldn't get Motion Blur from the postprocessing profile.");
        }

        if (!postProcessingVolume.profile.TryGet<Vignette>(out vignette))
        {
            Debug.Log("ERROR: Couldn't get Vignette from the postprocessing profile.");
        }

        lensDistortion.intensity.value = 0f;
        motionBlur.intensity.value = 0f;
        vignette.intensity.value = 0f;
    }

    private void Update()
    {
        if (punchStartTime + punchDuration > GameplayManager.Instance.GameTime)
        {
            lensDistortion.intensity.value = Mathf.Lerp(
                punchAmount, 0f,
                (GameplayManager.Instance.GameTime - punchStartTime)/punchDuration
            );
        }
        else
        {
            lensDistortion.intensity.value = 0f;
        }
    }

    public void Punch()
    {
        punchStartTime = GameplayManager.Instance.GameTime;
    }

    public void SetMotionBlurEnabled(bool enabled)
    {
        motionBlur.intensity.value = enabled ? motionBlurActiveValue : 0f;
    }

    public void ToggleVignette(bool enabled)
    {
        vignette.intensity.value = enabled ? VIGNETTE_VALUE : 0f;
    }
}
