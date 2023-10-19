using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Created for when you have sprites with identical animations (sprite sheets) and you want 
/// to re-use the animation for one without manually having to recreate it for
/// the other.
/// </summary>
public class SpriteSwapper : MonoBehaviour 
{
	// The prefix of MY new sprites - should be everything in their names up to and including the last underscore
	public string mySpriteNamePrefix;

	public Sprite[] sprites;

	SpriteRenderer spriteRenderer;

	// Store this globally because sprites are global references anyway
	static Dictionary<string, Sprite> spritesByName = new Dictionary<string, Sprite>();

	// For <mySpriteNamePrefix>, get mapping of start sprites = destination sprites
	static Dictionary<string, Dictionary<Sprite, Sprite>> spriteMapping = new Dictionary<string, Dictionary<Sprite, Sprite>>();

	void Start()
	{
		spriteRenderer = GetComponent<SpriteRenderer> ();

		if (!spriteMapping.ContainsKey(mySpriteNamePrefix))
		{
			spriteMapping [mySpriteNamePrefix] = new Dictionary<Sprite, Sprite> ();
		}

		if (sprites.Length > 0)
		{
			if (spritesByName.ContainsKey(sprites[0].name))
			{
				//our set of sprites has already been indexed
				return;
			}
		}

		foreach (Sprite s in sprites)
		{
			spritesByName [s.name] = s;
		}
	}

	bool visible = false;
	void OnBecameVisible()
	{
		visible = true;
	}

	void OnBecameInvisible()
	{
		visible = false;
	}

	void LateUpdate()
	{
		if (!visible)
			return;

		if (spriteMapping [mySpriteNamePrefix].ContainsKey(spriteRenderer.sprite))
		{
			spriteRenderer.sprite = spriteMapping [mySpriteNamePrefix][spriteRenderer.sprite];
			return;
		}

		string originalName = spriteRenderer.sprite.name;
		string[] split = originalName.Split ('_');
		string suffix = split [split.Length - 1];

		Sprite newSprite = spritesByName [mySpriteNamePrefix + suffix];

		if (!newSprite)
		{
			Debug.LogError ("Error: Sprite not found for suffix " + suffix.ToString () + " from sprite name set " + mySpriteNamePrefix);
		}

		spriteMapping [mySpriteNamePrefix] [spriteRenderer.sprite] = newSprite;

		spriteRenderer.sprite = newSprite;
	}
}
