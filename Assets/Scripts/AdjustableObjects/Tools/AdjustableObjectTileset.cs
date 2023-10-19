using System;
using UnityEngine;


[CreateAssetMenu(fileName = "AOTileset", menuName = "Moustachevania/Create AO Tileset", order = 1)]
public class AdjustableObjectTileset : ScriptableObject
{

    public string TilesetName;

    [Header("Always the same")]
    public GameObject TileBoxPrefab;

    [Header("Sparkly Shapes yay")]
    public Sprite TriangleDark;
    public Sprite DiamondDark;
    public Sprite CircleDark;
    public Sprite SquareDark;
    public Sprite SquareBDark;

    public Sprite TriangleLight;
    public Sprite DiamondLight;
    public Sprite CircleLight;
    public Sprite SquareLight;
    public Sprite SquareBLight;

    [Header("Used for UI only at this point")]
    public Sprite EmptySprite;
    public Sprite BackgroundSprite;

    [Header("Probably temporary")]
    public Sprite Normal;
    public Sprite Spike;
    public Sprite Bounce;
    public Sprite NoClip;
    public Sprite Breakable;

    [Header("Probably permanent")]
    public AdjustableObjectTilesetData NormalTiles;

    public Sprite GetSpriteForTileType(AdjustableObjectTileType type)
    {
        switch (type)
        {
            case AdjustableObjectTileType.Normal:
                return Normal;
            case AdjustableObjectTileType.Bounce:
                return Bounce;
            case AdjustableObjectTileType.NoClip:
                return NoClip;
            case AdjustableObjectTileType.Spike:
                return Spike;
            case AdjustableObjectTileType.Breakable:
                return Breakable;
        }

        return Normal;        
    }

    public Sprite GetTargetSpriteFromTarget(AdjustableObjectTargetType shape, bool activated=false)
    {
        switch (shape)
        {
            case AdjustableObjectTargetType.Circle:
                return activated ? CircleLight : CircleDark;
            case AdjustableObjectTargetType.Diamond:
                return activated ? DiamondLight : DiamondDark;
            case AdjustableObjectTargetType.SquareA:
                return activated ? SquareLight : SquareDark;
            case AdjustableObjectTargetType.Triangle:
                return activated ? TriangleLight : TriangleDark;
            case AdjustableObjectTargetType.SquareB:
                return activated ? SquareBLight : SquareBDark;
        }

        throw new ArgumentException("GetShapeSpriteFromEnum fell through the switch statement, this is bad.");
    }

}

[Serializable]
public class AdjustableObjectTilesetData
{
    public Sprite TopLeftCorner;
    public Sprite TopEdge;
    public Sprite TopRightCorner;
    public Sprite LeftEdge;
    public Sprite MiddleFill;
    public Sprite RightEdge;
    public Sprite BottomLeftCorner;
    public Sprite BottomEdge;
    public Sprite BottomRightCorner;
    public Sprite BottomRightEdge;

    public Sprite Standalone;

}

public enum AdjustableObjectTileType
{
    Normal, Spike, Bounce, NoClip, Breakable
}
