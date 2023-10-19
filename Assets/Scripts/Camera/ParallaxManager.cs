using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxManager : MonoBehaviour
{
    [HideInInspector]
    public Camera MainCamera;

    public ParallaxSettings LayerSettings;
    protected ParallaxObject[] BackgroundObjects;

    protected Dictionary<ParallaxObject, Vector2> OriginalPositions = new Dictionary<ParallaxObject, Vector2>();
    
    void Awake()
    {
        BackgroundObjects = GetComponentsInChildren<ParallaxObject>();
        foreach (var o in BackgroundObjects)
        {
            OriginalPositions[o] = o.transform.position;
            o.parallaxSettings = LayerSettings.GetByName(o.LayerName);
        }
    }

    private void Start()
    {
        MainCamera = GameStateManager.Instance.sceneSettings.gameplayCamera;
    }

    void LateUpdate()
    {
        foreach (var o in BackgroundObjects)
        {
            UpdatePosition(o);
        }
    }

    /// <summary>
    /// Calculates position relative to the object's original position
    /// This would put every object at its origin when the camera is centered on it.
    /// </summary>
    /// <param name="o"></param>
    protected virtual void UpdatePosition(ParallaxObject o)
    {
        o.transform.position = (Vector2)MainCamera.transform.position + (MainCamera.transform.position * OriginalPositions[o] * o.parallaxSettings.ScrollSpeedMultiplier);
    }
}
