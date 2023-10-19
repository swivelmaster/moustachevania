using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperJumpEffectManager : MonoBehaviour
{
    [SerializeField]
    private GameObject SuperJumpField = null;

    [SerializeField]
    private ParticleSystem SuperJumpParticleSystem = null;

    private void Start()
    {
        SuperJumpField.SetActive(false);
        SuperJumpParticleSystem.Stop();
    }

    bool superJumpEnabled = false;

    // Maintain state in case we want to manage transitions
    public void Enable()
    {
        if (superJumpEnabled)
            return;

        SuperJumpField.SetActive(true);
        SuperJumpParticleSystem.Play();

        superJumpEnabled = true;
    }

    public void Disable()
    {
        if (!superJumpEnabled)
            return;

        SuperJumpField.SetActive(false);
        SuperJumpParticleSystem.Stop();

        superJumpEnabled = false;
    }
}
