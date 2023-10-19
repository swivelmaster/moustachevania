using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsController : MonoBehaviour
{
	// Efficient lookup/caching of expensive GetComponent<> operation
	protected static Dictionary<Collider2D, BasicMovement> platformDict = new Dictionary<Collider2D, BasicMovement>();

	// (Tilemap only)
	public LayerMask GroundLayer;

	public LayerMask PlatformLayer;

	int PlatformsLayerInt;
	int GroundLayerInt;

	// Tilemap and platforms
	public LayerMask AllCollisionsLayer;

	// Things that can break!
	public LayerMask breakableCheckLayerMask;

	protected Rigidbody2D rb;

	ContactFilter2D groundCheckContactFilter;
	ContactFilter2D groundedToPlatformCheckFilter;

	// Use to check for being squished by a horizontally moving platform
	ContactFilter2D leftPlatformContactFilter;
	ContactFilter2D rightPlatformContactFilter;

	ContactFilter2D leftWallContactFilter;
	ContactFilter2D rightWallContactFilter;

	ContactFilter2D leftWallBackupContactFilter;
	ContactFilter2D leftPlatformBackupContactFilter;

	[HideInInspector]
	public Collider2D myCollider;

	[HideInInspector]
	public bool Grounded = false;
	[HideInInspector]
	public bool GroundedToGroundOnly = false;
	[HideInInspector]
	public bool HeadBump = false;
	[HideInInspector]
	public bool HitWall = false;

	// Efficiency stuff
	ContactPoint2D[] contacts = new ContactPoint2D[8];

	protected BasicMovement currentPlatform = null;

	public virtual void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		myCollider = GetComponent<Collider2D>();

		PlatformsLayerInt = LayerMask.NameToLayer("Platforms");
		GroundLayerInt = LayerMask.NameToLayer("Ground");

		groundCheckContactFilter = new ContactFilter2D();
		groundCheckContactFilter.SetNormalAngle(89f, 91f);
		groundCheckContactFilter.layerMask = GroundLayer;
		groundCheckContactFilter.useLayerMask = true;

		groundedToPlatformCheckFilter = new ContactFilter2D();
		groundedToPlatformCheckFilter.SetNormalAngle(89f, 91f);
		groundedToPlatformCheckFilter.layerMask = PlatformLayer;
		groundedToPlatformCheckFilter.useLayerMask = true;

		leftPlatformContactFilter = new ContactFilter2D();
		leftPlatformContactFilter.SetNormalAngle(-10f, 10f);
		leftPlatformContactFilter.layerMask = PlatformLayer;
		leftPlatformContactFilter.useLayerMask = true;

		rightPlatformContactFilter = new ContactFilter2D();
		rightPlatformContactFilter.SetNormalAngle(175f, 185f);
		rightPlatformContactFilter.layerMask = PlatformLayer;
		rightPlatformContactFilter.useLayerMask = true;

		leftWallContactFilter = new ContactFilter2D();
		leftWallContactFilter.SetNormalAngle(-10f, 10f);
		leftWallContactFilter.layerMask = GroundLayer;
		leftWallContactFilter.useLayerMask = true;

		rightWallContactFilter = new ContactFilter2D();
		rightWallContactFilter.SetNormalAngle(175f, 185f);
		rightWallContactFilter.layerMask = GroundLayer;
		rightWallContactFilter.useLayerMask = true;


		leftWallBackupContactFilter = new ContactFilter2D();
		leftWallBackupContactFilter.SetNormalAngle(350f, 370f);
		leftWallBackupContactFilter.layerMask = GroundLayer;
		leftWallBackupContactFilter.useLayerMask = true;

		leftPlatformBackupContactFilter = new ContactFilter2D();
		leftPlatformBackupContactFilter.SetNormalAngle(350f, 370f);
		leftPlatformBackupContactFilter.layerMask = PlatformLayer;
		leftPlatformBackupContactFilter.useLayerMask = true;
	}

	/**
     * Use this to test accerlation/deceleration code with breakpoints and
     * stuff to see how everything interacts.
     */

	/**
	   void TestHorizontalAccelerations()
	   {
		   rb.velocity = new Vector2(-4f, 0f);

		   this.manageHorizontalAcceleration(new Vector2(4f, 0f));

		   rb.velocity = new Vector2(4f, 0f);

		   this.manageHorizontalAcceleration(new Vector2(-4f, 0f));
	   }
   */

	public void Suspend()
	{
		rb.simulated = false;
	}

	public void Resume()
	{
		rb.simulated = true;
	}

	public virtual void Die()
	{
		rb.bodyType = RigidbodyType2D.Kinematic;
	}


	/// <summary>
	/// Making this a public method so it can be used outside of the normal
	/// physics flow. As of making this, it's needed to do a double-check
	/// for Grounded when coming out of dash with a jump so the player can get
	/// their dash
	/// </summary>
	/// <returns></returns>
	public bool IsGrounded()
    {
		return GroundedToGround() || GroundedToPlatform();
    }

	bool GroundedToGround()
    {
		System.Array.Clear(contacts, 0, 8);
		int groundCollisionCount = myCollider.GetContacts(groundCheckContactFilter, contacts);
		return groundCollisionCount > 0;
	}

	bool GroundedToPlatform()
    {
		System.Array.Clear(contacts, 0, 8);
		int platformCollisionCount = myCollider.GetContacts(groundedToPlatformCheckFilter, contacts);
		return platformCollisionCount > 0;
	}

	/// <summary>
	/// Checks contacts so we can set Grounded, Squished, and Dash Stop values.
	/// This is why we can check the horizontal input only - we just need to know
	/// which direction the player is TRYING to go so we know which contacts to check.
	/// Again, we are only checking CONTACTS here and now doing any casts.
	/// </summary>
	/// <param name="horizontalInput"></param>
	public void UpdateCollisions(HorizontalInput horizontalInput, bool justJumped)
	{
		Grounded = GroundedToGround();

		// Set this to the value of Grounded here because this is where Grounded only
		// is true if we were on the ground itself.
		// It will be set to true later on if on a platform.
		GroundedToGroundOnly = Grounded;

		// Check to see if we're on top of a moving platform
		// We will need this value so we can move with horizontally moving
		// platforms later.
		bool OnPlatform = GroundedToPlatform();

		// todo: Figure out why this changed: I didn't used to have to see
		// if we jumped on the last frame when checking for Grounded specifically
		// for platforms. At some point, we started getting an extra frame of
		// contact after jumping.
		if (OnPlatform && !justJumped)
		{
			Grounded = true;
			// Note: We're using the contacts cache array, which has
			// been updated in the GroundedToPlatform() method
			currentPlatform = GetBMFromCollider(contacts[0].collider);
		}
		else
		{
			currentPlatform = null;
		}

		// Account for apparent single-frame delay between applying force
		// and actually getting off the ground?
		// This is a hack. I have no idea why it seems to take a frame
		// between applying force and actually leaving the ground.
		if (justJumped && !OnPlatform)
			Grounded = false;

		UpdateHorizontalCollisions(horizontalInput);
	}

	protected Vector2 CheckForBounceAdjustableObject(out BoxCollider2D bouncedCollider)
	{
		bouncedCollider = null;

		foreach (ContactPoint2D point in contacts)
		{
			if (point.collider == null)
				continue;

			// Only bounce up
			if (GetDirectionFromNormal(point.normal) != 3)
				continue;

			if (point.collider.gameObject.CompareTag(Constants.Tags.AdjustableObject))
			{
				var ao = AdjustableObjectManager.Instance.GetAdjustableObjectFromGameObject(point.collider.gameObject);

				if (ao == null)
				{
					Debug.LogError("AdjustableObject tag found with no corresponding component. Go name " + point.collider.gameObject.name);
					continue;
				}

				// Pass true for this check because rotated AO's can't bounce the
				// player (since bounces only occur facing upwards right now,
				// a thing that will need to change!
				if (ao.ShouldBounce(true))
				{
					bouncedCollider = (BoxCollider2D)point.collider;
					return point.normal * AdjustableObjectManager.BounceVelocity;
				}
			}
		}

		return new Vector2();
	}

	protected Vector2 PreventVerticalPlatformBounce()
	{
		// It's the responsibility of the Player object to call to check for collisions
		// earlier in the frame, so we should have an up-to-date Grounded value.

		// If the player is NOT grounded, we have to double check that the platform
		// is still below them to account for the case where they walk off of a 
		// downward moving platform.

		// Only need to cast from bottom left and bottom right, because there are 
		// no objects smaller than the player box collider that they can stand on.
		Vector2 start = new Vector2(myCollider.bounds.min.x, myCollider.bounds.min.y);
		Vector2 end = new Vector2(myCollider.bounds.max.x, myCollider.bounds.min.y);

		// Structured so we do one raycast, if we don't hit anything we do another,
		// if we STILL don't hit anything, we return.
		// (In other words, if either raycast hits, we continue)
		if (!(bool)Physics2D.Raycast(start, Vector2.down, 0.25f, AllCollisionsLayer))
		{
			if (!(bool)Physics2D.Raycast(end, Vector2.down, 0.25f, AllCollisionsLayer))
			{
				// Don't add downward force - there is no platform below us!
				currentPlatform = null;
				return Vector2.zero;
			}
		}

		// Okay, if that's NOT the case, then they didn't jump and they're now
		// over a downward-moving platform, which means they're about to bounce.
		// To prevent the bounce, apply downward force.
		if (currentPlatform.lastFrameVelocity.y < 0)
		{
			return new Vector2(0f, -10f);
		}

		return Vector2.zero;
	}

	BasicMovement GetBMFromCollider(Collider2D fromCollider)
	{
		if (platformDict.ContainsKey(fromCollider))
		{
			return platformDict[fromCollider];
		}

		var collider = fromCollider.gameObject.GetComponent<BasicMovement>();
		if (collider == null)
		{
			// hack: Landed on a BoxCollider from an AO, need to find BM in parent
			// todo: fix this when CompositeColliders are working again?
			platformDict[fromCollider] = fromCollider.gameObject.GetComponentInParent<BasicMovement>();
        }
		else
        {
			platformDict[fromCollider] = collider;
		}
		

		return platformDict[fromCollider];
	}

	int leftWallContactCount = 0;
	int rightWallContactCount = 0;

	HashSet<BasicMovement> basicMovements = new HashSet<BasicMovement>();

	public bool squished = false;

	List<ContactPoint2D> leftPlatformContacts = new List<ContactPoint2D>();
	List<ContactPoint2D> rightPlatformContacts = new List<ContactPoint2D>();

	int GetDirectionFromNormal(Vector2 normal)
	{
		float x = Mathf.Round(normal.x * 10f) / 10f;
		float y = Mathf.Round(normal.y * 10f) / 10f;

		if (x == 1f && y == 0f)
		{
			return 1;
		}
		else if (x == -1f && y == 0f)
		{
			return 2;
		}
		else if (x == 0 && y == 1)
		{
			return 3;
		}
		else if (x == 0 && y == -1)
		{
			return 4;
		}

		return 0;
	}

	public void UpdateHorizontalCollisions(HorizontalInput horizontalInput, bool skipDashCancelCheck = false)
	{
		HitWall = false;
		leftWallContactCount = rightWallContactCount = 0;
		leftPlatformContacts.Clear();
		rightPlatformContacts.Clear();

		System.Array.Clear(contacts, 0, 8);
		myCollider.GetContacts(contacts);

		foreach (ContactPoint2D point in contacts)
		{
			if (point.collider == null)
				continue;

			int direction = GetDirectionFromNormal(point.normal);

			if (direction == 1)
			{
				if (point.collider.gameObject.layer == GroundLayerInt)
					leftWallContactCount++;
				else if (point.collider.gameObject.layer == PlatformsLayerInt)
				{
					leftPlatformContacts.Add(point);
				}
			}
			else if (direction == 2)
			{
				if (point.collider.gameObject.layer == GroundLayerInt)
					rightWallContactCount++;
				else if (point.collider.gameObject.layer == PlatformsLayerInt)
				{
					rightPlatformContacts.Add(point);
				}
			}
		}

		/**
         * ------------------------------------
         * DASH CANCELING WALL HIT CHECKS START
         * ------------------------------------
         * 
         * Okay, I'm doing this in a kind of weird way for dash canceling detection.
         * I tried it just using just contact points but that can give false positives in the sense
         * that if the player is dashing and passes over a ledge at the exact moment of the 
         * frame advance, that can register as a contact... and HitWall() is used to determine
         * if we should cancel the dash, so that's a bad thing.
         * 
         * So next up - I wanted to see if I could find a way to prevent those false positives
         * without rewriting everything. 
         * 
         * I started with using rb.velocity to filter which things I was checking, but it actually 
         * doesn't quite work because, for instance, if the player is dashing to the right and hits
         * a moving platform that pushes them left, they'll start moving to the left and the 'right wall'
         * check will never happen, thus not canceling the dash.
         * 
         * But the dash itself provides a constant input to movementForFrame, so I can use movementForFrame
         * to filter the contact point check. Thus, dashing to the right will NEVER register a false positive
         * on the left, and if we're dashing towards a left-moving platform, the right input will cause
         * the contact points on the right side to correctly be checked for collision. (And of course if the
         * platform is moving to the right, the collision will still register.)
         * 
         */
		if (!skipDashCancelCheck)
		{
			if (horizontalInput == HorizontalInput.Right && (rightWallContactCount > 0 || rightPlatformContacts.Count > 0))
			{
				//Debug.Log("Right wall contact");
				HitWall = true;
			}

			if (horizontalInput == HorizontalInput.Left && (leftWallContactCount > 0 || leftPlatformContacts.Count > 0))
			{
				//Debug.Log("Left wall contact");
				HitWall = true;
			}
		}
		/**
         * ------------------------------------
         * DASH CANCELING WALL HIT CHECKS END
         * ------------------------------------
         */

		squished = false;

		/**
         * Next up is checking for being squished between a platform
         * and a wall.
         *
         * We already have wall contact data from above so now we need
         * platform contact data.
         *
         * Unfortunately this requires a lot more checking of values
         * because we need to know which direction a platform is going in...
         */

		if (rightWallContactCount == 0 && leftWallContactCount == 0)
		{
			// No need to check for platforms because we're not touching a wall anyway
			// So we can't be squished!
			return;
		}

		if (leftPlatformContacts.Count == 0 && rightPlatformContacts.Count == 0)
		{
			return;
		}

		basicMovements.Clear();

		if (rightWallContactCount > 0 && leftPlatformContacts.Count > 0)
		{
			squished = true;
			return;
		}
		else if (leftWallContactCount > 0 && rightPlatformContacts.Count > 0)
		{
			squished = true;
			return;
		}
	}

	public bool CheckForBreakables(bool above, float distance)
	{
		float yBounds = above ? myCollider.bounds.max.y : myCollider.bounds.min.y;

		Vector2 pointA = new Vector2(myCollider.bounds.min.x, yBounds);
		Vector2 pointB = new Vector2(myCollider.bounds.max.x, yBounds);

		// We need BOTH OF THESE to either hit the same block, or one hit a block and the other hit nothing
		// that nothing needs to include a check for walls and platforms

		// NOTE: This has the limit of - hitting two breakables at the same time will return early, even though
		// logically they should both break. So uh... in the level... just don't put two next to each other. Sorry.
		// todo: fix above
		RaycastHit2D hit1 = Physics2D.Raycast(pointA, above ? Vector2.up : Vector2.down, distance, breakableCheckLayerMask);
		RaycastHit2D hit2 = Physics2D.Raycast(pointB, above ? Vector2.up : Vector2.down, distance, breakableCheckLayerMask);

		if (!hit1 && !hit2)
			return false;

		// Hit two different things, so we're halfway between or something.
		if (hit1 && hit2 && hit1.collider != hit2.collider)
			return false;

		RaycastHit2D hitToCheck = hit1 ? hit1 : hit2;

		// No early return, so we may have found a breakable!
		if (hitToCheck.collider.gameObject.CompareTag(Constants.Tags.DestroyableBlock))
		{
			hitToCheck.collider.gameObject.GetComponent<Destroyable>().Destroyed();
			return true;
		}
		else if (hitToCheck.collider.gameObject.CompareTag(Constants.Tags.AdjustableObject))
		{
			AdjustableObject o = AdjustableObjectManager.Instance.GetAdjustableObjectFromGameObject(hitToCheck.collider.transform.gameObject);
			if (o == null)
			{
				Debug.LogError("Something was tagged with AdjustableObject but didn't have the component!");
				return false;
			}

			if (o.CanBreak())
			{
				o.Break();
				return true;
			}
		}

		return false;
	}

}
