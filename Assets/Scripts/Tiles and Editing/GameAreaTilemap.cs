using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameAreaTilemap : MonoBehaviour
{
    public GameAreaTilemapType TilemapType;
}

public enum GameAreaTilemapType
{
    Foreground, Background, DamagePlain, DamageSpecial
}