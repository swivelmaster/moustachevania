using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField]
    private GameObject playerPrefab = null;

    [SerializeField]
    private GameObject PlayerAppearEffect = null;

    [Header("Runtime Variables")]
    GameObject currentPlayerObject;
    public Player currentPlayer { private set; get; }

    [SerializeField]
    GameCameraAdapter currentCamera = null;

    public Player playerInstance { private set; get; }

    CheckpointManager checkpointManager;
    SceneSettings sceneSettings;

    GameObject[] deadBodyPieces = new GameObject[4];

    public ParticleSystem deathParticle;
    public GameObject deathPieces;

    public PlayerState currentPlayerState = PlayerState.Alive;
    public bool PlayerIsAlive { get { return currentPlayerState == PlayerState.Alive; } }

    // How much time should elapse after player dies before pressing Jump
    // restarts at the last checkpoint.
    public float timeBeforeRestartInput = 1.5f;
    float timeOfDeath = 0f;

    public LayerMask PlayerMask;
    ContactFilter2D PlayerContactFilter;

    public static PlayerManager Instance;
    private void Awake()
    {
        Instance = this;

        PlayerContactFilter = new ContactFilter2D();
        PlayerContactFilter.SetLayerMask(PlayerMask);
    }

    public GameCameraAdapter GetCamera()
    {
        return currentCamera;
    }

    public void Init(CheckpointManager checkpointManager,
        SceneSettings sceneSettings)
    {
        this.checkpointManager = checkpointManager;
        this.sceneSettings = sceneSettings;
    }

    public void ForceMovePlayerToLocation(Vector2 location)
    {
        currentPlayer.transform.position = location;
    }

    public void SpawnPlayerAtCheckpoint(Vector3 location,
        SavedPlayerState withState,
        CheckpointManager checkpointManager)
    {
        currentPlayerObject = Instantiate(playerPrefab,
            location,
            playerPrefab.transform.rotation);

        currentCamera.SetObjectToFollow(currentPlayerObject);

        currentPlayer = currentPlayerObject.GetComponent<Player>();
        currentPlayer.Init(this);

        // If we don't have a currentSave that means we just started and we'll use the Player prefab's defaults.
        if (withState != null)
            currentPlayer.RestoreState(withState);

        Instantiate(PlayerAppearEffect, currentPlayer.transform.position, PlayerAppearEffect.transform.rotation);

        currentPlayerState = PlayerState.Alive;
    }

    public void AdvanceFrame(ControlInputFrame input)
    {
        if (currentPlayerState == PlayerState.Alive)
        {
            currentPlayer.AdvanceFrame(input);

            if (currentPlayer.transform.position.y < sceneSettings.verticalLowerLimit.transform.position.y)
                checkpointManager.ForceRestart();
        }
        else
        {
            if (timeOfDeath + timeBeforeRestartInput < GameplayManager.Instance.GameTime)
            {
                checkpointManager.ReadyToRestart = true;
            }
        }
    }

    public void PhysicsStep(ControlInputFrame input)
    {
        currentPlayer.PhysicsStep(input);
    }

    public void ForceDestroyPlayerObject()
    {
        if (currentPlayer != null)
            Destroy(currentPlayer.gameObject);
    }

    public void Suspend()
    {
        currentPlayer.Suspend();
    }

    public void Resume()
    {
        currentPlayer.Resume();
    }

    /// <summary>
    /// Use this to determine when object colliders should be disabled
    /// because they're too far away from the view AND the player for us to
    /// care about them. Use for moving objects only.
    /// The main reason for this is to curb physics calcs when rotating
    /// adjustable objects.
    /// </summary>
    /// <returns>Tuple of Vector2's for player and camera. Need both so
    /// that if the player just respawned and the camera is catching up to them,
    /// we don't disable physics around one or the other and cause weird
    /// behavior.</returns>
    public (Vector2, Vector2) GetPlayerAndCameraPositions()
    {
        if (playerInstance == null)
            return (currentCamera.transform.position, currentCamera.transform.position);

        return (playerInstance.transform.position, currentCamera.transform.position);
    }

    public void PlayerDied(DeathReasons reason)
    {
        PersistenceManager.Instance.PlayerDied(reason);
        //todo: Log where the player died for analytics porpoises

        timeOfDeath = GameplayManager.Instance.GameTime;

        MainCameraPostprocessingEffects.instance.Punch();

        Instantiate(deathParticle, currentPlayer.transform.position, deathParticle.transform.rotation);

        // This is now an instance var so we can
        // make the pieces explode if the corpse is squished by a
        // horizontallymoving platform
        //GameObject[] deadBodyPieces = new GameObject[4];
        GameObject body = null;
        Rigidbody2D bodyRB = null;

        Rigidbody2D[] rb2ds = new Rigidbody2D[4];

        // Magic number 4, that's how many dead body pieces there are!
        for (int i = 0; i < 4; i++)
        {
            Vector3 position = deathPieces.transform.GetChild(i).transform.localPosition;
            if (currentPlayer.facing > 0)
                position = new Vector3(position.x *= -1, position.y, position.z);

            deadBodyPieces[i] = Instantiate(
                deathPieces.transform.GetChild(i),
                currentPlayer.transform.position + position,
                deathPieces.transform.GetChild(i).transform.rotation
            ).gameObject;

            // Fix name for clarity
            deadBodyPieces[i].name = deadBodyPieces[i].name.Replace("(Clone)", "");

            if (deadBodyPieces[i].name == "Body")
            {
                body = deadBodyPieces[i];
                bodyRB = body.GetComponent<Rigidbody2D>();
            }

            if (currentPlayer.facing > 0)
            {
                deadBodyPieces[i].transform.localScale = new Vector3(-1f, 1f, 1f);
            }

            rb2ds[i] = deadBodyPieces[i].GetComponent<Rigidbody2D>();
        }

        GameCameraAdapter.instance.SetObjectToFollow(body);

        // Parent to body object so zone colliders are properly triggered
        // UNLESS player was squished, because they're not going anywhere
        // and when the player is squished we blow up the pieces
        // soooo the player script doesn't get to keep processing stuff.
        if (reason != DeathReasons.Squished)
        {
            currentPlayer.transform.parent = body.transform;
            currentPlayer.transform.localPosition = new Vector2(0, 0);
            currentPlayer.transform.localRotation = Quaternion.identity;
        }

        GameCameraAdapter.instance.Shake();

        foreach (Rigidbody2D rb in rb2ds)
        {
            if (reason == DeathReasons.Squished)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.velocity = new Vector2(0f, 0f);
            }
            else
            {
                // Not sure why velocity doesn't translate directly 
                rb.velocity = currentPlayer.getVelocity() * .25f;
            }
        }

        // Don't send pieces flying if the player hit lava, just let them fall

        if (reason == DeathReasons.Squished)
        {
            foreach (Rigidbody2D rb in rb2ds)
            {
                rb.GetComponent<DeadBodyPiece>().ExplodeAfter(.5f);
            }
        }
        else if (reason == DeathReasons.ClipChange)
        {
            foreach (var rb in rb2ds)
            {
                if (rb.gameObject.name == "Body")
                {
                    rb.isKinematic = true;
                    rb.velocity = new Vector2();
                    rb.GetComponent<DeadBodyPiece>().ExplodeAfter(.5f);
                }
                else
                {
                    // YES THIS IS DUPLICATED CODE
                    // These death sequences will get more complicated
                    // and funny later so I assume these will need to change
                    // anyway.
                    if (rb.gameObject.name == "Hat")
                    {
                        // Boop the hat up
                        rb.AddForce(new Vector2((Random.value - .5f) * 2f, 10f), ForceMode2D.Impulse);
                        rb.AddTorque((Random.value - .5f) * 25f);
                    }
                    else
                    {
                        rb.AddForce(new Vector2((Random.value - .5f) * 2f, 8f), ForceMode2D.Impulse);
                        rb.AddTorque((Random.value - .5f) * 10f);
                    }
                }
            }
        }
        else if (reason == DeathReasons.Lava)
        {
            for (int i = 0; i < 4; i++)
            {
                var rb = rb2ds[i];

                // Hat goes farther, because it's funnier that way
                if (rb.gameObject.name == "Hat")
                {
                    // Boop the hat up
                    rb.AddForce(new Vector2(Random.value - .5f, 6f), ForceMode2D.Impulse);
                    rb.AddTorque((Random.value - .5f) * 25f);
                }
                else
                {
                    rb.AddForce(new Vector2(Random.value - .5f, 5f), ForceMode2D.Impulse);
                    rb.AddTorque((Random.value - .5f) * 10f);
                }
            }
        }
        else
        {
            // Send pieces flying in every direction
            foreach (Rigidbody2D rb in rb2ds)
            {
                // Keep body piece in-place when squished
                if (reason == DeathReasons.Squished && rb == bodyRB)
                    continue;

                Vector2 v = currentPlayer.getVelocity() * (1 / Time.deltaTime);
                rb.AddForce(new Vector2(v.x + ((Random.value * 200f) - 100f), v.y + ((Random.value * 200f) - 100f)));
                rb.AddTorque((Random.value * 40f) - 20f);
            }
        }

        currentPlayerState = PlayerState.Dead;
        Destroy(currentPlayer.gameObject);

        SoundEffects.instance.death.Play();
    }

    Vector2[] colliderPath;
    Collider2D[] collisionResults = new Collider2D[1];
    const float CLIP_CHECK_DISTANCE_THRESHOLD = 20f;
    public void AOBecameClip(AdjustableObject ao)
    {
        if (currentPlayer == null || !currentPlayer.alive)
            return;

        // Distance check first for efficiency
        // This will cull like 95% of objects from the later checks
        if (Vector2.Distance(currentPlayer.transform.position, ao.transform.position)
            > CLIP_CHECK_DISTANCE_THRESHOLD)         
            return;            

        bool foundCollision = false;

        System.Array.Clear(collisionResults, 0, 1);

        foreach (var box in ao.temp_GetGridItems())
        {
            // Cheaper distance check before checking physics?
            // I ASSUME THIS IS CHEAPER
            // todo: check?
            if (Vector2.Distance(box.transform.position, currentPlayer.transform.position) > 2f)
                continue;
                
            var count = Physics2D.OverlapCollider(box, PlayerContactFilter, collisionResults);

            if (count > 0)
            {
                foundCollision = true;
                break;
            }
        }
            
        if (!foundCollision)
            return;

        currentPlayer.DeadFromClip();

        ao.PauseCollisionAfterPlayerDeath();
    }

    /// <summary>
    /// todo: Re-enable this once composite colliders are working again
    /// </summary>
    /// <param name="ao"></param>
    public void AOBecameClip_CompositeCollidersEnabled(AdjustableObject ao)
    {
        var playerRect = currentPlayer.GetPlayerRect();

        // Need to get player position RELATIVE to the AdjustableObject's origin
        // because that's how the AdjustableObject's collider's paths will be
        // defined (ie they're in local space, not in world space)
        // You could also fix this problem by transforming all of the path's
        // points by adding the world position of the object's transform,
        // but mathematically subtracting its world position from the player's
        // world position should get the same result with a lot less work!
        for (int i = 0; i < 4; i++)
            playerRect[i] = playerRect[i] - (Vector2)ao.transform.position;

        bool foundCollision = false;
        int pathCount = ao.compositeCollider2D.pathCount;

        for (int i = 0; i < pathCount; i++)
        {
            colliderPath = new Vector2[ao.compositeCollider2D.GetPathPointCount(i)];
            ao.compositeCollider2D.GetPath(i, colliderPath);

            for (int j = 0; j < 4; j++)
            {
                if (GameUtils.ContainsPoint(colliderPath, playerRect[j]))
                {
                    foundCollision = true;
                    break;
                }
            }

            if (foundCollision)
                break;
        }
    }
}

public enum DeathReasons
{
    Squished, Lava, IndestructibleEnemy, Enemy, ClipChange
}

public enum PlayerState
{
    Alive, Dead
}