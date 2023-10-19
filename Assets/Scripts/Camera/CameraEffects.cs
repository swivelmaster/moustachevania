using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Experimental.Rendering.Universal;

public class CameraEffects : MonoBehaviour
{
    //[SerializeField]
    //private Camera PrimaryCamera = null;

    [SerializeField]
    private Camera gameplayCamera;

    [SerializeField]
    private SpriteRenderer mainColorOverlay = null;

//    [SerializeField]
//    private Transform MainRenderTexture = null;
    //Vector3 mainRenderTextureStartScale;

    [SerializeField]
    private MainCameraPostprocessingEffects mainGameCameraEffects = null;

    [SerializeField]
    private GameCameraAdapter mainGameCameraScript = null;

    //[SerializeField]
    // "Zoom out" the main view by just scaling ortho size
    //private Camera mainCamera = null;

    [SerializeField]
    private Transform overlayVFXDestination = null;

    float startingGameplayCameraOrthoSize;

    private void Awake()
    {
        mainColorOverlay.enabled = false;
        startingGameplayCameraOrthoSize = gameplayCamera.orthographicSize;
        //mainRenderTextureStartScale = MainRenderTexture.localScale;
    }

    public void TurnOverlayOn(Color color, float fadeTime)
    {
        mainColorOverlay.enabled = true;

        if (fadeTime > Mathf.Epsilon)
        {
            var fadedOutColor = color;
            fadedOutColor.a = 0f;
            mainColorOverlay.color = fadedOutColor;
            mainColorOverlay.DOColor(color, fadeTime);
        }
        else
        {
            mainColorOverlay.color = color;
        }
    }

    public void TurnOverlayOff(float fadeTime)
    {
        if (fadeTime > Mathf.Epsilon)
        {
            var fadedOutColor = mainColorOverlay.color;
            fadedOutColor.a = 0f;
            mainColorOverlay.DOColor(fadedOutColor, fadeTime).onComplete =
                () => mainColorOverlay.enabled = false;
        }
        else
        {
            mainColorOverlay.enabled = false;
        }
    }

    public void TurnGameCameraShakeOn(float amount)
    {
        mainGameCameraScript.ShakeOn(amount);
    }

    public void TurnGameCameraShakeOff()
    {
        mainGameCameraScript.ShakeOff();
    }

    Tween currentZoomTween;
    public void ZoomMainGameCamera(float from, float to, float duration)
    {
        if (currentZoomTween != null)
            currentZoomTween.Kill(true);

        gameplayCamera.orthographicSize = (1/from) * startingGameplayCameraOrthoSize;
        gameplayCamera.DOOrthoSize((1/to) * startingGameplayCameraOrthoSize, duration)
            .SetEase(Ease.InOutQuad);

        // Left over from when I was using a render texture because
        // I was trying to user pixel perfect camera when camera stacking
        // wasn't supported.
        // Now I'm using ProCamera2D's PPC instead of the Unity one.
        // Does the latest version for URP support PPC? I don't know.
        //
        //MainRenderTexture.localScale = mainRenderTextureStartScale * from;
        //currentZoomTween = MainRenderTexture
        //    .DOScale(mainRenderTextureStartScale * to, duration)
        //    .SetEase(Ease.InOutQuad)
        //    .SetUpdate(UpdateType.Manual);
    }

    public void ToggleMainCameraVignette(bool enabled)
    {
        mainGameCameraEffects.ToggleVignette(enabled);
    }

    public GameObject InstantiateOverlayVFX(GameObject go)
    {
        return Instantiate(go, overlayVFXDestination.transform.position, go.transform.rotation, overlayVFXDestination) as GameObject;
    }

}
