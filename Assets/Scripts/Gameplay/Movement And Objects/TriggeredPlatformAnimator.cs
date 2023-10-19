using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggeredPlatformAnimator : MonoBehaviour
{
    const float ROTATION_SPEED = 50f;

    [Header("Prefab Values")]
    [SerializeField]
    private SpriteRenderer mySprite = null;
    [SerializeField]
    private SpriteRenderer rotator = null;

    [Header("Assets")]
    [SerializeField]
    private Sprite offSprite = null;
    [SerializeField]
    private Sprite onSprite = null;

    [SerializeField]
    private FlickerOverlay FlickerOverlay;

    public void AdvanceFrame(bool isMoving)
    {
        mySprite.sprite = isMoving ? onSprite : offSprite;

        HandleRotation(isMoving);

        FlickerOverlay.AdvanceFrame(isMoving);
    }

    void HandleRotation(bool isMoving)
    {
        var angles = rotator.transform.eulerAngles;

        if (isMoving)
        {
            angles.z += ROTATION_SPEED * Time.deltaTime;
        }
        else
        {
            angles.z = 0f;
        }

        rotator.transform.eulerAngles = angles;
    }
}
