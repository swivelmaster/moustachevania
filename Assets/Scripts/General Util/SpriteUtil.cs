using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteUtil
{
    public static Dictionary<Sprite, Color> AverageSpriteColors = new Dictionary<Sprite, Color>();

    public static Color GetAverageColorForSprite(Sprite sprite)
    {
        if (!sprite.texture.isReadable)
        {
            Debug.LogError("This sprite is not readable so we can't calculate average color.");
            return Color.white;
        }

        if (AverageSpriteColors.ContainsKey(sprite))
            return AverageSpriteColors[sprite];

        // Thanks https://stackoverflow.com/questions/33663035/how-to-get-the-average-color-of-a-sprite-unity3d
        Color32[] texColors = sprite.texture.GetPixels32();

        int total = texColors.Length;

        float r = 0;
        float g = 0;
        float b = 0;

        for (int i = 0; i < total; i++)
        {
            r += texColors[i].r;
            g += texColors[i].g;
            b += texColors[i].b;
        }

        var newColor = new Color32((byte)(r / total), (byte)(g / total), (byte)(b / total), (byte)255f);
        AverageSpriteColors[sprite] = newColor;

        return AverageSpriteColors[sprite];
    }
}
