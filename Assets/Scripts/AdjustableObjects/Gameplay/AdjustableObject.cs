using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AdjustableObject : MonoBehaviour
{
    public const float ROTATION_SPEED = .125f;
    public const float MORPH_SPEED = .15f;

    public AdjustableObjectTargetType TargetType;

    public AdjustableObjectConfiguration DefaultConfiguration;
    public AdjustableObjectModifierConfiguration Modifiers { private set; get; }

    private Quaternion StartingRotation;
    private AdjustableObjectConfiguration CurrentConfiguration;

    private AdjustableObjectInsigniaAuto insignia;

    bool isCurrentlyBroken = false;

    public AdjustableObjectTileset Tileset;

    [SerializeField]
    private AdjustableObjectDrawGrid MyGrid = null;

    [SerializeField]
    private AdjustableObjectPhysics Physics = null;

    [SerializeField]
    private AdjustableObjectVFX VFX = null;

    [SerializeField]
    private CompositeCollider2D compositeCollider = null;
    public CompositeCollider2D compositeCollider2D { get { return compositeCollider; } }

    int startLayer;
    int swapLayer;

    float morphTweenStart = -100f;

    float lastRotation;
    float goalRotation;
    float rotationTweenStart = -100f;

    [Header("DEBUG")]
    public bool debugThisObject = false;

    // Use Start to initialize all references, but don't CONFIGURE
    // anything that's configurable!
    void Start()
    {
        startLayer = LayerMask.NameToLayer("Platforms");
        swapLayer = LayerMask.NameToLayer("Background Objects");

        insignia = GetComponentInChildren<AdjustableObjectInsigniaAuto>();

        StartingRotation = MyGrid.transform.rotation;

        AdjustableObjectManager.Instance.RegisterAdjustableObject(this);

        MyGrid.SetContainerPosition();
    }

#if UNITY_EDITOR
    public void InitObjectForEditing()
    {
        insignia = GetComponentInChildren<AdjustableObjectInsigniaAuto>();
        CurrentConfiguration = DefaultConfiguration;
    }
#endif

    /// <summary>
    /// Starting point for every time configuration changes, and of course for when
    /// initializing the object. Basically, anytime something might have changed,
    /// call this.
    /// </summary>
    /// <param name="modifiers">A set of modifers that will be applied
    /// on top of the existing configuration. If fresh modifier config is
    /// passed in, this will just initialize whatever default settings
    /// are on the object. </param>
    /// <param name="targetActive">Whether or not the modifier set passed
    /// in was an empty set, which will determine visual state of the
    /// insignia (glow or no glow) (also set to true if square type
    /// and current active square is the same square type).</param>
    public void InitConfiguation(AdjustableObjectModifierConfiguration modifiers, bool targetActive)
    {
        var currentTemp = CurrentConfiguration;
        bool clip = CurrentConfiguration == null ? true : CurrentConfiguration.Clip;

        CurrentConfiguration = GetModifiedConfiguration(modifiers);
        bool changed = currentTemp != CurrentConfiguration;

        // Override clip behavior for this special case
        // todo: generalize this out more betterly for the final version
        if (this.IsSquareType() && targetActive)
            CurrentConfiguration.Clip = false;

        if (changed)
            HandleRotationModifiers(modifiers);

        if (changed)
            morphTweenStart = GameplayManager.Instance.GameTime;

        MyGrid.SetTilesType(GetCorrectSprites());

        SetCollisionState();

        SetRendererOpacity();

        if (!clip && CurrentConfiguration.Clip)
            GameEventManager.Instance.AOBecameClip.Invoke(this);

        insignia.SetState(TargetType, targetActive);
    }

    private void HandleRotationModifiers(AdjustableObjectModifierConfiguration modifiers)
    {
        // todo: This probably should be checking against past and current
        // RotationAdjust values instead of against the actual rotation
        if (modifiers.RotationAdjust != 0 || // if modified rotation is not 0
                                             // or it IS zero but the transform isn't at starting rotation
            MyGrid.SpriteDestination.transform.rotation != StartingRotation)
        {
            lastRotation = MyGrid.SpriteDestination.transform.rotation.eulerAngles.z;
            goalRotation = modifiers.RotationAdjust * 90f;

            rotationTweenStart = GameplayManager.Instance.GameTime;

            VFX.RotationStart();
        }
    }

    void SetRendererOpacity()
    {
        bool opaque = CurrentConfiguration.Clip && !isCurrentlyBroken;

        MyGrid.SetOpacity(opaque ? Color.white : AdjustableObjectManager.Instance.NoClipColor);
    }

    void SetCollisionState()
    {
        MyGrid.SetCollisionState(CurrentConfiguration.Clip || isCurrentlyBroken, CurrentConfiguration.Damages);
    }

    public bool CurrentlyTransitioning()
    {
        return isMorphing() || isRotating();
    }

    bool isMorphing()
    {
        return morphTweenStart + MORPH_SPEED > GameplayManager.Instance.GameTime;
    }

    bool isRotating()
    {
        return rotationTweenStart + ROTATION_SPEED > GameplayManager.Instance.FixedGameTime;
    }

    public void AdvanceFrame()
    {
        if (isMorphing())
        {
            float progress = Mathf.Clamp(
                            1f - (morphTweenStart + MORPH_SPEED - GameplayManager.Instance.FixedGameTime)
                            / MORPH_SPEED, 0f, 1f);

            VFX.SetMorphProgress(progress);
        }
        // ensure we only set this once, but guarantee that if there
        // was some kind of frame hiccup and we missed the progress
        // that gets us to 0 exactly, set it here.
        else 
            VFX.SetMorphProgress(0f);
        
    }

    bool lastIsRotatingValue = false;

    // Use for movement tweens
    public void PhysicsStep()
    {
        if (CurrentConfiguration == null)
        {
            // PhysicsStep was called before Start()?
            return;
        }

        bool rotating = isRotating();

        if (rotating)
        {
            float progress = Mathf.Clamp(
                1f - (rotationTweenStart + ROTATION_SPEED - GameplayManager.Instance.FixedGameTime)
                / ROTATION_SPEED, 0f, 1f);

            var e = Physics.GetRotation();
            e = Mathf.LerpAngle(lastRotation, goalRotation, progress);

            Physics.SetRotation(e);
        }
        else if (Physics.GetRotation() != goalRotation)
        {
            Physics.SetRotation(goalRotation);
        }

        // Disable all collisions while transitioning if AO should clip
        // (otherwise they'll be disabled anyway so who cares?)
        if (CurrentConfiguration.Clip)
            Physics.SetCollisionsEnabled(!rotating);

        if (!rotating && lastIsRotatingValue)
            GameEventManager.Instance.AOBecameClip.Invoke(this);

        lastIsRotatingValue = rotating;
    }

    public AdjustableObjectConfiguration GetModifiedConfiguration(AdjustableObjectModifierConfiguration modifiers)
    {
        var newConfig = DefaultConfiguration.GetDupe();

        newConfig.Bounce = (newConfig.Bounce && !modifiers.AntiBounce) || modifiers.Bouncy;
        newConfig.Breakable = (newConfig.Breakable || modifiers.Breakable) && !modifiers.AntiBreakable;
        newConfig.Damages = newConfig.Damages && !modifiers.Harmless;

        // ReClip OVERRIDES NoClip, very important
        newConfig.Clip = (newConfig.Clip && !modifiers.NoClip) || modifiers.ReClip;

        // Overrides all
        if (modifiers.Stop)
        {
            newConfig.Movement = MovementConfig.None;
        }
        else
        {
            int movementModifier = modifiers.Faster - modifiers.Slower;
            newConfig.Speed = GetNewMovementSpeed(newConfig.Speed, movementModifier);
        }

        newConfig.Rotation = modifiers.RotationAdjust;
        
        return newConfig;
    }

    MovementSpeedConfig GetNewMovementSpeed(MovementSpeedConfig config, int modifier)
    {
        if (modifier == 0)
            return config;

        int newSpeed = AdjustableObjectManager.Instance.GetIntFromMovementSpeed(config) + modifier;
        return AdjustableObjectManager.Instance.GetMovementSpeedFromInt(newSpeed);
    }

    public AdjustableObjectConfiguration GetCurrentConfiguration()
    {
        return CurrentConfiguration;
    }

    // Check if object is breakable and hasn't already been broken
    public bool CanBreak()
    {
        if (!CurrentConfiguration.Clip)
            return false;

        if (!CurrentConfiguration.Breakable)
            return false;

        if (isCurrentlyBroken)
            return false;

        return true;
    }

    /// <summary>
    /// todo: Really need to re-do all of this physics stuff
    /// so the player can bounce in any direction.
    /// This will require the animation squash/stretch
    /// on the AO's to be adjusted anyway
    /// </summary>
    /// <param name="accountForRotation">
    /// Currently not allowing rotated AO's to bounce the player.
    /// (This method is used both for checking which is the correct
    /// sprite to use AND for checking if the player should bounce
    /// off of a given tile. Pass accountForRotation=true for the
    /// second case.
    /// </param>
    /// <returns></returns>
    public bool ShouldBounce(bool accountForRotation=false)
    {
        if (!CurrentConfiguration.Bounce)
            return false;

        if (!CurrentConfiguration.Clip)
            return false;

        if (isCurrentlyBroken)
            return false;

        if (accountForRotation && CurrentConfiguration.Rotation != 0)
            return false;

        return true;
    }

    public bool ShouldDamage()
    {
        if (!CurrentConfiguration.Clip)
            return false;

        if (CurrentConfiguration.Damages)
            return true;

        return false;
    }

    public void Break()
    {
        if (!CanBreak())
            return;

        // todo: Combine all sprite renderer meshes and then spawn with mesh
        // as particle system shape source.
        AdjustableObjectManager.Instance.SpawnSolidBreakableSystem(
            MyGrid.SpriteDestination.transform.position,
            MyGrid.SpriteDestination.transform.rotation,
            MyGrid.GetExampleSprite(),
            MyGrid.CachedCompositeColliderMesh
        );

        isCurrentlyBroken = true;

        SetRendererOpacity();
        SetCollisionState();
    }

    public bool IsBroken()
    {
        return isCurrentlyBroken;
    }

    public void Restore()
    {
        if (!isCurrentlyBroken)
            return;

        isCurrentlyBroken = false;

        SetRendererOpacity();
        SetCollisionState();
    }

    AdjustableObjectTileType GetCorrectSprites()
    {
        Sprite correctSprite = Tileset.Normal;
        if (IsBroken() || !CurrentConfiguration.Clip)
            return AdjustableObjectTileType.NoClip;
        if (CurrentConfiguration.Damages)
            return AdjustableObjectTileType.Spike;
        if (CurrentConfiguration.Breakable)
            return AdjustableObjectTileType.Breakable;
        if (ShouldBounce())
            return AdjustableObjectTileType.Bounce;

        return AdjustableObjectTileType.Normal;
    }

    public void RedrawGridOnly()
    {
        MyGrid.RedrawGrid(GetCorrectSprites());
    }

    public bool IsSquareType()
    {
        return TargetType == AdjustableObjectTargetType.SquareA
            || TargetType == AdjustableObjectTargetType.SquareB;
    }

    public void PauseCollisionAfterPlayerDeath()
    {
        StartCoroutine(PauseCollisionSub());
    }

    IEnumerator PauseCollisionSub()
    {
        compositeCollider.gameObject.layer = swapLayer;
        yield return new WaitForSeconds(1f);
        compositeCollider.gameObject.layer = startLayer;
    }

    /// <summary>
    /// Need to get grid items for lookup purposes for when
    /// the player collides with spikes, so we can kill them.
    /// </summary>
    /// <returns></returns>
    public List<BoxCollider2D> temp_GetGridItems()
    {
        return MyGrid.boxColliders;
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (debugThisObject)
        {
            Gizmos.DrawWireSphere(transform.position, 2f);
            Handles.Label(transform.position, "DEBUG IS ON");
        }
    }

    /// <summary>
    /// Returns true most of the time.
    /// Returns false if and only if the AO grid has only one block at 0,0
    /// and no half-offset flags
    /// </summary>
    /// <returns></returns>
    public bool ShouldShowInsigniaHolder()
    {
        if (MyGrid.HalfXOffset || MyGrid.HalfYOffset)
            return true;

        // Return true if any non-center block exists
        for (int outer = 0; outer < AdjustableObjectDrawGrid.GRID_SIZE; outer++)
            for (int inner = 0; inner < AdjustableObjectDrawGrid.GRID_SIZE; inner++)
                if (MyGrid.Grid[outer].mask[inner] && (inner != AdjustableObjectDrawGrid.GRID_CENTER_INDEX && outer != AdjustableObjectDrawGrid.GRID_CENTER_INDEX))
                    return true;

        // If we reached here, no non-center blocks exist, so check if the center block exists
        if (MyGrid.Grid[AdjustableObjectDrawGrid.GRID_CENTER_INDEX].mask[AdjustableObjectDrawGrid.GRID_CENTER_INDEX])
            return false;

        // No blocks active...?
        return true;
    }

#endif

}

#if UNITY_EDITOR
[CustomEditor(typeof(AdjustableObject))]
public class AdjustableObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Separator();

        AdjustableObject o = target as AdjustableObject;
        o.InitObjectForEditing();

        if (GUILayout.Button("Propagate Tags"))
        {
            PropagateTags(o.gameObject, o.transform);
        }

        if (GUILayout.Button("Set Insignia Sprites"))
        {
            AdjustableObjectInsigniaAuto insignia = o.GetComponentInChildren<AdjustableObjectInsigniaAuto>();
            if (insignia == null)
            {
                Debug.LogError("No insignia object found. Boo.");
                return;
            }

            insignia.SetSprites(o.TargetType, o.ShouldShowInsigniaHolder());
        }

        if (GUILayout.Button("Set Sprites"))
        {
            o.RedrawGridOnly();
        }
    }

    void PropagateTags(GameObject original, Transform transform)
    {
        transform.gameObject.tag = original.tag;
        EditorUtility.SetDirty(transform);

        for (int i = 0; i < transform.childCount; i++)
            PropagateTags(original, transform.GetChild(i));
    }
}
#endif