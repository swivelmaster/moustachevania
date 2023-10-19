using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneActor : MonoBehaviour
{
    [SerializeField]
    protected string ActorName = "";

    [SerializeField]
    protected SpriteRenderer ActorSprite = null;

    public string GetActorName() { return ActorName; }
    public void FlipX() { ActorSprite.flipX = !ActorSprite.flipX; }
}
