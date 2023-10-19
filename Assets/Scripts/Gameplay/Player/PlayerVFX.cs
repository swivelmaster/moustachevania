using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVFX : MonoBehaviour
{
    [SerializeField]
    private GameObject jumpParticle = null;
    [SerializeField]
    private  Transform jumpParticleOrigin = null;

    [SerializeField]
    private ParticleSystem landingParticle = null;

    [SerializeField]
    private GameObject dashFieldPrefab = null;
    DashField dashField;

    [SerializeField]
    private SuperJumpEffectManager superJumpEffectManager = null;
    [SerializeField]
    private GameObject SuperJumpFieldPrefab = null;
    SuperJumpField superJumpField;

    [SerializeField]
    private GameObject OverSpeedInitiateParticlePrefab = null;

    [SerializeField]
    private GameObject OverSpeedParticlePrefab = null;
    GameObject OverSpeedParticle;

    [SerializeField]
    private GameObject TeleportStartParticlePrefab = null;

    [SerializeField]
    private GameObject TeleportSuccessParticlePrefab = null;

    [SerializeField]
    private GameObject TeleportFailParticlePrefab = null;

    [SerializeField]
    private GameObject AdjustableObjectAdjustPrefab = null;

    Player player;

    public void Init(Player player)
    {
        this.player = player;

        dashFieldPrefab = Instantiate(dashFieldPrefab, transform);
        dashFieldPrefab.transform.localPosition = new Vector3(0, 0, 0);
        dashField = dashFieldPrefab.GetComponent<DashField>();
        SetDashFieldActive(false);

        GameObject superJumpFieldObject = Instantiate(SuperJumpFieldPrefab, transform);
        superJumpFieldObject.transform.position = jumpParticleOrigin.transform.position;
        superJumpField = superJumpFieldObject.GetComponent<SuperJumpField>();
        superJumpField.SetState(SuperJumpField.SuperJumpChargeState.None);

        OverSpeedParticle = Instantiate(OverSpeedParticlePrefab, transform);
        OverSpeedParticle.SetActive(false);
    }

    public void AdvanceFrame(PlayerFrameState CurrentFrame)
    {
        if (CurrentFrame.HitGroundThisFrame)
        {
            Instantiate(landingParticle, jumpParticleOrigin.transform.position, landingParticle.transform.rotation);
        }

        if (CurrentFrame.StartedJump)
        {
            GameObject spawned = Instantiate(jumpParticle);
            spawned.transform.position = jumpParticleOrigin.transform.position;
        }

        if (CurrentFrame.CurrentJumpIsSuperJump)
            EnableSuperJump();
        else
            DisableSuperJump();

        SetDashFieldActive(CurrentFrame.DashState == PlayerDashState.Dashing);

        OverSpeedParticle.SetActive(CurrentFrame.OverSpeed);

        if (CurrentFrame.TeleportFailedThisFrame)
            Instantiate(
                TeleportFailParticlePrefab,
                player.teleportDestination,
                TeleportFailParticlePrefab.transform.rotation);

        if (CurrentFrame.TeleportStartedThisFrame)
            Instantiate(
                TeleportStartParticlePrefab,
                transform.position,
                TeleportStartParticlePrefab.transform.rotation);

        if (CurrentFrame.TeleportExecutedOnThisFrame)
            Instantiate(
                TeleportSuccessParticlePrefab,
                transform.position,
                TeleportStartParticlePrefab.transform.rotation);

        if (CurrentFrame.ActionState == PlayerActionState.BoostPause
            && CurrentFrame.PreviousFrame.ActionState != PlayerActionState.BoostPause)
            Instantiate(
                OverSpeedInitiateParticlePrefab,
                transform.position,
                OverSpeedInitiateParticlePrefab.transform.rotation
            );

        if (CurrentFrame.AOChangedThisFrame)
            Instantiate(AdjustableObjectAdjustPrefab,
                transform.position,
                AdjustableObjectAdjustPrefab.transform.rotation
            );
    }

    public void SetDashFieldActive(bool active)
    {
        if (active)
        {
            dashField.ShowDashField();
        }
        else
        {
            dashField.HideDashField();
        }
    }

    public void SetSuperJumpState(SuperJumpField.SuperJumpChargeState state)
    {
        superJumpField.SetState(state);
    }

    public void EnableSuperJump()
    {
        superJumpEffectManager.Enable();
    }

    public void DisableSuperJump()
    {
        superJumpEffectManager.Disable();
    }

    public void SetDashFieldFacing(bool facing)
    {
        dashField.SetFacing(facing);
    }

    public void PlayerIsDead()
    {
        SetDashFieldActive(false);
        superJumpEffectManager.Disable();
    }

}
