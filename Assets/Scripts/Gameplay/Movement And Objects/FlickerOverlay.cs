using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlickerOverlay : MonoBehaviour
{
    const float OVERLAY_FLICKER_INTERVAL = .2f;
    const float OVERLAY_FLICKER_VARIANCE = .05f;
    const float OVERLAY_FLICKER_MIN = .5f;
    const float OVERLAY_FLICKER_MAX = 1f;

    [SerializeField]
    private SpriteRenderer overlay = null;

    [SerializeField]
    private Sprite overlaySprite = null;

    /// <summary>
    /// Stores timing for overlay flicker. Randomizes between interval - variance
    /// and inverval + variance. Subtract deltaTime every frame, set random
    /// alpha level when reaching zero
    /// </summary>
    float flickerTimer;

    // Set to true if no parent object uses AdvanceFrame, will force this to use Update instead
    // todo: remove this
    public bool AutoRun = false;

    public bool IsActiveAuto = false;

    private void Start()
    {
        SetRandomFlickerValue();
    }

    private void Update()
    {
        if (AutoRun)
            AdvanceFrame(IsActiveAuto);
    }

    public void AdvanceFrame(bool isActive)
    {
        HandleOverlay(isActive);
    }

    void HandleOverlay(bool isActive)
    {
        // Overlay is optional
        if (overlay == null)
            return;

        if (!isActive)
        {
            overlay.enabled = false;
            return;
        }

        overlay.enabled = true;
        overlay.sprite = overlaySprite;

        flickerTimer -= Time.deltaTime;

        if (flickerTimer <= Mathf.Epsilon)
        {
            SetRandomFlickerValue();
            overlay.color = new Color(1f, 1f, 1f,
                Random.Range(OVERLAY_FLICKER_MIN, OVERLAY_FLICKER_MAX));
        }
    }

    void SetRandomFlickerValue()
    {
        flickerTimer = Random.Range(
            OVERLAY_FLICKER_INTERVAL - OVERLAY_FLICKER_VARIANCE,
            OVERLAY_FLICKER_INTERVAL + OVERLAY_FLICKER_VARIANCE);
    }
}
