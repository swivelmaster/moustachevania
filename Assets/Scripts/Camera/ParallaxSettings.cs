using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="ParallaxSettings", menuName ="Moustachevania/Create Parallax Settings Object")]
public class ParallaxSettings : ScriptableObject
{
    public ParallaxLayerSetting[] LayerSettings;

    public ParallaxLayerSetting GetByName(string name)
    {
        foreach (var setting in LayerSettings)
            if (setting.LayerName == name)
                return setting;

        return null;
    }
}

[System.Serializable]
public class ParallaxLayerSetting
{
    public string LayerName;
    public Vector2 ScrollSpeedMultiplier;
}