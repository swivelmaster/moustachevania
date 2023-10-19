using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicMovement : GameObjectBase, IFakeDestroyable 
{
	public LayerMask collisionMask;

	public bool debug = false;

	public MovementType MyMovementType = MovementType.None;
	public bool startReversed = false;

	[HideInInspector]
	public Collider2D myCollider;
	[SerializeField]
	private CompositeCollider2D AdjustableObjectCollider = null;

	[Tooltip("Default is 2")]
	public float overrideDefaultSpeed = 0f;
	float speed = 2f;

    public bool FlipSpriteOnChangeDirection = false;

	// Use a square for collision detection for movement 
	public float forceCollisionSizeForMovement = 0f;

	public Animator animator;

	// Hack for grouping together multiple animated enemies in a single moving platform wall thing
	// otherwise we can't easily control their animations from one spot.
	Animator[] animatorsInChildren;

	// There's probably only going to be one but we might as well make this an array anyway.
	ParticleSystem[] particleSystemsInChildren;

	Rigidbody2D rb2d;

	[HideInInspector]
	public Vector2 lastFrameVelocity;

	protected List<SpriteRenderer> myRenderers;

    protected bool movementHandledByParent = false;
    protected BasicMovement movementParent = null;

	// Set this to add an offset to the positional calculation
	// on triggered platforms. Set to GameplayManager.Instance.GameTime to start movement
	// calcs at a given time.
	protected float triggeredTime = 0f;

    List<BasicMovement> childMovements = new List<BasicMovement>();

    public float PercentTraversed { private set; get; }

    // Use this to determine if the player should lock onto a
    // downward-moving vertical platform
    // (otherwise if they're jumping, moving up, and pass over
    // a downward-moving platform, they'll lock onto it when
    // they shouldn't!)
    public bool JustChangedDirection { private set; get; }

	public virtual void Start() {

		SetCollider();

		foreach (BasicMovement basicMovement in GetComponentsInChildren<BasicMovement>())
        {
            if (basicMovement == this)
                continue;
            basicMovement.movementHandledByParent = true;
            childMovements.Add(basicMovement);
        }

		// If this isn't moving, destroy the rigidbody. Efficiency! Yay!
		if (MyMovementType == MovementType.None)
		{
			Destroy (GetComponent<Rigidbody2D>());
		} else
		{
			// Temp special case for BoxCollider2D solution to CompositeCollider problem
			if (myCollider == null)
            {
				rb2d = GetComponentInChildren<Rigidbody2D>();
            }
			else if (AdjustableObjectCollider != null)
            {
				rb2d = AdjustableObjectCollider.transform.GetComponent<Rigidbody2D>();
            }
			else
            {
				rb2d = GetComponent<Rigidbody2D>();
			}
		}

		myRenderers = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer> ());
		animatorsInChildren = GetComponentsInChildren<Animator> ();
		particleSystemsInChildren = GetComponentsInChildren<ParticleSystem> ();

		// Default to this.
		SetAnimation (AnimationDirection.None);	

		FindMovementBoundaries ();

		lastPosition = transform.position;

		Register();
	}

	void SetCollider()
    {
		var ao = GetComponentInChildren<AdjustableObject>();

		// Currently setting collider to null if this is an adjustable object
		// Temp solution for using lots of box colliders instead of a single
		// composite collider. For BasicMovement we only need the collider
		// to get the bounds, but we'll need to do that manually by just
		// calculating stuff based on all those damn colliders.
		// todo: Remove this when CompositeColliders are working again
		// todo: May need to change the order of everything once above is
		// true because any AO that starts with the Damages flag on will not
		// use the composite collider while Damaging, but should still have
		// one attached/generated for if the player turns Damaging off.
		if (ao != null)
        {
			return;
        }

		// Different behavior betwee regular basicMovement platforms
		// and AdjustableObject platforms
		if (AdjustableObjectCollider != null)
		{
			// This will work at some point
			myCollider = AdjustableObjectCollider;
		}
		else
		{
			myCollider = GetComponent<BoxCollider2D>();
		}
	}

    public override void Register()
    {
		ObjectManager.Instance.RegisterGameObject(this);
    }

    protected Vector2 pointA;
	protected Vector2 pointB;

	float distanceToTravel;
	float timeToTraverse;

	// This is in seconds IE seconds this starts offset from its cycle
	float startPositionOffset = 0;

	protected Vector2 lastPosition;

	Bounds GetBounds()
    {
		if (myCollider == null)
		{
			Bounds bounds = new Bounds();
			Vector2 min = new Vector2();
			Vector2 max = new Vector2();
			var grid = GetComponentInChildren<AdjustableObjectDrawGrid>();
			foreach (var collider in grid.boxColliders)
            {
				if (collider.bounds.min.x < min.x)
					min.x = collider.bounds.min.x;
				if (collider.bounds.min.y < min.y)
					min.y = collider.bounds.min.y;

				if (collider.bounds.max.x < max.x)
					max.x = collider.bounds.max.x;
				if (collider.bounds.min.y < max.y)
					max.y = collider.bounds.max.y;
			}

			bounds.SetMinMax(min, max);
			return bounds;
		}

		return myCollider.bounds;
    }

	void FindMovementBoundaries()
	{
		if (MyMovementType == MovementType.None || movementHandledByParent)
			return;

		RaycastHit2D pointAHit;
		RaycastHit2D pointBHit;

		// Beacuse we're moving/casting from center but we actually want to bounce
		// when the EDGE hits the collider, we need to subtract half of the height
		// or width (Depending on if we're moving left/right or up/down) from the 
		// final destination points.
		Vector2 offset;
		Bounds bounds = GetBounds();
		Vector2 startPosition = bounds.center;

		if (debug)
		{
			Debug.Log ("My starting position is " + startPosition.ToString ());
			Debug.DrawLine(startPosition, startPosition + Vector2.up, Color.cyan);
		}

		if (MyMovementType == MovementType.LeftRight)
		{
			pointAHit = Physics2D.Raycast (startPosition, Vector2.left, 100f, collisionMask);
			pointBHit = Physics2D.Raycast (startPosition, Vector2.right, 100f, collisionMask);

			if (debug)
			{
				Debug.DrawRay (startPosition, Vector2.left, Color.green, 100f);
				Debug.DrawRay (startPosition, Vector2.right, Color.red, 100f);
			}

			if (forceCollisionSizeForMovement > 0)
			{
				offset = new Vector2 (forceCollisionSizeForMovement / 2, 0f);
			}
			else 
			{
				offset = new Vector2 (bounds.size.x / 2, 0f);
			}


			pointA = pointAHit.point + offset;
			pointB = pointBHit.point - offset;

			// Prevent translation on the vertical axis
			pointA = new Vector2(pointA.x, rb2d.transform.position.y);
			pointB = new Vector2(pointB.x, rb2d.transform.position.y);
		}
		else 
		{
			pointAHit = Physics2D.Raycast (startPosition, Vector2.up, 100f, collisionMask);
			pointBHit = Physics2D.Raycast (startPosition, Vector2.down, 100f, collisionMask);

			if (debug)
			{
				Debug.DrawRay (startPosition, Vector2.up, Color.green, 100f);
				Debug.DrawRay (startPosition, Vector2.down, Color.red, 100f);
			}

			if (forceCollisionSizeForMovement > 0)
			{
				offset = new Vector2 (0f, forceCollisionSizeForMovement / 2);
			}
			else 
			{
				offset = new Vector2 (0f, bounds.size.y / 2);	
			}

			pointA = pointAHit.point - offset;
			pointB = pointBHit.point + offset;

			// Prevent translation on the horizontal axis
			pointA = new Vector2(rb2d.transform.position.x, pointA.y);
			pointB = new Vector2(rb2d.transform.position.x, pointB.y);
		}
			
		distanceToTravel = Vector2.Distance (pointA, pointB);

		timeToTraverse = overrideDefaultSpeed == 0 ?  distanceToTravel/speed : distanceToTravel/overrideDefaultSpeed;

		// fudge factor to adjust for a change in how we handle movement speed
		// if anyone ever makes another game out of code, remove this line before you start.
		timeToTraverse *= 1.676695f;

		// Sorry, I built it this way.
		if (!startReversed)
		{
			Vector2 temp = pointA;
			pointA = pointB;
			pointB = temp;
		}

		startPositionOffset = (1 - (Vector2.Distance (pointB, startPosition) / distanceToTravel)) * timeToTraverse * .5f;

		if (debug)
		{
			Debug.Log ("BasicMovement info: Unique id is " +
				GetComponent<UniqueId>().uniqueId +
				", pointA " + pointA.ToString () +
				" B: " + pointB.ToString () +
				" startPosOffset " + startPositionOffset.ToString ());
		}
	}

	// IFakeDestroyable... maybe I should change the terminology here.
	public void Stop()
	{
		BecomeInvisible();
	}

	public void Restart()
	{
		BecomeVisible ();
	}

	// Used for zone management but also used by itself for killing enemies
	// so they can become invisible and keep moving in sync with their friends.
	// (This is necessary because if they are killed but the player dies before
	// reaching a checkpoint, they will come back and need to stay in sync!)
	public void BecomeInvisible()
	{
		if (myRenderers.Count > 0)
		{
			// Can become null and still be in the array... hmm
			// todo: fix that later
			myRenderers.ForEach(myRenderer => { if (myRenderer != null) myRenderer.enabled = false; });
		}

		if (particleSystemsInChildren != null)
		{
			foreach (ParticleSystem system in particleSystemsInChildren)
			{
				system.Stop ();
			}	
		}	
	}

	public void BecomeVisible()
	{
		if (myRenderers.Count > 0)
		{
			// There was a bug where TriggeredMovement was being managed by the ZoneCollider
			// even though it destroys itself (so trying to show it again would result in a 
			// null reference)... I fixed that but I'm keeping the null check and the catch
			// in case anything else weird happens.
			try
            {
                myRenderers.ForEach(myRenderer => { if (myRenderers != null) { myRenderer.enabled = true; } });
            }
			catch (MissingReferenceException e)
			{
				Debug.Log ("IT HAPPENED! GameObject name: " + gameObject.name + " " + e.ToString ());
			}
        }

		if (particleSystemsInChildren != null)
		{
			foreach (ParticleSystem system in particleSystemsInChildren)
			{
				system.Play ();
			}	
		}	
	}

	// Use this for zone management.
	bool forceStop = false;
	public void StopMoving()
	{
		forceStop = true;
	}

	public void StartMoving()
	{
		forceStop = false;
	}

    public override void AdvanceFrame()
    {
        // Nothing?
    }

    public override void PhysicsStep() 
	{
		if (forceStop)
		{
			return;
		}

		JustChangedDirection = false;

		if (MyMovementType == MovementType.None)
		{
			if (lastAnimationDirection != AnimationDirection.None)
			{
				SetAnimation (AnimationDirection.None);	
			}

			return;
		}

        if (movementHandledByParent)
            return;

		Vector2 goalPosition;
		float value = (GameplayManager.Instance.FixedGameTime + startPositionOffset - triggeredTime) % timeToTraverse;
		PercentTraversed = (value / timeToTraverse) * 2f;

		if (PercentTraversed <= 1)
		{
			goalPosition = Vector2.Lerp (pointA, pointB, PercentTraversed);	
		}
		else 
		{
			goalPosition = Vector2.Lerp (pointB, pointA, PercentTraversed - 1);	
		}

		Vector2 velocity = goalPosition - lastPosition;

#pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator
		if ((MyMovementType == MovementType.LeftRight && lastFrameVelocity.x == 0f) ||
			(MyMovementType == MovementType.UpDown && lastFrameVelocity.y == 0f))
			JustChangedDirection = true;

		// I'm setting these velocity values to zero because I noticed some 
		// weird floating point stuff going on when debugging. bleh.
		if (MyMovementType == MovementType.LeftRight)
		{
			velocity.y = 0;

            if (Mathf.Sign(velocity.x) != Mathf.Sign(lastFrameVelocity.x) && lastFrameVelocity.x != 0)
            {
                if (FlipSpriteOnChangeDirection)
                {
					myRenderers.ForEach(myRenderer => myRenderer.flipX = !myRenderer.flipX);
                }
				JustChangedDirection = true;
				HitReverseTrigger ();
			}
		}
		else 
		{
			velocity.x = 0;
			if (Mathf.Sign(velocity.y) != Mathf.Sign(lastFrameVelocity.y) && lastFrameVelocity.y != 0)
#pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator

            {
				JustChangedDirection = true;
				HitReverseTrigger ();
			}
		}

		// todo: hack: When MyCollider is null, we're moving an AdjustableObject
		// In an AO, the RigidBody is actually a level deeper than the parent
		// object, so if we use it to move, the insignia will not move with it.
		// In the future we'll need to either move the RB2D or restructure the
		// AO prefab to put the insignia sprite inside the GO with the RB2D
		// This current method is supposedly less efficient for the physics
		// but I'm not using it very much in the demo so it shouldn't be an issue.
		if (myCollider == null)
        {
			transform.position = goalPosition;
        }
		else
        {
			rb2d.MovePosition(goalPosition);
		}

		lastFrameVelocity = velocity;
		lastPosition = goalPosition;


        // Handle this at the parent level.
        // The way this is structured, the child movements don't
        // need to do any movement calculation, BUT when the player
        // lands on a moving platform, they extract the velocities
        // from it in order to determine the right way to move along
        // with it. So we're going to set it from here.
        foreach (BasicMovement basicMovement in childMovements)
        {
            basicMovement.lastFrameVelocity = lastFrameVelocity;
        }

        if (animator != null)
            DetermineProperAnimationValues(velocity);
    }

    void DetermineProperAnimationValues(Vector2 velocity)
    {
        AnimationDirection animationDirection;

        if (velocity.y > 0)
        {
            animationDirection = AnimationDirection.Up;
        }
        else if (velocity.y < 0)
        {
            animationDirection = AnimationDirection.Down;
        }
        else if (velocity.x < 0)
        {
            animationDirection = AnimationDirection.Left;
        }
        else if (velocity.x > 0)
        {
            animationDirection = AnimationDirection.Right;
        }
        else
        {
            animationDirection = AnimationDirection.None;
        }

        // Optimization here - setting animation every frame is more expensive than expected
        if (animationDirection != lastAnimationDirection)
        {
            lastAnimationDirection = animationDirection;
            SetAnimation(animationDirection);
        }
    }

	enum AnimationDirection {
		None, Left, Right, Up, Down
	}

	AnimationDirection lastAnimationDirection = AnimationDirection.None;

	void SetAnimation(AnimationDirection direction)
	{
		if (!animator)
		{
			return;
		}

		SetAnimation (direction, animator);

		if (animatorsInChildren.Length > 0)
		{
			foreach (Animator _animator in animatorsInChildren)
			{
				SetAnimation (direction, _animator);
			}
		}
	}

	void SetAnimation(AnimationDirection direction, Animator _animator)
	{
		// First, if we're not moving to the right, don't flip horizontal
		myRenderers.ForEach(
			myRenderer => myRenderer.flipX = direction == AnimationDirection.Right);

		switch (direction)
		{
		case AnimationDirection.None:
                _animator.SetInteger ("Direction", 0);
			break;
		case AnimationDirection.Up:
                _animator.SetInteger ("Direction", 1);
			break;
		case AnimationDirection.Down:
                _animator.SetInteger ("Direction", 2);
			break;
		case AnimationDirection.Left:
		case AnimationDirection.Right:
                _animator.SetInteger ("Direction", -1);
			break;
		}
	}
		
	public virtual void HitReverseTrigger()
	{
		// Does nothing at this level, needed for subclasses.
	}

	public enum MovementType 
	{
		LeftRight, UpDown, None
	}

	public bool isMoving()
    {
		return lastFrameVelocity.magnitude > Mathf.Epsilon;
    }
}
