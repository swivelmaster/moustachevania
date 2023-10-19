using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class AdjustableObjectSprites : MonoBehaviour
{
    // todo: Turn this into a scriptable object
    public Color IconSelectorUsedTint;

    public Sprite ModifierIconBackground;

    [SerializeField]
    private ModifierSprite[] ModifierIcons = new ModifierSprite[] { };
    Dictionary<AdjustableObjectModifierType, Sprite> ModifierIconsDict;

    public static AdjustableObjectSprites Instance { private set; get; }

    void Awake()
    {
        Instance = this;
        ModifierIconsDict = new Dictionary<AdjustableObjectModifierType, Sprite>();
        foreach (var item in ModifierIcons)
        {
            ModifierIconsDict[item.Type] = item.Sprite;
        }
    }

    [System.Serializable]
    public class ModifierSprite
    {
        public AdjustableObjectModifierType Type;
        public Sprite Sprite;
    }

    public Sprite GetSpriteForModifierType(AdjustableObjectModifierType type)
    {
        return ModifierIconsDict[type];
    }
}
