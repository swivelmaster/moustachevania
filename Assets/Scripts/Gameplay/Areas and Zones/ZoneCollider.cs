using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ZoneCollider is for bulk enable/disable of moving and physics
/// components/objects when the player enters or exits an area.
/// The collider is expected to be just big enough to fit all the 
/// objects when created in the editor, and will automatically
/// grab all relevant objects within that box to manage.
/// Once the game starts, it creates another collider that is 
/// a full screen width/height bigger so that the enable/disable
/// happens just outside of the player's view.
/// </summary>
[RequireComponent (typeof(BoxCollider2D))]
public class ZoneCollider : MonoBehaviour 
{
	public LayerMask layerMask;

	List<Rigidbody2D> rigidbodies = new List<Rigidbody2D>();
	List<BasicMovement> basicMovements = new List<BasicMovement>();

    Collider2D cameraCollider;

	void Start () {
        SetUpZoneCollider();
	}

    void SetUpZoneCollider()
    {
        cameraCollider = GameObject.Find("Camera Zone Collider").GetComponent<Collider2D>();

        BoxCollider2D myCollider = GetComponent<BoxCollider2D>();

        Collider2D[] results = Physics2D.OverlapAreaAll(new Vector2(myCollider.bounds.min.x, myCollider.bounds.min.y), new Vector2(myCollider.bounds.max.x, myCollider.bounds.max.y), layerMask);

        foreach (Collider2D c in results)
        {
            BasicMovement bm = c.gameObject.GetComponent<BasicMovement>();
            if (bm)
            {
				// TriggeredMovement object destroys itself, so don't manage it with the ZoneCollider.
				TriggeredMovement tm = c.gameObject.GetComponent<TriggeredMovement>();
				if (!tm)
				{
                    basicMovements.Add(bm);
                }
            }

            Rigidbody2D rb = c.gameObject.GetComponent<Rigidbody2D>();
            if (rb)
            {
                rigidbodies.Add(rb);
            }
        }

        // By default everything is asleep.
        ChangeState(false);

        // Expand collider by screen width/height plus a bit to detect player
        // Size is represented as a scale value so I have to figure out what percentage 
        // of the current height and width would be one unit... kind of silly.
        Bounds bounds = myCollider.bounds;
        float oneUnitWidth = 1 / bounds.size.x;
        float oneUnitHeight = 1 / bounds.size.y;
        myCollider.size = new Vector2(1f + oneUnitWidth * 24f, 1f + oneUnitHeight * 12f);
    }

	void ChangeState(bool activated)
	{
		foreach (BasicMovement movement in basicMovements)
		{
			if (activated)
			{
				movement.BecomeVisible ();
				movement.StartMoving ();
			}
			else 
			{
				movement.BecomeInvisible ();
				movement.StopMoving ();
			}
		}

		foreach (Rigidbody2D _rigidbody in rigidbodies)
		{
			// BM's with no movement will remove their RB's
			if (_rigidbody == null)
				continue;

			if (activated)
			{
				_rigidbody.WakeUp ();
			}
			else 
			{
				_rigidbody.Sleep ();	
			}
		}
	}

	void OnTriggerEnter2D(Collider2D other)
	{
        if (other == cameraCollider)
		{
            //Debug.Log("Entering zone: " + gameObject.name);
			ChangeState (true);
		}
	}

	void OnTriggerExit2D(Collider2D other)
	{
        if (other == cameraCollider)
		{
            //Debug.Log("Exiting zone: " + gameObject.name);
            ChangeState (false);
		}
	}


}
