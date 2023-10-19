using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Developer camera mode for taking screenshots of the scenery.
public class CameraMode : MonoBehaviour
{

    float speed = 10f;

    public SpriteRenderer Indicator;

    public void AdvanceFrame(ControlInputFrame input)
    {
        transform.position += new Vector3(
            input.RawHorizontal * Time.deltaTime * speed,
            input.RawVertical * speed * Time.deltaTime,
            0f
        );

        // Hide indicator sprite when holding jump
        // for screenshots
        Indicator.enabled = !input.JumpButtonDownHold;
    }
}
