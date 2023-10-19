using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSounds
{
    Player player;
    public SoundEffects sfx { private set; get; }

    public PlayerSounds(Player player)
    {
        sfx = SoundEffects.instance;
        this.player = player;
    }

	public void StopSoundsOnPlayerDeath()
	{
        sfx.xJumpReady.Stop();
        sfx.xJumpStart.Stop();
        sfx.xJumpReadyLoop.Stop();
    }

    public void PhysicsStep(PlayerFrameState CurrentFrame)
    {
		if (CurrentFrame.StartedDash)
		{
			sfx.dash.Play();
		}
		else if (CurrentFrame.StartedJump)
		{
			sfx.jump.Play();
		}
		else if (CurrentFrame.HitGroundThisFrame)
		{
			sfx.floorHit.Play();
		}
		else if (CurrentFrame.HitCeiling && !CurrentFrame.PreviousFrame.HitCeiling)
		{
			sfx.wallHit.Play();
		}

		if (CurrentFrame.JustCameOutOfDash())
			sfx.dash.Stop();

		// Ended dash because we hit a wall
		if (CurrentFrame.EndedDash && CurrentFrame.Controller_ShouldCancelDash)
			sfx.bonk.Play();

		if (!CurrentFrame.XJumpCharging)
		{
			sfx.xJumpReady.Stop();
			sfx.xJumpStart.Stop();
			sfx.xJumpReadyLoop.Stop();
		}

		if (CurrentFrame.DestroyedBreakableThisFrame)
            sfx.PlayExplosion(CurrentFrame.ThingsDestroyedThisSuperjump);

		if (CurrentFrame.DestroyedMonsterThisFrame)
			sfx.PlayExplosion();

		if (CurrentFrame.TeleportFailedThisFrame)
			sfx.bonk.Play();

		if (CurrentFrame.MoveCommandResult.DidBounce)
			sfx.adjustableObjectBounce.Play();
	}
}
