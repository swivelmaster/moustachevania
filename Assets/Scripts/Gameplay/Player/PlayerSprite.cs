using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSprite : MonoBehaviour
{
    [SerializeField]
    SpriteRenderer playerSprite = null;

    const float JUMP_STRETCH_AMOUNT = 1.5f;
    const float JUMP_STRETCH_DURATION = .15f;
    float jumpLastStart = -100f;

    const float LAND_SQUISH_AMOUNT = .75f;
    const float LAND_SQUISH_DURTION = .15f;
    float lastLand = -100f;

    const float FALL_STRETCH_AMOUNT = 1.25f;
    //const float FALL_STRETCH_EASE_IN_TIME = .5f;
    //float lastFall = -100f;

    // Was handling this with an animation timing type thing (above)
    // but switched to just straight setting stretch based on speed
    const float FALL_STRETCH_START_SPEED = -10f;
    const float FALL_STRETCH_END_SPEED = -13f;

    Vector2 startRelativePosition;
    float spriteHeight;

    bool init = false;

    public void SetFlip(bool flip)
    {
        playerSprite.flipX = flip;
    }

    public void Hide()
    {
        playerSprite.enabled = false;
    }

    public void AdvanceFrame(PlayerFrameState frameState)
    {
        if (!init)
        {
            startRelativePosition = transform.localPosition;
            spriteHeight = playerSprite.bounds.size.y;
            init = true;
        }

        // Probably a better way to do this, todo: refactor
        if (frameState.StartedJump)
        {
            jumpLastStart = GameplayManager.Instance.GameTime;
        }
        else if (frameState.HitGroundThisFrame)
        {
            lastLand = GameplayManager.Instance.GameTime;
        }

        float pctJumpEffect = GameUtils.CountupPct(
            jumpLastStart, GameplayManager.Instance.GameTime, JUMP_STRETCH_DURATION);
        if (pctJumpEffect != 1f)
        {
            SetScale(Mathf.Lerp(JUMP_STRETCH_AMOUNT, 1f, pctJumpEffect));
            return;
        }
        
        float pctSquishEffect = GameUtils.CountupPct(
            lastLand, GameplayManager.Instance.GameTime, LAND_SQUISH_DURTION);
        if (pctSquishEffect != 1f)
        {
            SetScale(Mathf.Lerp(LAND_SQUISH_AMOUNT, 1f, pctSquishEffect));
            return;
        }

        if (!frameState.Grounded && frameState.controllerVelocity.y <= FALL_STRETCH_START_SPEED)
        {
            var pctFallEffect = Mathf.Abs((frameState.controllerVelocity.y - FALL_STRETCH_START_SPEED) / (FALL_STRETCH_END_SPEED - FALL_STRETCH_START_SPEED));
            var lerpResult = Mathf.Lerp(1f, FALL_STRETCH_AMOUNT, pctFallEffect);
            SetScale(lerpResult);
            return;
        }

        SetScale(1f);        
    }

    void SetScale(float scale)
    {
        playerSprite.transform.localScale = new Vector2(1f, scale);
        playerSprite.transform.localPosition =
            startRelativePosition + new Vector2(0f, spriteHeight * .5f * (scale - 1f));
    }
}
