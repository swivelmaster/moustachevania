using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashField : MonoBehaviour
{
    public SpriteRenderer MySprite;
    public ParticleSystem MyParticles;

    public ParticleSystem dashEndParticleSystem;

    private void Start()
    {
        MyParticles.Stop();
        MySprite.enabled = false;
    }

    bool previousActiveValue = false;

    public void HideDashField()
    {
        if (!previousActiveValue)
            return;

        MyParticles.Stop();
        MySprite.enabled = false;
        previousActiveValue = false;

        Instantiate (dashEndParticleSystem, transform.position + (Vector3.right * (MySprite.flipX ? -1f : 1f) * 0.15f), dashEndParticleSystem.transform.rotation);
    }

    public void ShowDashField()
    {
        if (previousActiveValue)
            return;

        MySprite.enabled = true;
        MyParticles.Play();
        previousActiveValue = true;
    }

    public void SetFacing(bool facing)
    {
        MySprite.flipX = facing;
    }
}
