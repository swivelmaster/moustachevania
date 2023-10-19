using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(UniqueId))]
public class ManagedTriggeredMovement : TriggeredMovement
{
    const float FADE_LENGTH = 1f;

    [SerializeField]
    private float PercentOfRangeToSpawnNew = 0.5f;

    [SerializeField]
    private float spawnSlideIntoPlaceTime = 1f;

    bool initAsSpawned = false;
    bool hitSpawnTrigger = false;

    bool ready = false;
    bool forceTriggerWhenDoneSliding = false;

    public UniqueId myId { private set; get; }

    public void InitAsSpawned()
    {
        initAsSpawned = true;
    }

    public override void Start()
    {
        base.Start();

        myId = GetComponent<UniqueId>();
        TriggeredMovementManager.instance.Register(this);

        if (!initAsSpawned)
        {
            ready = true;
            return;
        }

        myRenderers.ForEach(r => r.enabled = false);
        myCollider.enabled = false;
        StartCoroutine(MoveToStart());
    }

    IEnumerator MoveToStart()
    {
        yield return new WaitForEndOfFrame();

        myRenderers.ForEach(r => r.enabled = true);
        myCollider.enabled = true;

        Vector2 amountFrom = MyMovementType == MovementType.LeftRight ? Vector2.left : Vector2.up;
        if (startReversed) amountFrom *= -1f;
        amountFrom *= MyMovementType == MovementType.LeftRight ? myCollider.bounds.size.x : myCollider.bounds.size.y;

        myCollider.attachedRigidbody.DOMove(transform.position + (Vector3)amountFrom, spawnSlideIntoPlaceTime).
            From().OnComplete(OnMoveToStartComplete);
    }

    void OnMoveToStartComplete()
    {
        ready = true;
    }

    public override void RegisterToReset()
    {
        // Do nothing!
    }

    public override void HitReverseTrigger()
    {
        triggered = false;
        myRenderers.ForEach(r => r.DOFade(0f, FADE_LENGTH));
        StartCoroutine(OnFadeComplete());
    }

    IEnumerator OnFadeComplete()
    {
        yield return new WaitForSeconds(FADE_LENGTH + .01f);
        Destroy(gameObject);
    }

    public override void Trigger()
    {
        if (!triggered)
        {
            if (!ready)
            {
                forceTriggerWhenDoneSliding = true;
                return;
            }
        }

        base.Trigger();
    }

    public override void PhysicsStep()
    {
        base.PhysicsStep();

        if (!hitSpawnTrigger && NormalizePctTraversed() >= PercentOfRangeToSpawnNew)
        {
            hitSpawnTrigger = true;
            TriggeredMovementManager.instance.SpawnNew(this);
        }

        if (!triggered && ready && forceTriggerWhenDoneSliding)
        {
            forceTriggerWhenDoneSliding = false;
            Trigger();
        }
    }

    // Pct traversed is stored as a float between 0 and 2 to represent
    // "to point B" as 0-1 and "and back to point A" as 1-2.
    // If "start reversed" is checked, we start at 1 and move to 2, so
    // if we want to wait until a certain % of the way through the path,
    // and we're interested in the distance between the platform and its
    // start position, we need to do the below.
    float NormalizePctTraversed()
    {
        if (PercentTraversed < 1f)
            return PercentTraversed;
        return PercentTraversed - 1f;
    }
}
