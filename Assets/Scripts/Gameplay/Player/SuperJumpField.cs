using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperJumpField : MonoBehaviour
{
    public GameObject ChargingObject;
    public GameObject ReadyObject;
    public ParticleSystem ReadyBurst;

    public enum SuperJumpChargeState
    {
        None, Charging, Ready
    }

    public SuperJumpChargeState CurrentState { private get; set; }

    public void SetState(SuperJumpChargeState state)
    {
        if (state != SuperJumpChargeState.None && state == CurrentState)
            return;

        switch (state)
        {
            case SuperJumpChargeState.Charging:
                ChargingObject.SetActive(true);
                ReadyObject.SetActive(false);
                break;
            case SuperJumpChargeState.None:
                ChargingObject.SetActive(false);
                ReadyObject.SetActive(false);
                ReadyBurst.Stop();
                break;
            case SuperJumpChargeState.Ready:
                ReadyBurst.Play();
                ChargingObject.SetActive(false);
                ReadyObject.SetActive(true);
                break;
        }
    }
}
