using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelPerfectCameraAspectRatioFitter : MonoBehaviour
{
    float minWidth = 480f;
    float minHeight = 270f;

    Camera myCamera;

    void Start()
    {
        myCamera = GetComponent<Camera>();
        OnUpdateViewportSizeForPixelPerfect();
        UpdateLastSizes();
    }

    int lastWidth;
    int lastHeight;
    private void Update()
    {
        if (lastWidth != Screen.width || lastHeight != Screen.height)
        {
            OnUpdateViewportSizeForPixelPerfect();
            UpdateLastSizes();
        }
    }

    // Making separate method to prevent OnUpdateViewportSizeForPixelPerfect from
    // having side effects that you wouldn't guess from the method name.
    void UpdateLastSizes()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
    }

    void OnUpdateViewportSizeForPixelPerfect()
    {
        var widthFactor = Mathf.Floor(Screen.width / minWidth);
        var heightFactor = Mathf.Floor(Screen.height / minHeight);

        // Use the smaller one
        var factorToUse = widthFactor > heightFactor ? heightFactor : widthFactor;
        
        Rect rect = myCamera.pixelRect;
        rect.width = minWidth * factorToUse;
        rect.height = minHeight * factorToUse;
        rect.x = Mathf.Floor((Screen.width - rect.width) / 2f);
        rect.y = Mathf.Floor((Screen.height - rect.height) / 2f);

        myCamera.pixelRect = rect;
    }
}
